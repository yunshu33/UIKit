using System.Collections.Generic;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Panel 提供器。
    /// 
    /// 负责：
    /// - Panel 加载
    /// - Panel 缓存
    /// - Panel 回收
    /// 
    /// 不负责：
    /// - UI 层级
    /// - Canvas
    /// - 生命周期
    /// - Stack
    /// </summary>
    public interface IPanelProvider 
    {
        /// <summary>
        /// 当前缓存。
        /// 只读暴露，避免外部直接修改内部状态。
        /// </summary>
        IReadOnlyDictionary<string, BasePanel> Cache { get; }

        /// <summary>
        /// 加载 Panel。
        /// 
        /// 如果缓存存在：
        ///     返回缓存。
        /// 
        /// 如果缓存不存在：
        ///     创建并缓存。
        /// </summary>
        BasePanel Load(string key);

        /// <summary>
        /// 回收 Panel。
        /// 
        /// Provider 自己决定：
        /// - Disable
        /// - Pool
        /// - Destroy
        /// </summary>
        void Release(BasePanel panel);

        /// <summary>
        /// 注册已有 Panel。
        /// 
        /// 主要用于：
        /// - SceneUI
        /// - 预放置 UI
        /// </summary>
        void Register(string key, BasePanel panel);

        /// <summary>
        /// 尝试获取缓存中的 Panel。
        /// </summary>
        bool TryGet(string key, out BasePanel panel);

        /// <summary>
        /// 从缓存移除。
        /// 不负责 Destroy。
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// 清空缓存。
        /// 不负责 Destroy。
        /// </summary>
        void Clear();

        /// <summary>
        /// 导入缓存。
        /// 
        /// 用于：
        /// Provider 热切换。
        /// </summary>
        void Import(Dictionary<string, BasePanel> panels)
        {
            foreach (var kv in panels)
            {
                Register(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// 导出缓存。
        /// 
        /// 导出后会自动清空当前 Provider。
        /// 
        /// 用于：
        /// Provider 热切换。
        /// </summary>
        Dictionary<string, BasePanel> Export()
        {
            var result = new Dictionary<string, BasePanel>();

            foreach (var kv in Cache)
            {
                result[kv.Key] = kv.Value;
            }

            Clear();

            return result;
        }
    }
}