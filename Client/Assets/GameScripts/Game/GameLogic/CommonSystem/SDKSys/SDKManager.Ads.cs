using System;
using Cysharp.Threading.Tasks;
using TK_Engine;
using UnityEngine.Events;
#if USE_UP2SDK
using CoreLite;
#endif

namespace GameLogic
{
    public partial class SDKManager
    {
        public H5CheckInfo H5CheckInfo;

        public bool IsDebugFreeAds = false;
        /// <summary>
        /// 含去广告商品必接
        /// </summary>
        public static void SetRemoveAds()
        {
#if USE_UP2SDK
            CoreSdkMgr.SetRemoveAds();
#endif
        }

        public struct HandCallbackInfo
        {
            public string msg;
            public bool isReady;
        }

        /// <summary>
        /// 展示插屏广告。H5/原生竞价由 CoreSdkMgr 内部处理。
        /// </summary>
        /// <param name="adPos">插屏点位</param>
        /// <param name="_IvType">插屏 type</param>
        /// <param name="_closeCallback">插屏关闭回调，true：正常关闭，false：规则限制或未准备好</param>
        public void Button_ShowIV(HCIVPositionName adPos, IvType _IvType, Action<bool> _closeCallback = null)
        {
#if UNITY_EDITOR
            _closeCallback?.Invoke(true);
            GetInterstitialTimes++;
            return;
#endif
            if (UserController.Instance.AdFree ||
                SettingsUtils.FrameworkGlobalSettings.IsWhitePackage)
            {
                _closeCallback?.Invoke(true);
                Log.Info("插屏广告关闭");
                return;
            }

            _currentIvCloseCallback = _closeCallback;
#if USE_UP2SDK
            CoreSdkMgr.ShowInterstitial(adPos.ToString(), (CoreLite.IvType)_IvType, () =>
            {
                PostToMainNextFrame(() =>
                {
                    GetInterstitialTimes++;
                    _currentIvCloseCallback?.Invoke(true);
                    _currentIvCloseCallback = null;
                });
            });
#else
            // 等待新 SDK 接入：未接入时等同直接放过
            _closeCallback?.Invoke(true);
            _currentIvCloseCallback = null;
#endif
        }

        /// <summary>
        /// 播放激励视频广告。H5/原生竞价由 CoreSdkMgr 内部处理。
        /// </summary>
        public void Button_ShowRV(HCRVPositionName adPos, Action<bool> rewardCallback = null,
            Action showFailedCallback = null,
            bool useSDKToast = true, string toastContent = "")
        {
            Button_ShowRV(adPos.ToString(), rewardCallback, showFailedCallback, useSDKToast, toastContent);
        }

        public void Button_ShowRV(string adPos, Action<bool> rewardCallback = null,
            Action showFailedCallback = null,
            bool useSDKToast = true, string toastContent = "")
        {
#if UNITY_EDITOR
            OnRewardSuccess(rewardCallback);
            return;
#endif
            if (SettingsUtils.FrameworkGlobalSettings.IsWhitePackage || IsDebugFreeAds)
            {
                OnRewardSuccess(rewardCallback);
                Log.Info("激励广告关闭");
                return;
            }

#if USE_UP2SDK
            CoreSdkMgr.ShowRewardedVideo(adPos,
                () => PostToMainNextFrame(() => OnRewardSuccess(rewardCallback)),
                () => PostToMainNextFrame(() => showFailedCallback?.Invoke()));
#else
            // 等待新 SDK 接入：未接入时直接发奖
            OnRewardSuccess(rewardCallback);
#endif
        }

        private void OnRewardSuccess(Action<bool> rewardCallback)
        {
            UserController.Instance.AddDreamPassAdProgress();
            rewardCallback?.Invoke(true);
        }

        /// <summary>
        /// 把 SDK 回调切回 Unity 主线程并延迟一帧再执行。
        /// 1) TopOn (secmtp) 的视频广告回调在自己的 Worker 线程池触发，子线程访问 Unity API/GfxDevice 会引发 IL2CPP fail-fast (SIGTRAP)；
        /// 2) 即便在主线程，广告 Activity 刚 onResume 时 Graphics 设备可能尚未就绪，延迟一帧可避开该窗口；
        /// 详见 Docs/CrashAnalysis/2026-05-14_TPN_Reward_Callback_OffMainThread.md。
        /// </summary>
        private static void PostToMainNextFrame(Action action)
        {
            if (action == null) return;
            DelayInvokeAsync(action).Forget();
        }

        private static async UniTaskVoid DelayInvokeAsync(Action action)
        {
            await UniTask.DelayFrame(1);
            action();
        }

        /// <summary>
        /// 展示Banner广告
        /// </summary>
        public void Button_ShowBanner()
        {
            if (UserController.Instance.AdFree || SettingsUtils.FrameworkGlobalSettings.IsWhitePackage)
            {
                Log.Info("触发Banner广告，但广告位关闭");
                return;
            }
            Log.Info("开始展示Banner");
#if USE_UP2SDK
            CoreSdkMgr.ShowBanner();
#endif
        }

        public void Button_HideBanner()
        {
            Log.Info("隐藏启动Banner1");
#if USE_UP2SDK
            CoreSdkMgr.HideBanner();
#endif
            Log.Info("隐藏启动Banner2");
        }

        /// <summary>
        /// 强制触发H5广告，仅用于功能验证。
        /// 新 SDK 已将 H5 竞价能力收编到 CoreSdkMgr.ShowInterstitial / ShowRewardedVideo 内部，
        /// 这里不再做强制平台选择，按 H5CheckType 直接走对应类型的展示。
        /// </summary>
        public void ForceShowH5Ad()
        {
            if (H5CheckInfo.H5CheckType == 0)
            {
                GameEvent.Send(EventType.ShowTips, "uitips_zhuanpan", true);
                return;
            }
#if USE_UP2SDK
            switch (H5CheckInfo.H5CheckType)
            {
                case 1:
                    CoreSdkMgr.ShowRewardedVideo("RV_H5Check", null, () =>
                    {
                        PostToMainNextFrame(() =>
                            GameEvent.Send(EventType.ShowTips, "uitips_zhuanpan", true));
                    });
                    break;
                case 2:
                    CoreSdkMgr.ShowInterstitialIgnoreFrequency("IV_H5Check", null);
                    break;
                default:
                    GameEvent.Send(EventType.ShowTips, "uitips_zhuanpan", true);
                    break;
            }
#else
            GameEvent.Send(EventType.ShowTips, "uitips_zhuanpan", true);
#endif
        }
    }
}
