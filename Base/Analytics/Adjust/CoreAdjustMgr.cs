#if ADJUST
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using AdjustSdk;
using ThinkingData.Analytics;
using UnityEngine;

namespace CoreLite
{
    public class CoreAdjustMgr : CoreSingleton<CoreAdjustMgr>
    {
        private AdjustEnvironment _environment = AdjustEnvironment.Production;
        private AdjustLogLevel _logLevel = AdjustLogLevel.Verbose;
        private const int DEFAULT_MAX_POLLING_TIME = 180;
        private const string REMOTE_CONFIG_KEY_MAX_POLLING_TIME = "adjust_max_polling_time";
        private const int POLLING_INTERVAL = 1; 
        private string _currentCallbackNetwork;
        private bool _isPollingStarted = false;
        private int _callbackTimes = 0;
        private float _initTime = 0;
        private bool _hasInited = false;

        public void Init()
        {
            if (_hasInited) return;
            _hasInited = true;

            var adjId = CoreConfigMgr.Instance.GetAdjustId();
            if (string.IsNullOrEmpty(adjId))
            {
                CoreLogUtils.Debug("AdjustId is null or empty, please check your config.");
                return;
            }

#if USE_XMSDK
            if (CoreXmSdkMgr.Instance.UseXMSDK)
            {
                CoreXmSdkMgr.Instance.CheckRiskDetection(isNormal =>
                {
                    if (isNormal)
                    {
                        DoInitAdjust(adjId);
                    }
                    else
                    {
                        CoreLogUtils.Debug("XMSDK: Abnormal device detected, skip Adjust init");
                    }
                });
                return;
            }
#endif
            DoInitAdjust(adjId);
        }

        private void DoInitAdjust(string adjId)
        {
            _initTime = Time.time;

            Adjust.AddGlobalCallbackParameter("ta_distinct_id", TDAnalytics.GetDistinctId());
            Adjust.AddGlobalCallbackParameter("CoreLite_version", CoreSdkMgr.GetSDKVersion());
            Adjust.AddGlobalPartnerParameter("ta_distinct_id", TDAnalytics.GetDistinctId());
            Adjust.AddGlobalPartnerParameter("CoreLite_version", CoreSdkMgr.GetSDKVersion());
            _environment = CoreFirebaseMgr.Instance.GetConfigStr("log_enable","0").Equals("1") ? AdjustEnvironment.Sandbox : AdjustEnvironment.Production;
            AdjustConfig config = new AdjustConfig(adjId, _environment)
            {
                AttributionChangedDelegate = RegistDelegate,
                LogLevel = _logLevel,
                SessionFailureDelegate = SessionFailureCallback,
                SessionSuccessDelegate = SessionSuccessCallback
            };
            Adjust.InitSdk(config);
#if UNITY_ANDROID
            LoadIds();
#endif
            if (!string.IsNullOrEmpty(CoreAdjustPrefs.AdjustNetwork))
            {
                bool isPaid = !IsOrganic(CoreAdjustPrefs.AdjustNetwork);
#if USE_XMSDK
                CoreXmSdkMgr.Instance.SetPaidUser(isPaid);
#endif
                CoreSdkMgr.Instance.OnUserAttributionReceived?.Invoke(isPaid, CoreAdjustPrefs.AdjustNetwork, CoreAdjustPrefs.AdjustClickLabel);
            }
            CoreAnalyticsMgr.TrackEvent("Adjust_Start");
            CoreLogUtils.Debug("Adjust init success");
            CoreAdjustPrefs.HasInitialized = true;
            CoreAdjustEvents.FlushCachedAdImpressionEvents();
        }

        private void SessionFailureCallback(AdjustSessionFailure sessionFailureData)
        {
            CoreSdkMgr.TrackEvent("Adj_SessionFail", new Dictionary<string, object>()
            {
                { "Message", sessionFailureData.Message},
                { "Timestamp", sessionFailureData.Timestamp},
                { "Adid", sessionFailureData.Adid},
            });
        }

        private void SessionSuccessCallback (AdjustSessionSuccess sessionSuccessData)
        {
            CoreSdkMgr.TrackEvent("Adj_SessionSuccess", new Dictionary<string, object>()
            {
                { "Message", sessionSuccessData.Message},
                { "Timestamp", sessionSuccessData.Timestamp},
                { "Adid", sessionSuccessData.Adid},
            });
        }

        private void RegistDelegate(AdjustAttribution att)
        {
            CoreLogUtils.Debug("adjust callback:"+att.Network+" adgroup"+att.Adgroup+" creative"+att.Creative+" clicklabel"+att.ClickLabel);
            SetAttributionsData(att);

            CoreSdkMgr.TrackEvent("Adj_Receive_Attribution",new Dictionary<string, object>()
            {
                { "adgroup", att.Adgroup?? "" },
                { "creative", att.Creative?? "" },
                { "clicklabel", att.ClickLabel?? "" },
                { "network", att.Network?? "" },
                { "campaign", att.Campaign?? "" }
            });

            if (_isPollingStarted) return;
            _isPollingStarted = true;
            _currentCallbackNetwork = att.Network;
            if (IsOrganic(att.Network) == false)
            {
                OnAttributionFinish(att);
                return;
            }
            InvokeRepeating(nameof(CallGetAttributionAsync), 0, POLLING_INTERVAL);
        }

        /// <summary>
        /// 从 Firebase 在线参数 adjust_max_polling_time 获取归因轮询最大时长（秒），未配置或未就绪时使用默认值 180。
        /// </summary>
        private int GetMaxPollingTime()
        {
            int value = CoreFirebaseMgr.Instance.GetConfigInt(REMOTE_CONFIG_KEY_MAX_POLLING_TIME, DEFAULT_MAX_POLLING_TIME);
            return value > 0 ? value : DEFAULT_MAX_POLLING_TIME;
        }

        private bool IsOrganic(string _network)
        {
            if (string.IsNullOrEmpty(_network) || _network.Equals("Organic") || _network.Equals("Untrusted Devices") || _network.Equals("Google Organic Search"))
            {
                return true;
            }
            return false;
        }

        private async Task CallGetAttributionAsync()
        {
            _callbackTimes++;
            AdjustAttribution att = await GetAttributionAsync();

            if (att != null)       
            {
                if (att.Network.Equals(_currentCallbackNetwork) == false) 
                {
                    CancelInvoke(nameof(CallGetAttributionAsync));
                    OnAttributionFinish(att);
                }
            }

            if (_callbackTimes >= GetMaxPollingTime())
            {
                if (att != null)
                {
                    OnAttributionFinish(att);
                }
                else
                {
                    OnAttributionFinish(null);
                }
            }
        }

        public static Task<AdjustAttribution> GetAttributionAsync()
        {
            var tcs = new TaskCompletionSource<AdjustAttribution>();

            Adjust.GetAttribution(attribution =>
            {
                try
                {
                    tcs.TrySetResult(attribution);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(null);
                }
            });

            return tcs.Task;
        }

        private void OnAttributionFinish(AdjustAttribution attribution)
        {
            CancelInvoke(nameof(CallGetAttributionAsync));
            SetAttributionsData(attribution);

            CoreThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> 
            { 
                { "user_ad_network", CoreAdjustPrefs.AdjustNetwork }, 
                { "user_campaign", CoreAdjustPrefs.AdjustCampaign },
                { "user_adgroup", CoreAdjustPrefs.AdjustAdgroup }, 
                { "user_creative", CoreAdjustPrefs.AdjustCreative }, 
                { "user_clicklabel", CoreAdjustPrefs.AdjustClickLabel } 
            },true);

            CoreThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> 
            { 
                { "source_network", CoreAdjustPrefs.AdjustNetwork }, 
                { "source_campaign", CoreAdjustPrefs.AdjustCampaign },
                { "source_adgroup", CoreAdjustPrefs.AdjustAdgroup }, 
                { "source_creative", CoreAdjustPrefs.AdjustCreative }, 
                { "source_clicklabel", CoreAdjustPrefs.AdjustClickLabel } 

            });

            var para = new Dictionary<string, object> {  { "Pass_Time", Mathf.CeilToInt(Time.time - _initTime) } };
            CoreAnalyticsMgr.TrackEvent("Adjust_Time", para);
            CoreAnalyticsMgr.TrackEvent(string.Format("Adjust_Time_{0}", CoreAdjustPrefs.AdjustNetwork), para);
            CoreAnalyticsMgr.TrackEvent("Adjust_Get_Succ", para);
            
            bool isPaidUser = !IsOrganic(CoreAdjustPrefs.AdjustNetwork);
#if USE_XMSDK
            CoreXmSdkMgr.Instance.SetPaidUser(isPaidUser);
#endif
            CoreSdkMgr.Instance.OnUserAttributionReceived?.Invoke(isPaidUser, CoreAdjustPrefs.AdjustNetwork, CoreAdjustPrefs.AdjustClickLabel);
        }

        private void SetAttributionsData(AdjustAttribution attribution)
        {
            var network = attribution?.Network;
            if(string.IsNullOrEmpty(network)) network = "Organic";
            CoreAdjustPrefs.AdjustNetwork = network;
            CoreAdjustPrefs.AdjustCampaign = attribution?.Campaign;
            CoreAdjustPrefs.AdjustAdgroup = attribution?.Adgroup;
            CoreAdjustPrefs.AdjustCreative = attribution?.Creative;
            CoreAdjustPrefs.AdjustClickLabel = attribution?.ClickLabel;
        }

        /// <summary>
        /// 获取gaid,idfa,idfv,adid
        /// </summary>
        public void LoadIds()
        {

            Adjust.GetAdid(adid =>
            {
                CoreAdjustPrefs.Adid = adid;
                CoreThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "adid", adid } });
                CoreThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "adid", adid } });
            });

            Adjust.GetIdfa(idfa =>
            {
                CoreLogUtils.Debug("adjust get ios idfa:"+idfa);
                CoreAdjustPrefs.IDFA = idfa;
                CoreThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "idfa", idfa } });
                CoreThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "idfa", idfa } });
            });

            Adjust.GetGoogleAdId(gaid =>
            {
                CoreAdjustPrefs.Gaid = gaid;
                CoreThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "gaid", gaid } });
                CoreThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "gaid", gaid } });
            });

            Adjust.GetIdfv(idfv =>
            {
                CoreLogUtils.Debug("adjust get ios idfv:"+idfv);
                CoreAdjustPrefs.IDFV = idfv;
                CoreThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "idfv", idfv } });
                CoreThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "idfv", idfv } });
            });
        }
    }
}

#endif