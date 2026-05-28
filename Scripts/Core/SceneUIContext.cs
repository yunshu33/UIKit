using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 场景 UI 上下文 — 持有预置面板列表。
    /// Start 时注册到 UIManager，OnDestroy 时注销。
    /// </summary>
    public class SceneUIContext : MonoBehaviour
    {
        [SerializeField] [Tooltip("场景中预置的 UI Panel 列表")]
        private List<PreplacedPanelEntry> _preplacedPanels = new();

        public IReadOnlyList<PreplacedPanelEntry> PreplacedPanels => _preplacedPanels;

        private void Start()
        {
            UIManager.Instance.RegisterSceneContext(this).Forget();
        }

        private void OnDestroy()
        {
            UIManager.Instance.UnregisterSceneContext().Forget();
        }
    }
}