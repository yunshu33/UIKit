using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using VoyageForge.Depot.Runtime.Utilities;

namespace VoyageForge.UIKit.Runtime
{
    public class UIManager : MonoSingleton<UIManager>
    {
        private readonly ViewStack _stack = new ViewStack();
        private SceneUIContext _sceneContext;

        private readonly PopupManager _popupManager = new PopupManager();
        public PopupManager PopupManager => _popupManager;

        private IPanelProvider _provider = new ResourcesProvider();

        private IPanelProvider PanelProvider
        {
            get => _provider;
            set
            {
                if (_provider != null)
                {
                    value.Import(_provider.Export());
                }

                _provider = value;
            }
        }

        private void OnDestroy()
        {
            _stack?.Dispose();
        }

        protected override void OnApplicationQuit()
        {
            _stack?.Dispose();
        }

        // ---- 场景上下文管理 ----

        internal async UniTask RegisterSceneContext(SceneUIContext context)
        {
            _sceneContext = context;

            foreach (var entry in context.PreplacedPanels)
            {
                if (entry.Panel == null || string.IsNullOrEmpty(entry.PanelKey)) continue;
                entry.Panel.PanelKey = entry.PanelKey;

                if (entry.Panel.gameObject.activeSelf)
                    await ShowAsync(entry.PanelKey, entry.Panel);
                else
                    PanelProvider.Register(entry.PanelKey, entry.Panel);
            }
        }

        /// <summary> 注销场景上下文 — 清缓存 + Hide 全部场景面板。 </summary>
        internal async UniTask UnregisterSceneContext()
        {
            if(_sceneContext == null)
                return;
            
            foreach (var entry in _sceneContext.PreplacedPanels)
                PanelProvider.Remove(entry.PanelKey);

            if (_stack != null)
                while (_stack.Count > 0)
                {
                    var panel = _stack.Peek();
                    if (panel == null) break;

                    if (_sceneContext.PreplacedPanels.Any(p => p.PanelKey == panel.PanelKey))
                    {
                        await _stack.Pop();
                    }
                    else
                    {
                        return;
                    }
                }
        }

        // ---- 公开 API ----

        /// <summary> 显示 FullPanel（压栈，Fire & Forget）。 </summary>
        public void Show(string key) => ShowAsync(key).Forget();

        /// <summary> 显示 FullPanel（压栈，异步）。 </summary>
        public async UniTask<FullPanel> ShowAsync(string key)
        {
            var panel = PanelProvider.Load(key) as FullPanel;

            if (panel == null)
            {
                Debug.LogError($"[UIManager] Show failed: no FullPanel found for key '{key}'.");
                return null;
            }

            panel.PanelKey = key;
            await _stack.Push(panel);
            return panel;
        }

        /// <summary> 显示已有 FullPanel 实例（Fire & Forget）。 </summary>
        public void Show(string key, FullPanel panel) => ShowAsync(key, panel).Forget();

        /// <summary> 显示已有 FullPanel 实例（异步）。 </summary>
        public async UniTask ShowAsync(string key, FullPanel panel)
        {
            if (panel == null)
            {
                Debug.LogError("[UIManager] Show failed: panel is null.");
                return;
            }

            panel.PanelKey = key;
            await _stack.Push(panel);
        }

        /// <summary> 隐藏栈顶面板（Fire & Forget）。 </summary>
        public void Hide() => HideAsync().Forget();

        /// <summary> 隐藏栈顶面板（异步）。 </summary>
        public async UniTask HideAsync()
        {
            PanelProvider.Release(await _stack.Pop());
        }

        public void Hide(FullPanel panel) => HideAsync(panel).Forget();

        public async UniTask HideAsync(FullPanel panel)
        {
            if (_stack.Peek() == panel)
                await _stack.Pop();

            await panel.Hide();
        }


      

        /// <summary> 获取栈顶面板。 </summary>
        public FullPanel GetActivePanel()
        {
            return _stack.Count > 0 ? _stack.Peek() : null;
        }

        /// <summary> 输入路由 — 发给栈顶 Active 面板。 </summary>
        public bool OnInput(KeyCode key, bool down)
        {
            var top = _stack.Peek();
            return top != null && top.State == BasePanel.PanelState.Active && top.OnInput(key, down);
        }

        /// <summary> 关闭面板（Fire & Forget）。 </summary>
        public void Close(BasePanel panel) => CloseAsync(panel).Forget();

        /// <summary> 关闭面板（异步）。 </summary>
        public async UniTask CloseAsync(BasePanel panel)
        {
            if (_stack.Peek() == panel)
                await _stack.Pop();

            await panel.Close();
        }
    }
}