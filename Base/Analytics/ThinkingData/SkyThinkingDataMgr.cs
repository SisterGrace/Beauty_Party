#if SSThinkingData
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ThinkingData.Analytics;
using UnityEngine;


namespace Sky
{
    public class SkyThinkingDataMgr : SkySingleton<SkyThinkingDataMgr>
    {
        /// <summary>TDAnalytics.Init 完成并延迟就绪后为 true，表示访客 ID 等用户属性已可用。</summary>
        public bool IsReady { get; private set; }

        public void Init()
        {
            SkyTimeUtils.SetInstallTime();
            if(IsAppIdOrServerUrlEmpty()) return;
            
            TDAnalytics.Init(SkyConfigMgr.Instance.GetSSAppId(), SkyConfigMgr.Instance.GetSSUrl());
            TDAnalytics.EnableAutoTrack(TDAutoTrackEventType.AppInstall | TDAutoTrackEventType.AppStart | TDAutoTrackEventType.AppEnd);
            TDAnalytics.SetSuperProperties(new Dictionary<string, object>{["Sky_version"] = SkySdkMgr.GetSDKVersion()});

            SetProperties();

            StartCoroutine(MarkReadyAfterDelay());
        }

        private IEnumerator MarkReadyAfterDelay()
        {
            yield return new WaitForSeconds(1.5f);
            IsReady = true;
            SkyLogUtils.Debug("[ThinkingData] SDK ready");
        }

        private void SetProperties()
        {
            if(IsAppIdOrServerUrlEmpty()) return;
            UserSet(new Dictionary<string, object>
            {
                { "user_register_time", DateTime.Now },
                { "user_register_timestamp", SkyTimeUtils.CurrentTimestamp() }
            },true);

            UserSet(new Dictionary<string, object>
            {
                { "user_login_day", SkyTimeUtils.GetIntervalDays()}
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
            var appid = SkyConfigMgr.Instance.GetSSAppId();
            var serverUrl = SkyConfigMgr.Instance.GetSSUrl();
            if(string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(serverUrl))
            {
                SkyLogUtils.Error("ss appid or serverUrl is null or empty, please check your config file.");
                return true;
            }
            return false;
        }
    }
}
#endif