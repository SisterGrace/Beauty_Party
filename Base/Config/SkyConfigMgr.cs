using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sky
{
    public class SkyConfigMgr : SkySingleton<SkyConfigMgr>
    {
        
        public SkyGameConfigData gameConfig;

        protected override void OnInstanceCreate()
        {
            gameConfig = Resources.Load<SkyGameConfigData>("SkyGameConfigData");
            if (gameConfig == null) return;
            if (gameConfig.toponLocalConfig == null)
                gameConfig.toponLocalConfig = new SkyToponLocalConfig();
            if (gameConfig.mtgLocalConfig == null)
                gameConfig.mtgLocalConfig = new SkyMtgLocalConfig();
        }
        public string GetGA_gameKey()
        {
            return gameConfig?.GetGA_gameKey() ?? "";
        }
        public string GetGA_secretKey()
        {
            return gameConfig?.GetGA_secretKey() ?? "";
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
            return gameConfig?.GetIAPReportUrl() ?? "";
        }

        public string GetLoginUrl()
        {
            return gameConfig?.GetLoginUrl() ?? "";
        }
        
        public string GetProductListUrl()
        {
            return gameConfig?.GetProductListUrl() ?? "";
        }

        public string GetGameConfigUrl()
        {
            return gameConfig?.GetGameConfigUrl() ?? "";
        }
        
        public string GetCreateOrderUrl()
        {
            return gameConfig?.GetCreateOrderUrl() ?? "";
        }

        public string GetReportOrderRewardUrl()
        {
            return gameConfig?.GetReportOrderRewardUrl() ?? "";
        }
        
        public string GetQueryOrderUrl()
        {
            return gameConfig?.GetQueryOrderUrl() ?? "";
        }

        public string GetQueryOrderListUrl()
        {
            return gameConfig?.GetQueryOrderListUrl() ?? "";
        }
    }
}