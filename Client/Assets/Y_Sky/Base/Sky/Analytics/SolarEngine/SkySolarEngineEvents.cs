using System.Collections.Generic;

namespace Sky
{
    // 热云 SolarEngine 事件常量与上报入口。
    // 业务侧应通过 SkyAnalyticsMgr.DesignEvent 走统一分发，热云自定义事件极少需要直接调用本类。
    // 仅在需要上报「热云预置事件」（_appStart 等）时使用 TrackPresetEvent。
    public static class SkySolarEngineEvents
    {
        // 热云预置事件名（参考官方文档）
        public const string AppStart = "_appStart";
        public const string AppLogin = "_appLogin";
        public const string AppPur   = "_appPur";
        public const string AppReg   = "_appReg";

        #region 自定义事件
        public static void TrackEvent(string eventName)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackEvent(eventName);
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            if (string.IsNullOrEmpty(key) || value == null)
            {
                SkySolarEngineMgr.Instance.TrackEvent(eventName);
                return;
            }
            SkySolarEngineMgr.Instance.TrackEvent(eventName,
                new Dictionary<string, object> { [key] = value });
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackEvent(eventName, properties);
        }
        #endregion

        #region 预置事件
        public static void TrackPresetEvent(string eventName)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackPresetEvent(eventName);
        }

        public static void TrackPresetEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackPresetEvent(eventName, properties);
        }
        #endregion

        /// <summary>
        /// 广告收益事件上报（适配 SolarEngine 预置事件 _appImp）
        /// 使用官方桥接方法 trackAdImpression
        /// </summary>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            
            if (revenue < 0)
            {
                LogTool.DebugLog($"[SolarEngine] ad_impression skipped: revenue={revenue} < 0");
                return;
            }

            // 解析广告平台名称（从 network 参数提取）
            string adNetworkPlatform = ParseAdNetworkPlatform(network);
            if (string.IsNullOrEmpty(adNetworkPlatform))
            {
                LogTool.Error($"[SolarEngine] Cannot parse adNetworkPlatform from network: {network}");
                return;
            }

            // 解析聚合平台
            string mediationPlatform = platform.ToLower() == "tpn" ? "topon" : platform.ToLower();
            
            // 映射广告类型
            int adType = 0;
            string formatLower = format.ToLower();
            if (formatLower.Contains("reward")) adType = 1;
            else if (formatLower.Contains("splash")) adType = 2;
            else if (formatLower.Contains("inter")) adType = 3;
            else if (formatLower.Contains("banner")) adType = 5;

            // 调用桥接方法（使用官方 SEAdImpEventModel）
            SkySolarEngineMgr.Instance.TrackAdImpression(
                adNetworkPlatform: adNetworkPlatform,
                mediationPlatform: mediationPlatform,
                adType: adType,
                adNetworkAppID: "",
                adNetworkADID: unit,
                ecpm: revenue*1000,
                currencyType: "USD",
                isRenderSuccess: true,
                customProperties: new Dictionary<string, object>
                {
                    { "ad_format", format },
                    { "ad_platform", platform },
                    { "ad_source", network },
                    { "position", pos }
                }
            );
        }

        /// <summary>
        /// 广告点击事件（_appClick）
        /// 在广告点击回调中调用
        /// </summary>
        public static void TrackAdClickEvent(string network, string unit, string format, string platform, string pos = "")
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            
            if (string.IsNullOrEmpty(network) || string.IsNullOrEmpty(unit))
            {
                LogTool.DebugLog("[SolarEngine] TrackAdClick skipped: missing required params");
                return;
            }

            // 解析参数（与展示事件逻辑一致）
            string adNetworkPlatform = ParseAdNetworkPlatform(network);
            if (string.IsNullOrEmpty(adNetworkPlatform))
            {
                LogTool.Error($"[SolarEngine] Cannot parse adNetworkPlatform from network: {network}");
                return;
            }

            string mediationPlatform = platform.ToLower() == "tpn" ? "topon" : platform.ToLower();
            
            int adType = 0;
            string formatLower = format.ToLower();
            if (formatLower.Contains("reward")) adType = 1;
            else if (formatLower.Contains("splash")) adType = 2;
            else if (formatLower.Contains("inter")) adType = 3;
            else if (formatLower.Contains("banner")) adType = 5;

            // 调用桥接方法（使用官方 SEAdClickEventModel）
            SkySolarEngineMgr.Instance.TrackAdClick(
                adNetworkPlatform: adNetworkPlatform,
                mediationPlatform: mediationPlatform,
                adType: adType,
                adNetworkADID: unit,
                customProperties: new Dictionary<string, object>
                {
                    { "ad_format", format },
                    { "ad_platform", platform },
                    { "ad_source", network },
                    { "position", pos }
                }
            );
        }

        /// <summary>
        /// 应用内购买事件（_appPur）
        /// 在支付成功/失败回调中调用
        /// </summary>
        public static void TrackPurchaseEvent(
            double payAmount,
            string currencyType = "CNY",
            int payStatus = 1,
            string orderId = "",
            string payType = "",
            string productId = "",
            string productName = "",
            int productNum = 1,
            string failReason = "")
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            
            if (payAmount < 0)
            {
                LogTool.DebugLog($"[SolarEngine] TrackPurchase skipped: payAmount={payAmount} < 0");
                return;
            }

            // payStatus: 1=成功, 2=失败
            if (payStatus != 1 && payStatus != 2)
            {
                payStatus = 1;
            }

            // 调用桥接方法（使用官方 SEPurchaseEventModel）
            SkySolarEngineMgr.Instance.TrackPurchase(
                payAmount: payAmount,
                currencyType: currencyType,
                payStatus: payStatus,
                orderId: orderId,
                payType: payType,
                productId: productId,
                productName: productName,
                productNum: productNum,
                failReason: failReason
            );
        }

        /// <summary>
        /// 从 network 参数解析广告平台名称
        /// network 格式: "Topon_2", "Topon_12" 等
        /// </summary>
        private static string ParseAdNetworkPlatform(string network)
        {
            if (string.IsNullOrEmpty(network)) return "";
            
            // 提取 firmId
            if (network.StartsWith("Topon_"))
            {
                string firmIdStr = network.Substring(6);
                if (int.TryParse(firmIdStr, out int firmId))
                {
                    return MapFirmIdToPlatform(firmId);
                }
            }
            
            return "";
        }

        /// <summary>
        /// 映射 TopOn firmId 到平台名称
        /// </summary>
        private static string MapFirmIdToPlatform(int firmId)
        {
            switch (firmId)
            {
                case 1: return "facebook";
                case 2: return "admob";
                case 3: return "inmobi";
                case 4: return "applovin";
                case 5: return "mintegral";
                case 7: return "tencent";
                case 8: return "chartboost";
                case 10: return "is";
                case 11: return "unity";
                case 12: return "vungle";
                case 15: return "csj";
                case 16: return "baidu";
                case 20: return "klein";
                case 21: return "adcolony";
                case 22: return "kuaishou";
                case 23: return "sigmob";
                case 27: return "fyber";
                case 28: return "pangle";
                case 35: return "oppo";
                case 36: return "huawei";
                case 37: return "mi";
                case 38: return "vivo";
                default: return "";
            }
        }

    }
}
