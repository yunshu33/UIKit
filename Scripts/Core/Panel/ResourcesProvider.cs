using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    public class ResourcesProvider : PanelProviderBase
    {
        protected override async UniTask<T> InstantiateAsync<T>(string path) 
        {
            var idx = path.LastIndexOf("Resources/", StringComparison.Ordinal);
            var resPath = idx >= 0 ? path[(idx + 10)..] : path;

            var req = Resources.LoadAsync<GameObject>(resPath);
            await req;
            if (req.asset == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed: {path} → {resPath}");
                return null;
            }
            var instance = Object.Instantiate((GameObject)req.asset);
            return instance.GetComponent<T>();
        }
    }
}
