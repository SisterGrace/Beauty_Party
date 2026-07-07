using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CoreLite
{
    public static class CoreAdsConfigParser
    {
        private static AdsConfig _config;
        private static Dictionary<string, object> _configDict = new Dictionary<string, object>();

        private static bool _enableOnlineAdUnits = false;
        public static bool IsOnlineAdUnitsEnabled => _enableOnlineAdUnits;

        // 【新增】缓存上一次成功解析的原始 JSON 字符串，用于变更检测
        private static string _lastParsedJson = null;

        public static void SetEnableOnlineAdUnits(bool enabled)
        {
            _enableOnlineAdUnits = enabled;
            CoreLogUtils.Info($"AdConfig online ad units override: {enabled}");
        }

        private static CoreGameConfigData GameConfigOrNull()
        {
            return CoreConfigMgr.Instance != null ? CoreConfigMgr.Instance.gameConfig : null;
        }

        private static List<string> SinglePlacementList(string placementId)
        {
            if (string.IsNullOrEmpty(placementId))
                return new List<string>();
            return new List<string> { placementId };
        }

        /// <summary>
        /// 解析 AdUnits JSON，并通过 out changed 参数告知调用者配置内容是否发生了实质变化。
        /// </summary>
        public static AdsConfig Parse(string json, out bool changed)
        {
            changed = false;

            // 如果当前 JSON 与上次缓存完全一致，直接返回，不做任何更新
            if (_lastParsedJson == json)
            {
                CoreLogUtils.Debug("AdUnits JSON unchanged, skip reload.");
                return _config;
            }

            // 内容发生了变化（或首次加载），开始解析
            CoreLogUtils.Info("AdUnits JSON changed, parsing new config...");
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    CoreLogUtils.Error("AdsConfig JSON string is null or empty");
                    // 即使为空，也要记录变更（用于清空逻辑）
                    _lastParsedJson = json;
                    _config = null;
                    _configDict.Clear();
                    changed = true;
                    return null;
                }

                // 尝试解析新 JSON
                var newConfig = JsonUtility.FromJson<AdsConfig>(json);
                var newDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (newConfig == null)
                {
                    CoreLogUtils.Error("Failed to parse JSON to AdConfig");
                    return null;
                }

                // 解析成功，更新缓存和配置
                _lastParsedJson = json;
                _config = newConfig;
                _configDict = newDict;

                // 读取远程开关
                bool onlineEnabled = CoreFirebaseMgr.Instance.GetConfigBool("enable_online_ad_units", false);
                SetEnableOnlineAdUnits(onlineEnabled);

                changed = true;
                CoreLogUtils.Info("AdConfig parsed successfully (changed=true)");
                return _config;
            }
            catch (Exception e)
            {
                CoreLogUtils.Error($"Error parsing AdConfig: {e.Message}");
                changed = false; // 解析失败不触发重载
                return null;
            }
        }

        public static Dictionary<string, object> JsonToDictionary(string jsonStr)
        {
            Dictionary<string, object> dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);
            return dic;
        }

        #region  LevelPlay
        public static List<string> GetLevelPlayAdUnits(CoreLiteAdsType adType)
        {
            if (_config == null)
                return new List<string>();

            switch (adType)
            {
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("levelplay_rewarded_units"))
                        return _config.levelplay_rewarded_units ?? new List<string>();
                    else
                        return new List<string>();
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("levelplay_interstitial_units"))
                        return _config.levelplay_interstitial_units ?? new List<string>();
                    else
                        return new List<string>();
                default:
                    return new List<string>();
            }
        }

        public static string GetLevelPlayAppId()
        {
            if (_config == null) return "";
            return _config.levelplay_app_key ?? "";
        }
        #endregion

        #region  Max
        public static List<string> GetMaxAdUnits(CoreLiteAdsType adType)
        {
            if (_config == null)
                return new List<string>();

            switch (adType)
            {
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("max_rewarded_units"))
                        return _config.max_rewarded_units ?? new List<string>();
                    else
                        return new List<string>();
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("max_interstitial_units"))
                        return _config.max_interstitial_units ?? new List<string>();
                    else
                        return new List<string>();
                default:
                    return new List<string>();
            }
        }

        public static string GetMaxAppKey()
        {
            if (_config == null) return "";
            return _config.max_app_key ?? "";
        }
        #endregion

        #region  Pangle
        public static List<string> GetPangleAdUnits(CoreLiteAdsType adType)
        {
            if (_config == null)
                return new List<string>();

            switch (adType)
            {
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("pangle_rewarded_units"))
                        return _config.pangle_rewarded_units ?? new List<string>();
                    else
                        return new List<string>();
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("pangle_interstitial_units"))
                        return _config.pangle_interstitial_units ?? new List<string>();
                    else
                        return new List<string>();
                case CoreLiteAdsType.Splash:
                    if (_configDict.ContainsKey("pangle_splash_units"))
                        return _config.pangle_splash_units ?? new List<string>();
                    else
                        return new List<string>();
                default:
                    return new List<string>();
            }
        }

        public static string GetPangleAppId()
        {
            if (_config == null) return "";
            return _config.pangle_app_id ?? "";
        }
        #endregion

        #region  MTG
        public static List<MtgAdUnit> GetMtgInterstitialAdUnits()
        {
            // 如果开启在线下发，优先使用在线配置
            if (_enableOnlineAdUnits && _config != null && _configDict.ContainsKey("mtg_interstitial_units"))
            {
                // 在线配置中可能存在，直接返回（可能为空列表）
                return _config.mtg_interstitial_units ?? new List<MtgAdUnit>();
            }

            // 否则使用本地配置
            var m = GameConfigOrNull()?.mtgLocalConfig;
            if (m == null || string.IsNullOrEmpty(m.interstitialPlacementId) || string.IsNullOrEmpty(m.interstitialUnitId))
            {
                CoreLogUtils.Debug("No MTG interstitial ad units in local CoreGameConfigData");
                return new List<MtgAdUnit>();
            }

            return new List<MtgAdUnit>
            {
                new MtgAdUnit { placement_id = m.interstitialPlacementId, unit_id = m.interstitialUnitId }
            };
        }

        public static List<MtgAdUnit> GetMtgRewardedAdUnits()
        {
            // 如果开启在线下发，优先使用在线配置
            if (_enableOnlineAdUnits && _config != null && _configDict.ContainsKey("mtg_rewarded_units"))
            {
                return _config.mtg_rewarded_units ?? new List<MtgAdUnit>();
            }

            // 否则使用本地配置
            var m = GameConfigOrNull()?.mtgLocalConfig;
            if (m == null || string.IsNullOrEmpty(m.rewardedPlacementId) || string.IsNullOrEmpty(m.rewardedUnitId))
            {
                CoreLogUtils.Debug("No MTG rewarded ad units in local CoreGameConfigData");
                return new List<MtgAdUnit>();
            }

            return new List<MtgAdUnit>
            {
                new MtgAdUnit { placement_id = m.rewardedPlacementId, unit_id = m.rewardedUnitId }
            };
        }

        public static string GetMTGAppId()
        {
            return GameConfigOrNull()?.mtgLocalConfig?.appId ?? "";
        }

        public static string GetMTGAppKey()
        {
            return GameConfigOrNull()?.mtgLocalConfig?.appKey ?? "";
        }
        #endregion

        #region TPN
        public static List<string> GetTpnAdUnits(CoreLiteAdsType adType)
        {
            // 如果开启在线下发，优先使用在线配置
            if (_enableOnlineAdUnits && _config != null)
            {
                switch (adType)
                {
                    case CoreLiteAdsType.Rewarded:
                        if (_configDict.ContainsKey("topon_rewarded_units"))
                            return _config.topon_rewarded_units ?? new List<string>();
                        break;
                    case CoreLiteAdsType.Interstitial:
                        if (_configDict.ContainsKey("topon_interstitial_units"))
                            return _config.topon_interstitial_units ?? new List<string>();
                        break;
                    case CoreLiteAdsType.Splash:
                        if (_configDict.ContainsKey("topon_splash_units"))
                            return _config.topon_splash_units ?? new List<string>();
                        break;
                    case CoreLiteAdsType.Banner:
                        if (_configDict.ContainsKey("topon_banner_units"))
                            return _config.topon_banner_units ?? new List<string>();
                        break;
                }
                // 在线配置中不存在该 key，继续回退到本地
            }

            // 关闭开关或在线缺失时，使用本地配置
            var t = GameConfigOrNull()?.toponLocalConfig;
            if (t == null)
                return new List<string>();

            switch (adType)
            {
                case CoreLiteAdsType.Rewarded:
                    return SinglePlacementList(t.rewardedPlacementId);
                case CoreLiteAdsType.Interstitial:
                    return SinglePlacementList(t.interstitialPlacementId);
                case CoreLiteAdsType.Splash:
                    return SinglePlacementList(t.splashPlacementId);
                case CoreLiteAdsType.Banner:
                    return SinglePlacementList(t.bannerPlacementId);
                default:
                    return new List<string>();
            }
        }

        public static string GetTpnAppId()
        {
            return GameConfigOrNull()?.toponLocalConfig?.appId ?? "";
        }

        public static string GetTpnAppKey()
        {
            return GameConfigOrNull()?.toponLocalConfig?.appKey ?? "";
        }
        #endregion

        #region Kwai
        public static string GetKwaiAppId()
        {
            if (_config == null) return "";
            return _config.kwai_app_id ?? "";
        }

        public static string GetKwaiAppToken()
        {
            if (_config == null) return "";
            return _config.kwai_app_token ?? "";
        }

        public static List<string> GetKwaiAdUnits(CoreLiteAdsType adType)
        {
            if (_config == null) return new List<string>();
            switch (adType)
            {
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("kwai_interstitial_units"))
                        return _config.kwai_interstitial_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("kwai_rewarded_units"))
                        return _config.kwai_rewarded_units ?? new List<string>();
                    return new List<string>();
                default:
                    return new List<string>();
            }
        }
        #endregion

        #region Admob
        // TODO: 测试完成后移除此标记并删除 GetAdmobTestUnits 方法，恢复正式广告位
        private static readonly bool USE_TEST_AD_UNITS = false;

        private static List<string> GetAdmobTestUnits(CoreLiteAdsType adType)
        {
            switch (adType)
            {
                case CoreLiteAdsType.Interstitial:
                    return new List<string> { "ca-app-pub-3940256099942544/1033173712" };
                case CoreLiteAdsType.Splash:
                    return new List<string> { "ca-app-pub-3940256099942544/9257395921" };
                case CoreLiteAdsType.Rewarded:
                    return new List<string> { "ca-app-pub-3940256099942544/5224354917" };
                case CoreLiteAdsType.Banner:
                    return new List<string> { "ca-app-pub-3940256099942544/9214589741" };
                case CoreLiteAdsType.Native:
                    return new List<string> { "ca-app-pub-3940256099942544/2247696110" };
                default:
                    return new List<string>();
            }
        }

        public static List<string> GetAdmobAdUnits(CoreLiteAdsType adType)
        {
            if (USE_TEST_AD_UNITS)
            {
                return GetAdmobTestUnits(adType);
            }

            if (_config == null)
            {
                return new List<string>();
            }

            switch (adType)
            {
                case CoreLiteAdsType.Banner:
                    if (_configDict.ContainsKey("admob_banner_units"))
                        return _config.admob_banner_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("admob_rewarded_units"))
                        return _config.admob_rewarded_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.Native:
                    if (_configDict.ContainsKey("admob_native_units"))
                        return _config.admob_native_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.NativeFullscreen:
                    if (_configDict.ContainsKey("admob_native_fullscreen_units"))
                        return _config.admob_native_fullscreen_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.NativeSmall:
                    if (_configDict.ContainsKey("admob_native_small_units"))
                        return _config.admob_native_small_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.Splash:
                    if (_configDict.ContainsKey("admob_splash_units"))
                        return _config.admob_splash_units ?? new List<string>();
                    return new List<string>();
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("admob_interstitial_units"))
                        return _config.admob_interstitial_units ?? new List<string>();
                    return new List<string>();
                default:
                    return new List<string>();
            }
        }
        #endregion 

        #region Ad Quality
        public static string GetAdQualityAppKey()
        {
            if (_config == null) return "";
            if (_configDict.ContainsKey("ad_quality_app_key") && !string.IsNullOrEmpty(_config.ad_quality_app_key))
                return _config.ad_quality_app_key;
            return _config.levelplay_app_key ?? "";
        }
        #endregion

        #region 获取广告过期时间
        public static int GetAdExpireInSec()
        {
            int value = _config?.ad_expire_in_sec ?? -1;
            return value > 0 ? value : -1;
        }
        #endregion
    }

    [Serializable]
    public class MtgAdUnit
    {
        public string placement_id;
        public string unit_id;
    }

    [Serializable]
    public class AdsConfig
    {
        // Admob 配置
        public List<string> admob_interstitial_units;
        public List<string> admob_splash_units;
        public List<string> admob_rewarded_units;
        public List<string> admob_banner_units;
        public List<string> admob_native_units;
        public List<string> admob_native_fullscreen_units;
        public List<string> admob_native_small_units;

        public List<string> levelplay_interstitial_units;
        public List<string> levelplay_rewarded_units;
        public List<string> levelplay_banner_units;
        public List<string> levelplay_native_units;
        public string levelplay_app_key;

        public List<string> pangle_interstitial_units;
        public List<string> pangle_splash_units;
        public List<string> pangle_rewarded_units;
        public List<string> pangle_banner_units;
        public List<string> pangle_native_units;
        public string pangle_app_id;

        public List<string> max_interstitial_units;
        public List<string> max_splash_units;
        public List<string> max_rewarded_units;
        public List<string> max_banner_units;
        public List<string> max_native_units;
        public string max_app_key;

        public List<MtgAdUnit> mtg_interstitial_units;
        public List<MtgAdUnit> mtg_rewarded_units;
        public string mtg_app_id;
        public string mtg_app_key;

        public List<string> topon_interstitial_units;
        public List<string> topon_rewarded_units;
        public List<string> topon_splash_units;
        public List<string> topon_banner_units;
        public string topon_app_id;
        public string topon_app_key;

        public string kwai_app_id;
        public string kwai_app_token;
        public List<string> kwai_interstitial_units;
        public List<string> kwai_rewarded_units;

        public string ad_quality_app_key;

        public int ad_expire_in_sec;
    }
}