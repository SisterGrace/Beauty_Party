using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Sky
{
    public class SkySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (null != _instance) return _instance;
                _instance = FindObjectOfType<T>();
                if (_instance != null)
                {
                    // 如果从场景中找到，也保证它不被销毁
                    if (Application.isPlaying)
                        DontDestroyOnLoad(_instance.gameObject);
                    return _instance;
                }
                var container = new GameObject
                {
                    name = typeof(T).ToString(),
#if UNITY_EDITOR

#else
hideFlags = HideFlags.HideInHierarchy
#endif



                };
                _instance = container.AddComponent(typeof(T)) as T;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(container);
                }
                else
                {
                    (_instance as SkySingleton<T>)?.OnInstanceCreate();
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