using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ThinkingData.Analytics;
using UnityEngine;


namespace CoreLite
{
    public class CoreThinkingDataMgr : CoreSingleton<CoreThinkingDataMgr>
    {
        /// <summary>TDAnalytics.Init 完成并延迟就绪后为 true，表示访客 ID 等用户属性已可用。</summary>
        public bool IsReady { get; private set; }

        public void Init()
        {
            CoreTimeUtils.SetInstallTime();
            if(IsAppIdOrServerUrlEmpty()) return;
            
            TDAnalytics.Init(CoreConfigMgr.Instance.GetSSAppId(), CoreConfigMgr.Instance.GetSSUrl());
            TDAnalytics.EnableAutoTrack(TDAutoTrackEventType.AppInstall | TDAutoTrackEventType.AppStart | TDAutoTrackEventType.AppEnd);
            TDAnalytics.SetSuperProperties(new Dictionary<string, object>{["CoreLite_version"] = CoreSdkMgr.GetSDKVersion()});

            SetProperties();

            StartCoroutine(MarkReadyAfterDelay());
        }

        private IEnumerator MarkReadyAfterDelay()
        {
            yield return new WaitForSeconds(1.5f);
            IsReady = true;
            CoreLogUtils.Debug("[ThinkingData] SDK ready");
        }

        private void SetProperties()
        {
            if(IsAppIdOrServerUrlEmpty()) return;
            UserSet(new Dictionary<string, object>
            {
                { "user_register_time", DateTime.Now },
                { "user_register_timestamp", CoreTimeUtils.CurrentTimestamp() }
            },true);

            UserSet(new Dictionary<string, object>
            {
                { "user_login_day", CoreTimeUtils.GetIntervalDays()}
            });

            SetSuperProperties(new Dictionary<string, object>
            { 
#if UNITY_IOS
                { "platform_channel", "ios" },
#else
                { "platform_channel", "gp" },
#endif            
            });
        }

        public void SetLoginAccount(string account)
        {
            if(IsAppIdOrServerUrlEmpty()) return;
            TDAnalytics.Login(account);
        }

        public void Logout()
        {
            if(IsAppIdOrServerUrlEmpty()) return;
            TDAnalytics.Logout();
        }

        public void UserSet(Dictionary<string, object> dic,bool setOnce = false)
        {
            if(IsAppIdOrServerUrlEmpty()) return;
            if (setOnce)
            {
                TDAnalytics.UserSetOnce(dic);
            }
            else
            { 
                TDAnalytics.UserSet(dic);
            }
        }

        public void SetSuperProperties(Dictionary<string, object> superProperties)
        {
            if(IsAppIdOrServerUrlEmpty()) return;
            TDAnalytics.SetSuperProperties(superProperties);
        }

        public static bool IsAppIdOrServerUrlEmpty()
        {
            var appid = CoreConfigMgr.Instance.GetSSAppId();
            var serverUrl = CoreConfigMgr.Instance.GetSSUrl();
            if(string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(serverUrl))
            {
                CoreLogUtils.Error("ss appid or serverUrl is null or empty, please check your config file.");
                return true;
            }
            return false;
        }
    }
}
