#if LAUNCHER_AD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// Launcher SDK 广告管理类，实现 ISkyAdService。
    /// 加载由 SDK 层自动处理，Unity 侧仅负责查询状态和展示。
    /// </summary>
    public class SkyLauncherMgr : SkySingleton<SkyLauncherMgr>, ISkyAdService
    {
        private const string BridgeClass = "com.xg.launcher.unity.LauncherUnityBridge";
        private const string PlatformName = "Launcher";
        private bool _isInitialized;

        // 回调相关
        private const string CallbackGameObjectName = "LauncherAdCallback";
        private GameObject _callbackGameObject;
        private LauncherAdCallback _callback;

        // 缓存 AndroidJavaClass 实例，避免重复创建
        private static AndroidJavaClass _bridgeInstance;
        
        /// <summary>
        /// 获取缓存的 Bridge 类引用，仅在 Android 平台且非编辑器模式下有效
        /// </summary>
        public static AndroidJavaClass Bridge
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_bridgeInstance == null)
                {
                    try
                    {
                        _bridgeInstance = new AndroidJavaClass(BridgeClass);
                        SkyLogUtils.Debug("[SkyLauncherMgr] Bridge class cached successfully.");
                    }
                    catch (Exception e)
                    {
                        SkyLogUtils.Error("[SkyLauncherMgr] Failed to create Bridge class: " + e.Message);
                    }
                }
                return _bridgeInstance;
#else
                return null;
#endif
            }
        }

        public string ClientName => PlatformName;
        public SkyPlatformType PlatformType => SkyPlatformType.Launcher; // 需要在 SkyPlatformType 中新增

        #region 初始化

        public void Initialize()
        {
            if (_isInitialized) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // 1. 创建回调 GameObject
                CreateCallbackGameObject();

                // 2. 设置 Unity 回调目标
                SetUnityCallbackTarget(CallbackGameObjectName);

                _isInitialized = true;
                SkyLogUtils.Debug("[SkyLauncherMgr] SDK initialized, callback target set.");
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] Initialize failed: " + e.Message);
            }
#else
            SkyLogUtils.Debug("[SkyLauncherMgr] Skip init on non-Android platform");
#endif
        }

        /// <summary>
        /// 创建用于接收 Android 回调的 Unity GameObject。
        /// </summary>
        private void CreateCallbackGameObject()
        {
            if (_callbackGameObject != null) return;

            _callbackGameObject = new GameObject(CallbackGameObjectName);
            _callback = _callbackGameObject.AddComponent<LauncherAdCallback>();
            UnityEngine.Object.DontDestroyOnLoad(_callbackGameObject);
            SkyLogUtils.Debug("[SkyLauncherMgr] Callback GameObject created.");
        }

        /// <summary>
        /// 设置 Unity 回调目标名称，Android 侧通过该名称找到 GameObject 并调用方法。
        /// </summary>
        private void SetUnityCallbackTarget(string gameObjectName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Bridge != null)
            {
                Bridge.CallStatic("setUnityCallbackTarget", gameObjectName);
                SkyLogUtils.Debug("[SkyLauncherMgr] setUnityCallbackTarget: " + gameObjectName);
            }
#endif
        }

        #endregion

        #region 配置获取

        /// <summary>
        /// 获取 Firebase Remote Config 的 AD_Game 原始字符串。
        /// </summary>
        public string GetAdGameConfig()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    return Bridge.CallStatic<string>("getAdGameConfig");
                }
                return "";
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] GetAdGameConfig failed: " + e.Message);
                return "";
            }
#else
            return "";
#endif
        }

        #endregion


        #region ISkyAdService - 通用

        public void ClearAllAds()
        {
            DestroyInterstitial();
            DestroyRewarded();
            SkyLogUtils.Debug("[SkyLauncherMgr] All ads cleared.");
        }

        #endregion

        #region ISkyAdService - Interstitial（加载部分注释，由 SDK 层处理）

        public void LoadInterstitial()
        {
            // 加载由 SDK 层自动处理，Unity 侧无需调用
            // 如需手动触发加载，取消注释以下代码：
            // #if UNITY_ANDROID && !UNITY_EDITOR
            // try {
            //     using (var bridge = new AndroidJavaClass(BridgeClass)) {
            //         bridge.CallStatic("loadBiddingInterstitial");
            //     }
            // } catch (Exception e) {
            //     SkyLogUtils.Error("[SkyLauncherMgr] LoadInterstitial failed: " + e.Message);
            // }
            // #endif
            SkyLogUtils.Debug("[SkyLauncherMgr] LoadInterstitial: SDK handles loading automatically.");
        }

        public bool IsInterstitialAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    return Bridge.CallStatic<bool>("isBiddingInterstitialReady");
                }
                return false;
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] IsInterstitialAvailable failed: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// 获取插屏缓存数量（可用于判断是否有多个可用广告）。
        /// </summary>
        public int GetInterstitialCacheCount()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    return Bridge.CallStatic<int>("getBiddingInterstitialCacheCount");
                }
                return 0;
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] GetInterstitialCacheCount failed: " + e.Message);
                return 0;
            }
#else
            return 0;
#endif
        }

        public void ShowInterstitial()
        {
            ShowInterstitial("");
        }

        /// <summary>
        /// 展示插屏广告，可传入场景 ID 用于统计。
        /// </summary>
        public void ShowInterstitial(string position)
        {
            if (!IsInterstitialAvailable())
            {
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    Bridge.CallStatic("showBiddingInterstitial", position ?? "");
                    SkyLogUtils.Debug("[SkyLauncherMgr] ShowInterstitial: " + position);
                }
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] ShowInterstitial failed: " + e.Message);
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
            }
#endif
        }

        public void DestroyInterstitial()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    Bridge.CallStatic("destroyBiddingInterstitial");
                    SkyLogUtils.Debug("[SkyLauncherMgr] DestroyInterstitial");
                }
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] DestroyInterstitial failed: " + e.Message);
            }
#endif
        }

        public double GetInterstitialRevenue()
        {
            // Launcher SDK 未提供直接获取 eCPM 的接口，需通过回调获取
            // 此处返回 -1，竞价逻辑中会按 0 处理
            return -1;
        }

        public (string adId, string adSource, double adEcpm) GetInterstitialAdInfo()
        {
            // Launcher SDK 未提供直接获取广告信息的接口，需通过回调获取
            return ("", "", 0);
        }

        #endregion

        #region ISkyAdService - Rewarded（加载部分注释，由 SDK 层处理）

        public void LoadRewarded()
        {
            // 加载由 SDK 层自动处理，Unity 侧无需调用
            // 如需手动触发加载，取消注释以下代码：
            // #if UNITY_ANDROID && !UNITY_EDITOR
            // try {
            //     using (var bridge = new AndroidJavaClass(BridgeClass)) {
            //         bridge.CallStatic("loadBiddingRewarded");
            //     }
            // } catch (Exception e) {
            //     SkyLogUtils.Error("[SkyLauncherMgr] LoadRewarded failed: " + e.Message);
            // }
            // #endif
            SkyLogUtils.Debug("[SkyLauncherMgr] LoadRewarded: SDK handles loading automatically.");
        }

        public bool IsRewardedAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    return Bridge.CallStatic<bool>("isBiddingRewardedReady");
                }
                return false;
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] IsRewardedAvailable failed: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// 获取激励视频缓存数量。
        /// </summary>
        public int GetRewardedCacheCount()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    return Bridge.CallStatic<int>("getBiddingRewardedCacheCount");
                }
                return 0;
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] GetRewardedCacheCount failed: " + e.Message);
                return 0;
            }
#else
            return 0;
#endif
        }

        /// <summary>
        /// 展示激励视频广告，可传入场景 ID 用于统计。
        /// </summary>
        public void ShowRewarded(string position =null)
        {
            if (!IsRewardedAvailable())
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "ad_platform", PlatformName }
                });
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    Bridge.CallStatic("showBiddingRewarded", position ?? "");
                    SkyLogUtils.Debug("[SkyLauncherMgr] ShowRewarded: " + position);
                }
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] ShowRewarded failed: " + e.Message);
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "ad_platform", PlatformName }
                });
            }
#endif
        }

        public void DestroyRewarded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (Bridge != null)
                {
                    Bridge.CallStatic("destroyBiddingRewarded");
                    SkyLogUtils.Debug("[SkyLauncherMgr] DestroyRewarded");
                }
            }
            catch (Exception e)
            {
                SkyLogUtils.Error("[SkyLauncherMgr] DestroyRewarded failed: " + e.Message);
            }
#endif
        }

        public double GetRewardedRevenue()
        {
            // Launcher SDK 未提供直接获取 eCPM 的接口，需通过回调获取
            return -1;
        }

        #endregion

        #region ISkyAdService - Banner（不支持）

        public void LoadBanner()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Banner not supported.");
        }

        public bool IsBannerReady() => false;

        public void ShowBanner()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Banner not supported.");
        }

        public void HideBanner()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Banner not supported.");
        }

        public double GetBannerRevenue() => -1;

        #endregion

        #region ISkyAdService - Splash（不支持）

        public void LoadSplash()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Splash not supported.");
        }

        public bool IsSplashAvailable() => false;

        public void ShowSplash()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Splash not supported.");
        }

        public double GetSplashRevenue() => -1;

        #endregion

        #region ISkyAdService - Native（不支持）

        public void LoadNative()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Native not supported.");
        }

        public bool IsNativeAvailable() => false;

        public void ShowNative()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Native not supported.");
        }

        public void RemoveNative()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] Native not supported.");
        }

        public double GetNativeRevenue() => -1;

        public bool IsNativeFullscreenAvailable() => false;
        public void ShowNativeFullscreen()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] NativeFullscreen not supported.");
        }
        public void RemoveNativeFullscreen()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] NativeFullscreen not supported.");
        }
        public double GetNativeFullscreenRevenue() => -1;

        public bool IsNativeSmallAvailable() => false;
        public void ShowNativeSmall()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] NativeSmall not supported.");
        }
        public void RemoveNativeSmall()
        {
            SkyLogUtils.Debug("[SkyLauncherMgr] NativeSmall not supported.");
        }
        public double GetNativeSmallRevenue() => -1;

        #endregion
    }

    #region Android 回调接收器

    /// <summary>
    /// 挂载在 Unity GameObject 上，用于接收 Android 侧通过 setUnityCallbackTarget 发送的回调。
    /// Android 侧会通过 UnitySendMessage 调用以下方法。
    /// </summary>
    public class LauncherAdCallback : MonoBehaviour
    {
        // 回调方法名需要与 Android 侧约定一致，以下为示例
        // 具体方法名和参数请参考 Launcher SDK 的 Android 端实现

        /// <summary>
        /// 插屏广告加载成功回调。
        /// </summary>
        public void OnInterstitialLoaded(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnInterstitialLoaded: " + message);
            // 可在此触发预加载逻辑或更新 UI
        }

        /// <summary>
        /// 插屏广告加载失败回调。
        /// </summary>
        public void OnInterstitialLoadFailed(string error)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnInterstitialLoadFailed: " + error);
        }

        /// <summary>
        /// 插屏广告展示成功回调。
        /// </summary>
        public void OnInterstitialShown(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnInterstitialShown: " + message);
            // 触发 SkyAdsMgr 的展示回调
            SkyAdsMgr.Instance._ivShowCallback?.Invoke(new Dictionary<string, object>
            {
                { "ad_platform", "Launcher" }
            });
        }

        /// <summary>
        /// 插屏广告展示失败回调。
        /// </summary>
        public void OnInterstitialShowFailed(string error)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnInterstitialShowFailed: " + error);
            SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
        }

        /// <summary>
        /// 插屏广告关闭回调。
        /// </summary>
        public void OnInterstitialClosed(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnInterstitialClosed: " + message);
            SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
        }

        /// <summary>
        /// 插屏广告点击回调。
        /// </summary>
        public void OnInterstitialClicked(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnInterstitialClicked: " + message);
        }

        /// <summary>
        /// 激励视频加载成功回调。
        /// </summary>
        public void OnRewardedLoaded(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewardedLoaded: " + message);
        }

        /// <summary>
        /// 激励视频加载失败回调。
        /// </summary>
        public void OnRewardedLoadFailed(string error)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewardedLoadFailed: " + error);
        }

        /// <summary>
        /// 激励视频展示成功回调。
        /// </summary>
        public void OnRewardedShown(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewardedShown: " + message);
            SkyAdsMgr.Instance._rvShowCallback?.Invoke();
        }

        /// <summary>
        /// 激励视频展示失败回调。
        /// </summary>
        public void OnRewardedShowFailed(string error)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewardedShowFailed: " + error);
            SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                { "ad_platform", "Launcher" }
            });
        }

        /// <summary>
        /// 激励视频关闭回调。
        /// </summary>
        public void OnRewardedClosed(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewardedClosed: " + message);
            SkyAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object>
            {
                { "isended", true }
            });
        }

        /// <summary>
        /// 激励视频达成奖励条件回调。
        /// </summary>
        public void OnRewarded(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewarded: " + message);
        }

        /// <summary>
        /// 激励视频点击回调。
        /// </summary>
        public void OnRewardedClicked(string message)
        {
            SkyLogUtils.Debug("[LauncherAdCallback] OnRewardedClicked: " + message);
        }
    }

    #endregion
}
#endif