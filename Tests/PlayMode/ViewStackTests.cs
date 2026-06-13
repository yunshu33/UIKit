using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// ViewStack 导航栈测试 — 验证 FullPanel 的 Push/Pop/Pause/Resume 流程。
    /// </summary>
    public class ViewStackTests
    {
        private ViewStack _stack;
        private GameObject _go1, _go2;
        private TestFullPanel _panel1, _panel2;

        [SetUp]
        public void SetUp()
        {
            _stack = new ViewStack();
            _go1 = new GameObject("TestPanel1");
            _panel1 = _go1.AddComponent<TestFullPanel>();
            _panel1.gameObject.SetActive(false);
            _go2 = new GameObject("TestPanel2");
            _panel2 = _go2.AddComponent<TestFullPanel>();
            _panel2.gameObject.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            _stack?.Dispose();
            if (_go1 != null) Object.DestroyImmediate(_go1);
            if (_go2 != null) Object.DestroyImmediate(_go2);
        }

        /// <summary>
        /// Push 单个面板 → Count=1, State=Active, OnCreate/OnShow 各触发一次。
        /// </summary>
        [UnityTest]
        public IEnumerator PushSingle_ActivatesPanel() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnCreateCount);
            Assert.AreEqual(1, _panel1.OnShowCount);
        });

        /// <summary>
        /// Push 两个不同类型面板 → 第一个被 Pause，第二个 Active。
        /// ABB 规则: 同类型不能连续 Push，因此必须用不同类型。
        /// </summary>
        [UnityTest]
        public IEnumerator PushTwo_PausesFirst() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            Assert.AreEqual(2, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Paused, _panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State);
            Assert.AreEqual(1, _panel1.OnPauseCount);

            Object.DestroyImmediate(goB);
        });

        /// <summary>
        /// Pop → 栈顶 Hide 并出栈，下层 Resume。
        /// ABB 规则: Push 两个必须不同类型。
        /// </summary>
        [UnityTest]
        public IEnumerator Pop_ResumesPrevious() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            var popped = await _stack.Pop();

            Assert.AreSame(panelB, popped);
            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnResumeCount);
            Assert.AreEqual(1, panelB.OnHideCount);

            Object.DestroyImmediate(goB);
        });

        /// <summary>
        /// 空栈 Pop 应抛出 InvalidOperationException。
        /// </summary>
        [UnityTest]
        public IEnumerator PopEmpty_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await _stack.Pop();
                Assert.Fail("期望抛出 InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                Assert.Pass();
            }
        });

        /// <summary>
        /// Push → Pop → 再次 Push 同一个面板 → OnCreate 只触发一次（面板实例被缓存复用）。
        /// </summary>
        [UnityTest]
        public IEnumerator PushPopPush_SameInstance_OnCreateOnce() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Pop();
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _panel1.OnCreateCount, "OnCreate 只应触发一次");
            Assert.AreEqual(2, _panel1.OnShowCount, "OnShow 应触发两次");
        });

        /// <summary>
        /// ABB: 连续 Push 同类型 → 第二次跳过，栈不变。
        /// </summary>
        [UnityTest]
        public IEnumerator Push_ABB_SkipsDuplicate() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _stack.Count, "ABB: 同类型不能连续 Push");
            Assert.AreEqual(1, _panel1.OnShowCount, "OnShow 只应触发一次");
            Assert.AreEqual(1, _panel1.OnCreateCount, "OnCreate 只应触发一次");
        });

        /// <summary>
        /// ABA: Push A → Push B → Push A → 第三次应报错，栈保持 [A, B]。
        /// </summary>
        [UnityTest]
        public IEnumerator Push_ABA_LogsError() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);       // [A]
            await _stack.Push(panelB);        // [A, B]
            LogAssert.Expect(LogType.Error, "[ViewStack] 不允许 ABA: TestFullPanel 已在栈中，不能重复 Push");
            await _stack.Push(_panel1);       // ABA 报错，不压栈

            Assert.AreEqual(2, _stack.Count, "ABA 被拒绝，栈应保持 2 个 entry");
            Assert.AreSame(panelB, _stack.Peek(), "栈顶仍为 B");

            Object.DestroyImmediate(goB);
        });
    }
}
