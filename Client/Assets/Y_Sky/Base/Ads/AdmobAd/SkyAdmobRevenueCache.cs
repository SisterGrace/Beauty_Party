#if ADMOB_AD
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob 展示价格缓存（仅用于竞价）。
    ///
    /// 背景：AdMob 直连在 Load 阶段拿不到 Revenue。Google 只在广告真正产生展示后，
    /// 通过 OnAdPaid 回调下发本次展示的真实收益（Android 原生 SDK 与 Unity 插件行为一致）。
    /// 因此这里统一缓存「上一次实际展示」的 Revenue（单次展示收益，单位：美元）：
    ///   - 每次 OnAdPaid 触发时覆盖写入（价格随市场波动自动更新）；
    ///   - 下一次竞价直接读缓存，作为 AdMob 的出价；
    ///   - 没有历史记录时返回 0，由 SkyAdsMgr 走「首次强制展示一次」逻辑完成价格校准。
    ///
    /// 注：Revenue 代表单次展示收益，eCPM（每千次展示成本）= Revenue × 1000。
    /// 货币转换已移至独立的 SkyCurrencyConverter 类，支持 20+ 常见货币。
    /// </summary>
    public class SkyAdmobRevenueCache : SkySingleton<SkyAdmobRevenueCache>
    {

        private const string KeyRewardedRevenue = "admob_rewarded_last_show_revenue";
        private const string KeyInterstitialRevenue = "admob_interstitial_last_show_revenue";
        private const string KeySplashRevenue = "admob_splash_last_show_revenue";
        private const string KeyBannerRevenue = "admob_banner_last_show_revenue";
        private const string KeyRewardedTime = "admob_rewarded_last_show_time";
        private const string KeyInterstitialTime = "admob_interstitial_last_show_time";
        private const string KeySplashTime = "admob_splash_last_show_time";
        private const string KeyBannerTime = "admob_banner_last_show_time";

        private const string CurrencyUsd = "USD";

        #region 读取

        /// <summary>激励视频上一次展示的 Revenue（单次收益，USD）；无记录返回 0。</summary>
        public double GetRewardedLastShowRevenue()
        {
            return SkyPrefs.GetFloat(KeyRewardedRevenue, 0f);
        }

        /// <summary>插屏上一次展示的 Revenue（单次收益，USD）；无记录返回 0。</summary>
        public double GetInterstitialLastShowRevenue()
        {
            return SkyPrefs.GetFloat(KeyInterstitialRevenue, 0f);
        }

        /// <summary>开屏上一次展示的 Revenue（单次收益，USD）；无记录返回 0。</summary>
        public double GetSplashLastShowRevenue()
        {
            return SkyPrefs.GetFloat(KeySplashRevenue, 0f);
        }

        /// <summary>Banner 上一次展示的 Revenue（单次收益，USD）；无记录返回 0。</summary>
        public double GetBannerLastShowRevenue()
        {
            return SkyPrefs.GetFloat(KeyBannerRevenue, 0f);
        }

        /// <summary>判断是否有历史展示记录（以时间戳为准，而非金额）。</summary>
        public bool HasRewardedRecord => GetRewardedLastShowTime() > 0;
        public bool HasInterstitialRecord => GetInterstitialLastShowTime() > 0;
        public bool HasSplashRecord => GetSplashLastShowTime() > 0;
        public bool HasBannerRecord => GetBannerLastShowTime() > 0;

        public long GetRewardedLastShowTime() => (long)SkyPrefs.GetFloat(KeyRewardedTime, 0f);
        public long GetInterstitialLastShowTime() => (long)SkyPrefs.GetFloat(KeyInterstitialTime, 0f);
        public long GetSplashLastShowTime() => (long)SkyPrefs.GetFloat(KeySplashTime, 0f);
        public long GetBannerLastShowTime() => (long)SkyPrefs.GetFloat(KeyBannerTime, 0f);

        #endregion

        #region 写入（OnAdPaid 回调中调用）

        /// <summary>
        /// 更新激励视频「上一次展示」的 Revenue（单次收益）。
        /// 只要回调触发，先更新时间戳（标记已有展示记录），再验证收益有效性。
        /// </summary>
        /// <param name="revenue">本次展示的真实收益（单次，美元）</param>
        /// <param name="currencyCode">AdValue.CurrencyCode，非 USD 不参与竞价</param>
        /// <param name="unitId">广告位，仅用于日志</param>
        public void UpdateRewardedLastShowRevenue(double revenue, string currencyCode, string unitId)
        {
            // 优先更新时间戳，标记该广告位已有展示记录
            SkyPrefs.SetFloat(KeyRewardedTime, (float)SkyAdsPrefs.CurrentTimestamp());

            // 验证收益有效性
            if (!IsValidRevenue(revenue, currencyCode, "Rewarded", unitId))
            {
                // 无效收益，保存 0 作为基准价
                SkyPrefs.SetFloat(KeyRewardedRevenue, 0f);
                SkyPrefs.Save();
                return;
            }

            // 有效收益，正常保存
            SkyPrefs.SetFloat(KeyRewardedRevenue, (float)revenue);
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新激励视频上一次展示 Revenue: ${revenue:F6} unit:{unitId}");
        }

        /// <summary>
        /// 更新插屏「上一次展示」的 Revenue（单次收益）。
        /// 只要回调触发，先更新时间戳（标记已有展示记录），再验证收益有效性。
        /// </summary>
        public void UpdateInterstitialLastShowRevenue(double revenue, string currencyCode, string unitId)
        {
            // 优先更新时间戳，标记该广告位已有展示记录
            SkyPrefs.SetFloat(KeyInterstitialTime, (float)SkyAdsPrefs.CurrentTimestamp());

            // 验证收益有效性
            if (!IsValidRevenue(revenue, currencyCode, "Interstitial", unitId))
            {
                // 无效收益，保存 0 作为基准价
                SkyPrefs.SetFloat(KeyInterstitialRevenue, 0f);
                SkyPrefs.Save();
                return;
            }

            // 有效收益，正常保存
            SkyPrefs.SetFloat(KeyInterstitialRevenue, (float)revenue);
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新插屏上一次展示 Revenue: ${revenue:F6} unit:{unitId}");
        }

        /// <summary>
        /// 更新开屏「上一次展示」的 Revenue（单次收益）。
        /// 只要回调触发，先更新时间戳（标记已有展示记录），再验证收益有效性。
        /// </summary>
        public void UpdateSplashLastShowRevenue(double revenue, string currencyCode, string unitId)
        {
            // 优先更新时间戳，标记该广告位已有展示记录
            SkyPrefs.SetFloat(KeySplashTime, (float)SkyAdsPrefs.CurrentTimestamp());

            // 验证收益有效性
            if (!IsValidRevenue(revenue, currencyCode, "Splash", unitId))
            {
                // 无效收益，保存 0 作为基准价
                SkyPrefs.SetFloat(KeySplashRevenue, 0f);
                SkyPrefs.Save();
                return;
            }

            // 有效收益，正常保存
            SkyPrefs.SetFloat(KeySplashRevenue, (float)revenue);
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新开屏上一次展示 Revenue: ${revenue:F6} unit:{unitId}");
        }

        /// <summary>
        /// 更新 Banner「上一次展示」的 Revenue（单次收益）。
        /// 只要回调触发，先更新时间戳（标记已有展示记录），再验证收益有效性。
        /// </summary>
        public void UpdateBannerLastShowRevenue(double revenue, string currencyCode, string unitId)
        {
            // 优先更新时间戳，标记该广告位已有展示记录
            SkyPrefs.SetFloat(KeyBannerTime, (float)SkyAdsPrefs.CurrentTimestamp());

            // 验证收益有效性
            if (!IsValidRevenue(revenue, currencyCode, "Banner", unitId))
            {
                // 无效收益，保存 0 作为基准价
                SkyPrefs.SetFloat(KeyBannerRevenue, 0f);
                SkyPrefs.Save();
                return;
            }

            // 有效收益，正常保存
            SkyPrefs.SetFloat(KeyBannerRevenue, (float)revenue);
            SkyPrefs.Save();
            SkyLogUtils.Debug($"[Admob] 更新 Banner 上一次展示 Revenue: ${revenue:F6} unit:{unitId}");
        }

        /// <summary>
        /// 验证收益数据有效性。
        /// 注意：
        /// 1. revenue = 0 也被视为无效（测试广告或填充失败），但时间戳仍会保存
        /// 2. 货币验证已移除 - 现在所有货币都在 HandleAdPaid 中统一转换为 USD
        /// </summary>
        private bool IsValidRevenue(double revenue, string currencyCode, string format, string unitId)
        {
            if (double.IsNaN(revenue) || double.IsInfinity(revenue) || revenue <= 0)
            {
                SkyLogUtils.Debug($"[Admob] {format} OnAdPaid 收益无效或为0({revenue})，记录时间但金额归0。unit:{unitId}");
                return false;
            }

            return true;
        }

        #endregion

        /// <summary>清空缓存（调试用）。</summary>
        public void ResetAll()
        {
            SkyPrefs.SetFloat(KeyRewardedRevenue, 0f);
            SkyPrefs.SetFloat(KeyInterstitialRevenue, 0f);
            SkyPrefs.SetFloat(KeySplashRevenue, 0f);
            SkyPrefs.SetFloat(KeyBannerRevenue, 0f);
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
