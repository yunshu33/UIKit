using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    public class ResourcesProvider : PanelProviderBase
    {
        protected override BasePanel Instantiate(string path)
        {
            // "UI/Samples/Resources/MainPanel" → "MainPanel"
            var idx = path.LastIndexOf("Resources/");
            var resPath = idx >= 0 ? path[(idx + 10)..] : path;

            var prefab = Resources.Load<BasePanel>(resPath);
            if (prefab == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed: {path} → {resPath}");
                return null;
            }
            return Object.Instantiate(prefab);
        }
    }
}
