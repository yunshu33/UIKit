using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    public class ResourcesProvider : PanelProviderBase
    {
        protected override UniTask<BasePanel> InstantiateAsync(string path)
        {
            var idx = path.LastIndexOf("Resources/");
            var resPath = idx >= 0 ? path[(idx + 10)..] : path;

            var prefab = Resources.Load<BasePanel>(resPath);
            if (prefab == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed: {path} → {resPath}");
                return UniTask.FromResult<BasePanel>(null);
            }
            return UniTask.FromResult<BasePanel>(Object.Instantiate(prefab));
        }
    }
}
