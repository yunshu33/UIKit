using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Popup Provider。
    /// 
    /// 继承自 IPanelProvider。
    /// 
    /// 额外负责：
    /// - Popup Canvas Root
    /// </summary>
    public interface IPopupProvider : IPanelProvider
    {
        /// <summary>
        /// Popup 挂载 Root。
        /// 
        /// 一般为：
        /// DontDestroyOnLoad Canvas Root。
        /// </summary>
        Transform Root { get; }
    }
}