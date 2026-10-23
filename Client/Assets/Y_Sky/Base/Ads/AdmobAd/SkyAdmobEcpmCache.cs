#if ADMOB_AD
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob 展示价格缓存（仅用于竞价）。
    ///
    /// 背景：AdMob 直连在 Load 阶段拿不到 eCPM。Google 只在广告真正产生展示后，
    /// 通过 OnAdPaid 回调下发本次展示的真实收益（Android 原生 SDK 与 Unity 插件行为一致）。
    /// 因此这里统一缓存「上一次实际展示」的 eCPM：
    ///   - 每次 OnAdPaid 触发时覆盖写入（价格随市场波动自动更新）；
    ///   - 下一次竞价直接读缓存，作为 AdMob 的出价；
    ///   - 没有历史记录时返回 0，由 SkyAdsMgr 走「首次强制展示一次」逻辑完成价格校准。
    /// </summary>
    public class SkyAdmobEcpmCache : SkySingleton<SkyAdmobEcpmCache>
    {
        private const string KeyRewardedEcpm = "admob_rewarded_last_show_ecpm";
        private const string KeyInterstitialEcpm = "admob_interstitial_last_show_ecpm";
        private const string KeySplashEcpm = "admob_splash_last_show_ecpm";
        private const string KeyBannerEcpm = "admob_banner_last_show_ecpm";
        private const string KeyRewardedTime = "admob_rewarded_last_show_time";
        private const string KeyInterstitialTime = "admob_interstitial_last_show_time";
        private const string KeySplashTime = "admob_splash_last_show_time";
        private const string KeyBannerTime = "admob_banner_last_show_time";

        private const string CurrencyUsd = "USD";

        #region 读取

        /// <summary>激励视频上一次展示的 eCPM（USD）；无记录返回 0。</summary>
        public double GetRewardedLastShowEcpm()
        {
            return SkyPrefs.GetFloat(KeyRewardedEcpm, 0f);
        }

        /// <summary>插屏上一次展示的 eCPM（USD）；无记录返回 0。</summary>
        public double GetInterstitialLastShowEcpm()
        {
            return SkyPrefs.GetFloat(KeyInterstitialEcpm, 0f);
        }

        /// <summary>开屏上一次展示的 eCPM（USD）；无记录返回 0。</summary>
        public double GetSplashLastShowEcpm()
        {
            return SkyPrefs.GetFloat(KeySplashEcpm, 0f);
        }

        /// <summary>Banner 上一次展示的 eCPM（USD）；无记录返回 0。</summary>
        public double GetBannerLastShowEcpm()
        {
            return SkyPrefs.GetFloat(KeyBannerEcpm, 0f);
        }

        public bool HasRewardedRecord => GetRewardedLastShowEcpm() > 0;
        public bool HasInterstitialRecord => GetInterstitialLastShowEcpm() > 0;
        public bool HasSplashRecord => GetSplashLastShowEcpm() > 0;
        public bool HasBannerRecord => GetBannerLastShowEcpm() > 0;

        public long GetRewardedLastShowTime() => (long)SkyPrefs.GetFloat(KeyRewardedTime, 0f);
        public long GetInterstitialLastShowTime() => (long)SkyPrefs.GetFloat(KeyInterstitialTime, 0f);
        public long GetSplashLastShowTime() => (long)SkyPrefs.GetFloat(KeySplashTime, 0f);
        public long GetBannerLastShowTime() => (long)SkyPrefs.GetFloat(KeyBannerTime, 0f);

        #endregion

        #region 写入（OnAdPaid 回调中调用）

        /// <summary>
        /// 更新激励视频「上一次展示」的 eCPM。
        /// </summary>
        /// <param name="ecpm">本次展示的真实 eCPM（美元）</param>
        /// <param name="currencyCode">AdValue.CurrencyCode，非 USD 不参与竞价</param>
        /// <param name="unitId">广告位，仅用于日志</param>
        public void UpdateRewardedLastShowEcpm(double ecpm, string currencyCode, string unitId)
        {
            if (!IsValidRevenue(ecpm, currencyCode, "Rewarded", unitId)) return;

            SkyPrefs.SetFloat(KeyRewardedEcpm, (float)ecpm);
            SkyPrefs.SetFloat(KeyRewardedTime, (float)SkyAdsPrefs.CurrentTimestamp());
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新激励视频上一次展示 eCPM: ${ecpm:F4} unit:{unitId}");
        }

        /// <summary>
        /// 更新插屏「上一次展示」的 eCPM。
        /// </summary>
        public void UpdateInterstitialLastShowEcpm(double ecpm, string currencyCode, string unitId)
        {
            if (!IsValidRevenue(ecpm, currencyCode, "Interstitial", unitId)) return;

            SkyPrefs.SetFloat(KeyInterstitialEcpm, (float)ecpm);
            SkyPrefs.SetFloat(KeyInterstitialTime, (float)SkyAdsPrefs.CurrentTimestamp());
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新插屏上一次展示 eCPM: ${ecpm:F4} unit:{unitId}");
        }

        /// <summary>
        /// 更新开屏「上一次展示」的 eCPM。
        /// </summary>
        public void UpdateSplashLastShowEcpm(double ecpm, string currencyCode, string unitId)
        {
            if (!IsValidRevenue(ecpm, currencyCode, "Splash", unitId)) return;

            SkyPrefs.SetFloat(KeySplashEcpm, (float)ecpm);
            SkyPrefs.SetFloat(KeySplashTime, (float)SkyAdsPrefs.CurrentTimestamp());
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新开屏上一次展示 eCPM: ${ecpm:F4} unit:{unitId}");
        }

        /// <summary>
        /// 更新 Banner「上一次展示」的 eCPM。
        /// </summary>
        public void UpdateBannerLastShowEcpm(double ecpm, string currencyCode, string unitId)
        {
            if (!IsValidRevenue(ecpm, currencyCode, "Banner", unitId)) return;

            SkyPrefs.SetFloat(KeyBannerEcpm, (float)ecpm);
            SkyPrefs.SetFloat(KeyBannerTime, (float)SkyAdsPrefs.CurrentTimestamp());
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新 Banner 上一次展示 eCPM: ${ecpm:F4} unit:{unitId}");
        }

        private bool IsValidRevenue(double ecpm, string currencyCode, string format, string unitId)
        {
            if (double.IsNaN(ecpm) || double.IsInfinity(ecpm) || ecpm <= 0)
            {
                SkyLogUtils.Debug($"[Admob] {format} OnAdPaid 收益无效({ecpm})，忽略。unit:{unitId}");
                return false;
            }

            // 竞价统一按 USD 比较，其他币种直接丢弃，避免汇率问题导致出价错误
            if (!string.IsNullOrEmpty(currencyCode) && currencyCode != CurrencyUsd)
            {
                SkyLogUtils.Debug($"[Admob] {format} OnAdPaid 币种为 {currencyCode}，非 USD 不参与竞价。unit:{unitId}");
                return false;
            }

            return true;
        }

        #endregion

        /// <summary>清空缓存（调试用）。</summary>
        public void ResetAll()
        {
            SkyPrefs.SetFloat(KeyRewardedEcpm, 0f);
            SkyPrefs.SetFloat(KeyInterstitialEcpm, 0f);
            SkyPrefs.SetFloat(KeySplashEcpm, 0f);
            SkyPrefs.SetFloat(KeyBannerEcpm, 0f);
            SkyPrefs.SetFloat(KeyRewardedTime, 0f);
            SkyPrefs.SetFloat(KeyInterstitialTime, 0f);
            SkyPrefs.SetFloat(KeySplashTime, 0f);
            SkyPrefs.SetFloat(KeyBannerTime, 0f);
            SkyPrefs.Save();
            SkyLogUtils.Debug("[Admob] 展示价格缓存已清空");
        }
    }
}
#endif
