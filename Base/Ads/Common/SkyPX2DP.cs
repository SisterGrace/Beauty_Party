using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public class SkyPX2DP : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaClass _unityPlayerClass;
        private static AndroidJavaObject _currentActivity;
        private static AndroidJavaObject _resources;
        private static AndroidJavaObject _displayMetrics;
        
        // 初始化Android相关类引用
        private static void Init()
        {
            if (_unityPlayerClass == null)
            {
                _unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _currentActivity = _unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                _resources = _currentActivity.Call<AndroidJavaObject>("getResources");
                _displayMetrics = _resources.Call<AndroidJavaObject>("getDisplayMetrics");
            }
        }
#endif

            /// <summary>
            /// Android平台dp转px
            /// </summary>
            /// <param name="dp">需要转换的dp值</param>
            /// <returns>转换后的px值</returns>
            public static int DpToPx(float dp)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            Init();
            float scale = _displayMetrics.Get<float>("density");
            return (int)(dp * scale + 0.5f);
#else
                // 非Android平台返回近似值
                return (int)dp;
#endif
            }

            /// <summary>
            /// Android平台px转dp
            /// </summary>
            /// <param name="px">需要转换的px值</param>
            /// <returns>转换后的dp值</returns>
            public static int PxToDp(float px)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            Init();
            float scale = _displayMetrics.Get<float>("density");
            return (int)(px / scale + 0.5f);
#else
                // 非Android平台返回近似值
            return (int)px;
#endif
            }
    }

}
