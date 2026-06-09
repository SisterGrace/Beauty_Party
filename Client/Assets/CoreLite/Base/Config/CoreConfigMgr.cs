using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace CoreLite
{
    public class CoreConfigMgr : CoreSingleton<CoreConfigMgr>
    {
        
        public CoreGameConfigData gameConfig;

        protected override void OnInstanceCreate()
        {
            gameConfig = Resources.Load<CoreGameConfigData>("CoreGameConfigData");
            if (gameConfig == null) return;
            if (gameConfig.toponLocalConfig == null)
                gameConfig.toponLocalConfig = new CoreToponLocalConfig();
            if (gameConfig.mtgLocalConfig == null)
                gameConfig.mtgLocalConfig = new CoreMtgLocalConfig();
        }

        public string GetAsnUrl()
        {
            return gameConfig?.GetAsnUrl() ?? "";
        }

        public string GetAdmobAppId()
        {
            return gameConfig?.GetAdmobAppId() ?? "";
        }

        public string GetAdjustId()
        {
            return gameConfig?.GetAdjustId() ?? "";
        }

        public string GetSSAppId()
        {
            return gameConfig?.GetSSAppId() ?? "";
        }

        public string GetSSUrl()
        {
            return gameConfig?.GetSSUrl() ?? "";
        }
        
        // 服务器URL相关
        public string GetIapReport()
        {
            return null;
            return gameConfig?.GetIAPReportUrl() ?? "";
        }

        public string GetLoginUrl()
        {
            return null;
            return gameConfig?.GetLoginUrl() ?? "";
        }
        
        public string GetProductListUrl()
        {
            return null;
            return gameConfig?.GetProductListUrl() ?? "";
        }

        public string GetGameConfigUrl()
        {
            return null;
            return gameConfig?.GetGameConfigUrl() ?? "";
        }
        
        public string GetCreateOrderUrl()
        {
            return null;
            return gameConfig?.GetCreateOrderUrl() ?? "";
        }

        public string GetReportOrderRewardUrl()
        {
            return null;
            return gameConfig?.GetReportOrderRewardUrl() ?? "";
        }
        
        public string GetQueryOrderUrl()
        {
            return null;
            return gameConfig?.GetQueryOrderUrl() ?? "";
        }

        public string GetQueryOrderListUrl()
        {
            return null;
            return gameConfig?.GetQueryOrderListUrl() ?? "";
        }
    }
}