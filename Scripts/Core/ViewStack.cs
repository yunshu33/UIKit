using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 视图导航栈 — 管理 FullPanel 进出栈。
    /// Push: Pause 当前 → Show 新面板 → 入栈
    /// Pop:  Hide 栈顶 → 出栈 → Resume 下层
    /// </summary>
    [Serializable]
    public class ViewStack : IDisposable
    {
        [SerializeField] private List<FullPanel> _panels = new();

        public event Action<FullPanel> TopPanelChanged;
        public event Action<int> CountChanged;

        public int Count => _panels.Count;

        /// <summary> 查看栈顶面板。 </summary>
        public FullPanel Peek() => _panels.Count > 0 ? _panels[^1] : null;

        public void Dispose()
        {
            TopPanelChanged = null;
            CountChanged = null;
        }

        /// <summary> 推入：暂停当前 → 入栈 → Show。 </summary>
        public async UniTask Push(FullPanel panel)
        {
            //判断panel 是不是已经在栈内 ，如果是 则在栈顶 重新压入一次
            var t = panel.GetType();

            if (_panels.Count > 0)
            {
                //如果栈顶 就是他自身 则直接 跳过
                if (t == Peek().GetType())
                {
                    return;
                }
                
                await _panels[^1].Pause();
               
            }

            foreach (var p in _panels)
            {
                if (p.GetType() == t)
                {
                    panel = p;
                    break;
                }
            }

            _panels.Add(panel);
            NotifyChanged();
            await panel.Show();
        }

        /// <summary> 弹出：Hide → 出栈 → Resume 下层。 </summary>
        public async UniTask<FullPanel> Pop()
        {
            if (_panels.Count == 0)
                throw new InvalidOperationException();

            var exiting = _panels[^1];
            _panels.RemoveAt(_panels.Count - 1);
            NotifyChanged();
            await exiting.Hide();

            if (_panels.Count > 0)
            {
                var last = _panels[^1];

                await last.Resume();
            }


            return exiting;
        }

        private void NotifyChanged()
        {
            CountChanged?.Invoke(_panels.Count);
            TopPanelChanged?.Invoke(_panels.Count > 0 ? _panels[^1] : null);
        }
    }
}