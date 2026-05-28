using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 滑入/滑出方向枚举。
    /// </summary>
    public enum SlideDirection
    {
        /// <summary>从左侧滑入，或向左侧滑出。</summary>
        Left,
        /// <summary>从右侧滑入，或向右侧滑出。</summary>
        Right,
        /// <summary>从上方滑入，或向上方滑出。</summary>
        Up,
        /// <summary>从下方滑入，或向下方滑出。</summary>
        Down
    }

    /// <summary>
    /// UI 转场动画播放器。
    /// 提供一组静态方法，全部返回 UniTask，支持 CancellationToken 取消。
    /// </summary>
    /// <remarks>
    /// <para><b>动画类型：</b></para>
    /// - Fade:   CanvasGroup alpha 淡入/淡出。
    /// - Slide:  RectTransform 锚点位置滑动（方向 + 屏幕尺寸自动计算偏移距离）。
    /// - Scale:  Transform.localScale 弹性弹出效果。
    ///
    /// <para><b>使用方式：</b></para>
    /// <code>
    ///   // 在 BasePanel 子类中覆写：
    ///   protected override UniTask PlayEnterAnimation(CancellationToken ct)
    ///       => TransitionPlayer.SlideIn((RectTransform)transform, SlideDirection.Right, 0.3f, ct);
    ///
    ///   protected override UniTask PlayExitAnimation(CancellationToken ct)
    ///       => TransitionPlayer.SlideOut((RectTransform)transform, SlideDirection.Left, 0.2f, ct);
    /// </code>
    ///
    /// <para><b>性能说明：</b></para>
    /// 帧循环使用 UniTask.Yield(PlayerLoopTiming.Update, ct)，在主线程 Update 后执行。
    /// 只有一个动画实例在跑，开销可忽略。如果需要同时播放大量动画，考虑使用 DOTween。
    /// </remarks>
    public static class TransitionPlayer
    {
        // ---- 淡入淡出 ----

        /// <summary>
        /// 淡入动画。CanvasGroup.alpha 从 0 过渡到 1。
        /// </summary>
        /// <param name="cg">目标 CanvasGroup。</param>
        /// <param name="duration">动画时长（秒）。</param>
        /// <param name="ct">取消令牌。取消时抛出 OperationCanceledException。</param>
        public static async UniTask FadeIn(CanvasGroup cg, float duration, CancellationToken ct = default)
        {
            cg.alpha = 0f;
            await Animate(duration, t => cg.alpha = t, ct);
            cg.alpha = 1f;
        }

        /// <summary>
        /// 淡出动画。CanvasGroup.alpha 从 1 过渡到 0。
        /// </summary>
        /// <param name="cg">目标 CanvasGroup。</param>
        /// <param name="duration">动画时长（秒）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async UniTask FadeOut(CanvasGroup cg, float duration, CancellationToken ct = default)
        {
            cg.alpha = 1f;
            await Animate(duration, t => cg.alpha = 1f - t, ct);
            cg.alpha = 0f;
        }

        // ---- 滑入滑出 ----

        /// <summary>
        /// 滑入动画。面板从指定方向外滑入到原始位置。
        /// 偏移距离自动取屏幕宽/高（通过 Canvas.pixelRect 计算）。
        /// </summary>
        /// <param name="rt">面板的 RectTransform。</param>
        /// <param name="from">滑入来源方向（例如 Right 表示从右侧滑入）。</param>
        /// <param name="duration">动画时长（秒）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async UniTask SlideIn(RectTransform rt, SlideDirection from, float duration, CancellationToken ct = default)
        {
            var target = rt.anchoredPosition;
            var offset = GetOffset(from, rt);
            rt.anchoredPosition = target + offset;

            await Animate(duration, t =>
            {
                rt.anchoredPosition = Vector2.Lerp(target + offset, target, EaseOutCubic(t));
            }, ct);

            rt.anchoredPosition = target;
        }

        /// <summary>
        /// 滑出动画。面板从原始位置滑出到指定方向外。
        /// </summary>
        /// <param name="rt">面板的 RectTransform。</param>
        /// <param name="to">滑出目标方向（例如 Left 表示向左侧滑出）。</param>
        /// <param name="duration">动画时长（秒）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async UniTask SlideOut(RectTransform rt, SlideDirection to, float duration, CancellationToken ct = default)
        {
            var start = rt.anchoredPosition;
            var offset = GetOffset(to, rt);

            await Animate(duration, t =>
            {
                rt.anchoredPosition = Vector2.Lerp(start, start + offset, EaseOutCubic(t));
            }, ct);

            rt.anchoredPosition = start + offset;
        }

        // ---- 缩放弹出 ----

        /// <summary>
        /// 弹性弹出动画。localScale 从 0.6 过渡到 1.0，使用 easeOutBack 缓动产生回弹效果。
        /// 适用于 Dialog 弹窗的出场效果。
        /// </summary>
        /// <param name="rt">面板的 RectTransform。</param>
        /// <param name="duration">动画时长（秒）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async UniTask ScalePop(RectTransform rt, float duration, CancellationToken ct = default)
        {
            rt.localScale = Vector3.one * 0.6f;

            await Animate(duration, t =>
            {
                rt.localScale = Vector3.one * EaseOutBack(0.6f, 1f, t);
            }, ct);

            rt.localScale = Vector3.one;
        }

        // ---- 内部实现 ----

        /// <summary>
        /// 通用逐帧动画循环。
        /// </summary>
        /// <param name="duration">总时长（秒）。</param>
        /// <param name="step">每帧回调，参数 t 的范围为 [0, 1]。</param>
        /// <param name="ct">取消令牌。取消时抛出异常终止动画。</param>
        private static async UniTask Animate(float duration, Action<float> step, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 每帧检查取消令牌，确保可以被外部取消
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                step(Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            // 确保最终状态精确到位
            step(1f);
        }

        /// <summary>
        /// 根据滑动方向计算偏移量。偏移距离 = 屏幕宽度（水平）或高度（垂直）。
        /// </summary>
        private static Vector2 GetOffset(SlideDirection dir, RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            float w = canvas != null ? canvas.pixelRect.width : Screen.width;
            float h = canvas != null ? canvas.pixelRect.height : Screen.height;
            return dir switch
            {
                SlideDirection.Left => new Vector2(-w, 0),
                SlideDirection.Right => new Vector2(w, 0),
                SlideDirection.Up => new Vector2(0, h),
                SlideDirection.Down => new Vector2(0, -h),
                _ => Vector2.zero
            };
        }

        // ---- 缓动函数 ----

        /// <summary>easeOutCubic 缓动：开头快，结尾慢。</summary>
        private static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);

        /// <summary>easeOutBack 缓动：超过目标值后回弹，达到弹性效果。</summary>
        private static float EaseOutBack(float from, float to, float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float v = 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
            return from + (to - from) * v;
        }
    }
}
