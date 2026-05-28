using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 默认 Resources Provider。
    /// 
    /// 同时：
    /// - 提供 Popup Root
    /// - 管理 Panel 缓存
    /// </summary>
    public class PopupResourcesProvider : IPopupProvider
    {
        /// <summary>
        /// Panel 缓存。
        /// </summary>
        private readonly Dictionary<string, BasePanel> _cache = new();

        /// <summary>
        /// Popup Root。
        /// </summary>
        private Transform _root;

        public IReadOnlyDictionary<string, BasePanel> Cache => _cache;

        public Transform Root
        {
            get
            {
                if (_root != null) return _root;

                var rootGO = new GameObject("[PopupCanvas]");

                Object.DontDestroyOnLoad(rootGO);

                var canvas = rootGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;

                rootGO.AddComponent<CanvasScaler>();
                rootGO.AddComponent<GraphicRaycaster>();

                _root = rootGO.transform;

                return _root;
            }
        }

        /// <summary>
        /// 加载 Panel。
        /// </summary>
        public BasePanel Load(string key)
        {
            // 已缓存
            if (_cache.Remove(key, out var panel))
            {
                return panel;
            }

            // Resources 加载
            var prefab = Resources.Load<BasePanel>(key);

            if (prefab == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed : {key}");
                return null;
            }

            // 创建实例
            panel = Object.Instantiate(prefab);

            panel.gameObject.SetActive(false);

            return panel;
        }

        /// <summary>
        /// 回收 Panel。
        /// </summary>
        public void Release(BasePanel panel)
        {
            if (panel == null)
                return;

            if (_cache.TryAdd(panel.PanelKey, panel))
            {
                
            }
            else
            {
                throw new Exception($"[ResourcesProvider] Release failed : {panel.PanelKey}");
            }
        }

        /// <summary>
        /// 注册已有 Panel。
        /// </summary>
        public void Register(string key, BasePanel panel)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (panel == null)
                return;

            _cache[key] = panel;
        }

        /// <summary>
        /// 尝试获取缓存。
        /// </summary>
        public bool TryGet(string key, out BasePanel panel)
        {
            return _cache.TryGetValue(key, out panel);
        }

        /// <summary>
        /// 移除缓存。
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// 清空缓存。
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }
}