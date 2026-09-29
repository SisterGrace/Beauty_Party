using UnityEngine;

namespace Sky
{
    // 热云 SolarEngine Java Proxy 的 C# 反射封装。
    // 原生类: com.unity3d.player.SolarEngineProxy（Assets/Plugins/Android/SolarEngineProxy.java）
    // 仅在 Android 真机生效；编辑器 / iOS 走空实现，避免反射异常。
    internal static class SkySolarEngineBridge
    {
        private const string ProxyClass = "com.unity3d.player.SolarEngineProxy";
        public static string GetDistinctId()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using var proxy = new AndroidJavaClass(ProxyClass);
        return proxy.CallStatic<string>("getDistinctId") ?? "";
    }
    catch (System.Exception e)
    {
        LogTool.Error("[SolarEngine] getDistinctId exception: " + e);
        return "";
    }
#else
            return "";
#endif
        }
        // 告知 Java 把归因/事件回调发给哪个 GameObject（SkySingleton 命名含命名空间）
        public static void SetUnityCallbackTarget(string gameObjectName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("setUnityCallbackTarget", gameObjectName);
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] SetUnityCallbackTarget bridge exception: " + e);
            }
#endif
        }

        public static void PreInit(string appKey)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("preInit", appKey);
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] PreInit bridge exception: " + e);
            }
#endif
        }

        public static void Initialize(string appKey, bool enableLog, bool enableDebugModel)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using var proxy = new AndroidJavaClass(ProxyClass);
        proxy.CallStatic("initialize", appKey, enableLog, enableDebugModel);
    }
    catch (System.Exception e)
    {
        LogTool.Error("[SolarEngine] Initialize bridge exception: " + e);
    }
#endif
        }

        // 自定义事件
        public static void TrackEvent(string eventName, string propsJson)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("trackEvent", eventName, propsJson ?? string.Empty);
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] TrackEvent bridge exception: " + e);
            }
#endif
        }

        // 预置事件（_appStart / _appLogin / _appPur 等）
        public static void TrackPresetEvent(string eventName, string propsJson)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("trackPresetEvent", eventName, propsJson ?? string.Empty);
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] TrackPresetEvent bridge exception: " + e);
            }
#endif
        }

        // 主动同步拉取归因结果。无结果或异常时返回空串
        public static string GetAttribution()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                return proxy.CallStatic<string>("getAttribution") ?? string.Empty;
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] GetAttribution bridge exception: " + e);
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }

        // 广告展示收益（_appImp）
        public static void TrackAdImpression(
            string adNetworkPlatform,
            string mediationPlatform,
            int adType,
            string adNetworkAppID,
            string adNetworkADID,
            float ecpm,
            string currencyType,
            bool isRenderSuccess,
            string customPropertiesJson)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("trackAdImpression",
                    adNetworkPlatform ?? "",
                    mediationPlatform ?? "",
                    adType,
                    adNetworkAppID ?? "",
                    adNetworkADID ?? "",
                    ecpm,
                    currencyType ?? "USD",
                    isRenderSuccess,
                    customPropertiesJson ?? "{}"
                );
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] TrackAdImpression bridge exception: " + e);
            }
#endif
        }

        // 广告点击（_appClick）
        public static void TrackAdClick(
            string adNetworkPlatform,
            string mediationPlatform,
            int adType,
            string adNetworkADID,
            string customPropertiesJson)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("trackAdClick",
                    adNetworkPlatform ?? "",
                    mediationPlatform ?? "",
                    adType,
                    adNetworkADID ?? "",
                    customPropertiesJson ?? "{}"
                );
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] TrackAdClick bridge exception: " + e);
            }
#endif
        }

        // 应用内购买（_appPur）
        public static void TrackPurchase(
            string orderId,
            double payAmount,
            string currencyType,
            string payType,
            string productId,
            string productName,
            int productNum,
            int payStatus,
            string failReason,
            string customPropertiesJson)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("trackPurchase",
                    orderId ?? "",
                    payAmount,
                    currencyType ?? "CNY",
                    payType ?? "",
                    productId ?? "",
                    productName ?? "",
                    productNum,
                    payStatus,
                    failReason ?? "",
                    customPropertiesJson ?? "{}"
                );
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] TrackPurchase bridge exception: " + e);
            }
#endif
        }
    }
}
