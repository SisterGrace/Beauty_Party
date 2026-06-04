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
        private static Dictionary<string,object> _configDict = new Dictionary<string, object>();

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

        public static AdsConfig Parse(string json)
        {
            CoreLogUtils.Info("AdConfig start parsed jsonstr:"+json);
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    CoreLogUtils.Error("AdsConfig JSON string is null or empty");
                    return null;
                }

                _config = JsonUtility.FromJson<AdsConfig>(json);
                _configDict = JsonToDictionary(json);

                if (_config == null)
                {
                    CoreLogUtils.Error("Failed to parse JSON to AdConfig");
                    return null;
                }

                CoreLogUtils.Info("AdConfig parsed successfully");
                return _config;
            }
            catch (Exception e)
            {
                CoreLogUtils.Error($"Error parsing AdConfig: {e.Message}");
                return null;
            }
        }
        
        public static Dictionary<string,object> JsonToDictionary(string jsonStr)
        {
            Dictionary<string,object> dic= JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);
            return dic;
        }

#region  LevelPlay
        public static List<string> GetLevelPlayAdUnits(CoreLiteAdsType adType)
        {

            if (_config == null)
            {
                return new List<string>();
            }

            switch (adType)
            {
                
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("levelplay_rewarded_units"))
                    {
                        return _config.levelplay_rewarded_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
 
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("levelplay_interstitial_units"))
                    {
                        return _config.levelplay_interstitial_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
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
            {
                return new List<string>();
            }

            switch (adType)
            {
                
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("max_rewarded_units"))
                    {
                        return _config.max_rewarded_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
 
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("max_interstitial_units"))
                    {
                        return _config.max_interstitial_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
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
            {
                return new List<string>();
            }

            switch (adType)
            {
                
                case CoreLiteAdsType.Rewarded:
                    if (_configDict.ContainsKey("pangle_rewarded_units"))
                    {
                        return _config.pangle_rewarded_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
 
                case CoreLiteAdsType.Interstitial:
                    if (_configDict.ContainsKey("pangle_interstitial_units"))
                    {
                        return _config.pangle_interstitial_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
                case CoreLiteAdsType.Splash:
                    if (_configDict.ContainsKey("pangle_splash_units"))
                    {
                        return _config.pangle_splash_units ?? new List<string>();
                    }
                    else
                    {
                        return new List<string> ();
                    }
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

        // 获取MTG激励广告单元列表
        public static List<MtgAdUnit> GetMtgRewardedAdUnits()
        {
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

        /// <summary>
        /// 获取 Kwai 广告位 ID 列表。JSON key：kwai_interstitial_units / kwai_rewarded_units。
        /// </summary>
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

        /// <summary>
        /// 获取 Admob 广告单元ID。仅来自在线参数，无在线时返回空。
        /// </summary>
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
        /// <summary>
        /// 获取 Ad Quality app key。优先使用独立字段 ad_quality_app_key，为空时回退到 levelplay_app_key。
        /// </summary>
        public static string GetAdQualityAppKey()
        {
            if (_config == null) return "";
            if (_configDict.ContainsKey("ad_quality_app_key") && !string.IsNullOrEmpty(_config.ad_quality_app_key))
                return _config.ad_quality_app_key;
            return _config.levelplay_app_key ?? "";
        }
        #endregion

        #region 获取广告过期时间
        /// <summary>
        /// 获取广告过期时间（秒）。-1 或 ≤0 表示不启用过期判定。
        /// </summary>
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
        /// <summary> 在线 AdUnits JSON：全屏 Native 多广告位。 </summary>
        public List<string> admob_native_fullscreen_units;
        /// <summary> 在线 AdUnits JSON：Small 模板 Native 多广告位。 </summary>
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

        public List<string> mtg_interstitial_units;
        public List<string> mtg_rewarded_units;
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

        // Ad Quality 配置
        public string ad_quality_app_key;

        // 通用配置
        public int ad_expire_in_sec;
    
    }
}

