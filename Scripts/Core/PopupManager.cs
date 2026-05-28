using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 默认 PopupManager。
    /// </summary>
    public class PopupManager : IPopupManager
    {
        /// <summary>
        /// 当前激活 Popup。
        /// 
        /// key:
        ///     PanelKey
        /// 
        /// value:
        ///     Popup实例
        /// </summary>
        private readonly Dictionary<string, PopupPanel> _active = new();

        /// <summary>
        /// Popup Provider。
        /// </summary>
        private IPopupProvider _provider = new PopupResourcesProvider();

        /// <summary>
        /// 当前 Provider。
        /// 
        /// 支持运行时热切换。
        /// </summary>
        public IPopupProvider Provider
        {
            get => _provider;
            set
            {
                if (value == null)
                    return;

                // 迁移缓存
                if (_provider != null)
                {
                    value.Import(_provider.Export());
                }

                _provider = value;

                // 重新挂载 Popup Root
                foreach (var panel in _active.Values)
                {
                    if (panel == null)
                        continue;

                    panel.transform.SetParent(
                        _provider.Root,
                        false);
                }
            }
        }

        /// <summary>
        /// 显示 Popup。
        /// 
        /// Provider 自动加载。
        /// </summary>
        public async UniTask<T> ShowAsync<T>(string key)
            where T : PopupPanel
        {
            PopupPanel panel;

            // 已激活
            if (_active.TryGetValue(key, out panel))
            {
                await panel.Show();
                return panel as T;
            }

            // Provider 加载
            panel = _provider.Load(key) as PopupPanel;

            if (panel == null)
            {
                Debug.LogError($"[PopupManager] Popup not found : {key}");
                return null;
            }

            panel.PanelKey = key;

            // 挂载 Root
            panel.transform.SetParent(
                _provider.Root,
                false);

            // 激活记录
            _active[key] = panel;

            await panel.Show();

            return panel as T;
        }

        /// <summary>
        /// 显示已有 Popup。
        /// 
        /// 支持：
        /// - Scene Popup
        /// - Runtime实例
        /// - Prefab Asset（自动Instantiate）
        /// </summary>
        public async UniTask ShowAsync(
            string key,
            PopupPanel popupPanel)
        {
            if (popupPanel == null)
                return;

            // 已激活
            if (_active.TryGetValue(key, out var active))
            {
                if (active == popupPanel)
                {
                    await active.Show();
                    return;
                }
            }

            // Prefab Asset
            if (!popupPanel.gameObject.scene.IsValid())
            {
                popupPanel = Object.Instantiate(popupPanel);
            }

            popupPanel.PanelKey = key;

            // 挂载 Root
            popupPanel.transform.SetParent(
                _provider.Root,
                false);

            // 激活记录
            _active[key] = popupPanel;

            await popupPanel.Show();
        }

        /// <summary>
        /// 隐藏 Popup。
        /// </summary>
        public async UniTask HideAsync(string key)
        {
            if (!_active.Remove(key, out var panel))
                return;

            await panel.Hide();
            

            _provider.Release(panel);
        }
        
        

        /// <summary>
        /// 隐藏指定 Popup。
        /// </summary>
        public async UniTask HideAsync(
            string key,
            PopupPanel popupPanel)
        {
            if (popupPanel == null)
                return;

            // 当前激活的是该对象
            if (_active.TryGetValue(key, out var active))
            {
                if (active == popupPanel)
                {
                    await active.Hide();
                    
                    _active.Remove(key);

                    _provider.Release(active);

                    return;
                }
            }

            // 独立对象
            await popupPanel.Hide();

            _provider.Release(popupPanel);
        }

        
        
        /// <summary>
        /// 关闭 Popup。
        /// </summary>
        public async UniTask CloseAsync(string key)
        {
            if (!_active.Remove(key, out var panel))
                return;

            await panel.Close();

        }

        /// <summary>
        /// 关闭指定 Popup。
        /// </summary>
        public async UniTask CloseAsync(
            string key,
            PopupPanel popupPanel)
        {
            if (popupPanel == null)
                return;

            // 当前激活的是该对象
            if (_active.TryGetValue(key, out var active))
            {
                if (active == popupPanel)
                {
                    _active.Remove(key);

                    await active.Close();

                    _provider.Release(active);

                    return;
                }
            }

            // 独立对象
            await popupPanel.Close();

        }

        /// <summary>
        /// 是否正在显示。
        /// </summary>
        public bool IsShowing(string key)
        {
            return _active.TryGetValue(key, out var panel)
                   && panel.State == BasePanel.PanelState.Active;
        }

        /// <summary>
        /// Dispose。
        /// </summary>
        public void Dispose()
        {
            _active.Clear();
        }

       
    }
}