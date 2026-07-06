using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdjustSdk;
#if FIRBASE
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;
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
            // 注册 Messaging Token 回调（注意：这里注册了一次，Activate成功后又会注册一次，建议只保留一处，但这里先不动）
            FirebaseMessaging.TokenReceived += OnTokenReceived;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                // 1. 依赖检查失败
                if (task.IsFaulted)
                {
                    Debug.LogError($"[Firebase] 依赖检查异常: {task.Exception}");
                    return;
                }

                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[Firebase] 依赖不可用: {task.Result}");
                    return;
                }

                // 依赖检查通过，开始拉取远程配置
                FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero)
                    .ContinueWithOnMainThread(fetchTask =>
                    {
                        // 2. Fetch 任务异常（网络错误、超时等）
                        if (fetchTask.IsFaulted)
                        {
                            Debug.LogError($"[Firebase] 远程配置 Fetch 异常 可能时未挂载VPN : {fetchTask.Exception}");
                            return;
                        }

                        var info = FirebaseRemoteConfig.DefaultInstance.Info;
                        // 3. Fetch 完成但状态不是 Success
                        if (info.LastFetchStatus != LastFetchStatus.Success)
                        {
                            Debug.LogError($"[Firebase] 远程配置 Fetch 未成功, 状态: {info.LastFetchStatus}, 失败原因: {info.LastFetchFailureReason}");
                            // 即使失败，也不继续 Activate，因为没新数据
                            return;
                        }

                        // Fetch 成功，开始激活
                        FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                            .ContinueWithOnMainThread(activateTask =>
                            {
                                // 4. Activate 异常
                                if (activateTask.IsFaulted)
                                {
                                    Debug.LogError($"[Firebase] 远程配置 Activate 异常: {activateTask.Exception}");
                                    return;
                                }

                                // Activate 成功（即使结果为 false 也表示没有新配置被激活，但通常不算错误）
                                bool activated = activateTask.Result;
                                if (!activated)
                                {
                                    Debug.LogWarning("[Firebase] 远程配置 Activate 返回 false（没有新配置被激活）");
                                }
                                Sky.LogTool.DebugLog("[Firebase] 远程配置 成功！ ", Sky.LogTool.Color.purple);
                                // ---- 原有的初始化逻辑（保持不变） ----
                                _isInitialized = true;
                                CoreAdsConfigParser.Parse(GetConfigStr("AdUnits"));
                                FirebaseMessaging.TokenReceived += OnTokenReceived; // 建议只注册一次，这里可删除
#if ADJUST
                        CoreAdjustEvents.UpdateEventToken();
                        ApplyAdjustInitByRemoteConfig();
#endif
                                CoreSdkMgr.OnFirebaseActivated?.Invoke();
                                CoreAnalyticsMgr.TrackEvent("Parameter_Connect",
                                    FirebaseRemoteConfig.DefaultInstance.AllValues.ToDictionary(
                                        kvp => kvp.Key,
                                        kvp => (object)kvp.Value.StringValue.Replace("-", "_")));
                                //Sky.LogTool.Init(GetConfigStr("log_enable", "0").Equals("1"));
                                CoreLogUtils.Enabled = GetConfigStr("log_enable", "0").Equals("1");
                                GroupingSet();
                                CoreBidPlatformMgr.Instance.InitializePlatformStates(
                                    CoreAdsConfigParser.GetAdExpireInSec());
                               
                            });
                    });
            });
#endif
        }

        public string GetConfigStr(string key, string defaultValue = "")
        {
            if (!_isInitialized)
            {
                Sky.LogTool.DebugLog("未初始化"+ key +" :"+ defaultValue,Sky.LogTool.Color.red);
                return defaultValue;
            }
            var value = GetRemoteValue(key);
            Sky.LogTool.DebugLog("GetRemotekey:"+key+" GetRemoteValue:" + value,Sky.LogTool.Color.lightblue);
            if (string.IsNullOrEmpty(value)) return defaultValue;
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
        private void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
            Adjust.SetPushToken(token?.Token); 
        }
#endif

        private void GroupingSet()
        {
            string value = GetConfigStr("Grouping");
            if(string.IsNullOrEmpty(value)) return;
            var eventName = $"Grouping_{value}";
            CoreAnalyticsMgr.TrackEvent(eventName);
        }

        private const string ADJ_INIT_CONFIG_KEY = "adj_init";
#if ADJUST
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
#endif
    }
}

