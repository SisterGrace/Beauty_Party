using System;
using UnityEngine;

namespace Up2SDK
{
    /// <summary>
    /// 用户事件枚举，对应 Java FineEvent.UserEvent（REGISTER, LOGIN 等）。
    /// </summary>
    public enum Up2UserEvent
    {
        REGISTER,
        LOGIN
    }

    public class Up2SDKManager : D_MonoSingleton<Up2SDKManager>
    {
        private const string FineAnalyticsClass = "com.unity3d.player.Up";

        public void OnMTGAdShow(AdsType adsType, string adId,double ecpm, string showId = "")
        {
            OnMaxAdShow("MTG", adsType, adId, ecpm);
        }
        public void OnMTGAdClick(AdsType adsType, string adId)
        {
            OnMaxAdClick("MTG", adsType, adId);
        }

        /// <summary>
        /// 广告点击上报
        /// </summary>
        public void OnToponAdShow(int toponFirmId,AdsType adsType, string adId,double ecpm, string showId = "")
        {
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass))
                {
                    fineAnalytics.CallStatic("onToponAdShow", toponFirmId,GetAdsTypeString(adsType),adId, ecpm,showId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] OnToponAdShow failed: " + e.Message);
            }
        }
        public void OnMaxAdShow(string maxPlatform,AdsType adsType, string adId,double ecpm)
        {
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass))
                {
                    fineAnalytics.CallStatic("onMaxAdShow", maxPlatform,GetAdsTypeString(adsType),adId, ecpm);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] OnMaxAdShow failed: " + e.Message);
            }
        }
        /// <summary>
        /// 广告点击上报
        /// </summary>
        public void OnToponAdClick(int toponFirmId,AdsType adsType, string adId, string showId = "")
        {
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass))
                {
                    fineAnalytics.CallStatic("onToponAdClick", toponFirmId,GetAdsTypeString(adsType),adId, showId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] OnToponAdClick failed: " + e.Message);
            }
        }
        /// <summary>
        /// 广告点击上报
        /// </summary>
        public void OnMaxAdClick(string maxPlatform,AdsType adsType, string adId)
        {
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass))
                {
                    fineAnalytics.CallStatic("onMaxAdClick", maxPlatform,GetAdsTypeString(adsType),adId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] OnMaxAdClick failed: " + e.Message);
            }
        }

        private string GetAdsTypeString(AdsType adsType)
        {
            string atype = adsType.ToString();
            if (adsType == AdsType.Banner)
            {
                atype = "BANNER";
            }
            if (adsType == AdsType.Rewarded)
            {
                atype = "REWARD_VIDEO";
            }
            if (adsType == AdsType.Interstitial)
            {
                atype = "INTERSTITIAL";
            }
            if (adsType == AdsType.Splash)
            {
                atype = "SPLASH";
            }
            if (adsType == AdsType.Native)
            {
                atype = "NATIVE";
            }
            return atype;
        }

        /// <summary>
        /// 用户事件上报（Up2UserEvent.REGISTER, Up2UserEvent.LOGIN）
        /// </summary>
        public void OnUserEvent(Up2UserEvent userEvent)
        {
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass))
                {
                    fineAnalytics.CallStatic("onUserEvent", userEvent.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] OnUserEvent failed: " + e.Message);
            }
        }


        /// <summary>
        /// AB 测试上报 向SDK传递AB测试信息
        /// </summary>
        public void SetABTest(string testName, int testVersion, params string[] userTags)
        {
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass))
                {
                    fineAnalytics.CallStatic("setABTest", testName ?? "", testVersion, userTags ?? new string[0]);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] SetABTest failed: " + e.Message);
            }
        }
        
        /**
         * 上报渠道号;如果应用启动后无法立即获取到当前发行渠道号，可以在获取到渠道号后调用如下代码向SDK传入渠道号
         */
        public void SetChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return;
            try
            {
                using (var fineAnalytics = new AndroidJavaClass(FineAnalyticsClass)) {
                    fineAnalytics.Call("setChannel", channel);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Up2SDKManager] SetChannel failed: " + e.Message);
            }
        }

    }
}