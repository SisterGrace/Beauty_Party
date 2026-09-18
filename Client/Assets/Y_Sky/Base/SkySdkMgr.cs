using System;
using System.Collections.Generic;
using static Sky.SkyGameConfig;
using static Sky.SkyIapH5Mgr;
using static Sky.SkyProductListRequest;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Sky
{
    public class SkySdkMgr : SkySingleton<SkySdkMgr>
    {
        public static string GetSDKVersion()
        {
            return "1.1.2";
        }

        /// <summary>
        /// 设置asn回调
        /// </summary>
        public static Action<bool> OnAsnUserReceived;
        /// <summary>
        /// 设置asn回调
        /// </summary>
        /// <param name="_callback"> true：是asn用户；false：不是asn用户 </param>
        public static void RegisterAsnUserHandler(Action<bool> _callback) => OnAsnUserReceived = _callback;

        /// <summary>
        /// 去广告后调用
        /// </summary>
        public static void SetRemoveAds()
        {
            SkyPrefs.IsRemoveAds = true;
        }

        /// <summary>
        /// 返回APP是否下架
        /// </summary>
        /// <param name="_callback"> 1：未上线，2：已上线，3：已下架，4：不适用 </param>
        public static void GameConfig(Action<AppStatus, string> _callback) => SkyGameConfig.GameConfig(_callback);

        /// <summary>
        /// 回调：bool 是否为买量用户；string  返回的 network 字段；string  返回的 clickLabel 字段。
        /// </summary>
        public Action<bool, string, string> OnUserAttributionReceived;
        public void RegisterUserAttributionHandler(Action<bool, string, string> _callback) => OnUserAttributionReceived = _callback;

        /// <summary>
        /// 发送邮件反馈
        /// </summary>
        /// <param name="email"></param>
        public static void SendFeedbackEmail(string email) => SkyEmailFeedback.SendFeedbackEmail(email);

        /// <summary>
        /// 初始化SDK（不启用风控）
        /// </summary>
        /// <param name="_splashCloseCallback">开屏关闭回调</param>
        public static void InitSDK(Action _splashCloseCallback)
        {
            DoInitSDK(_splashCloseCallback, null);
        }

        /// <summary>
        /// 初始化SDK（启用风控）
        /// </summary>
        /// <param name="_splashCloseCallback">开屏关闭回调</param>
        /// <param name="baseUrl">风控服务地址，传入非空值时启用 XMSDK 风控能力</param>
        public static void InitSDK(Action _splashCloseCallback, string baseUrl)
        {
            DoInitSDK(_splashCloseCallback, baseUrl);
        }

        private static void DoInitSDK(Action _splashCloseCallback, string baseUrl)
        {
            // 必须在所有 SDK 初始化之前创建 Loom 宿主，确保后续 SDK 异步回调（尤其是 TopOn 各 Network）
            // 可以通过 Loom.QueueOnMainThread 安全派发回主线程。Loom.Initialize 内部会创建 GameObject，
            // 必须在主线程调用一次。
            Loom.Initialize();
#if USE_XMSDK
            SkyXmSdkMgr.Instance.InitRiskDetection(baseUrl);
#endif
            /*SkyAsnMgr.Instance.StartReqAsn();//*/
#if IAP_ENABLE
            SkyIapNativeMgr.Instance.PreInitialize();
            SkyIapNativeMgr.Instance.Initialize();
#endif
#if H5
            SkyIapH5Mgr.Instance.Init();
#endif
            
#if UNITY_ANDROID || UNITY_EDITOR
            SkyAnalyticsMgr.Init();
            SkyAdsMgr.Instance.Init(_splashCloseCallback);
#else
            Adjust.RequestAppTrackingAuthorization((code) =>
            {
                SkyPrefs.UserAtt = code;
                SkyAdjustMgr.Instance.LoadIds();
                SkyAnalyticsMgr.Init();
                SkyAdsMgr.Instance.Init(_splashCloseCallback);
                SkyAnalyticsMgr.TrackEvent("ReqAtt", new Dictionary<string, object> { ["code"] = code });
            });
#endif
        }

        #region  支付相关
        /// <summary>
        /// 获取支付列表
        /// </summary>
        /// <param name="callback">bool 成功/失败; msg: 错误信息; List<ProductListData>: 支付列表</param>
        public static void GetProductList(Action<bool, string, ProductListData> callback) => SkyIapH5Mgr.Instance.GetProductList(callback);

        /// <summary>
        /// iOS为三位码，Android为2位码。iOS：CHN/USA，Android：CN/US
        /// </summary>
        public static string GetStoreCountryCode() => SkyIapH5Mgr.Instance.storeCountryCode;

        // 创建订单
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback">如果不使用SDK处理支付时，可根据回调信息自行处理</param>
        /// <param name="useSDKPayment">是否使用SDK处理支付逻辑，不使用时可根据回调自行处理</param>
        public static void CreatePaymentOrder(SkyCreateOrder.Request request, Action<bool, string, SkyCreateOrder.ResponseData> callback = null, bool useSDKPayment = true)
            => SkyIapH5Mgr.Instance.CreatePaymentOrder(request, callback, useSDKPayment);

        /// <summary>
        /// 订单列表接口
        /// </summary>
        /// <param name="req">用户ID，支付状态，发货状态</param>
        /// <param name="callback">回调: bool 成功/失败; msg: 错误信息; List&lt;OrderListData&gt;: 订单列表</param>
        public static void QueryOrderList(SkyQueryOrderList.Request req, Action<bool, string, SkyQueryOrder.OrderListData> callback)
            => SkyIapH5Mgr.Instance.QueryOrderList(req, callback);
#if IAP_ENABLE
        /// <summary>
        /// 根据商品ID获取商品信息
        /// </summary>
        public static Product GetProductInfoById(string productID) => SkyIapNativeMgr.Instance.GetProductInfoByID(productID);

        /// <summary>
        /// 获取带货币符号价格字符串
        /// </summary>
        public static string GetLocalizedPriceById(string productID) => SkyIapNativeMgr.Instance.GetPriceByID(productID);

        /// <summary>
        /// 点击购买方法
        /// </summary>
        /// <param name="req">请求参数包含，商品信息，orderId，支付方式:原生支付/h5支付，h5支付url</param>
        public static void PurchaseProductById(SkyBuy.Request req) => SkyIapNativeMgr.Instance.BuyProductByID(req);

        /// <summary>
        /// 动态添加商品ID
        /// </summary>
        public static void AddProducts(Dictionary<string, ProductType> products) => SkyIapNativeMgr.Instance.AddProducts(products);

        /// <summary>
        /// 初始化前静态添加商品ID
        /// </summary>
        public static void AddProductsStatic(Dictionary<string, ProductType> products) => SkyIapNativeMgr.Instance.ProductDic = products;
#endif
        /// <summary>
        /// 支付回调
        /// </summary>
        public static Action<SkyPurchase.Result> OnPurchaseComplete;

        public static void RegisterPurchaseCompletionHandler(Action<SkyPurchase.Result> _action) => OnPurchaseComplete = _action;

        /// <summary>
        /// 内购是否初始化成功
        /// </summary>
        /// <param name="result">初始化结果</param>
        /// <param name="errorMsg">错误信息</param>
        public static Action<bool, string> OnPurchaseInitComplete;

        /// <summary>
        /// 设置iap是否初始化成功回调，返回&lt;初始化结果，错误信息&gt;
        /// </summary>
        public static void RegisterPurchaseInitHandler(Action<bool, string> _action) => OnPurchaseInitComplete = _action;


        /// <summary>
        /// 游戏下发奖励后上报
        /// </summary>
        /// <param name="req">商品名称：productName, 商品ID：productId, 订单号：orderId, 游戏拓展字段：gameExtra01, 游戏拓展字段：gameExtra02, 支付类型：原生/h5</param>
        /// <param name="_callback">int：int == 0 上报服务端成功/失败；msg 成功/失败信息</param>
        public static void LogCheckingOrder(SkyQueryOrder.Result req, Action<int, string> _callback)
        {
			if (SkyIapH5Mgr.HasServerBackend)
			{
				SkyLogUtils.Debug("[无服务器模式] 跳过 LogCheckingOrder（无服务器）");
				return;
			}
			if (!req.payment_method.Contains("stripe"))
            {
#if IAP_ENABLE
                SkyIapNativeMgr.Instance.RemoveVerifyFailOrderId(req.order_no);
                SkyIapNativeMgr.Instance.DoConfirmPendingPurchaseByID(req.product_id);
                SkyIapIosOrderMgr.RemoveOrder(req.product_id);
#endif
            }
            SkyIapH5Mgr.Instance.ReportOrderReward(new SkyQueryOrder.Request(req.order_no, SkyPrefs.UserId), _callback);
            SkyIapEvents.IAPCheckingOrder(req);
        }
#endregion

        #region  在线参数
        public static Action OnFirebaseActivated;
        public static void RegisterFirebaseActivateHandler(Action _callback) => OnFirebaseActivated = _callback;

        public static string GetConfigStr(string key, string defaultValue = "") => SkyAnalyticsMgr.GetConfigStr(key, defaultValue);
        public static int GetConfigInt(string key, int defaultValue = 0) => SkyAnalyticsMgr.GetConfigInt(key, defaultValue);
        public static bool GetConfigBool(string key, bool defaultValue = false) => SkyAnalyticsMgr.GetConfigBool(key, defaultValue);
        #endregion

        #region  XMSDK 风控
#if USE_XMSDK
        /// <summary>
        /// 获取风控 Device ID 及 XMSDK 集成状态。
        /// </summary>
        public static XMDeviceInfo GetXMDeviceID()
        {
            return new XMDeviceInfo
            {
                XMDeviceId = SkyXmSdkMgr.Instance.XMDeviceId ?? "",
                IsXMSDKIntegrated = true,
                IsRiskDetectionObtained = SkyXmSdkMgr.Instance.IsRiskDetectionObtained
            };
        }

        /// <summary>
        /// 上报广告点击（所有广告类型），仅买量用户有效。
        /// </summary>
        public static void TrackAdClick(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId)
        {
            SkyXmSdkMgr.Instance.TrackAdClick(revenue, adType, platformName, adUnitId, networkName, networkPlacementId);
        }
#else
        public static XMDeviceInfo GetXMDeviceID()
        {
            return new XMDeviceInfo
            {
                XMDeviceId = "",
                IsXMSDKIntegrated = false,
                IsRiskDetectionObtained = false
            };
        }

        public static void TrackAdClick(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId) { }
#endif
#endregion

        #region  事件上报
        public static void TrackRewardButtonClick(string position)
            => SkyAnalyticsMgr.TrackEvent("RV_Button_Show", new Dictionary<string, object> { ["position"] = position });
        public static void TrackIAPButtonShow(string productId, string productName, Dictionary<string, object> eventData = null)
            => SkyIapEvents.IAPButtonShow(productId, productName, eventData);
        public static void TrackEvent(string eventName) => SkyAnalyticsMgr.TrackEvent(eventName);
        public static void TrackEvent(string eventName, string key, object value) => SkyAnalyticsMgr.TrackEvent(eventName, key, value);
        public static void TrackEvent(string eventName, Dictionary<string, object> dict) => SkyAnalyticsMgr.TrackEvent(eventName, dict);
        #endregion

        #region  公共事件属性/用户属性
        public static void UserSet(Dictionary<string, object> dic, bool setOnce = false) => SkyAnalyticsMgr.UserSet(dic, setOnce);
        public static void SetSuperProperties(Dictionary<string, object> superProperties) => SkyAnalyticsMgr.SetSuperProperties(superProperties);
        #endregion

        #region  账号相关

        /// <summary>
        /// 获取当前 userid
        /// </summary>
        public static string GetCurrentUserId() => SkyPrefs.UserId;

        /// <summary>
        /// 登录接口
        /// </summary>
        /// <param name="onComplete">bool:是否登录成功，string:登录失败原因，string:登录成功后的userid</param>
        public async static void Login(Action<bool, string, string> onComplete = null) => SkyAccountMgr.Instance.Login(onComplete);

        public static void Logout() => SkyAccountMgr.Instance.Logout();
        #endregion

        #region  广告相关
        public static bool IsRewardedVideoAvailable() => SkyAdsMgr.Instance.IsRewardedVideoAvailable();
        public static void ShowRewardedVideo(string _pos, Action _closeCallback, Action _showFailCallback)
            => SkyAdsMgr.Instance.ShowRewardedVideo(_pos, _closeCallback, _showFailCallback);
        public static void ShowInterstitial(string _adPos, IvType _IvType = IvType.IV1, Action _closeCallback = null, Action<HandCallbackInfo, Action> _action = null)
            => SkyAdsMgr.Instance.ShowInterstitial(_adPos, _IvType, _closeCallback, _action);
        public static bool IsBannerAvailable() => SkyAdsMgr.Instance.IsBannerAvailable();

        /// <summary>
        /// 展示横幅广告
        /// </summary>
        /// <param name="_position">位置</param>
        public static void ShowBanner() => SkyAdsMgr.Instance.ShowBanner();
        public static void HideBanner() => SkyAdsMgr.Instance.HideBanner();

        /// <summary>
        /// 返回当前竞价成功的激励视频价格（eCPM）。仅做竞价查询，不触发展示或频次逻辑。无可用出价时返回 -1。
        /// </summary>
        public static double GetCurrentRewardedVideoBidPrice() => SkyAdsMgr.Instance.GetCurrentRewardedVideoBidPrice();

        /// <summary>
        /// 返回当前可用插屏广告的最高出价（eCPM）。不检查频次规则，无可用出价时返回 -1。
        /// </summary>
        /// <param name="ivType">保留参数，兼容旧调用</param>
        public static double GetCurrentInterstitialBidPrice(IvType ivType = IvType.IV1) => SkyAdsMgr.Instance.GetCurrentInterstitialBidPrice(ivType);

        /// <summary>
        /// 展示插屏广告，不受频次规则控制；有可用插屏则展示，无则直接回调。
        /// </summary>
        /// <param name="_adPos">广告位</param>
        /// <param name="_closeCallback">关闭或未展示时的回调</param>
        public static void ShowInterstitialIgnoreFrequency(string _adPos, Action _closeCallback = null)
            => SkyAdsMgr.Instance.ShowInterstitialIgnoreFrequency(_adPos, _closeCallback);

        /// <summary>
        /// 返回当前竞价成功的原生广告价格（eCPM）。仅做竞价查询，不触发展示逻辑。无可用出价时返回 -1。
        /// </summary>
        public static double GetCurrentNativeBidPrice() => SkyAdsMgr.Instance.GetCurrentNativeBidPrice();

        public static double GetCurrentNativeFullscreenBidPrice() => SkyAdsMgr.Instance.GetCurrentNativeFullscreenBidPrice();

        public static double GetCurrentSmallNativeBidPrice() => SkyAdsMgr.Instance.GetCurrentSmallNativeBidPrice();

        #endregion

        #region  H5 广告事件上报

        /// <summary>
        /// 互动页 H5 广告事件类型，用于 TrackH5AdInteractiveEvent 区分上报。
        /// </summary>
        public enum SkyH5AdInteractiveEventType
        {
            /// <summary>互动页广告展示失败 interactive_ad_fail</summary>
            AdFail,
            /// <summary>互动页广告点击 interactive_ad_click</summary>
            AdClick,
            /// <summary>互动页广告关闭 interactive_ad_close</summary>
            AdClose,
            /// <summary>互动页激励生效 interactive_reward_reach</summary>
            RewardReach,
            /// <summary>互动页广告展示成功 interactive_ad_show</summary>
            AdShow
        }

        /// <summary>
        /// 上报互动页 H5 广告事件（统一入口，通过枚举区分事件类型）。会上报到 Firebase、Adjust、ThinkingData。
        /// number（第几次展示）按 adFormat+platform（SpinGO/Okspin 单个广告格式）计数，与触发点位无关，无需外部传入。
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="adFormat">广告格式（Interstitial / Rewarded）</param>
        /// <param name="platform">聚合平台（Okspin / SpinGO）</param>
        /// <param name="adUnitNameUrl">广告位 ID，传链接 URL</param>
        /// <param name="position">广告展示点位</param>
        /// <param name="value">单次展示收益（仅 AdShow 使用），默认 0</param>
        /// <param name="passTime">激励等待触发生效秒数（仅 RewardReach 使用），默认 0</param>
        /// <param name="reason">失败原因（仅 AdFail 使用），可为 null</param>
        public static void TrackH5AdInteractiveEvent(SkyH5AdInteractiveEventType eventType, SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position, double value = 0, float passTime = 0, string reason = null)
        {
            string formatKey = NormalizeH5AdFormatKey(adFormat.ToString());
            string platformKey = platform.ToString();
            int number = eventType == SkyH5AdInteractiveEventType.AdShow ? GetAndIncrementH5AdImpressionNumber(formatKey, platformKey) : GetH5AdImpressionNumber(formatKey, platformKey);
            switch (eventType)
            {
                case SkyH5AdInteractiveEventType.AdFail:
                    SkyH5AdsEvents.InvokeAdFail(adFormat, platform, adUnitNameUrl, position, reason ?? string.Empty);
                    break;
                case SkyH5AdInteractiveEventType.AdClick:
                    SkyH5AdsEvents.InvokeAdClick(adFormat, platform, adUnitNameUrl, position, number);
                    break;
                case SkyH5AdInteractiveEventType.AdClose:
                    SkyH5AdsEvents.InvokeAdClose(adFormat, platform, adUnitNameUrl, position, number);
                    break;
                case SkyH5AdInteractiveEventType.RewardReach:
                    SkyH5AdsEvents.InvokeRewardReach(adFormat, platform, adUnitNameUrl, position, number, passTime);
                    break;
                case SkyH5AdInteractiveEventType.AdShow:
                    SkyH5AdsEvents.InvokeAdShow(adFormat, platform, adUnitNameUrl, position, number, value);
                    break;
            }
        }

        /// <summary>
        /// 上报 H5 广告 Init 调用。在调用外部 SpinH5Ad.Init(platform) 时同时调用此方法上报。
        /// </summary>
        public static void TrackH5AdInit(SkyH5AdPlatform platform) => SkyH5AdsEvents.InvokeInit(platform);

        /// <summary>
        /// 上报 H5 激励视频展示调用。在调用外部 SpinH5Ad.ShowRewardedAd 时同时调用此方法上报。
        /// </summary>
        public static void TrackH5AdShowRewardedAd(string url, SkyH5AdPlatform platform, int rewardAfter = 15, bool startOnPageLoaded = true)
            => SkyH5AdsEvents.InvokeShowRewardedAd(url, platform, rewardAfter, startOnPageLoaded);

        /// <summary>
        /// 上报 H5 插屏广告展示调用。在调用外部 SpinH5Ad.ShowInterstitialAd 时同时调用此方法上报。
        /// </summary>
        public static void TrackH5AdShowInterstitialAd(string url, SkyH5AdPlatform platform, int closeDelay = 5, bool startOnPageLoaded = true)
            => SkyH5AdsEvents.InvokeShowInterstitialAd(url, platform, closeDelay, startOnPageLoaded);

        /// <summary>
        /// 上报 H5 广告展示（广告收益）。供 H5 广告调用，会同步上报到 Adjust、Firebase、ThinkingData。
        /// currency=USD；format_number/position_number 由 SDK 按格式、点位独立累计（与原生广告共用计数逻辑）。
        /// </summary>
        /// <param name="revenue">广告收益金额（Revenue，USD）</param>
        /// <param name="adUrl">广告链接 URL，对应 AdRevenueUnit、AdRevenuePlacement 及 ad_unit_name</param>
        /// <param name="format">广告形式，如 "interstitial"、"rewarded" 等</param>
        /// <param name="position">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="adPlatform">H5 广告平台枚举</param>
        public static void TrackH5AdImpressionEvent(double revenue, string adUrl, string format, string position, SkyH5AdPlatform adPlatform)
        {
            string platform = adPlatform.ToString();
            string url = adUrl ?? string.Empty;
            string adFormat = string.IsNullOrEmpty(format) ? "unknown" : NormalizeH5AdFormatKey(format);
            // position 为空时用 adFormat 作为默认点位，便于区分不同广告位的 position_number
            string pos = string.IsNullOrEmpty(position) ? adFormat : position;
            int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(adFormat);
            int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);
            SkyAnalyticsMgr.TrackAdImpressionEvent(revenue, platform, url, url, adFormat, platform, pos, formatNumber, positionNumber);
        }

        private const string H5AdImpressionCountKeyPrefix = "h5_ad_impression_";

        /// <summary>互动广告 number 计数 key：format + platform（如 Interstitial|Okspin）。</summary>
        private static string GetH5AdImpressionKey(string format, string platformOrPosition) => H5AdImpressionCountKeyPrefix + format + "|" + platformOrPosition;

        /// <summary>
        /// 统一 format 的 key 格式，供 TrackH5AdImpressionEvent 与 TrackH5AdInteractiveEvent 的 number 计数使用。
        /// </summary>
        private static string NormalizeH5AdFormatKey(string format)
        {
            if (string.IsNullOrEmpty(format)) return "unknown";
            switch (format.Trim().ToLowerInvariant())
            {
                case "interstitial": return "Interstitial";
                case "rewarded": return "Rewarded";
                default: return format;
            }
        }

        /// <summary>
        /// 从本地存储读取当前展示次数（不自增），与 GetAndIncrementH5AdImpressionNumber 共用同一 key。互动广告按 format+platform 计数。
        /// </summary>
        private static int GetH5AdImpressionNumber(string format, string platformOrPosition) => SkyPrefs.GetInt(GetH5AdImpressionKey(format, platformOrPosition), 0);

        /// <summary>
        /// 按 format+platform（SpinGO/Okspin）从本地存储获取并自增展示次数（从 1 开始），结果持久化到 SkyPrefs。
        /// </summary>
        private static int GetAndIncrementH5AdImpressionNumber(string format, string platformOrPosition)
        {
            string key = GetH5AdImpressionKey(format, platformOrPosition);
            int count = SkyPrefs.GetInt(key, 0) + 1;
            SkyPrefs.SetInt(key, count);
            SkyPrefs.Save();
            return count;
        }
        #endregion
    }
}

