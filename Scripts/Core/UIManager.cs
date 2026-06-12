using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VoyageForge.Depot.Runtime.Utilities;

namespace VoyageForge.UIKit.Runtime
{
    public class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField] private ViewStack _stack = new();

        private SceneUIContext _sceneContext;

        private readonly PopupManager _popup = new();
        public static PopupManager Popup => Instance._popup;

        private IPanelProvider _provider = new ResourcesProvider();

        public static IPanelProvider PanelProvider
        {
            get => Instance._provider;
            set
            {
                if (Instance._provider != null) value.Import(Instance._provider.Export());
                Instance._provider = value;
            }
        }

        private void OnDestroy() => _stack?.Dispose();

        protected override void OnApplicationQuit() => _stack?.Dispose();

        // ---- 场景上下文 ----

        internal async UniTask RegisterSceneContext(SceneUIContext context)
        {
            _sceneContext = context;
            foreach (var entry in context.PreplacedPanels)
            {
                if (entry.Panel == null) continue;
                if (entry.Panel.gameObject.activeSelf)
                    await ShowAsync(entry.Panel);
                else
                    PanelProvider.Register(entry.Panel);
            }
        }

        internal async UniTask UnregisterSceneContext()
        {
            if (_sceneContext == null) return;

            foreach (var entry in _sceneContext.PreplacedPanels)
                PanelProvider.Remove(entry.Panel.GetType());

            if (_stack != null)
                while (_stack.Count > 0)
                {
                    var panel = _stack.Peek();
                    if (panel == null) break;
                    if (_sceneContext.PreplacedPanels.Any(p => p.Panel == panel))
                        await _stack.Pop();
                    else
                        return;
                }
        }

        // ---- 公开 API ----

        /// <summary>
        /// 获得一个面板
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async UniTask<T> GetPanelAsync<T>() where T : BasePanel
        {
            throw new NotImplementedException();
        }


        /// <summary> 显示 FullPanel（异步）。 </summary>
        public async UniTask<T> ShowAsync<T>() where T : FullPanel
        {
            var panel = await PanelProvider.LoadAsync<T>();

            if (panel == null)
            {
                Debug.LogError($"[UIManager] Show failed: {typeof(T).Name}");
                return null;
            }

            //Full window 为唯一 如果多次调用判断是不是在顶层 如果是 直接返回
            var peek = _stack.Peek();

            if (_stack.Peek().GetType() == panel.GetType())
                return peek as T;

            await _stack.Push(panel);

            return panel;
        }

        /// <summary> 显示已有 FullPanel 实例（异步）。 </summary>
        public async UniTask ShowAsync(FullPanel panel)
        {
            if (panel == null) return;
            await _stack.Push(panel);
        }

        /// <summary> 隐藏栈顶（异步）。 </summary>
        public async UniTask HideAsync()
        {
            //TODO: 需要先判断面板是否还在站内 HideAsync 同理
            PanelProvider.Release(await _stack.Pop());
        }

        /// <summary> 隐藏指定 FullPanel（异步）。 </summary>
        public async UniTask HideAsync(FullPanel panel)
        {
            var top = _stack.Peek();

            if (top == panel)
            {
                await HideAsync();
            }
            else
            {
                await panel.Hide();
                await top.Resume();

                PanelProvider.Register(panel);
            }
        }

        /// <summary> 获取栈顶面板。 </summary>
        public FullPanel GetActivePanel() => _stack.Count > 0 ? _stack.Peek() : null;

        /// <summary> 输入路由。 </summary>
        public bool OnInput(KeyCode key, bool down)
        {
            var top = _stack.Peek();
            return top != null && top.State == BasePanel.PanelState.Active && top.OnInput(key, down);
        }

        /// <summary> 关闭面板（异步）。 </summary>
        public async UniTask CloseAsync(BasePanel panel)
        {
            if (_stack.Peek() == panel) await _stack.Pop();
            await panel.Close();
        }
    }
}