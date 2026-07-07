using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AnyThinkAds.Api;

namespace CoreLite
{
    public class CoreAdsMgr : CoreSingleton<CoreAdsMgr>
    {
        // 在 CoreAdsMgr 类顶部添加
        private bool _enableRemoteAdConfig = false; // V1.0.0 默认关闭，V1.0.1 时改为 true
        public bool _adActive = false;
        public string _rvPos = "";
        public string _ivPos = "";
        public string _nativePos = "";
        public string _nativeFullscreenPos = "";
        public string _nativeSmallPos = "";

        public Action _rvShowCallback;
        public Action<Dictionary<string, object>> _ivShowCallback;
        public Action<Dictionary<string, object>> _rvCloseCallback;
        public Action<Dictionary<string, object>> _rvShowFailCallback;
        public Action _ivCloseCallback;
        public Action _nativeFullscreenCloseCallback;
        private List<ICoreAdService> _adNetworks = new List<ICoreAdService>();

        // ---- 新增：广告是否启用（在线参数为空时禁用） ----
        private bool _adEnabled = true;
        // ---- 新增：远程配置是否已应用 ----
        private bool _remoteConfigApplied = false;
        // ---- 新增：缓存开屏回调 ----
        private Action _splashAction;

        public void Init(Action _action)
        {
            // 初始化广告网络（但先不加载广告，等待远程配置）
            InitializeAdNetworks();
            CoreBidPlatformMgr.Instance.InitializePlatformStates(CoreAdsConfigParser.GetAdExpireInSec());

            // 检查 Firebase 是否已激活（远程配置已拉取）
            if (CoreFirebaseMgr.Instance != null && CoreFirebaseMgr.Instance.TrackingEnable())
            {
                // 只有开关打开时才应用远程配置
                if (_enableRemoteAdConfig)
                {
                    // 已激活，直接应用配置
                    ApplyRemoteConfig(CoreFirebaseMgr.Instance.GetConfigStr("AdUnits", ""));
                }
                // 加载开屏（如果启用）
                ShowSplashWithCallback(_action);
            }
            else
            {
                // 订阅 Firebase 激活事件
                CoreSdkMgr.OnFirebaseActivated += OnFirebaseActivated;
                _splashAction = _action;
                // 但需求要求"开屏需要本地参数请求"，所以先使用本地参数加载开屏（但不展示，等待配置？）
                // 为了体验，可以先加载但不展示，等配置回来后再展示，或者直接展示（如果在线参数为空则禁用）
                // 这里我们选择先用本地参数初始化并加载，但展示等待配置（避免在线参数禁用时已展示）
                // 但更好的做法是：先用本地参数预加载（只加载不展示），等配置回来后再决定是否展示。
                // 为了简化，我们先加载，然后展示时再判断 _adEnabled，如果禁用则不展示。
                // 这里我们直接调用 SetSplash，它会检测 _adEnabled？但 _adEnabled 还未确定，所以先缓存。
                // 我们改为：在 OnFirebaseActivated 中调用 ShowSplashWithCallback
                // 但在此之前，为了让开屏尽快准备，我们先用本地参数加载（只加载不展示）
                foreach (var network in _adNetworks)
                {
                    network.Initialize(); // 确保网络初始化（但加载可能依赖本地参数）
                    network.LoadSplash(); // 预加载开屏（本地参数）
                }
                // 注意：不调用 SetSplash 展示，等待配置
                CoreLogUtils.Debug("[CoreAdsMgr] Waiting for Firebase config before showing splash");
            }
        }

        private void OnFirebaseActivated()
        {
            CoreSdkMgr.OnFirebaseActivated -= OnFirebaseActivated;
            ApplyRemoteConfig(CoreFirebaseMgr.Instance.GetConfigStr("AdUnits", ""));
            // 加载开屏（展示）
            ShowSplashWithCallback(_splashAction);
            _splashAction = null;
        }

        /// <summary>
        /// 应用远程配置（在线参数 AdUnits）
        /// </summary>
        /// <param name="adUnitsJson">JSON 字符串</param>
        public void ApplyRemoteConfig(string adUnitsJson)
        {
            if (!_enableRemoteAdConfig) return; // V1.0.0 直接跳过
            if (_remoteConfigApplied) return; // 避免重复应用
            _remoteConfigApplied = true;

            // 解析配置（CoreAdsConfigParser 需实现 Parse 并更新内部数据）
            CoreAdsConfigParser.Parse(adUnitsJson);

            // 检查在线参数是否为空（所有平台的广告位都为空）
            bool hasValidUnits = CheckAnyAdUnitAvailable();

            if (!hasValidUnits)
            {
                _adEnabled = false;
                CoreLogUtils.Debug("[CoreAdsMgr] Remote config AdUnits is empty, ads disabled");
                // 清理已加载的广告（可选）
                foreach (var net in _adNetworks)
                {
                    net.HideBanner(); // 至少隐藏banner
                    // 其他类型广告无法直接清理，但后续 Show 会被拦截
                }
                return;
            }

            _adEnabled = true;
            CoreLogUtils.Debug("[CoreAdsMgr] Applying remote config, reloading ad units");

            // 刷新各广告网络广告位（目前仅 TPN 实现，其他可扩展）
            foreach (var network in _adNetworks)
            {
                if (network is CoreTpnMgr tpn)
                {
                    tpn.ReloadAdUnits();
                }
                // 其他平台类似
            }
        }

        private bool CheckAnyAdUnitAvailable()
        {
            // 检查 TPN 各广告类型是否有有效 ID
            var types = new[] { CoreLiteAdsType.Interstitial, CoreLiteAdsType.Rewarded, CoreLiteAdsType.Splash, CoreLiteAdsType.Banner };
            foreach (var type in types)
            {
                var list = CoreAdsConfigParser.GetTpnAdUnits(type);
                if (list != null && list.Count > 0) return true;
            }
            // 可扩展其他平台检查
            return false;
        }

        private void ShowSplashWithCallback(Action callback)
        {
            if (CorePrefs.IsRemoveAds || !_adEnabled)
            {
                callback?.Invoke();
                return;
            }
            CoreLiteSplashAdsMgr.Instance.OnSplashAdClose = callback;
            CoreLiteSplashAdsMgr.Instance.SetSplash();
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
            if (!_adEnabled || CorePrefs.IsRemoveAds) return false;
            return _adNetworks.Any(network => network.IsRewardedAvailable());
        }

        public void ShowRewardedVideo(string _pos, Action _closeCallback, Action _showFailCallback)
        {
            if (!_adEnabled || CorePrefs.IsRemoveAds)
            {
                _showFailCallback?.Invoke();
                return;
            }

            CoreAnalyticsMgr.TrackEvent("RV_Button_Click", new Dictionary<string, object>() { { "Position", _pos } });
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
                CoreAnalyticsMgr.TrackEvent("RV_Close", dic);
                CoreLiteSplashAdsMgr.Instance._backgroundTime = Time.realtimeSinceStartup;
                _closeCallback?.Invoke();
            };

            _rvShowFailCallback = (dic) =>
            {
                CoreLogUtils.Debug(" [ads] rewarded video show fail callback");
                _adActive = false;
                CoreAnalyticsMgr.TrackEvent("RV_ShowFail", dic);
                _showFailCallback?.Invoke();
            };

            if (IsRewardedVideoAvailable())
            {
                CoreLogUtils.Debug("CoreLite show rewarded video");
                var results = SelectBestAds(CoreLiteAdsType.Rewarded);
                var winner = results.Count > 0 ? results[0] : null;
                CoreLogUtils.Debug("CoreLite show rewarded video winner:" + winner?.ClientName);
                if (winner == null)
                {
                    _rvShowFailCallback?.Invoke(new Dictionary<string, object>()
                    {
                        { "Position", CoreAdsMgr.Instance._rvPos ?? ""},
                        //{ "show_fail_reason","winner is null"},
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
                    //{ "show_fail_reason","not ready"},
                    { "ad_platform", CorePlatformType.NULL.ToString() },
                });

                CoreAnalyticsMgr.TrackEvent("RV_ShowFail_Not_Ready", new Dictionary<string, object>() { { "Position", _pos } });
            }
        }
        #endregion


        #region  Interstitial
        private bool IsInterstitialAvailable()
        {
            if (!_adEnabled || CorePrefs.IsRemoveAds) return false;
            return _adNetworks.Any(network => network.IsInterstitialAvailable());
        }


        /*private bool IsInterstitialReady(IvType _ivType, string _adPos)
        {
            if (CorePrefs.IsRemoveAds || !_adEnabled) return false;

            // 使用 CanShowInterPeek 仅做只读检查,避免与 ShowInterstitial 内已调用的 CanShowInter 重复计次
            if (CoreInterstitialRules.Instance.CanShowInterPeek(_ivType) != null)
            {
                CoreLogUtils.Debug(" [ads] skip iv by condition");

                // 规则限制时，广告可能已经准备好了，实时获取广告信息
                var adInfo = GetInterstitialAdInfo();

                if (!string.IsNullOrEmpty(adInfo.adSource))
                {
                    // 有广告信息，上报完整参数
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit", new Dictionary<string, object>()
                    {
                        { "Position", _adPos },
                        { "ad_platform", "TPN" },
                        { "ad_source", adInfo.adSource },
                        { "ad_id", adInfo.adId },
                        { "ad_ecpm", adInfo.adEcpm }
                    });
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>()
                    {
                        { "Position", _adPos },
                        { "ad_platform", "TPN" },
                        { "ad_source", adInfo.adSource },
                        { "ad_id", adInfo.adId },
                        { "ad_ecpm", adInfo.adEcpm }
                    });
                }
                else
                {
                    // 广告未准备好
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit", new Dictionary<string, object>()
                    {
                        { "Position", _adPos }
                    });
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>()
                    {
                        { "Position", _adPos }
                    });
                }
                return false;
            }

            if (!IsInterstitialAvailable())
            {
                CoreLogUtils.Debug(" [ads] iv not ready");
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Not_Ready", new Dictionary<string, object>()
                {
                    {"Position", _adPos}
                    // 【无法获取】广告没有填充，IsInterstitialAvailable() 返回 false
                    // 无广告可用，无法使用 checkAdStatus
                });
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>()
                {
                    { "Position", _adPos }
                    // { "ad_platform", "" },
                    // { "ad_id", "" },
                    // { "ad_ecpm", 0 }
                });
                return false;
            }

            return true;
        }*/

        public void ShowInterstitial(string _adPos, IvType _IvType = IvType.IV1, Action _closeCallback = null, Action<HandCallbackInfo, Action> _action = null)
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

            // ---- 移除广告或广告禁用检查 ----
            if (CorePrefs.IsRemoveAds || !_adEnabled)
            {
                _closeCallback?.Invoke();
                return;
            }

            // ---- IV_Position 打点 ----
            var ivPosInfo = GetInterstitialAdInfo();
            CoreAnalyticsMgr.TrackEvent("IV_Position", new Dictionary<string, object>()
    {
        {"Position", _adPos},
        {"ad_platform", "TPN"},
        {"ad_source", ivPosInfo.adSource},
        {"ad_id", ivPosInfo.adId},
        {"ad_ecpm", ivPosInfo.adEcpm}
    });

            // ---- 1. 规则预检查（只读，不消耗计数） ----
            string peekMsg = CoreInterstitialRules.Instance.CanShowInterPeek(_IvType);
            if (peekMsg != null)
            {
                // 规则拦截，上报 IV_ShowFail_Rule_Limit（包含广告信息，如果有的话）
                var adInfo = GetInterstitialAdInfo();
                if (!string.IsNullOrEmpty(adInfo.adSource))
                {
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit", new Dictionary<string, object>()
            {
                { "Position", _adPos },
                { "ad_platform", "TPN" },
                { "ad_source", adInfo.adSource },
                { "ad_id", adInfo.adId },
                { "ad_ecpm", adInfo.adEcpm }
            });
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>()
            {
                { "Position", _adPos },
                { "ad_platform", "TPN" },
                { "ad_source", adInfo.adSource },
                { "ad_id", adInfo.adId },
                { "ad_ecpm", adInfo.adEcpm }
            });
                }
                else
                {
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit", new Dictionary<string, object>() { { "Position", _adPos } });
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>() { { "Position", _adPos } });
                }
                _action?.Invoke(MakeHandCallbackInfo(false, peekMsg, false), null);
                return;
            }

            // ---- 2. 广告就绪检查 ----
            if (!IsInterstitialAvailable())
            {
                CoreLogUtils.Debug(" [ads] iv not ready");
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Not_Ready", new Dictionary<string, object>() { { "Position", _adPos } });
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>() { { "Position", _adPos } });
                _action?.Invoke(MakeHandCallbackInfo(false, "Interstitial ad not ready.", true), null);
                LoadInterstitial(); // 触发加载
                return;
            }

            // ---- 3. 消耗规则计数（展示前最后一次确认） ----
            string ruleMsg = CoreInterstitialRules.Instance.CanShowInter(_IvType);
            if (ruleMsg != null)
            {
                // 理论上不会发生，但若发生，上报规则失败并返回
                var adInfo = GetInterstitialAdInfo();
                if (!string.IsNullOrEmpty(adInfo.adSource))
                {
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit", new Dictionary<string, object>()
            {
                { "Position", _adPos },
                { "ad_platform", "TPN" },
                { "ad_source", adInfo.adSource },
                { "ad_id", adInfo.adId },
                { "ad_ecpm", adInfo.adEcpm }
            });
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>()
            {
                { "Position", _adPos },
                { "ad_platform", "TPN" },
                { "ad_source", adInfo.adSource },
                { "ad_id", adInfo.adId },
                { "ad_ecpm", adInfo.adEcpm }
            });
                }
                else
                {
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail_Rule_Limit", new Dictionary<string, object>() { { "Position", _adPos } });
                    CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>() { { "Position", _adPos } });
                }
                _action?.Invoke(MakeHandCallbackInfo(false, ruleMsg, false), null);
                return;
            }

            // ---- 4. 执行展示（根据是否有 _action 回调走不同分支） ----
            if (_action != null)
            {
                _action(MakeHandCallbackInfo(true, "success", true), () =>
                {
                    var results = SelectBestAds(CoreLiteAdsType.Interstitial);
                    var winner = results.Count > 0 ? results[0] : null;
                    if (winner == null)
                    {
                        _ivCloseCallback?.Invoke();
                    }
                    else
                    {
                        CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Interstitial);
                        var matchInfo = GetInterstitialAdInfo();
                        CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Match", new Dictionary<string, object>()
                {
                    {"Position", _adPos},
                    {"ad_platform", "TPN"},
                    {"ad_source", matchInfo.adSource},
                    {"ad_id", matchInfo.adId},
                    {"ad_ecpm", matchInfo.adEcpm}
                });
                        winner.Client.ShowInterstitial();
                        CheckAndRefreshExpiredBids(CoreLiteAdsType.Interstitial);
                    }
                });
                _action = null;
            }
            else
            {
                var results = SelectBestAds(CoreLiteAdsType.Interstitial);
                var winner = results.Count > 0 ? results[0] : null;
                if (winner == null)
                {
                    _ivCloseCallback?.Invoke();
                }
                else
                {
                    CoreBidPlatformMgr.Instance.RecordBidSuccess(winner.PlatformType, CoreLiteAdsType.Interstitial);
                    var matchInfo = GetInterstitialAdInfo();
                    CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Match", new Dictionary<string, object>()
            {
                {"Position", _adPos},
                {"ad_platform", "TPN"},
                {"ad_source", matchInfo.adSource},
                {"ad_id", matchInfo.adId},
                {"ad_ecpm", matchInfo.adEcpm}
            });
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

            if (CorePrefs.IsRemoveAds || !_adEnabled)
            {
                _closeCallback?.Invoke();
                return;
            }

            if (!IsInterstitialAvailable())
            {
                _ivCloseCallback?.Invoke();
                return;
            }

            // IV_Position 打点：触发插屏展示时机（忽略频次版本）
            var ivPosInfo2 = GetInterstitialAdInfo();
            CoreAnalyticsMgr.TrackEvent("IV_Position", new Dictionary<string, object>()
            {
                { "Position", _adPos },
                { "ad_platform", "TPN" },
                { "ad_source", ivPosInfo2.adSource },
                { "ad_id", ivPosInfo2.adId },
                { "ad_ecpm", ivPosInfo2.adEcpm }
            });
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
                // 在 ShowInterstitialIgnoreFrequency 的 winner != null 分支中
                var matchInfo = GetInterstitialAdInfo();
                CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Match", new Dictionary<string, object>()
                {
                    {"Position", _adPos},
                    {"ad_platform", "TPN"},
                    {"ad_source", matchInfo.adSource},
                    {"ad_id", matchInfo.adId},
                    {"ad_ecpm", matchInfo.adEcpm}
                });
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
            if (CorePrefs.IsRemoveAds || !_adEnabled) return false;
            return _adNetworks.Any(network => network.IsBannerReady());
        }

        /// <returns>是否已向竞价胜出方发起展示（无填充时为 false）。</returns>
        public bool ShowBanner()
        {
            if (CorePrefs.IsRemoveAds || !_adEnabled) return false;
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
            if (!_adEnabled) return;
            foreach (var network in _adNetworks)
            {
                network.HideBanner();
            }
        }

        public void LoadBanner()
        {
            if (!_adEnabled || CorePrefs.IsRemoveAds) return;
            foreach (var network in _adNetworks)
                network.LoadBanner();
        }
        #endregion


        #region  Splash
        public void LoadSplash()
        {
            if (CorePrefs.IsRemoveAds || !_adEnabled) return;
            foreach (var network in _adNetworks)
            {
                CoreLogUtils.Debug("init ads sdk network:" + network.ToString());
                network.LoadSplash();
            }
        }

        public bool IsSplashAvailable()
        {
            if (CorePrefs.IsRemoveAds || !_adEnabled) return false;
            return _adNetworks.Any(network => network.IsSplashAvailable());
        }

        /// <returns>是否已向竞价胜出方发起展示（无填充时为 false）。</returns>
        public bool ShowSplash()
        {
            if (CorePrefs.IsRemoveAds || !_adEnabled) return false;
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
            if (!_adEnabled) return -1;
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
            if (!_adEnabled) return -1;
            var results = SelectBestAds(CoreLiteAdsType.Interstitial);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        /// <summary>
        /// 返回当前竞价成功的原生广告价格（eCPM）。仅做竞价查询，不触发展示逻辑。
        /// </summary>
        /// <returns>当前最高出价；无可用出价时返回 -1</returns>
        public double GetCurrentNativeBidPrice()
        {
            if (!_adEnabled) return -1;
            var results = SelectBestAds(CoreLiteAdsType.Native);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        public double GetCurrentNativeFullscreenBidPrice()
        {
            if (!_adEnabled) return -1;
            var results = SelectBestAds(CoreLiteAdsType.NativeFullscreen);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        public double GetCurrentSmallNativeBidPrice()
        {
            if (!_adEnabled) return -1;
            var results = SelectBestAds(CoreLiteAdsType.NativeSmall);
            return results.Count > 0 ? results[0].Revenue : -1;
        }

        #endregion

        #region  Others

        /// <summary>
        /// 获取插屏广告信息（adId, adSource, adEcpm），用于打点上报
        /// </summary>
        private (string adId, string adSource, double adEcpm) GetInterstitialAdInfo()
        {
            string adId = "";
            string adSource = "";
            double adEcpm = 0;

            string bestPlacement = CoreTpnInter.Instance.GetHighestPayingPlacement();
            if (!string.IsNullOrEmpty(bestPlacement))
            {
                try
                {
                    var statusJson = ATInterstitialAd.Instance.checkAdStatus(bestPlacement);
                    var status = JsonUtility.FromJson<CoreLiteTpnAdStatus>(statusJson);
                    if (status?.adInfo != null)
                    {
                        adId = status.adInfo.network_placement_id;
                        adSource = "Topon_" + status.adInfo.network_firm_id.ToString();
                        adEcpm = status.adInfo.publisher_revenue * 1000;
                    }
                }
                catch { }
            }

            return (adId, adSource, adEcpm);
        }

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

        // ---- 新增：加载方法（供外部调用） ----
        public void LoadInterstitial()
        {
            if (!_adEnabled || CorePrefs.IsRemoveAds) return;
            foreach (var network in _adNetworks)
                network.LoadInterstitial();
        }
        public void LoadRewarded()
        {
            if (!_adEnabled || CorePrefs.IsRemoveAds) return;
            foreach (var network in _adNetworks)
                network.LoadRewarded();
        }

        #endregion
    }
}