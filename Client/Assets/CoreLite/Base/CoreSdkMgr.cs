using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using static CoreLite.CoreGameConfig;
using static CoreLite.CoreIapH5Mgr;
using static CoreLite.CoreProductListRequest;
using AdjustSdk;
using UnityEngine;

namespace CoreLite
{
    public class CoreSdkMgr : CoreSingleton<CoreSdkMgr>
    {
        public static string GetSDKVersion()
        {
            return "1.0.4";
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
            CorePrefs.IsRemoveAds = true;
        }

        /// <summary>
        /// 返回APP是否下架
        /// </summary>
        /// <param name="_callback"> 1：未上线，2：已上线，3：已下架，4：不适用 </param>
        public static void GameConfig(Action<AppStatus, string> _callback) => CoreGameConfig.GameConfig(_callback);

        /// <summary>
        /// adjust 回调：bool 是否为买量用户；string adjust 返回的 network 字段；string adjust 返回的 clickLabel 字段。
        /// </summary>
        public Action<bool, string, string> OnUserAttributionReceived;
        public void RegisterUserAttributionHandler(Action<bool, string, string> _callback) => OnUserAttributionReceived = _callback;

        /// <summary>
        /// 发送邮件反馈
        /// </summary>
        /// <param name="email"></param>
        public static void SendFeedbackEmail(string email) => CoreEmailFeedback.SendFeedbackEmail(email);

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
            CoreXmSdkMgr.Instance.InitRiskDetection(baseUrl);
#endif
            CoreAsnMgr.Instance.StartReqAsn();
            CoreIapNativeMgr.Instance.PreInitialize();
            CoreIapH5Mgr.Instance.Init();
            CoreIapNativeMgr.Instance.Initialize();
#if UNITY_ANDROID
            CoreAnalyticsMgr.Init();
            CoreAdsMgr.Instance.Init(_splashCloseCallback);
#else
            Adjust.RequestAppTrackingAuthorization((code) =>
            {
                CorePrefs.UserAtt = code;
                CoreAdjustMgr.Instance.LoadIds();
                CoreAnalyticsMgr.Init();
                CoreAdsMgr.Instance.Init(_splashCloseCallback);
                CoreAnalyticsMgr.TrackEvent("ReqAtt", new Dictionary<string, object> { ["code"] = code });
            });
#endif
        }

        #region  支付相关
        /// <summary>
        /// 获取支付列表
        /// </summary>
        /// <param name="callback">bool 成功/失败; msg: 错误信息; List<ProductListData>: 支付列表</param>
        public static void GetProductList(Action<bool, string, ProductListData> callback) => CoreIapH5Mgr.Instance.GetProductList(callback);

        /// <summary>
        /// iOS为三位码，Android为2位码。iOS：CHN/USA，Android：CN/US
        /// </summary>
        public static string GetStoreCountryCode() => CoreIapH5Mgr.Instance.storeCountryCode;

        // 创建订单
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback">如果不使用SDK处理支付时，可根据回调信息自行处理</param>
        /// <param name="useSDKPayment">是否使用SDK处理支付逻辑，不使用时可根据回调自行处理</param>
        public static void CreatePaymentOrder(CoreLiteCreateOrder.Request request, Action<bool, string, CoreLiteCreateOrder.ResponseData> callback = null, bool useSDKPayment = true)
            => CoreIapH5Mgr.Instance.CreatePaymentOrder(request, callback, useSDKPayment);

        /// <summary>
        /// 订单列表接口
        /// </summary>
        /// <param name="req">用户ID，支付状态，发货状态</param>
        /// <param name="callback">回调: bool 成功/失败; msg: 错误信息; List&lt;OrderListData&gt;: 订单列表</param>
        public static void QueryOrderList(CoreLiteQueryOrderList.Request req, Action<bool, string, CoreLiteQueryOrder.OrderListData> callback)
            => CoreIapH5Mgr.Instance.QueryOrderList(req, callback);

        /// <summary>
        /// 根据商品ID获取商品信息
        /// </summary>
        public static Product GetProductInfoById(string productID) => CoreIapNativeMgr.Instance.GetProductInfoByID(productID);

        /// <summary>
        /// 获取带货币符号价格字符串
        /// </summary>
        public static string GetLocalizedPriceById(string productID) => CoreIapNativeMgr.Instance.GetPriceByID(productID);

        /// <summary>
        /// 点击购买方法
        /// </summary>
        /// <param name="req">请求参数包含，商品信息，orderId，支付方式:原生支付/h5支付，h5支付url</param>
        public static void PurchaseProductById(CoreLiteBuy.Request req) => CoreIapNativeMgr.Instance.BuyProductByID(req);

        /// <summary>
        /// 动态添加商品ID
        /// </summary>
        public static void AddProducts(Dictionary<string, ProductType> products) => CoreIapNativeMgr.Instance.AddProducts(products);

        /// <summary>
        /// 初始化前静态添加商品ID
        /// </summary>
        public static void AddProductsStatic(Dictionary<string, ProductType> products) => CoreIapNativeMgr.Instance.ProductDic = products;
        /// <summary>
        /// 支付回调
        /// </summary>
        public static Action<CoreLitePurchase.Result> OnPurchaseComplete;

        public static void RegisterPurchaseCompletionHandler(Action<CoreLitePurchase.Result> _action) => OnPurchaseComplete = _action;

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
        public static void LogCheckingOrder(CoreLiteQueryOrder.Result req, Action<int, string> _callback)
        {
            if (!req.payment_method.Contains("stripe"))
            {
                CoreIapNativeMgr.Instance.RemoveVerifyFailOrderId(req.order_no);
                CoreIapNativeMgr.Instance.DoConfirmPendingPurchaseByID(req.product_id);
                CoreIapIosOrderMgr.RemoveOrder(req.product_id);
            }
            CoreIapH5Mgr.Instance.ReportOrderReward(new CoreLiteQueryOrder.Request(req.order_no, CorePrefs.UserId), _callback);
            CoreIapEvents.IAPCheckingOrder(req);
        }
        #endregion

        #region  在线参数
        public static Action OnFirebaseActivated;
        public static void RegisterFirebaseActivateHandler(Action _callback) => OnFirebaseActivated = _callback;

        public static string GetConfigStr(string key, string defaultValue = "") => CoreAnalyticsMgr.GetConfigStr(key, defaultValue);
        public static int GetConfigInt(string key, int defaultValue = 0) => CoreAnalyticsMgr.GetConfigInt(key, defaultValue);
        public static bool GetConfigBool(string key, bool defaultValue = false) => CoreAnalyticsMgr.GetConfigBool(key, defaultValue);
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
                XMDeviceId = CoreXmSdkMgr.Instance.XMDeviceId ?? "",
                IsXMSDKIntegrated = true,
                IsRiskDetectionObtained = CoreXmSdkMgr.Instance.IsRiskDetectionObtained
            };
        }

        /// <summary>
        /// 上报广告点击（所有广告类型），仅买量用户有效。
        /// </summary>
        public static void TrackAdClick(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId)
        {
            CoreXmSdkMgr.Instance.TrackAdClick(revenue, adType, platformName, adUnitId, networkName, networkPlacementId);
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
            => CoreAnalyticsMgr.TrackEvent("RV_Button_Show", new Dictionary<string, object> { ["position"] = position });
        public static void TrackIAPButtonShow(string productId, string productName, Dictionary<string, object> eventData = null)
            => CoreIapEvents.IAPButtonShow(productId, productName, eventData);
        public static void TrackEvent(string eventName) => CoreAnalyticsMgr.TrackEvent(eventName);
        public static void TrackEvent(string eventName, string key, object value) => CoreAnalyticsMgr.TrackEvent(eventName, key, value);
        public static void TrackEvent(string eventName, Dictionary<string, object> dict) => CoreAnalyticsMgr.TrackEvent(eventName, dict);
        #endregion

        #region  公共事件属性/用户属性
        public static void UserSet(Dictionary<string, object> dic, bool setOnce = false) => CoreAnalyticsMgr.UserSet(dic, setOnce);
        public static void SetSuperProperties(Dictionary<string, object> superProperties) => CoreAnalyticsMgr.SetSuperProperties(superProperties);
        #endregion

        #region  账号相关

        /// <summary>
        /// 获取当前 userid
        /// </summary>
        public static string GetCurrentUserId() => CorePrefs.UserId;

        /// <summary>
        /// 登录接口
        /// </summary>
        /// <param name="onComplete">bool:是否登录成功，string:登录失败原因，string:登录成功后的userid</param>
        public async static void Login(Action<bool, string, string> onComplete = null) => CoreAccountMgr.Instance.Login(onComplete);

        public static void Logout() => CoreAccountMgr.Instance.Logout();
        #endregion

        #region  广告相关
        public static bool IsRewardedVideoAvailable() => CoreAdsMgr.Instance.IsRewardedVideoAvailable();
        public static void ShowRewardedVideo(string _pos, Action _closeCallback, Action _showFailCallback)
            => CoreAdsMgr.Instance.ShowRewardedVideo(_pos, _closeCallback, _showFailCallback);
        public static void ShowInterstitial(string _adPos, IvType _IvType = IvType.IV1, Action _closeCallback = null, Action<HandCallbackInfo, Action> _action = null)
            => CoreAdsMgr.Instance.ShowInterstitial(_adPos, _IvType, _closeCallback, _action);
        public static bool IsBannerAvailable() => CoreAdsMgr.Instance.IsBannerAvailable();

        /// <summary>
        /// 展示横幅广告
        /// </summary>
        /// <param name="_position">位置</param>
        public static void ShowBanner() => CoreAdsMgr.Instance.ShowBanner();
        public static void HideBanner() => CoreAdsMgr.Instance.HideBanner();

        /// <summary>
        /// 返回当前竞价成功的激励视频价格（eCPM）。仅做竞价查询，不触发展示或频次逻辑。无可用出价时返回 -1。
        /// </summary>
        public static double GetCurrentRewardedVideoBidPrice() => CoreAdsMgr.Instance.GetCurrentRewardedVideoBidPrice();

        /// <summary>
        /// 返回当前可用插屏广告的最高出价（eCPM）。不检查频次规则，无可用出价时返回 -1。
        /// </summary>
        /// <param name="ivType">保留参数，兼容旧调用</param>
        public static double GetCurrentInterstitialBidPrice(IvType ivType = IvType.IV1) => CoreAdsMgr.Instance.GetCurrentInterstitialBidPrice(ivType);

        /// <summary>
        /// 展示插屏广告，不受频次规则控制；有可用插屏则展示，无则直接回调。
        /// </summary>
        /// <param name="_adPos">广告位</param>
        /// <param name="_closeCallback">关闭或未展示时的回调</param>
        public static void ShowInterstitialIgnoreFrequency(string _adPos, Action _closeCallback = null)
            => CoreAdsMgr.Instance.ShowInterstitialIgnoreFrequency(_adPos, _closeCallback);

        /// <summary>
        /// 返回当前竞价成功的原生广告价格（eCPM）。仅做竞价查询，不触发展示逻辑。无可用出价时返回 -1。
        /// </summary>
        public static double GetCurrentNativeBidPrice() => CoreAdsMgr.Instance.GetCurrentNativeBidPrice();

        public static double GetCurrentNativeFullscreenBidPrice() => CoreAdsMgr.Instance.GetCurrentNativeFullscreenBidPrice();

        public static double GetCurrentSmallNativeBidPrice() => CoreAdsMgr.Instance.GetCurrentSmallNativeBidPrice();

        #endregion

        #region  H5 广告事件上报

        /// <summary>
        /// 互动页 H5 广告事件类型，用于 TrackH5AdInteractiveEvent 区分上报。
        /// </summary>
        public enum CoreH5AdInteractiveEventType
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
        public static void TrackH5AdInteractiveEvent(CoreH5AdInteractiveEventType eventType, SpinH5AdType adFormat, CoreH5AdPlatform platform, string adUnitNameUrl, string position, double value = 0, float passTime = 0, string reason = null)
        {
            string formatKey = NormalizeH5AdFormatKey(adFormat.ToString());
            string platformKey = platform.ToString();
            int number = eventType == CoreH5AdInteractiveEventType.AdShow ? GetAndIncrementH5AdImpressionNumber(formatKey, platformKey) : GetH5AdImpressionNumber(formatKey, platformKey);
            switch (eventType)
            {
                case CoreH5AdInteractiveEventType.AdFail:
                    CoreH5AdsEvents.InvokeAdFail(adFormat, platform, adUnitNameUrl, position, reason ?? string.Empty);
                    break;
                case CoreH5AdInteractiveEventType.AdClick:
                    CoreH5AdsEvents.InvokeAdClick(adFormat, platform, adUnitNameUrl, position, number);
                    break;
                case CoreH5AdInteractiveEventType.AdClose:
                    CoreH5AdsEvents.InvokeAdClose(adFormat, platform, adUnitNameUrl, position, number);
                    break;
                case CoreH5AdInteractiveEventType.RewardReach:
                    CoreH5AdsEvents.InvokeRewardReach(adFormat, platform, adUnitNameUrl, position, number, passTime);
                    break;
                case CoreH5AdInteractiveEventType.AdShow:
                    CoreH5AdsEvents.InvokeAdShow(adFormat, platform, adUnitNameUrl, position, number, value);
                    break;
            }
        }

        /// <summary>
        /// 上报 H5 广告 Init 调用。在调用外部 SpinH5Ad.Init(platform) 时同时调用此方法上报。
        /// </summary>
        public static void TrackH5AdInit(CoreH5AdPlatform platform) => CoreH5AdsEvents.InvokeInit(platform);

        /// <summary>
        /// 上报 H5 激励视频展示调用。在调用外部 SpinH5Ad.ShowRewardedAd 时同时调用此方法上报。
        /// </summary>
        public static void TrackH5AdShowRewardedAd(string url, CoreH5AdPlatform platform, int rewardAfter = 15, bool startOnPageLoaded = true)
            => CoreH5AdsEvents.InvokeShowRewardedAd(url, platform, rewardAfter, startOnPageLoaded);

        /// <summary>
        /// 上报 H5 插屏广告展示调用。在调用外部 SpinH5Ad.ShowInterstitialAd 时同时调用此方法上报。
        /// </summary>
        public static void TrackH5AdShowInterstitialAd(string url, CoreH5AdPlatform platform, int closeDelay = 5, bool startOnPageLoaded = true)
            => CoreH5AdsEvents.InvokeShowInterstitialAd(url, platform, closeDelay, startOnPageLoaded);

        /// <summary>
        /// 上报 H5 广告展示（广告收益）。供 H5 广告调用，会同步上报到 Adjust、Firebase、ThinkingData。
        /// currency=USD；format_number/position_number 由 SDK 按格式、点位独立累计（与原生广告共用计数逻辑）。
        /// </summary>
        /// <param name="revenue">广告收益金额（Revenue，USD）</param>
        /// <param name="adUrl">广告链接 URL，对应 AdRevenueUnit、AdRevenuePlacement 及 ad_unit_name</param>
        /// <param name="format">广告形式，如 "interstitial"、"rewarded" 等</param>
        /// <param name="position">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="adPlatform">H5 广告平台枚举</param>
        public static void TrackH5AdImpressionEvent(double revenue, string adUrl, string format, string position, CoreH5AdPlatform adPlatform)
        {
            string platform = adPlatform.ToString();
            string url = adUrl ?? string.Empty;
            string adFormat = string.IsNullOrEmpty(format) ? "unknown" : NormalizeH5AdFormatKey(format);
            // position 为空时用 adFormat 作为默认点位，便于区分不同广告位的 position_number
            string pos = string.IsNullOrEmpty(position) ? adFormat : position;
            int formatNumber = CoreAdsPrefs.GetAndIncrementFormatPlayCount(adFormat);
            int positionNumber = CoreAdsPrefs.GetAndIncrementPositionPlayCount(pos);
            CoreAnalyticsMgr.TrackAdImpressionEvent(revenue, platform, url, url, adFormat, platform, pos, formatNumber, positionNumber);
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
        private static int GetH5AdImpressionNumber(string format, string platformOrPosition) => CorePrefs.GetInt(GetH5AdImpressionKey(format, platformOrPosition), 0);

        /// <summary>
        /// 按 format+platform（SpinGO/Okspin）从本地存储获取并自增展示次数（从 1 开始），结果持久化到 CorePrefs。
        /// </summary>
        private static int GetAndIncrementH5AdImpressionNumber(string format, string platformOrPosition)
        {
            string key = GetH5AdImpressionKey(format, platformOrPosition);
            int count = CorePrefs.GetInt(key, 0) + 1;
            CorePrefs.SetInt(key, count);
            CorePrefs.Save();
            return count;
        }
        #endregion
    }
}

