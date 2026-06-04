using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Up2SDK
{
    public class D_MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (null != _instance) return _instance;
                _instance = FindObjectOfType(typeof(T)) as T;
                if (null != _instance) return _instance;

                var container = new GameObject
                {
                    name = typeof(T).ToString(),
                    hideFlags = HideFlags.HideInHierarchy
                };
                _instance = container.AddComponent(typeof(T)) as T;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(container);
                }
                else
                {
                    (_instance as D_MonoSingleton<T>)?.OnInstanceCreate();
                }

                return _instance;
            }
        }

        private void Awake()
        {
            OnInstanceCreate();
        }

        protected virtual void OnInstanceCreate()
        {
        }

        protected Task<TU> RunCoroutineAsync<TU>(Func<TaskCompletionSource<TU>, IEnumerator> coroutineFunc)
        {
            var tcs = new TaskCompletionSource<TU>();
            StartCoroutine(coroutineFunc(tcs));
            return tcs.Task;
        }
    }
}
