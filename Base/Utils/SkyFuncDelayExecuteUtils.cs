using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public class SkyFuncDelayExecuteUtils : SkySingleton<SkyFuncDelayExecuteUtils>
    {
        public void DelayExecute(float delay, System.Action action)
        {
            StartCoroutine(DelayExecuteCoroutine(delay, action));
        }
    
        private IEnumerator DelayExecuteCoroutine(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}

