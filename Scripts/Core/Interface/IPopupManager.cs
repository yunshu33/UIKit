using System;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Popup Manager。
    /// </summary>
    public interface IPopupManager : IDisposable
    {
        /// <summary>
        /// 显示 Popup。
        /// </summary>
        UniTask<T> ShowAsync<T>(string key)
            where T : PopupPanel;

        UniTask ShowAsync(string key, PopupPanel popupPanel);

        /// <summary>
        /// 隐藏 Popup。
        /// </summary>
        UniTask HideAsync(string key);

        /// <summary>
        /// 关闭 Popup。
        /// </summary>
        UniTask HideAsync(string key, PopupPanel popupPanel);

        /// <summary>
        /// 关闭 Popup。
        /// </summary>
        UniTask CloseAsync(string key);

        /// <summary>
        /// 关闭 Popup。
        /// </summary>
        UniTask CloseAsync(string key, PopupPanel popupPanel);

        /// <summary>
        /// 是否正在显示。
        /// </summary>
        bool IsShowing(string key);
    }
}