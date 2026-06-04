using UnityEngine;

namespace GameBase
{
    public static class SecureScreen
    {
        
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject activity;

    static SecureScreen()
    {
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        }
    }

    public static void EnableSecure()
    {
        if (!SettingsUtils.FrameworkGlobalSettings.EnableSecureScreen) return;
        if (activity == null) return;
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            int flagSecure = 0x00002000; // WindowManager.LayoutParams.FLAG_SECURE
            var window = activity.Call<AndroidJavaObject>("getWindow");
            window.Call("addFlags", flagSecure);
        }));
    }

    public static void DisableSecure()
    {
        if (!SettingsUtils.FrameworkGlobalSettings.EnableSecureScreen) return;
        if (activity == null) return;
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            int flagSecure = 0x00002000;
            var window = activity.Call<AndroidJavaObject>("getWindow");
            window.Call("clearFlags", flagSecure);
        }));
    }
#else
        public static void EnableSecure()
        {
        }

        public static void DisableSecure()
        {
        }
#endif
    }
}