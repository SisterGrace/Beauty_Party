using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public class SkySplashAdsMgr : SkySingleton<SkySplashAdsMgr>
    {
        public float _backgroundTime = 0;
        private int _timeoutDuration = 5;
        public Action OnSplashAdClose;
        private Coroutine _hotResumeCheckCo;
        /// <summary>激励/插屏等全屏关闭后，下一次 <see cref="CheckSplashAd"/>（热开）应忽略：避免系统随全屏广告触发的 OnApplicationPause(false) 被误判为从后台恢复。</summary>
        private bool _skipNextHotSplashCheck;

        /// <summary>热开已向聚合发起展示、尚未收到全屏关闭回调时为 true，用于防止同轮生命周期内二次热开。</summary>
        private bool _hotSplashPresenting;
        /// <summary>冷启动链路已发起开屏展示，待 SDK 关闭后再触发 <see cref="OnSplashAdClose"/>；热开关闭不得重复触发该回调。</summary>
        private bool _awaitingColdSplashDismiss;

        /// <summary>当前开屏触发的启动类型，供各平台广告实现层读取以携带到埋点参数中。</summary>
        public string CurrentLaunchType { get; private set; } = "cold";

        #region 冷启动开屏事件延迟上报

        private struct PendingSplashEvent
        {
            public string EventName;
            public Dictionary<string, object> Properties;
        }

        private static readonly List<PendingSplashEvent> _pendingSplashEvents = new List<PendingSplashEvent>();
        private bool _coldSplashEventsPending;
        private const float FLUSH_EXTRA_DELAY = 1f;
        private const float FLUSH_MAX_WAIT = 10f;

        /// <summary>
        /// 开屏事件统一入口：冷启动阶段缓存事件，等待数数 SDK 就绪后延迟刷出；热启动直接上报。
        /// </summary>
        public static void TrackSplashEvent(string eventName, Dictionary<string, object> dict = null)
        {
            if (Instance != null && Instance._coldSplashEventsPending)
            {
                _pendingSplashEvents.Add(new PendingSplashEvent
                {
                    EventName = eventName,
                    Properties = dict != null ? new Dictionary<string, object>(dict) : null
                });
                SkyLogUtils.Debug($"[Splash] event cached: {eventName}");
                return;
            }

            if (dict != null)
                SkyAnalyticsMgr.TrackEvent(eventName, dict);
            else
                SkyAnalyticsMgr.TrackEvent(eventName);
        }

        private IEnumerator FlushColdStartSplashEvents()
        {

            float waited = 0f;
            #if SSThinkingData
            while (!SkyThinkingDataMgr.Instance.IsReady && waited < FLUSH_MAX_WAIT)
            {
                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }
#endif
            yield return new WaitForSeconds(FLUSH_EXTRA_DELAY);

            _coldSplashEventsPending = false;
            SkyLogUtils.Debug($"[Splash] flushing {_pendingSplashEvents.Count} cold start events (waited {waited + FLUSH_EXTRA_DELAY:F1}s)");
            foreach (var evt in _pendingSplashEvents)
            {
                if (evt.Properties != null)
                    SkyAnalyticsMgr.TrackEvent(evt.EventName, evt.Properties);
                else
                    SkyAnalyticsMgr.TrackEvent(evt.EventName);
            }
            _pendingSplashEvents.Clear();
        }

#endregion

        public void SetSplash()
        {
            _timeoutDuration = SkyPrefs.GetInt("Firstcold_Splash_Switch", 0) == 0 ? 0 : GetSplashConfig().loadtime;
            StartCoroutine(LoadSplashAdAdWithTimeout());
            SkyLogUtils.Debug(" [AppOpen]  加载开屏："+_timeoutDuration);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _backgroundTime = Time.realtimeSinceStartup;
                _skipNextHotSplashCheck = false;
                return;
            }

            if (!_hotSplashPresenting
                && SkyAdsMgr.Instance.IsSplashAvailable() && !IsPaymentFlowBlockingHotSplash())
            {
                if (_hotResumeCheckCo != null)
                    StopCoroutine(_hotResumeCheckCo);
                _hotResumeCheckCo = StartCoroutine(HotResumeCheckAfterDelay());
            }
            else
                _backgroundTime = Time.realtimeSinceStartup;
        }

        private IEnumerator LoadSplashAdAdWithTimeout()
        {

            SkyAdsMgr.Instance.LoadSplash();
            float elapsedTime = 0f;
            while (elapsedTime < _timeoutDuration && !SkyAdsMgr.Instance.IsSplashAvailable())
            {
                yield return new WaitForSeconds(0.5f);
                elapsedTime += 0.5f;
            }

            CheckSplashAd(true);
        }

        private IEnumerator HotResumeCheckAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            _hotResumeCheckCo = null;
            CheckSplashAd(false);
        }

        /// <summary>由 <see cref="SkyAdsMgr"/> 在激励/插屏全屏结束时调用，防止随后误判热开。</summary>
        public void SkipNextHotResumeSplashCheck()
        {
            _skipNextHotSplashCheck = true;
        }

        private void CheckSplashAd(bool isCold)
        {
            CurrentLaunchType = isCold ? "cold" : "hot";
            SkyLogUtils.Debug("Admob AppOpen checksplash:" + isCold);

            if (isCold)
            {
                _coldSplashEventsPending = true;
                StartCoroutine(FlushColdStartSplashEvents());
            }

            // SP_Position 打点：触发开屏展示时机
            TrackSplashEvent("SP_Position", new Dictionary<string, object>
            {

            });
           
            if (!isCold)
            {
                if (_skipNextHotSplashCheck)
                {
                    _skipNextHotSplashCheck = false;
                    SkyLogUtils.Debug(" [AppOpen] skip hot splash (after IV/RV or other fullscreen ad)");
                    TrackSplashSkip("skip_after_fullscreen");
                    return;
                }
                if (SkyAdsMgr.Instance._adActive)
                {
                    TrackSplashSkip("ad_active");
                    return;
                }
                if (_hotSplashPresenting)
                {
                    TrackSplashSkip("already_presenting");
                    return;
                }
                if (IsPaymentFlowBlockingHotSplash())
                {
                    TrackSplashSkip("payment_blocking");
                    return;
                }
                var config = GetSplashConfig();
                if (config.hot_splash_switch != 1)
                {
                    TrackSplashSkip("config_disabled");
                    return;
                }
                if (Time.realtimeSinceStartup - _backgroundTime < config.hot_timegap)
                {
                    TrackSplashSkip("time_gap_insufficient");
                    return;
                }
                if (SkyAdsMgr.Instance.IsSplashAvailable())
                {
                    _hotSplashPresenting = true;
                    if (!SkyAdsMgr.Instance.ShowSplash())
                        _hotSplashPresenting = false;
                }
                else
                {
                        TrackSplashEvent("SP_ShowFail_Not_Ready", new Dictionary<string, object>
                        {
                            //{ "launch_type", CurrentLaunchType }  // 多余参数，已注释
                            // 【无法获取】广告没有填充，IsSplashAvailable() 返回 false，无法使用 checkAdStatus
                        });
                        
                        // SP_ShowFail 通用打点
                        TrackSplashEvent("SP_ShowFail", new Dictionary<string, object>
                        {
                            // 没有填充时拿不到 ad_platform/ad_id/ad_ecpm
                        });
                    SkyAdsMgr.Instance.LoadSplash();
                }   
            }
            else
            {
                var config = GetSplashConfig();
                bool isNewUser = SkyPrefs.GetInt("Firstcold_Splash_Switch", 0) == 0;
                if (isNewUser)
                {
                    SkyPrefs.SetInt("Firstcold_Splash_Switch", 1);
                    if (config.firstcold_splash_switch == 1)
                    {
                        if (SkyAdsMgr.Instance.IsSplashAvailable())
                        {
                            _awaitingColdSplashDismiss = true;
                            if (!SkyAdsMgr.Instance.ShowSplash())
                            {
                                _awaitingColdSplashDismiss = false;
                                OnSplashAdClose?.Invoke();
                            }
                        }
                        else
                        {
                            TrackSplashEvent("SP_ShowFail_Not_Ready", new Dictionary<string, object>
                            {
                                //{ "launch_type", CurrentLaunchType }  // 多余参数，已注释
                                // 【无法获取】广告没有填充，IsSplashAvailable() 返回 false，无法使用 checkAdStatus
                            });
                            
                            // SP_ShowFail 通用打点
                            TrackSplashEvent("SP_ShowFail", new Dictionary<string, object>
                            {
                                // 没有填充时拿不到 ad_platform/ad_id/ad_ecpm
                            });
                            
                            OnSplashAdClose?.Invoke();
                        }
                    }
                    else
                    {
                        TrackSplashSkip("config_disabled");
                        OnSplashAdClose?.Invoke();
                    }
                }
                else
                {
                    if (config.cold_splash_switch == 1)
                    {
                        if (SkyAdsMgr.Instance.IsSplashAvailable())
                        {
                            _awaitingColdSplashDismiss = true;
                            if (!SkyAdsMgr.Instance.ShowSplash())
                            {
                                _awaitingColdSplashDismiss = false;
                                OnSplashAdClose?.Invoke();
                            }
                        }    
                        else
                        {
                            TrackSplashEvent("SP_ShowFail_Not_Ready", new Dictionary<string, object>
                            {
                                //{ "launch_type", CurrentLaunchType }  // 多余参数，已注释
                                // 【无法获取】广告没有填充，IsSplashAvailable() 返回 false，无法使用 checkAdStatus
                            });
                            
                            // SP_ShowFail 通用打点
                            TrackSplashEvent("SP_ShowFail", new Dictionary<string, object>
                            {
                                // 没有填充时拿不到 ad_platform/ad_id/ad_ecpm
                            });
                            
                            OnSplashAdClose?.Invoke();
                        }  
                    }
                    else
                    {
                        TrackSplashSkip("config_disabled");
                        OnSplashAdClose?.Invoke();
                    }
                }
            }
        }

        private void TrackSplashSkip(string reason)
        {
            TrackSplashEvent("SP_ShowSkip", new Dictionary<string, object>
            {
                //{ "launch_type", CurrentLaunchType },
                { "reason", reason }
            });
        }

        /// <summary>开屏全屏结束（成功关闭或打开失败）时由广告实现层调用：仅在热开展示链路中刷新 <see cref="_backgroundTime"/> 并清除展示中状态。</summary>
        public void OnAppOpenSplashFullscreenClosed()
        {
            if (!_hotSplashPresenting) return;
            _hotSplashPresenting = false;
            _backgroundTime = Time.realtimeSinceStartup;
        }

        /// <summary>各平台 App Open 关闭或全屏打开失败时统一入口：处理热开状态，且仅在冷启动开屏场景下触发 <see cref="OnSplashAdClose"/>。</summary>
        public void NotifyAppOpenAdDismissedFromSdk()
        {
            OnAppOpenSplashFullscreenClosed();
            _skipNextHotSplashCheck = true;
            _backgroundTime = Time.realtimeSinceStartup;
            if (!_awaitingColdSplashDismiss) return;
            _awaitingColdSplashDismiss = false;
            OnSplashAdClose?.Invoke();
        }

        /// <summary>H5 跳转 / 订单轮询（purchasePending）或原生商店支付中会阻断热开；支付结束后会恢复。</summary>
        private static bool IsPaymentFlowBlockingHotSplash()
        {

#if IAP_ENABLE
            return SkyIapH5Mgr.Instance.purchasePending || SkyIapNativeMgr.Instance.IsPurchaseInProgress;
#else
            return false;
#endif
        }

        private static string SPLASH_CONFIG_KEY = "Splash_AD_RULES";
        private static string SPLASH_CONFIG_DEFAULT = "[{\"firstcold_splash_switch\":0,\"cold_splash_switch\":1,\"hot_splash_switch\":1,\"hot_timegap\":30,\"loadtime\":5}]";

        public SplashConfig GetSplashConfig()
        {
            string json = SkyAnalyticsMgr.GetConfigStr(SPLASH_CONFIG_KEY, SPLASH_CONFIG_DEFAULT);
            try
            {
                var configs = SkyDataUtils.FromJsonArray<SplashConfig>(json);
                return configs?.Length > 0 ? configs[0] : new SplashConfig 
                { 
                    firstcold_splash_switch = 0, 
                    cold_splash_switch = 1, 
                    hot_splash_switch = 1, 
                    hot_timegap = 30, 
                    loadtime = 15 
                };
            }
            catch (Exception)
            {
                return new SplashConfig 
                { 
                    firstcold_splash_switch = 0, 
                    cold_splash_switch = 1, 
                    hot_splash_switch = 1, 
                    hot_timegap = 30, 
                    loadtime = 15 
                };
            }
            
        }

        [System.Serializable]
        public class SplashConfig
        {
            public int firstcold_splash_switch;
            public int cold_splash_switch;
            public int hot_splash_switch;
            public int hot_timegap;
            public int loadtime;
        }
    }

}

