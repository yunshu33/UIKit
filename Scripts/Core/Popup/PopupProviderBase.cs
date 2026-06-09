using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VoyageForge.UIKit.Runtime
{
    public abstract class PopupProviderBase : PanelProviderBase, IPopupProvider
    {
        protected Transform _root;

        public virtual Transform Root
        {
            get => _root;
        }

       
    }
}