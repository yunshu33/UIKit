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
        private readonly Dictionary<Type, UniTaskCompletionSource<BasePanel>> _pendingTcs = new();
        public IReadOnlyDictionary<Type, BasePanel> Cache => _cache;

        public virtual async UniTask<T> LoadAsync<T>() where T : BasePanel
        {
            var type = typeof(T);

            if (_cache.TryGetValue(type, out var panel))
            {
                _cache.Remove(type);
                return panel as T;
            }

            // 已有飞行中的加载，复用同一个任务，防止并发创建多个实例
            if (_pendingTcs.TryGetValue(type, out var existingTcs))
            {
                var waited = await existingTcs.Task;
                return waited as T;
            }

            var tcs = new UniTaskCompletionSource<BasePanel>();
            _pendingTcs[type] = tcs;

            try
            {
                var path = PanelPathCache.GetPath<T>();
                var loaded = await InstantiateAsync<T>(path);

                if (loaded == null)
                {
                    tcs.TrySetResult(null);
                    return null;
                }

                loaded.gameObject.SetActive(false);
                tcs.TrySetResult(loaded);
                return (T)(BasePanel)loaded;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _pendingTcs.Remove(type);
            }
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
