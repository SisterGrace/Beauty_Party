/*using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sky
{
    public class ToponKwaiUtils
    {
        // 标准的topon版本
        // private const string KwaiATInitManagerPath = "com.anythink.network.kwai.KwaiATInitManager";
        // private const string IsInitPath = "j";
        // private const string MediationInitCallbackPath = "com.anythink.Sky.api.MediationInitCallback";


        // 定制的topon版本？
        private const string KwaiATInitManagerPath = "com.thinkup.network.kwai.KwaiTUInitManager";
        private const string IsInitPath = "mm";
        public const string MediationInitCallbackPath = "com.thinkup.Sky.api.MediationInitCallback";

        private static AndroidJavaObject Instance = null;

        private static AndroidJavaObject GetInstance()
        {
            Instance ??= new AndroidJavaClass(KwaiATInitManagerPath).CallStatic<AndroidJavaObject>("getInstance");
            return Instance;
        }
       
        /// <summary>
        /// 通过 topon 去初始化 kwai
        /// </summary>
        /// <param name="appID">kwai 的appid</param>
        /// <param name="appToken">kwai 的app token</param>
        /// <param name="callback">初始化的回调</param>
        public static void InitSDK(string appID, string appToken, Action<bool, string> callback)
        {
            try
            {
                var hashMap = new AndroidJavaObject("java.util.HashMap");
                hashMap.Call<AndroidJavaObject>("put", "app_id", appID);
                hashMap.Call<AndroidJavaObject>("put", "app_token", appToken);
                GetInstance().Call("initSDK", GetAndroidApplication(), hashMap, new MediationInitCallback(callback));
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] InitSDK , {e.Message}");
                callback.Invoke(false, e.Message);
            }
        }

         public static bool IsInit()
        {
            try
            {
                return GetInstance().Get<bool>(IsInitPath);
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] IsInit , {e.Message}");
            }

            return false;
        }

        public static string GetAdapterVersion()
        {
            try
            {
                return GetInstance().Call<string>("getAdapterVersion");
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] GetAdapterVersion , {e.Message}");
            }

            return "";
        }

        public static string GetNetworkVersion()
        {
            try
            {
                return GetInstance().Call<string>("getNetworkVersion");
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] GetNetworkVersion , {e.Message}");
            }

            return "";
        }

        public static string GetNetworkName()
        {
            try
            {
                return GetInstance().Call<string>("getNetworkName");
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] GetNetworkName , {e.Message}");
            }

            return "";
        }

        public static string GetNetworkSDKClass()
        {
            try
            {
                return GetInstance().Call<string>("getNetworkSDKClass");
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] GetNetworkSDKClass , {e.Message}");
            }

            return "";
        }


        /// <summary>
        /// 获取Android的Application对象（android.app.Application）
        /// </summary>
        /// <returns>Android Application对象（AndroidJavaObject），若在非Android平台返回null</returns>
        private static AndroidJavaObject GetAndroidApplication()
        {
            // 只在Android平台执行
            if (Application.platform != RuntimePlatform.Android)
            {
                return null;
            }

            try
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                return currentActivity.Call<AndroidJavaObject>("getApplication");
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[ToponKwaiUtils] GetAndroidApplication , {e.Message}");
                return null;
            }
        }
    }

    public class MediationInitCallback : AndroidJavaProxy
    {
        private readonly Action<bool, string> _callback;

        public MediationInitCallback(Action<bool, string> callback) : base(ToponKwaiUtils.MediationInitCallbackPath)
        {
            _callback = callback;
        }

        [Preserve]
        public void onSuccess()
        {
            _callback.Invoke(true, "success");
        }

        [Preserve]
        public void onFail(string error)
        {
            _callback.Invoke(false, error);
        }
    }
}*/