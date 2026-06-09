using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdjustSdk;
#if FIRBASE
using Firebase;
using Firebase.Extensions;
//using Firebase.Messaging;
using Firebase.RemoteConfig;
#endif
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CoreLite
{
    public class CoreFirebaseMgr : CoreSingleton<CoreFirebaseMgr>
    {
        private bool _isInitialized = false;

        public void Init()
        {
            #if FIRBASE
            //FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask =>
                    {
                        if (fetchTask.IsCompleted && 
                            FirebaseRemoteConfig.DefaultInstance.Info.LastFetchStatus == LastFetchStatus.Success)
                        {
                            FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(task =>
                            {
                                _isInitialized = true;
                                CoreAdsConfigParser.Parse(GetConfigStr("AdUnits"));
                                //FirebaseMessaging.TokenReceived += OnTokenReceived;
                                CoreAdjustEvents.UpdateEventToken();
                                ApplyAdjustInitByRemoteConfig();
                                CoreSdkMgr.OnFirebaseActivated?.Invoke();
                                CoreAnalyticsMgr.TrackEvent("Parameter_Connect", FirebaseRemoteConfig.DefaultInstance.AllValues.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.StringValue.Replace("-", "_")));
                                CoreLogUtils.Enabled = GetConfigStr("log_enable","0").Equals("1");
                                GroupingSet();
                                // TPN/MTG 已在 SDK 初始化时基于本地参数 init，这里仅用远程参数刷新过期判定。
                                CoreBidPlatformMgr.Instance.InitializePlatformStates(CoreAdsConfigParser.GetAdExpireInSec());
                            });
                            
                        }
                });
                }
            });
#endif
        }

        public string GetConfigStr(string key, string defaultValue = "")
        {
            if(!_isInitialized) return defaultValue;
            var value = GetRemoteValue(key);
            if(string.IsNullOrEmpty(value)) return defaultValue;
            return value;
        }
            
        public int GetConfigInt(string key, int defaultValue = 0) => 
            _isInitialized && int.TryParse(GetRemoteValue(key), out int value) ? value : defaultValue;
#if FIRBASE
        public bool GetConfigBool(string key, bool defaultValue = false) => 
            _isInitialized ? FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue : defaultValue;

        private string GetRemoteValue(string key) => 
            FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
#else
        public bool GetConfigBool(string key, bool defaultValue = false)
        {
            return false;
        }
        private string GetRemoteValue(string key)
        {
            return null;
        }
#endif
        public bool TrackingEnable()
        {
            return _isInitialized;
        }
#if FIRBASE
       /* private void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
            Adjust.SetPushToken(token?.Token);
        }*/
#endif

        private void GroupingSet()
        {
            string value = GetConfigStr("Grouping");
            if(string.IsNullOrEmpty(value)) return;
            var eventName = $"Grouping_{value}";
            CoreAnalyticsMgr.TrackEvent(eventName);
        }

        private const string ADJ_INIT_CONFIG_KEY = "adj_init";

        /// <summary>
        /// 按在线参数 adj_init（JSON 字符串，含 adj_init、adj_event_token）决定 Adjust 初始化时机。
        /// 若上次启动已执行过 Init，则本次直接初始化；否则 adj_init=0 或 adj_event_token 为空时启动初始化，adj_init=1 且 token 配置时等事件上报再初始化。
        /// </summary>
        private void ApplyAdjustInitByRemoteConfig()
        {
            if (CoreAdjustPrefs.HasInitialized)
            {
                CoreAdjustMgr.Instance.Init();
                return;
            }
            int adjInit = 0;
            string adjEventToken = "";
            string configJson = GetConfigStr(ADJ_INIT_CONFIG_KEY, "");
            if (!string.IsNullOrEmpty(configJson))
            {
                try
                {
                    var jobj = JObject.Parse(configJson);
                    adjInit = jobj["adj_init"]?.Value<int>() ?? 0;
                    adjEventToken = jobj["adj_event_token"]?.Value<string>() ?? "";
                }
                catch
                {
                    adjInit = 0;
                    adjEventToken = "";
                }
            }
            CoreLogUtils.Debug($"adjInit: {adjInit}, adjEventToken: {adjEventToken}");
            if (adjInit == 0 || string.IsNullOrEmpty(adjEventToken))
            {
                CoreAdjustMgr.Instance.Init();
                return;
            }
            if (adjInit == 1 && CoreAdjustEvents.IsTokenConfigured(adjEventToken))
            {
                CoreAdjustEvents.SetDeferredInitToken(adjEventToken);
                return;
            }
            CoreAdjustMgr.Instance.Init();
        }

    }
}

