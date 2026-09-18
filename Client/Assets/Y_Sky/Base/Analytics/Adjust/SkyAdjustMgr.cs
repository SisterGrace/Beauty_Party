#if ADJUST
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using AdjustSdk;
using ThinkingData.Analytics;
using UnityEngine;

namespace Sky
{
    public class SkyAdjustMgr : SkySingleton<SkyAdjustMgr>
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

            var adjId = SkyConfigMgr.Instance.GetAdjustId();
            if (string.IsNullOrEmpty(adjId))
            {
                SkyLogUtils.Debug("AdjustId is null or empty, please check your config.");
                return;
            }

#if USE_XMSDK
            if (SkyXmSdkMgr.Instance.UseXMSDK)
            {
                SkyXmSdkMgr.Instance.CheckRiskDetection(isNormal =>
                {
                    if (isNormal)
                    {
                        DoInitAdjust(adjId);
                    }
                    else
                    {
                        SkyLogUtils.Debug("XMSDK: Abnormal device detected, skip Adjust init");
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
            Adjust.AddGlobalCallbackParameter("Sky_version", SkySdkMgr.GetSDKVersion());
            Adjust.AddGlobalPartnerParameter("ta_distinct_id", TDAnalytics.GetDistinctId());
            Adjust.AddGlobalPartnerParameter("Sky_version", SkySdkMgr.GetSDKVersion());
            _environment = SkyFirebaseMgr.Instance.GetConfigStr("log_enable","0").Equals("1") ? AdjustEnvironment.Sandbox : AdjustEnvironment.Production;
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
            if (!string.IsNullOrEmpty(SkyAdjustPrefs.AdjustNetwork))
            {
                bool isPaid = !IsOrganic(SkyAdjustPrefs.AdjustNetwork);
#if USE_XMSDK
                SkyXmSdkMgr.Instance.SetPaidUser(isPaid);
#endif
                SkySdkMgr.Instance.OnUserAttributionReceived?.Invoke(isPaid, SkyAdjustPrefs.AdjustNetwork, SkyAdjustPrefs.AdjustClickLabel);
            }
            SkyAnalyticsMgr.TrackEvent("Adjust_Start");
            SkyLogUtils.Debug("Adjust init success");
            SkyAdjustPrefs.HasInitialized = true;
            SkyAdjustEvents.FlushCachedAdImpressionEvents();
        }

        private void SessionFailureCallback(AdjustSessionFailure sessionFailureData)
        {
            SkySdkMgr.TrackEvent("Adj_SessionFail", new Dictionary<string, object>()
            {
                { "Message", sessionFailureData.Message},
                { "Timestamp", sessionFailureData.Timestamp},
                { "Adid", sessionFailureData.Adid},
            });
        }

        private void SessionSuccessCallback (AdjustSessionSuccess sessionSuccessData)
        {
            SkySdkMgr.TrackEvent("Adj_SessionSuccess", new Dictionary<string, object>()
            {
                { "Message", sessionSuccessData.Message},
                { "Timestamp", sessionSuccessData.Timestamp},
                { "Adid", sessionSuccessData.Adid},
            });
        }

        private void RegistDelegate(AdjustAttribution att)
        {
            SkyLogUtils.Debug("adjust callback:"+att.Network+" adgroup"+att.Adgroup+" creative"+att.Creative+" clicklabel"+att.ClickLabel);
            SetAttributionsData(att);

            SkySdkMgr.TrackEvent("Adj_Receive_Attribution",new Dictionary<string, object>()
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
            int value = SkyFirebaseMgr.Instance.GetConfigInt(REMOTE_CONFIG_KEY_MAX_POLLING_TIME, DEFAULT_MAX_POLLING_TIME);
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

            SkyThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> 
            { 
                { "user_ad_network", SkyAdjustPrefs.AdjustNetwork }, 
                { "user_campaign", SkyAdjustPrefs.AdjustCampaign },
                { "user_adgroup", SkyAdjustPrefs.AdjustAdgroup }, 
                { "user_creative", SkyAdjustPrefs.AdjustCreative }, 
                { "user_clicklabel", SkyAdjustPrefs.AdjustClickLabel } 
            },true);

            SkyThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> 
            { 
                { "source_network", SkyAdjustPrefs.AdjustNetwork }, 
                { "source_campaign", SkyAdjustPrefs.AdjustCampaign },
                { "source_adgroup", SkyAdjustPrefs.AdjustAdgroup }, 
                { "source_creative", SkyAdjustPrefs.AdjustCreative }, 
                { "source_clicklabel", SkyAdjustPrefs.AdjustClickLabel } 

            });

            var para = new Dictionary<string, object> {  { "Pass_Time", Mathf.CeilToInt(Time.time - _initTime) } };
            SkyAnalyticsMgr.TrackEvent("Adjust_Time", para);
            SkyAnalyticsMgr.TrackEvent(string.Format("Adjust_Time_{0}", SkyAdjustPrefs.AdjustNetwork), para);
            SkyAnalyticsMgr.TrackEvent("Adjust_Get_Succ", para);
            
            bool isPaidUser = !IsOrganic(SkyAdjustPrefs.AdjustNetwork);
#if USE_XMSDK
            SkyXmSdkMgr.Instance.SetPaidUser(isPaidUser);
#endif
            SkySdkMgr.Instance.OnUserAttributionReceived?.Invoke(isPaidUser, SkyAdjustPrefs.AdjustNetwork, SkyAdjustPrefs.AdjustClickLabel);
        }

        private void SetAttributionsData(AdjustAttribution attribution)
        {
            var network = attribution?.Network;
            if(string.IsNullOrEmpty(network)) network = "Organic";
            SkyAdjustPrefs.AdjustNetwork = network;
            SkyAdjustPrefs.AdjustCampaign = attribution?.Campaign;
            SkyAdjustPrefs.AdjustAdgroup = attribution?.Adgroup;
            SkyAdjustPrefs.AdjustCreative = attribution?.Creative;
            SkyAdjustPrefs.AdjustClickLabel = attribution?.ClickLabel;
        }

        /// <summary>
        /// 获取gaid,idfa,idfv,adid
        /// </summary>
        public void LoadIds()
        {

            Adjust.GetAdid(adid =>
            {
                SkyAdjustPrefs.Adid = adid;
                SkyThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "adid", adid } });
                SkyThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "adid", adid } });
            });

            Adjust.GetIdfa(idfa =>
            {
                SkyLogUtils.Debug("adjust get ios idfa:"+idfa);
                SkyAdjustPrefs.IDFA = idfa;
                SkyThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "idfa", idfa } });
                SkyThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "idfa", idfa } });
            });

            Adjust.GetGoogleAdId(gaid =>
            {
                SkyAdjustPrefs.Gaid = gaid;
                SkyThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "gaid", gaid } });
                SkyThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "gaid", gaid } });
            });

            Adjust.GetIdfv(idfv =>
            {
                SkyLogUtils.Debug("adjust get ios idfv:"+idfv);
                SkyAdjustPrefs.IDFV = idfv;
                SkyThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "idfv", idfv } });
                SkyThinkingDataMgr.Instance.UserSet(new Dictionary<string, object> { { "idfv", idfv } });
            });
        }
    }
}

#endif