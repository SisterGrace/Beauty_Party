using UnityEngine;

namespace Sky
{
    // 【新增】SolarEngine（热云）Java 侧 Proxy 的 C# 反射封装。
    // 原生类: com.unity3d.player.SolarEngineProxy（见 Assets/Plugins/Android/SolarEngineProxy.java）
    // 仅在 Android 真机生效，编辑器/iOS 走空实现，避免反射异常影响主流程。
    internal static class SkySolarEngineBridge
    {
        private const string ProxyClass = "com.unity3d.player.SolarEngineProxy";

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

        public static void Initialize(string appKey, bool enableLog)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var proxy = new AndroidJavaClass(ProxyClass);
                proxy.CallStatic("initialize", appKey, enableLog);
            }
            catch (System.Exception e)
            {
                LogTool.Error("[SolarEngine] Initialize bridge exception: " + e);
            }
#endif
        }
    }
}
