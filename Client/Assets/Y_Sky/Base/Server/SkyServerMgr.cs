using System;
using System.Collections.Generic;
#if SSThinkingData
using ThinkingData.Analytics;
#endif
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using static Sky.SkyLoginEnum;
using static Sky.SkyIapH5Mgr;
using static Sky.SkyProductListRequest;
using static Sky.SkyGameConfig;

namespace Sky
{
    public class SkyServerMgr : SkySingleton<SkyServerMgr>
    {
        #region 是否下架
        public void GameConfig(Dictionary<string, object> args, Action<int, string, GameConfigInfo> callback)
        {
            Post(SkyConfigMgr.Instance.GetGameConfigUrl(), args, callback);
        }
        #endregion

        #region  支付
        public void IapReport(Dictionary<string, object> args,Action<int, string, DataInfo> callback)
        {
            Post(SkyConfigMgr.Instance.GetIapReport(), AddSSProperties(args), callback);
        }
        public void ProductList(Dictionary<string, object> args, Action<int, string, ProductListData> callback)
        {
            Post(SkyConfigMgr.Instance.GetProductListUrl(),args, callback);
        }
        // 创建订单
        public void CreatePaymentOrder(Dictionary<string, object> args, Action<int, string, SkyCreateOrder.ResponseData> callback)
        {
            
            Post(SkyConfigMgr.Instance.GetCreateOrderUrl(),AddSSProperties(args),callback);
        }

        // 订单奖励下发
        public void ReportOrderReward(Dictionary<string, object> args, Action<int, string, RewardReportResponse> callback)
        {
            Post(SkyConfigMgr.Instance.GetReportOrderRewardUrl(),AddSSProperties(args),callback);
        }

        // 订单查询
        public void QueryOrder(Dictionary<string, object> args, Action<int, string, SkyQueryOrder.ResponseData> callback)
        {
            Post(SkyConfigMgr.Instance.GetQueryOrderUrl(),AddSSProperties(args),callback);
        }

        public void QueryOrderList(Dictionary<string, object> args, Action<int, string, SkyQueryOrder.OrderListData> callback)
        {
            Post(SkyConfigMgr.Instance.GetQueryOrderListUrl(),AddSSProperties(args),callback);
        }
        #endregion

        #region 登录
        public void Login(Dictionary<string, object> args, Action<int, string, ServerBackLoginInfo> callback)
        {
            Post(SkyConfigMgr.Instance.GetLoginUrl(), args, callback);
        }
        #endregion

        private Dictionary<string,object> AddSSProperties(Dictionary<string, object> args)
        {
            if(string.IsNullOrEmpty(SkyConfigMgr.Instance.GetSSAppId()) || string.IsNullOrEmpty(SkyConfigMgr.Instance.GetSSUrl())) return args;
            var ssProperties = new Dictionary<string, object>(){};
            #if SSThinkingData
            if (TDAnalytics.GetSuperProperties().Count > 0)
            {
                ssProperties = ssProperties.Concat(TDAnalytics.GetSuperProperties()).ToDictionary(postParK => postParK.Key, PostParV => PostParV.Value);
            }

            if (TDAnalytics.GetPresetProperties().ToDictionary().Count > 0)
            {
                ssProperties = ssProperties.Concat(TDAnalytics.GetPresetProperties().ToDictionary()).ToDictionary(postParK => postParK.Key, PostParV => PostParV.Value);
            }
            #endif
            args.AddIfNotExists("ss_super_properties", JsonConvert.SerializeObject(ssProperties));
            return args;
        }

        public void Post<T>(string url, Dictionary<string, object> args, Action<int, string, T> callback,
            Dictionary<string, string> headers = null) where T : new()
        {
            if (string.IsNullOrEmpty(url))
            {
                SkyLogUtils.Error("url is null or empty");
                return;
            }
            args ??= new Dictionary<string, object>();

            headers ??= new Dictionary<string, string>();

            AddBaseParameters(args);

            SkyEncryptUtils.EncryptionParameters(args);
            var requestBody = SkyDataUtils.ConvertDictToJsonString(args);
            SkyLogUtils.Debug("[server] url=> " + url+ "requestBody: "+requestBody);

            var encryptBody = SkyEncryptUtils.EncryptRequestBody(requestBody);
            if (!string.IsNullOrEmpty(encryptBody))
            {
                headers["params"] = encryptBody;
                headers["encrypt"] = true.ToString().ToLower();
                requestBody = "";
            }

#if UNITY_EDITOR
            headers.AddIfNotExists("Is-Dev", "1");
#else
            headers.AddIfNotExists("Is-Dev", "0");
#endif

            try
            {
                headers.AddIfNotExists("unity-platform", Application.platform.ToString());
            }
            catch (Exception e)
            {
                // ignored
            }

            SkyServerRequest.Instance.SendPostRequest(url, requestBody, headers,(code, res) =>
            {
                SkyLogUtils.Debug($"[server] res ====> code : {code} res : {res} :url:{url}");
                if (code == 0)
                {
                    Response<T> resp = null;
                    var errorMsg = "";

                    try
                    {
                        res = SkyEncryptUtils.DecryptResponseBody(res);
                        resp = JsonUtility.FromJson<Response<T>>(res);
                    }
                    catch (Exception e)
                    {
                        errorMsg = $"[server] Data conversion exception。{e.Message} {res}";
                        SkyLogUtils.Error(errorMsg);
                    }

                    SkyLogUtils.Debug($"[server] resp ====> code : {resp.code} res : {resp.data}");
                    if (resp != null)
                    {
                        callback(resp.code, resp.msg, resp.data);
                    }
                    else
                    {
                        callback.Invoke(-1, errorMsg, new T());
                    }
                }
                else
                {
                    callback.Invoke(code, res, new T());
                }

                SkyLogUtils.Debug("[server]" + "[res] " + res);
            });
        }

        private static void AddBaseParameters(IDictionary<string, object> args)
        {
            var ss_account_id = SkyPrefs.UserId;
            args.AddIfNotExists("unity_sdk_version", SkySdkMgr.GetSDKVersion());
            args.AddIfNotExists("package_name", Application.identifier);

#if UNITY_ANDROID
            args.AddIfNotExists("platform", "android");
            args.AddIfNotExists("platform_os", "android");
            args.AddIfNotExists("channel", "googleplay");
            args.AddIfNotExists("device_type", "Android");
            args.AddIfNotExists("platform_channel", "gp");
#else
            args.AddIfNotExists("platform", "ios");
            args.AddIfNotExists("platform_os", "ios");
            args.AddIfNotExists("platform_channel", "ios");
            args.AddIfNotExists("channel", "AppStore");
            args.AddIfNotExists("device_type", "iPhone");
#endif
            args.AddIfNotExists("app_version", Application.version);
            args.AddIfNotExists("app_version_code", SkyDataUtils.GetVersionCode());
            args.AddIfNotExists("ip", "");
            args.AddIfNotExists("device_id", SkyAdjustPrefs.DeviceId);
            args.AddIfNotExists("adjust_adid", SkyAdjustPrefs.Adid);
            args.AddIfNotExists("app_u8id", "");
            args.AddIfNotExists("app_name", Application.productName);
            args.AddIfNotExists("model", "");
            args.AddIfNotExists("screen_size", "");
            args.AddIfNotExists("network_type", "");
            args.AddIfNotExists("ua", "");
            args.AddIfNotExists("idfa", SkyAdjustPrefs.IDFA);
            args.AddIfNotExists("idfv", SkyAdjustPrefs.IDFV);
            args.AddIfNotExists("gaid", SkyAdjustPrefs.Gaid);
            args.AddIfNotExists("oaid", "");
            args.AddIfNotExists("android_id", "");
            args.AddIfNotExists("adid", SkyAdjustPrefs.Adid);
            args.AddIfNotExists("fire_adid", "");
            args.AddIfNotExists("ad_network", SkyAdjustPrefs.AdjustNetwork);
            args.AddIfNotExists("campaign", SkyAdjustPrefs.AdjustCampaign);
            args.AddIfNotExists("adgroup", SkyAdjustPrefs.AdjustAdgroup);
            args.AddIfNotExists("creative", SkyAdjustPrefs.AdjustCreative);
            args.AddIfNotExists("clickLabel", SkyAdjustPrefs.AdjustClickLabel);
            args.AddIfNotExists("referrer", "");
            args.AddIfNotExists("language","zh");
            args.AddIfNotExists("memory", "");
            args.AddIfNotExists("memory_usage", "");
            args.AddIfNotExists("country", "");
            args.AddIfNotExists("user_id", SkyPrefs.UserId);
            args.AddIfNotExists("user_type", 0);
#if SSThinkingData
            args.AddIfNotExists("ss_distinct_id", TDAnalytics.GetDistinctId() ?? "");
#endif
            args.AddIfNotExists("ss_account_id", ss_account_id ?? "");
            args.AddIfNotExists("ts", "" + SkyTimeUtils.CurrentTimestamp());
        }


        [Serializable]
        public class Response<T>
        {
            public int code = -1;
            public string msg;
            public T data;
            public int ts;
        }

        [Serializable]
        public class DataInfo
        {
            public string environment;
            public int illegal_order = -1;
            public string illegal_msg;
            public string appstore_account_token;
        }



    }
}