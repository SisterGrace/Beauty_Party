using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{ 
    public static class CoreLogUtils
    {
        private static bool _enabled = false;

        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public static void Debug(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[DEBUG][CoreLite] {message}", context);
#else
            if (!_enabled) return;
            Log($"[DEBUG][CoreLite] {message}", context);
#endif
        }

        public static void Info(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[INFO][CoreLite] {message}", context);
#else
            if (!_enabled) return;
            Log($"[INFO][CoreLite] {message}", context);
#endif
        }

        public static void Warning(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[WARNING][CoreLite] {message}", context);
#else
            if (!_enabled) return;
            Log($"[WARNING][CoreLite] {message}", context);
#endif
        }

        public static void Error(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[ERROR][CoreLite] {message}", context);
#else
            if (!_enabled) return;
            Log($"[ERROR][CoreLite] {message}", context);
#endif
         }


        private static void Log(string message, Object context = null)
        {
            if (context != null)
            {
                UnityEngine.Debug.Log(message, context);
            }
            else
            {
                UnityEngine.Debug.Log(message);
            }
        }
    }
}

