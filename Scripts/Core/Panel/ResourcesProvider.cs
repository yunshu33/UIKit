using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 基于 Resources 的默认 Provider。
    /// </summary>
    public class ResourcesProvider : IPanelProvider
    {
        /// <summary>
        /// 内部缓存。
        /// </summary>
        private readonly Dictionary<string, BasePanel> _cache = new();

        /// <summary>
        /// 只读缓存。
        /// </summary>
        public IReadOnlyDictionary<string, BasePanel> Cache => _cache;

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

            // 从 Resources 加载
            var prefab = Resources.Load<BasePanel>(key);

            if (prefab == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed : {key}");
                return null;
            }

            // 创建实例
            panel = Object.Instantiate(prefab);

            // 默认隐藏
            panel.gameObject.SetActive(false);

            return panel;
        }

        /// <summary>
        /// 回收 Panel。
        /// 
        /// 当前策略：
        /// Disable。
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
        /// 从缓存移除。
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