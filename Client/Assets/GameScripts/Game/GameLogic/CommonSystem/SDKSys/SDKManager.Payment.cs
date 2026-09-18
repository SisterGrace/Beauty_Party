using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TK_Engine;
using UnityEngine;
using UnityEngine.Purchasing;
#if USE_UP2SDK && ENABLE_IAP
using Sky;
#endif

namespace GameLogic
{
    public partial class SDKManager
    {
        #region 支付相关

        /// <summary>
        /// 添加购买项
        /// </summary>
        public void AddProducts(Dictionary<string, ProductType> products)
        {
#if USE_UP2SDK && ENABLE_IAP
            // 与 SDKMgr.cs 保持一致：走 SkySdkMgr.AddProducts（兼容初始化前/后）
            // 原 AddProductsStatic 只会赋值 ProductDic，初始化后调用不会触发 FetchAdditionalProducts
            SkySdkMgr.AddProducts(products);
#endif
        }

        /// <summary>
        /// 点击购买方法
        /// </summary>
        /// <param name="productId"> 商品SKU </param>
        /// <param name="enProductName"> 商品名称 </param>
        /// <param name="gameExtraParam"> 游戏拓展参数 </param>
        public void BuyProduct(string productId, string enProductName, string productDesc = "", string gameExtraParam = "")
        {
#if USE_UP2SDK && ENABLE_IAP
            try
            {
                PayJudgment(productId);
                Log.Info($"在线参数判定的支付方式为：{MyPayType}");
                // 上报内购按钮点击位置2
                LogEvent("Game_IAP_BUTTON_CLICK", "Postion", 2);

                var productInfo = SkySdkMgr.GetProductInfoById(productId);
                if (productInfo == null)
                {
                    Log.Warning($"获取商品信息失败");
                    GameEvent.Send(EventType.SetWaitingUIAndTips, false, "uitips_buy_tips3");
                    GameEvent.Send(EventType.ShowTips, "uitips_buy_tips1", true);
                    return;
                }
                // 上报内购按钮点击位置3
                LogEvent("Game_IAP_BUTTON_CLICK", "Postion", 3);

                var price = productInfo.metadata.localizedPrice;
                var currencyCode = productInfo.metadata.isoCurrencyCode;

                var req = new SkyCreateOrder.Request(
                    enProductName,
                    productId,
                    productDesc,
                    (float)price,
                    1,
                    currencyCode,
                    gameExtraParam,
                    "0");
                Log.Info($"创建了一个订单：{req.product_name}---{req.product_id}---{req.product_desc}---{req.unit_price}---{req.quantity}---{req.game_extra_param}---此时支付方式为{MyPayType}");

                // ============================================================
                // 无服务器模式：
                //   SDK 内部（SkyIapH5Mgr.CreatePaymentOrder）会跳过服务器下单，
                //   直接走 SkyIapNativeMgr.BuyProductByID(NativeApp)，
                //   结果通过 SkySdkMgr.OnPurchaseComplete 全局回调派发。
                //   传 null 回调（不会被触发），useSDKPayment 传 true。
                // ============================================================
                if (!SkyIapH5Mgr.HasServerBackend)
                {
                    Log.Info("[无服务器模式] 交由 SDK 直接发起原生支付");
                    SkySdkMgr.CreatePaymentOrder(req, null, true);
                    return;
                }

                // ============================================================
                // 有服务器模式：
                //   自定义支付方式判定，useSDKPayment = false，
                //   由游戏根据返回的订单信息自行构造 SkyBuy.Request 并调起支付。
                // ============================================================
                SkySdkMgr.CreatePaymentOrder(req, (succeedCreat, info, OrderData) =>
                {
                    try
                    {
                        Log.Info($"创建订单后结果：{succeedCreat}");
                        if (succeedCreat)
                        {
                            Log.Info($"创建订单成功，结果：-------{info}-------订单id{OrderData.order_no}---订单数量：{OrderData.payment_list.Count}");
                            if (MyPayType != OrderData.payment_list[0].type && OrderData.payment_list.Count == 1)
                            {
                                Log.Info("支付方式发生变化，更新MyPayType");
                                Log.Info($"当前在线参数判定的支付方式为{MyPayType}，订单支付方式为{OrderData.payment_list[0].type}");
                                MyPayType = OrderData.payment_list[0].type;
                            }
                            string h5Url = OrderData.payment_list[0].h5_url;
                            var paymentData = OrderData.payment_list.Find(x => x.type == 2);
                            if (paymentData != null)
                            {
                                h5Url = paymentData.h5_url;
                            }
                            Log.Info($"此时支付方式为：{MyPayType},此时所在国为：{PayCountry}，而此时订单信息支付方式:{OrderData.payment_list[0].type}");

                            var payType = MyPayType == 2
                                ? SkyIapH5Mgr.SkyPaymentType.H5
                                : SkyIapH5Mgr.SkyPaymentType.NativeApp;
                            SkyBuy.Request buyRequest = new SkyBuy.Request(req, OrderData.order_no, payType, h5Url);
                            SkySdkMgr.PurchaseProductById(buyRequest);
                        }
                        else
                        {
                            Log.Warning($"创建订单失败，原因：{info}");
                            if (MyPayType == 2)
                            {
                                GetPayDailyTimes++;
                                GetPayTotalTimes++;
                            }
                            GameEvent.Send(EventType.SetWaitingUIAndTips, false, "uitips_buy_tips3");
                            GameEvent.Send(EventType.ShowTips, "uitips_buy_tips1", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"CreatePaymentOrder Callback Error: {ex.Message}And{ex.StackTrace}");
                        GameEvent.Send(EventType.SetWaitingUIAndTips, false, "uitips_buy_tips3");
                        GameEvent.Send(EventType.ShowTips, "uitips_buy_tips1", true);
                        RechargeController.Instance.RemoveOneSendEvent(productId);
                    }
                }, false);
            }
            catch (Exception e)
            {
                Log.Error($"BuyProduct Error: {e.Message}And{e.StackTrace}");
                GameEvent.Send(EventType.SetWaitingUIAndTips, false, "uitips_buy_tips3");
                GameEvent.Send(EventType.ShowTips, "uitips_buy_tips1", true);
                RechargeController.Instance.RemoveOneSendEvent(productId);
                LogEvent("Game_IAP_Failure", "Reason", e.Message);
            }
#else
            // 内购功能未启用 (ENABLE_IAP 未定义) 或 SDK 未接入，走失败兜底
            Log.Warning($"BuyProduct 未启用内购或未接入支付 SDK，productId={productId}");
            GameEvent.Send(EventType.SetWaitingUIAndTips, false, "uitips_buy_tips3");
            GameEvent.Send(EventType.ShowTips, "uitips_buy_tips1", true);
            RechargeController.Instance.RemoveOneSendEvent(productId);
#endif
        }

#if USE_UP2SDK && ENABLE_IAP
        /// <summary>
        /// 支付方式判断，根据在线参数判定走谷歌还是三方支付
        /// </summary>
        private void PayJudgment(string productId)
        {
            try
            {
                GetPayConfig();
                var rechargeInfo = RechargeController.GetRechargeInfo(productId);
                if (rechargeInfo == null)
                {
                    Log.Warning($"PayJudgment: 未找到商品配置信息，productId={productId}，回退为Google支付");
                    MyPayType = 1;
                    return;
                }
                float price = (float)rechargeInfo.Price;
                if (!IsRemovedApp)
                {
                    PayCountry = SkySdkMgr.GetStoreCountryCode();
                    if (string.IsNullOrEmpty(MyPayConfig.country_code))
                    {
                        Log.Warning("PayJudgment: country_code为空，回退为Google支付");
                        MyPayType = 1;
                        return;
                    }
                    if (string.IsNullOrEmpty(PayCountry))
                    {
                        Log.Warning("PayJudgment: GetStoreCountryCode返回空，回退为Google支付");
                        MyPayType = 1;
                        return;
                    }
                    if (MyPayConfig.country_code.Contains(PayCountry) || MyPayConfig.country_code.Contains("All"))
                    {
                        if (price > MyPayConfig.Price)
                        {
                            Log.Info($"当前订价格大于在线参数设定价格，开始判定失败次数");
                            Log.Info($"当前每日失败次数：{GetPayDailyTimes}；当前总失败次数：{GetPayTotalTimes}");
                            if (GetPayDailyTimes < MyPayConfig.Daily_times && GetPayTotalTimes < MyPayConfig.Total_times)
                            {
                                MyPayType = 2;
                                Log.Info($"当前失败次数小于在线参数设置");
                            }
                            else
                            {
                                Log.Info($"当前失败次数大于在线参数设置");
                                MyPayType = 1;
                                if (GetPayDailyTimes >= MyPayConfig.Daily_times)
                                {
                                    LogEvent("IAP_Fail_Swith", "Fail_Reason", " daily_times");
                                }
                                if (GetPayTotalTimes >= MyPayConfig.Total_times)
                                {
                                    LogEvent("IAP_Fail_Swith", "Fail_Reason", " totoal_times");
                                }
                            }
                        }
                        else
                        {
                            MyPayType = 1;
                        }
                    }
                    else
                    {
                        MyPayType = 1;
                    }
                }
                else
                {
                    MyPayType = 2;
                }
            }
            catch (Exception e)
            {
                Log.Error($"PayJudgment Error: {e.Message}\n{e.StackTrace}");
                MyPayType = 1;
            }
        }

        // ============================================================
        // 已删除 PurchaseCallback
        // 原因：该方法从未被 SkySdkMgr.RegisterPurchaseCompletionHandler 注册，
        //       新版 SDKMgr.cs 已经全局注册了购买完成回调（含 LogCheckingOrder、
        //       GiveRewards、关闭 UIIAPLoading 等）。保留只会导致未来接入方
        //       误注册后双倍发奖。
        // ============================================================
#endif

        /// <summary>
        /// 初始化内购回调
        /// </summary>
        public void OnPurchaseInitLinstener(bool result, string errorMsg)
        {
            if (result)
            {
                Log.Info("Iap init success");
            }
            else
            {
                Log.Debug("Iap init fail, errorMsg:" + errorMsg);
            }
        }
        /// <summary>
        /// 购买完成回调（由 SDKManager.Init 注册到 SkySdkMgr.RegisterPurchaseCompletionHandler）
        /// </summary>
        private void PurchaseCallback(SkyPurchase.Result result)
        {
            Debug.LogWarning(
                $"购买结果：{result.create_order_request?.product_id}----{result.purchase_result}-------{result.order_already_exists}---------{UserController.Instance.IsInit}");

            if (result.purchase_result)
            {
                if (result.create_order_request != null)
                {
                    var cfg = RechargeController.Instance.GetCfg(result.create_order_request.product_id);
                    GetIAPTotalCost += (float)cfg.Price;
                    RechargeController.Instance.SendReward(result.create_order_request.product_id);
                }

                SkySdkMgr.LogCheckingOrder(result.query_order_reuslt, (status, info) =>
                {
                    Log.Info($"支付上报服务端结果：{status}-------{info}");
                });
            }
            else
            {
                ReadFailOrderId(true);
                Log.Info($"此时支付失败的方式为：{result.payment_type}");

                if (result.payment_type == SkyIapH5Mgr.SkyPaymentType.H5)
                {
                    GetPayDailyTimes++;
                    GetPayTotalTimes++;
                }
                if (result.create_order_request != null)
                {
                    RechargeController.Instance.RemoveOneSendEvent(result.create_order_request.product_id);
                }
            }

            // 关闭购买等待 UI
            GameEvent.Send(EventType.SetWaitingUIAndTips, false, "uitips_buy_tips3");
        }

        /// <summary>
        /// 补单操作
        /// </summary>
        /// <param name="Resume">是否恢复购买</param>
        public void ReadFailOrderId(bool Resume = false)
        {
#if USE_UP2SDK && ENABLE_IAP
            // 无服务器模式：没有服务端订单接口可查，跳过补单，避免无效请求
            if (!SkyIapH5Mgr.HasServerBackend)
            {
                Log.Info("[无服务器模式] 跳过补单逻辑");
                return;
            }

            var currentUid = SkySdkMgr.GetCurrentUserId();
            Log.Info($"开始补单操作，此时uid：{currentUid}");
            if (string.IsNullOrEmpty(currentUid))
            {
                return;
            }
            SkySdkMgr.QueryOrderList(new SkyQueryOrderList.Request(currentUid, 1, 0), (result, msg, orderList) =>
            {
                Log.Debug("QueryOrderList result:" + result);
                if (!result)
                {
                    Log.Warning($"补单操作失败，原因:{msg}");
                    return;
                }
                Log.Info($"此时补单的商品列表：{JsonUtility.ToJson(orderList)}");
                if (!Resume)
                {
                    Log.Info($"游戏初始化进行补单操作，此时补单数量：{orderList.order_list.Count}");
                    foreach (var item in orderList.order_list)
                    {
                        Log.Info($"游戏初始化进行补单操作，此时补单商品id:{item.product_id}");
                        ProcessOrderItem(item, true);
                    }
                }
                else
                {
                    bool isMulti_orde = orderList.order_list.Count > 0;
                    Log.Info($"游戏内部进行补单操作，此时补单数量：{orderList.order_list.Count}");
                    foreach (var item in orderList.order_list)
                    {
                        if (isMulti_orde)
                        {
                            RechargeController.Instance.RemoveOneSendEvent(item.product_id);
                        }
                        Log.Info($"游戏内部进行补单操作，此时补单商品id:{item.product_id}");
                        ProcessOrderItem(item, isMulti_orde);
                    }
                    if (isMulti_orde)
                    {
                        GameEvent.Send(EventType.RefreshLife);
                        GameEvent.Send(EventType.RefreshCurrency, 0);
                        GameEvent.Send(EventType.ReadFailOrder);
                        GameEvent.Send(EventType.ShowTips, "uitips_buy_tips4", true);
                    }
                    else
                    {
                        GameEvent.Send(EventType.ShowTips, "uitips_buy_tips1", true);
                    }
                }
            });
#endif
        }

#if USE_UP2SDK && ENABLE_IAP
        /// <summary>
        /// 处理单个补单订单项：累加 IAP 金额、发放奖励、上报服务端
        /// </summary>
        private void ProcessOrderItem(SkyQueryOrder.ResponseData item, bool skipAnimation)
        {
            var recharge = RechargeController.GetRechargeInfo(item.product_id);
            GetIAPTotalCost += (float)recharge.Price;
            RechargeController.Instance.SendReward(item.product_id, skipAnimation, false);

            SkyQueryOrder.Result req = new SkyQueryOrder.Result(
                item.order_no,
                item.pay_status,
                item.shipping_status,
                item.transaction_id,
                item.pay_success_time,
                item.game_extra_param,
                item.product_name,
                item.product_id,
                item.price,
                item.currency,
                item.quantity,
                item.is_first_pay,
                item.payment_method);
            SkySdkMgr.LogCheckingOrder(req, (status, info) =>
            {
                Log.Info($"支付上报服务端结果：{status}-------{info}");
            });
        }
#endif

        #endregion

        #region 商品价格

        /// <summary>
        /// 获取商品价格，商品价格本地化
        /// </summary>
        public static string GetProductPrice(string productId)
        {
#if UNITY_EDITOR
            return GetFallbackPrice(productId);
#endif
#if USE_UP2SDK && ENABLE_IAP
            if (Instance.IsRemovedApp)
            {
                if (Instance._productListData != null)
                {
                    var tdata = Instance._productListData.items.Find(x => x.product_id.Contains(productId));
                    if (tdata != null)
                        return $"{tdata.currency}{tdata.price.ToString("F2")}";
                }
                return GetFallbackPrice(productId);
            }
            var price = SkySdkMgr.GetLocalizedPriceById(productId);
            return string.IsNullOrEmpty(price) ? GetFallbackPrice(productId) : price;
#else
            return GetFallbackPrice(productId);
#endif
        }

        private static string GetFallbackPrice(string productId)
        {
            var cfg = RechargeController.Instance.GetCfg(productId);
            return $"${cfg.Price}";
        }

        /// <summary>
        /// 获取价格列表，用于下架后还能价格本地化
        /// </summary>
        public void GetProductListData()
        {
#if USE_UP2SDK && ENABLE_IAP
            SkySdkMgr.GetProductList((result, msg, productList) =>
            {
                Log.Debug("GetProductList result:" + result + " msg:" + msg + " productList:" + JsonUtility.ToJson(productList));
                if (result)
                {
                    _productListData = productList;
                    foreach (var product in productList.items)
                    {
                        Log.Debug("product id:" + product.product_id + " product name:" + product.product_name + " product price:" + product.price + " product desc:" + product.product_desc + " currency:" + product.currency);
                    }
                }
            });
#endif
        }

        /// <summary>
        /// 获取商品信息
        /// </summary>
        public static Product GetProductById(string productId)
        {
#if USE_UP2SDK && ENABLE_IAP
            var productInfo = SkySdkMgr.GetProductInfoById(productId);
            return productInfo;
#else
            return null;
#endif
        }

        #endregion

        #region 支付在线参数

        /// <summary>
        /// 获取支付在线参数
        /// </summary>
        public void GetPayConfig()
        {
            string defaultConfig =
                "{\"country_code\":\"usd\",\"price\":\"0.99\",\"daily_times\":\"2\",\"total_times\":\"5\"}";
            string jsonString = GetRemoteConfigString("zhifu_times", defaultConfig);
            Log.Info($"当前支付在线参数：{jsonString}");
            if (string.IsNullOrEmpty(jsonString))
            {
                Log.Info("当前支付在线参数为空，使用默认配置");
            }
            MyPayConfig = ParsePayConfig(string.IsNullOrEmpty(jsonString) ? defaultConfig : jsonString);
            Log.Info($"当前支付信息结构体{JsonConvert.SerializeObject(MyPayConfig)}");
        }

        public static PayConfig ParsePayConfig(string jsonString)
        {
            PayConfig config = JsonConvert.DeserializeObject<PayConfig>(jsonString);
            return config;
        }

        /// <summary>
        /// 支付逻辑结构体
        /// </summary>
        public struct PayConfig
        {
            private const float DefaultPrice = 0.99f;
            private const int DefaultDailyTimes = 2;
            private const int DefaultTotalTimes = 5;

            public string country_code;
            public string price;
            public float Price => float.TryParse(price, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : DefaultPrice;
            public string daily_times;
            public int Daily_times => int.TryParse(daily_times, out var v) ? v : DefaultDailyTimes;
            public string total_times;
            public int Total_times => int.TryParse(total_times, out var v) ? v : DefaultTotalTimes;

            public PayConfig(string _countryCode, string _price, string _dailyTimes, string _totalTimes)
            {
                country_code = _countryCode;
                this.price = _price;
                this.daily_times = _dailyTimes;
                this.total_times = _totalTimes;
            }
        }

        #endregion
    }
}