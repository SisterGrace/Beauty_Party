using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{
    public class CoreFuncDelayExecuteUtils : CoreSingleton<CoreFuncDelayExecuteUtils>
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

