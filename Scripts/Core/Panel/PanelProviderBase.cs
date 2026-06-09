using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Provider 基类 — 缓存/PanelPath 统一处理。
    /// 子类只需实现 Instantiate(string path)。
    /// </summary>
    public abstract class PanelProviderBase : IPanelProvider
    {
        private readonly Dictionary<Type, BasePanel> _cache = new();
        public IReadOnlyDictionary<Type, BasePanel> Cache => _cache;

        public virtual async UniTask<T> LoadAsync<T>() where T : BasePanel
        {
            var type = typeof(T);

            if (_cache.TryGetValue(type, out var panel))
            {
                _cache.Remove(type);
                return panel as T;
            }

            var path = PanelPathCache.GetPath<T>();
            panel = await InstantiateAsync<T>(path);
            
            if (panel == null) return null;

            panel.gameObject.SetActive(false);
            return (T)panel;
        }

        /// <summary> 子类实现：根据路径异步创建实例。 </summary>
        protected abstract UniTask<T> InstantiateAsync<T>(string path) where T : BasePanel;

        public void Release(BasePanel panel)
        {
            if (panel == null) return;
            _cache[panel.GetType()] = panel;
        }

        public void Register(BasePanel panel)
        {
            if (panel == null) return;
            _cache[panel.GetType()] = panel;
        }

        public bool TryGet(Type type, out BasePanel panel) => _cache.TryGetValue(type, out panel);
        public void Remove(Type type) => _cache.Remove(type);
        public void Clear() => _cache.Clear();
    }
}
