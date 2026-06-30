using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public static class SkyLogUtils
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
            Log($"[DEBUG][Sky] {message}", context);
#else
            if (!_enabled) return;
            Log($"[DEBUG][Sky] {message}", context);
#endif
        }

        public static void Info(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[INFO][Sky] {message}", context);
#else
            if (!_enabled) return;
            Log($"[INFO][Sky] {message}", context);
#endif
        }

        public static void Warning(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[WARNING][Sky] {message}", context);
#else
            if (!_enabled) return;
            Log($"[WARNING][Sky] {message}", context);
#endif
        }

        public static void Error(object message, Object context = null)
        {
#if UNITY_EDITOR
            Log($"[ERROR][Sky] {message}", context);
#else
            if (!_enabled) return;
            Log($"[ERROR][Sky] {message}", context);
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

