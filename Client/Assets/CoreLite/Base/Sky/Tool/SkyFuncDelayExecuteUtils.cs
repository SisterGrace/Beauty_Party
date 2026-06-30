using System;
using System.Collections;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// 延迟执行工具类（单例），用于在指定延迟后执行回调。
    /// </summary>
    public class SkyFuncDelayExecuteUtils : SkySingleton<SkyFuncDelayExecuteUtils>
    {
        /// <summary>
        /// 延迟执行一个方法（协程方式，受 Time.timeScale 影响）。
        /// </summary>
        /// <param name="delay">延迟秒数</param>
        /// <param name="action">要执行的回调</param>
        public void DelayExecute(float delay, Action action)
        {
            StartCoroutine(DelayExecuteCoroutine(delay, action));
        }

        private IEnumerator DelayExecuteCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        /// <summary>
        /// 延迟执行一个方法（不受 Time.timeScale 影响）。
        /// </summary>
        /// <param name="delay">延迟秒数（真实时间）</param>
        /// <param name="action">要执行的回调</param>
        public void DelayExecuteRealtime(float delay, Action action)
        {
            StartCoroutine(DelayExecuteRealtimeCoroutine(delay, action));
        }

        private IEnumerator DelayExecuteRealtimeCoroutine(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }
    }
}