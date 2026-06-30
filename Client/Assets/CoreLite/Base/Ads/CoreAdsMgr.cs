using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreLite
{
    public class CoreAdsMgr : CoreSingleton<CoreAdsMgr>
    {
        public bool _adActive = false;
        public string _rvPos = "";
        public string _ivPos = "";
        public string _nativePos = "";
        public string _nativeFullscreenPos = "";
        public string _nativeSmallPos = "";

        public Action _rvShowCallback;
        public Action<Dictionary<string,object>> _ivShowCallback;
        public Action<Dictionary<string,object>> _rvCloseCallback;
        public Action<Dictionary<string,object>> _rvShowFailCallback;
        public Action _ivCloseCallback;
        public Action _nativeFullscreenCloseCallback;
        private List<ICoreAdService> _adNetworks = new List<ICoreAdService>();
        
        public void Init(Action _action)
        {
            // 广告 ID 全部走本地 CoreGameConfigData，初始化在此一次性完成，无需等在线参数。
            InitializeAdNetworks();
            CoreBidPlatformMgr.Instance.InitializePlatformStates(CoreAdsConfigParser.GetAdExpireInSec());
            foreach (var network in _adNetworks)
            {
                CoreLogUtils.Debug("[CoreLiteAds] init sdk network:" + network.ClientName);
                network.Initialize();
            }

            if (CorePrefs.IsRemoveAds)
            {
                _action?.Invoke();
            }
            else
            {
                CoreLiteSplashAdsMgr.Instance.OnSplashAdClose = _action;
                CoreLiteSplashAdsMgr.Instance.SetSplash();
            }
        }

        private void InitializeAdNetworks()
        {
#if UNITY_ANDROID
            Debug.Log("CoreAdsMgr InitializeAdNetworks: Android platform"); 
            //_adNetworks.Add(CoreMtgMgr.Instance);
#endif            
            _adNetworks.Add(CoreTpnMgr.Instance);
        }

        #region  Rewarded Video
        public bool IsRewardedVideoAvailable()
        {
            return _adNetworks.Any(network => network.IsRewardedAvailable());
        }

        public void ShowRewardedVideo(string _pos, Action _closeCallback, Action _showFailCallback)
        {
            CoreAnalyticsMgr.TrackEvent("RV_Button_Click",new Dictionary<string, object>() { {"Position", _pos} } );
            _rvPos = _pos;

            var rvDidEnterFullscreen = false;
            _rvShowCallback = () =>
            {
                CoreLogUtils.Debug(" [ads] rewarded video show callback");
                rvDidEnterFullscreen = true;
                _adActive = true;
                CoreInterstitialRules.Instance._videoStartTime = Time.realtimeSinceStartup;
                CoreAdsPrefs.SetFirstShowRvTime();
            };

            _rvCloseCallback = (dic) =>
            {
                CoreLogUtils.Debug(" [ads] rewarded video close callback");
                if (rvDidEnterFullscreen)
                    CoreLiteSplashAdsMgr.Instance.SkipNextHotResumeSplashCheck();
                _adActive = false;
                CoreAnalyticsMgr.TrackEvent("RV_Close",dic);
                CoreLiteSplashAdsMgr.Instance._backgroundTime = Time.realtimeSinceStartup;
                _closeCallback?.Invoke();
            };

            _rvShowFailCallback = (dic) =>
            {
                CoreLogUtils.Debug(" [ads] rewarded video show fail callback");
                _adActive = false;
                CoreAnalyticsMgr.TrackEvent("RV_ShowFail",dic);
                _showFailCallback?.Invoke();
            };

            if(IsRewardedVideoAvailable())
            {
                CoreLogUtils.Debug("CoreLite show rewarded video");
                var results = SelectBestAds(CoreLiteAdsType.Rewarded);
                var winner = results.Count > 0 ? results[0] : null;
                CoreLogUtils.Debug("CoreLite show rewarded video winner:"+winner?.ClientName);
                if (winner == null)
                {
                    _rvShowFailCallback?.Invoke(new Dictionary<string, object>()
                    { 
                        { "Position", CoreAdsMgr.Instance._rvPos ?? ""},
                        { "show_fail_reason","winner is null"},
                        { "ad_platform", CorePlatformType.NULL.ToString() },
                    });
                }
                else
                {
                    CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Rewarded);
                    //CoreAnalyticsMgr.TrackEvent("RV_Winner", BuildBidEventParams(results, _pos));
                    winner.Client.ShowRewarded();
                    CheckAndRefreshExpiredBids(CoreLiteAdsType.Rewarded);
                }  
            }
            else
            {
                CoreLogUtils.Debug("CoreLite rewarded not ready");
                _rvShowFailCallback?.Invoke(new Dictionary<string, object>()
                { 
                    { "Position", CoreAdsMgr.Instance._rvPos ?? ""},
                    { "show_fail_reason","not ready"},
                    { "ad_platform", CorePlatformType.NULL.ToString() },
                });
                    
                CoreAnalyticsMgr.TrackEvent("RV_ShowFail_Not_Ready",new Dictionary<string, object>() { {"Position", _pos} } );
            }
        }
        #endregion


        #region  Interstitial
        private bool IsInterstitialAvailable()
        {
            return _adNetworks.Any(network => network.IsInterstitialAvailable());
        }

        
        private bool IsInterstitialReady(IvType _ivType, string _adPos)
        {
            if(CorePrefs.IsRemoveAds) return false;
           
            // 使用 CanShowInterPeek 仅做只读检查，避免与 ShowInterstitial 内已调用的 CanShowInter 重复计次
            if (CoreInterstitialRules.Instance.CanShowInterPeek(_ivType) != null)
            {
                CoreLogUtils.Debug(" [ads] skip iv by condition");
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit",new Dictionary<string, object>() { {"Position", _adPos} } );
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>() { { "Position", _adPos } });
                return false;
            }

            if (!IsInterstitialAvailable())
            {
                CoreLogUtils.Debug(" [ads] iv not ready");
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Not_Ready",new Dictionary<string, object>() { {"Position", _adPos} } );
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>() { { "Position", _adPos } });
                return false;
            }

            return true;
        }

        public void ShowInterstitial(string _adPos, IvType _IvType = IvType.IV1, Action _closeCallback = null, Action<HandCallbackInfo, Action> _action = null)
        {
            _ivPos = _adPos;

            var ivDidEnterFullscreen = false;
            _ivShowCallback = (eventValue) =>
            {
                ivDidEnterFullscreen = true;
                _adActive = true;
                CoreInterstitialRules.Instance._videoStartTime = Time.realtimeSinceStartup;
                CoreAnalyticsMgr.TrackEvent("IV_Show",eventValue);
            };

            _ivCloseCallback = () =>
            {
                if (ivDidEnterFullscreen)
                    CoreLiteSplashAdsMgr.Instance.SkipNextHotResumeSplashCheck();
                _adActive = false;
                CoreLiteSplashAdsMgr.Instance._backgroundTime = Time.realtimeSinceStartup;
                _closeCallback?.Invoke();
            };
            
            if(CorePrefs.IsRemoveAds)
            {
                _closeCallback?.Invoke();
                return;
            }

            CoreAnalyticsMgr.TrackEvent("IV_Position",new Dictionary<string, object>() { {"Position", _adPos} } );
            string ivRuleMsg = CoreInterstitialRules.Instance.CanShowInter(_IvType);
            bool ruleAllowsShow = ivRuleMsg == null;
            // 规则未通过时必须直接返回：CheckIfReachFirstNTimes 会先自增计数再返回失败，
            // 随后 CanShowInterPeek 会读到已写入的更大值，易误判为可通过频次，从而错误展示。
            if (!ruleAllowsShow)
            {
                _action?.Invoke(MakeHandCallbackInfo(false, ivRuleMsg ?? "Interstitial blocked by rules.", false), null);
                return;
            }

            if (!IsInterstitialReady(_IvType, _adPos))
            {
                _action?.Invoke(MakeHandCallbackInfo(false, "Interstitial ad can not show yet.", ruleAllowsShow), null);
                return;
            }

            if (_action != null)
            {
                _action(MakeHandCallbackInfo(true, "success", true), () =>
                {
                    CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Match",new Dictionary<string, object>() { {"Position", _adPos} } );
                    var results = SelectBestAds(CoreLiteAdsType.Interstitial);
                    var winner = results.Count > 0 ? results[0] : null;
                    if (winner == null)
                    {
                        _ivCloseCallback?.Invoke();
                    }
                    else
                    {
                        //CoreAnalyticsMgr.TrackEvent("IV_Winner", BuildBidEventParams(results, _adPos));
                        CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Interstitial);
                        winner.Client.ShowInterstitial();
                        CheckAndRefreshExpiredBids(CoreLiteAdsType.Interstitial);
                    }
                });
                _action = null;
            }
            else
            { 
                CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Match",new Dictionary<string, object>() { {"Position", _adPos} } );
                var results = SelectBestAds(CoreLiteAdsType.Interstitial);
                var winner = results.Count > 0 ? results[0] : null;
                if (winner == null)
                {
                    _ivCloseCallback?.Invoke();
                }
                else
                {
                    //CoreAnalyticsMgr.TrackEvent("IV_Winner", BuildBidEventParams(results, _adPos));
                    CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Interstitial);
                    winner.Client.ShowInterstitial();
                    CheckAndRefreshExpiredBids(CoreLiteAdsType.Interstitial);
                }
            }
        }

        /// <summary>
        /// 展示插屏广告，不受频次规则控制；仅当有可用插屏时展示，无可用时直接回调。
        /// </summary>
        /// <param name="_adPos">广告位</param>
        /// <param name="_closeCallback">关闭或未展示时的回调</param>
        public void ShowInterstitialIgnoreFrequency(string _adPos, Action _closeCallback = null)
        {
            _ivPos = _adPos;

            var ivDidEnterFullscreen = false;
            _ivShowCallback = (eventValue) =>
            {
                ivDidEnterFullscreen = true;
                _adActive = true;
                CoreInterstitialRules.Instance._videoStartTime = Time.realtimeSinceStartup;
                CoreAnalyticsMgr.TrackEvent("IV_Show", eventValue);
            };

            _ivCloseCallback = () =>
            {
                if (ivDidEnterFullscreen)
                    CoreLiteSplashAdsMgr.Instance.SkipNextHotResumeSplashCheck();
                _adActive = false;
                CoreLiteSplashAdsMgr.Instance._backgroundTime = Time.realtimeSinceStartup;
                _closeCallback?.Invoke();
            };

            if (CorePrefs.IsRemoveAds)
            {
                _closeCallback?.Invoke();
                return;
            }

            if (!IsInterstitialAvailable())
            {
                _ivCloseCallback?.Invoke();
                return;
            }

            CoreAnalyticsMgr.TrackEvent("IV_Position", new Dictionary<string, object>() { { "Position", _adPos } });
            //CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_IgnoreFrequency", new Dictionary<string, object>() { { "Position", _adPos } });
            var results = SelectBestAds(CoreLiteAdsType.Interstitial);
            var winner = results.Count > 0 ? results[0] : null;
            if (winner == null)
            {
                _ivCloseCallback?.Invoke();
            }
            else
            {
                //CoreAnalyticsMgr.TrackEvent("IV_Winner", BuildBidEventParams(results, _adPos));
                CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Interstitial);
                winner.Client.ShowInterstitial();
                CheckAndRefreshExpiredBids(CoreLiteAdsType.Interstitial);
            }
        }

        private HandCallbackInfo MakeHandCallbackInfo(bool isReady, string msg, bool frequencyAllowsShow = false)
        {
            var action = new HandCallbackInfo
            {
                msg = msg,
                isReady = isReady,
                frequencyAllowsShow = frequencyAllowsShow
            };
            return action;
        }

        #endregion

        #region  banner

        public bool IsBannerAvailable()
        {
            if (CorePrefs.IsRemoveAds) return false;
            return _adNetworks.Any(network => network.IsBannerReady());
        }

        /// <returns>是否已向竞价胜出方发起展示（无填充时为 false）。</returns>
        public bool ShowBanner()
        {
            if (CorePrefs.IsRemoveAds) return false;
            var results = SelectBestAds(CoreLiteAdsType.Banner);
            var winner = results.Count > 0 ? results[0] : null;
            if (winner == null)
            {
                CoreAnalyticsMgr.TrackEvent("BN_ShowFail_Not_Ready", new Dictionary<string, object>
                {
                    { "ad_platform", CorePlatformType.NULL.ToString() }
                });
                foreach (var network in _adNetworks)
                {
                    network.LoadBanner();
                }
                return false;
            }
            CoreAnalyticsMgr.TrackEvent("BN_Winner", BuildBidEventParams(results, ""));
            CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Banner);
            winner.Client.ShowBanner();
            return true;
        }

        public void HideBanner()
        {
            foreach (var network in _adNetworks)
            {
                network.HideBanner();
            }
        }
        #endregion


        #region  Splash
        public void LoadSplash()
        {
            if(CorePrefs.IsRemoveAds) return;
            foreach (var network in _adNetworks)
            {
                CoreLogUtils.Debug("init ads sdk network:"+network.ToString());
                network.LoadSplash();
            }
        }

        public bool IsSplashAvailable()
        {
            if(CorePrefs.IsRemoveAds) return false;
            return _adNetworks.Any(network => network.IsSplashAvailable());
        }

        /// <returns>是否已向竞价胜出方发起展示（无填充时为 false）。</returns>
        public bool ShowSplash()
        {
            if (CorePrefs.IsRemoveAds) return false;
            var results = SelectBestAds(CoreLiteAdsType.Splash);
            var winner = results.Count > 0 ? results[0] : null;
            if (winner != null)
            {
                var winnerParams = BuildBidEventParams(results, "");
                winnerParams["launch_type"] = CoreLiteSplashAdsMgr.Instance?.CurrentLaunchType ?? "unknown";
                CoreLiteSplashAdsMgr.TrackSplashEvent("SP_Winner", winnerParams);
                CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Splash);
                winner.Client.ShowSplash();
                CheckAndRefreshExpiredBids(CoreLiteAdsType.Splash);
                return true;
            }

            return false;
        }
        #endregion

        #region 对外接口：当前竞价成功价格（仅查询竞价，不触发频次等规则）

        /// <summary>
        /// 返回当前竞价成功的激励视频价格（eCPM）。仅做竞价查询，不触发展示或频次逻辑。
        /// </summary>
        /// <returns>当前最高出价，单位与各网络一致；无可用出价时返回 -1</returns>
        public double GetCurrentRewardedVideoBidPrice()
        {
            var results = SelectBestAds(CoreLiteAdsType.Rewarded);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        /// <summary>
        /// 返回当前可用插屏广告的最高出价（eCPM）。仅做竞价查询，不检查频次规则、不增加插屏频次计数。
        /// </summary>
        /// <param name="ivType">保留参数，兼容旧调用，不参与计算</param>
        /// <returns>当前可用插屏最高出价；无可用出价时返回 -1</returns>
        public double GetCurrentInterstitialBidPrice(IvType ivType = IvType.IV1)
        {
            var results = SelectBestAds(CoreLiteAdsType.Interstitial);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        /// <summary>
        /// 返回当前竞价成功的原生广告价格（eCPM）。仅做竞价查询，不触发展示逻辑。
        /// </summary>
        /// <returns>当前最高出价；无可用出价时返回 -1</returns>
        public double GetCurrentNativeBidPrice()
        {
            var results = SelectBestAds(CoreLiteAdsType.Native);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        public double GetCurrentNativeFullscreenBidPrice()
        {
            var results = SelectBestAds(CoreLiteAdsType.NativeFullscreen);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        public double GetCurrentSmallNativeBidPrice()
        {
            var results = SelectBestAds(CoreLiteAdsType.NativeSmall);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        #endregion

        #region  Others

        /// <summary>
        /// 构建竞价事件参数：竞价成功的平台与收益、竞价失败的平台与收益
        /// </summary>
        private Dictionary<string, object> BuildBidEventParams(List<CoreAdsBidResult> results, string position)
        {
            var dic = new Dictionary<string, object> { { "Position", position ?? "" } };
            if (results == null || results.Count == 0)
            {
                dic["ad_platform"] = CorePlatformType.NULL.ToString();
                dic["bid_winner_revenue"] = -1d;
                dic["bid_failed_platforms"] = "";
                dic["bid_failed_revenues"] = "";
                return dic;
            }
            var winner = results[0];
            dic["ad_platform"] = winner.ClientName;
            dic["bid_winner_revenue"] = winner.Revenue;
            var failedPlatforms = new List<string>();
            var failedRevenues = new List<double>();
            for (int i = 1; i < results.Count; i++)
            {
                failedPlatforms.Add(results[i].ClientName);
                failedRevenues.Add(results[i].Revenue);
            }
            dic["bid_failed_platforms"] = string.Join(",", failedPlatforms);
            dic["bid_failed_revenues"] = string.Join(",", failedRevenues.Select(r => r.ToString()));
            return dic;
        }
        private List<CoreAdsBidResult> SelectBestAds(CoreLiteAdsType type)
        {
            var results = new List<CoreAdsBidResult>();

            foreach (var client in _adNetworks)
            {
                bool isAvailable = false;
                double revenue = 0;
                switch (type)
                {
                    case CoreLiteAdsType.Rewarded:
                        isAvailable = client.IsRewardedAvailable();
                        revenue = isAvailable ? client.GetRewardedRevenue() : -1;
                        break;
                        
                    case CoreLiteAdsType.Interstitial:
                        isAvailable = client.IsInterstitialAvailable();
                        revenue = isAvailable ? client.GetInterstitialRevenue() : -1;
                        break;
                        
                    case CoreLiteAdsType.Banner:
                        isAvailable = client.IsBannerReady();
                        revenue = isAvailable ? client.GetBannerRevenue() : -1;
                        break;
                        
                    case CoreLiteAdsType.Splash:
                        isAvailable = client.IsSplashAvailable();
                        revenue = isAvailable ? client.GetSplashRevenue() : -1;
                        break;
                    case CoreLiteAdsType.Native:
                        isAvailable = client.IsNativeAvailable();
                        revenue = isAvailable ? client.GetNativeRevenue() : -1;
                        break;
                    case CoreLiteAdsType.NativeFullscreen:
                        isAvailable = client.IsNativeFullscreenAvailable();
                        revenue = isAvailable ? client.GetNativeFullscreenRevenue() : -1;
                        break;
                    case CoreLiteAdsType.NativeSmall:
                        isAvailable = client.IsNativeSmallAvailable();
                        revenue = isAvailable ? client.GetNativeSmallRevenue() : -1;
                        break;
                    default:
                        CoreLogUtils.Warning($" [ads] Unknown ad type: {type}");
                        continue;
                }

                if (!isAvailable || revenue < 0)
                {
                    continue;
                }

                CoreLogUtils.Debug($" [ads] Get {client.ClientName} {type} price: {revenue}");
                results.Add(new CoreAdsBidResult
                {
                    ClientName = client.ClientName,
                    Revenue = revenue,
                    Client = client,
                    AdType = type,
                    PlatformType = client.PlatformType
                });
            }
            results.Sort();

            CoreLogUtils.Debug($" [ads] Bidding completed for {type}, {results.Count} available ads");
            if (results.Count > 0)
            {
                var bestAd = results[0];
                CoreLogUtils.Info($" [ads] Best {type} ad: {bestAd.ClientName}, Revenue: {bestAd.Revenue}");
            }

            return results;
        }
        #endregion

        #region 过期刷新广告
        private void CheckAndRefreshExpiredBids(CoreLiteAdsType _adsType)
        {
            Dictionary<CorePlatformType, List<CoreLiteAdsType>> expiredBids = CoreBidPlatformMgr.Instance.GetExpiredBids();

            foreach (var kvp in expiredBids)
            {
                CorePlatformType platformName = kvp.Key;
                foreach (CoreLiteAdsType adType in kvp.Value)
                {
                    if (_adsType == adType)
                    {
                        CoreLogUtils.Debug($"{platformName} 平台 {adType} 广告竞价已过期，重新加载广告");
                        RefreshPlatformAds(platformName, adType);
                    }
                }
            }
        }

        private void RefreshPlatformAds(CorePlatformType platformName, CoreLiteAdsType adType)
        {
            switch (platformName)
            {
                case CorePlatformType.TPN:
                    RefreshTpnAds(adType);
                    break;
                case CorePlatformType.Mintegral:
                    RefreshMtgAds(adType);
                    break;
                default:
                    break;
            }
        }


        private void RefreshTpnAds(CoreLiteAdsType adType)
        {
            switch (adType)
            {
                case CoreLiteAdsType.Rewarded:
                    CoreTpnMgr.Instance.LoadRewarded();
                    break;
                case CoreLiteAdsType.Interstitial:
                    CoreTpnMgr.Instance.LoadInterstitial();
                    break;
                default:
                    break;
            }
        }

        private void RefreshMtgAds(CoreLiteAdsType adType)
        {
            switch (adType)
            {
                case CoreLiteAdsType.Rewarded:
                    Debug.Log("CoreMtgMgr.Instance.LoadAllBidRewardAds();");
                    //CoreMtgMgr.Instance.LoadAllBidRewardAds();
                    break;
                case CoreLiteAdsType.Interstitial:
                    Debug.Log("CoreMtgMgr.Instance.LoadAllBidInterstitialAds();");
                    //CoreMtgMgr.Instance.LoadAllBidInterstitialAds();
                    break;
                default:
                    break;
            }
        }

        

        

        #endregion
    }
}

