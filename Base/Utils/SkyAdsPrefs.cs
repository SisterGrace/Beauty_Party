using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public class SkyAdsPrefs : MonoBehaviour
    {
        private const string _firstShowRvTime = "first_show_rv_time";
        public static int GetFirstShowRvTime()
        {
            return SkyPrefs.GetInt(_firstShowRvTime, (int)CurrentTimestamp()) - SkyTimeUtils.GetInstallTime();
        }

        public static void SetFirstShowRvTime()
        {
            if (!SkyPrefs.HasKey(_firstShowRvTime))
            {
                SkyPrefs.SetInt(_firstShowRvTime, (int)CurrentTimestamp());
            }
        }

        public static long CurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        #region  广告播放次数（按格式 / 按点位，用于收益上报）

        private const string FormatPlayCountKeyPrefix = "ads_format_play_count_";
        private const string PositionPlayCountKeyPrefix = "ads_position_play_count_";
        private const string EmptyPositionKey = "_default";

        /// <summary>
        /// 按广告格式获取并自增展示次数（format_number）。每个广告格式独立统计，展示1次增加1次，从1开始。
        /// </summary>
        /// <param name="format">广告格式，如 Reward、Interstitial、Splash、Native、Banner</param>
        /// <returns>本次展示的序号（1、2、3…）</returns>
        public static int GetAndIncrementFormatPlayCount(string format)
        {
            string key = FormatPlayCountKeyPrefix + (string.IsNullOrEmpty(format) ? "unknown" : format);
            try
            {
                int count = SkyPrefs.GetInt(key, 0) + 1;
                SkyPrefs.SetInt(key, count);
                SkyPrefs.Save();
                return count;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        /// <summary>
        /// 按广告展示点位获取并自增展示次数（position_number）。每个点位独立统计，展示1次增加1次，从1开始。
        /// </summary>
        /// <param name="position">广告展示点位，客户端根据实际位置传入；无业务点位时建议传入格式名（如 "Splash"、"Banner"）作为默认，以区分不同广告位的 position_number</param>
        /// <returns>该点位本次展示的序号（1、2、3…）</returns>
        public static int GetAndIncrementPositionPlayCount(string position)
        {
            string key = PositionPlayCountKeyPrefix + (string.IsNullOrEmpty(position) ? EmptyPositionKey : position);
            try
            {
                int count = SkyPrefs.GetInt(key, 0) + 1;
                SkyPrefs.SetInt(key, count);
                SkyPrefs.Save();
                return count;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        #endregion

        #region  旧版按 RV/插屏 的全局次数（兼容 ad_reward_num 等，建议逐步迁移到 format/position）

        public static int GetAdsPlayCount(bool rv)
        {
            var key = rv ? "rv_ads_play_count" : "interstitial_ads_play_count";
            try
            {
                return SkyPrefs.GetInt(key, 0);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public static void SetAdsPlayCount(bool rv, int count)
        {
            var key = rv ? "rv_ads_play_count" : "interstitial_ads_play_count";
            try
            {
                SkyPrefs.SetInt(key, count);
            }
            catch (Exception ex)
            {
            }
        }

        public static void IncrementAdsPlayCount(bool rv)
        {
            var key = rv ? "rv_ads_play_count" : "interstitial_ads_play_count";
            int currentCount = GetAdsPlayCount(rv);
            SetAdsPlayCount(rv, currentCount + 1);
        }
        #endregion
    }
}

