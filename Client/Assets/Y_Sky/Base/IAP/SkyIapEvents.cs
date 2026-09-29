using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{

    public static class SkyIapEvents
    {
        public static void IAPButtonShow(string productId, string productName, Dictionary<string, object> eventData = null)
        {
            eventData ??= new Dictionary<string, object>();
			var currency = "";
			var price = "";
#if IAP_ENABLE
			var productInfo = SkySdkMgr.GetProductInfoById(productId);
            if (productInfo != null)
            {
                currency = productInfo.metadata.isoCurrencyCode;
                price = productInfo.metadata.localizedPriceString;
            }
#endif
          
            

            if (!float.TryParse(price, out var fPrice))
            {
                fPrice = 0.0f;
            }

            eventData.AddIfNotExists("IAP", productName);
            eventData.AddIfNotExists("product_id", productId);
            eventData.AddIfNotExists("Currency", currency);
            eventData.AddIfNotExists("Price", fPrice);
            SkyAnalyticsMgr.TrackEvent("IAP_Button_Show", eventData);
        }
        // 创建订单上报
        public static void IAPCreateOrder(string productName, string productDesc,string productID, string currency, string price, string gameExtra)
        {
            SkyAnalyticsMgr.TrackEvent("IAP_Create_Order_Start", new Dictionary<string, object>
            {
                { "IAP", productName ?? "" },
                { "product_id", productID ?? "" },
                { "product_desc", productDesc ?? "" },
                { "currency", currency ?? "" },
                { "price", price ?? "" },
                { "game_extra", gameExtra ?? "" },
                { "user_id",SkyPrefs.UserId},
                { "game_user_id", SkyPrefs.GameUserId}
            });
        }

        // 创建订单结果上报
        public static void IAPCreateOrderResult(string productName, string productDesc, string productID, string currency, string price, string gameExtra, bool success, string msg, string resCode)
        {
            SkyAnalyticsMgr.TrackEvent(success ? "IAP_Create_Order_Success" : "IAP_Create_Order_Fail", new Dictionary<string, object>
            {
                { "IAP", productName ?? "" },
                { "product_id", productID ?? "" },
                { "product_desc", productDesc ?? "" },
                { "currency", currency ?? "" },
                { "price", price ?? "" },
                { "game_extra", gameExtra ?? "" },
                { "user_id",SkyPrefs.UserId},
                { "game_user_id", SkyPrefs.GameUserId},
                { "result_msg",msg ?? "" },
                { "result_code", resCode ?? "" }
            });
        }

        // 开始查询订单
        public static void IAPQueryOrder(string productName, string productDesc, string productID, string currency, string price, string gameExtra, string orderId)
        {
            SkyAnalyticsMgr.TrackEvent("IAP_Query_Order_Start", new Dictionary<string, object>
            {
                { "IAP", productName ?? "" },
                { "product_id", productID ?? "" },
                { "product_desc", productDesc ?? "" },
                { "currency", currency ?? "" },
                { "price", price ?? "" },
                { "game_extra", gameExtra ?? "" },
                { "user_id",SkyPrefs.UserId},
                { "game_user_id", SkyPrefs.GameUserId},
                { "order_id", orderId ?? "" }
            });
        }

        // 查询订单结果
        public static void IAPQueryOrderResult(string productName, string productDesc, string productID, string currency, string price, string gameExtra, string orderId, bool success, string msg, string resCode, int tryTime)
        {
            SkyAnalyticsMgr.TrackEvent(success ? "IAP_Query_Order_Success" : "IAP_Query_Order_Fail", new Dictionary<string, object>
            {
                { "IAP", productName ?? "" },
                { "product_id", productID ?? "" },
                { "product_desc", productDesc ?? "" },
                { "currency", currency ?? "" },
                { "price", price ?? "" },
                { "game_extra", gameExtra ?? "" },
                { "user_id",SkyPrefs.UserId},
                { "game_user_id", SkyPrefs.GameUserId},
                { "order_id", orderId ?? "" },
                { "result_msg",msg ?? "" },
                { "result_code", resCode ?? "" },
                { "try_time", tryTime }
            });
        }


        // 用户发放奖励时上报
        public static void IAPCheckingOrder(SkyQueryOrder.Result req)
        {
            SkyAnalyticsMgr.TrackEvent("IAP_Checking_Order", new Dictionary<string, object>
            {
                { "payment_method", req.payment_method},
                { "IAP", req.product_name },
                { "product_id", req.product_id },
                { "Currency", req.currency },
                { "Price", req.price },
                { "order_no", req.order_no},
                { "is_first", req.is_first_pay},
                { "product_type", "Consumable" },
                { "game_extra", req.game_extra_param },
                { "order_id",req.transaction_id},
                { "user_id",SkyPrefs.UserId},
                { "game_user_id", SkyPrefs.GameUserId},
            });
        }

        public static void IAPBtnShow(string productName, string productID, string currency, string price)
        {
            float fPrice;
            if (!float.TryParse(price, out fPrice))
            {
                fPrice = 0.0f;
            }

            SkyAnalyticsMgr.TrackEvent("IAP_Button_Show", new Dictionary<string, object>
            {
                { "IAP", productName },
                { "product_id", productID },
                { "Currency", currency },
                { "Price", fPrice }
            });
        }

        public static void IAPBtnClick(string eventName, string productName, string productID, string currency, string price,
            string gameExtra)
        {
            float fPrice;
            if (!float.TryParse(price, out fPrice))
            {
                fPrice = 0.0f;
            }

            var args = new Dictionary<string, object>
            {
                { "IAP", productName ?? "" },
                { "product_id", productID ?? "" },
                { "Currency", currency?? "" },
                { "Price", fPrice },
                { "game_extra", gameExtra ?? "" },
                { "payment_method", GetPaymentPlatform() }
            };
            
            SkyAnalyticsMgr.TrackEvent(eventName, args);
        }

        public static void IAPSuccess(string productName, string productID, string orderNo,string transactionId,
            string currency, string price, string gameExtra, 
            string productType, string environment,bool orderAlreadyExists,
            string paymentMethod)
        {
            // 总支付次数
            int payTimes = SkyPrefs.GetInt("PAY_TIME", 0);
            if (!orderAlreadyExists)
            {
               payTimes += 1;
            }
            float fPrice;
            if (!float.TryParse(price, out fPrice))
            {
                fPrice = 0.0f;
            }
            var args = new Dictionary<string, object>
            {
                // 新增
                { "payment_type",paymentMethod},
                { "order_id", transactionId },
                { "IAP", productName },
                { "ID", productID },
                { "Currency", currency },
                { "Price", fPrice },
                { "order_no", orderNo },
                { "game_extra", gameExtra ?? "" },
                { "is_first", SkyPrefs.GetInt("First_purchase", 0) == 0 },
                { "product_type", productType },
                { "orderAlreadyExists", orderAlreadyExists },
            };
            if (string.IsNullOrEmpty(environment)) environment = "";
            SkyAnalyticsMgr.TrackEvent("IAP_Success", args);

            // 首次订单 设置支付次数/累计支付次数/累计支付金额
            if (!orderAlreadyExists)
            {
                fPrice = fPrice + SkyPrefs.GetFloat("PURCHASE_PRICE", 0.0f);
                SkyPrefs.SetFloat("PURCHASE_PRICE", fPrice);

                SkyAnalyticsMgr.UserSet(new Dictionary<string, object>()
                {
                    { "user_total_pay_amount", fPrice },
                    { "user_total_pay_times", payTimes }
                });

                SkyAnalyticsMgr.SetSuperProperties(new Dictionary<string, object>()
                {
                    { "pay_times", payTimes }
                });

                SkyPrefs.SetInt("First_purchase", 1);
                 SkyAnalyticsMgr.UserSet(new Dictionary<string, object>()
                {
                    { "user_first_pay_time", DateTime.Now }
                },true);

            }

            SkyPrefs.SetInt("PAY_TIME", payTimes);
        }

        public static void IAPFail(string productName, string productID, string orderID, 
            string orderNo,string currency, string price,
            string gameExtra, string failReason,string paymentMethod,int retryCount)
        {
            float fPrice;
            if (!float.TryParse(price, out fPrice))
            {
                fPrice = 0.0f;
            }

            SkyAnalyticsMgr.TrackEvent("IAP_Fail", new Dictionary<string, object>
            {
                { "payment_type",paymentMethod},
                { "IAP", productName },
                { "ID", productID },
                { "currency_type", currency },
                { "pay_amount", fPrice },
                { "order_id", orderID },
                { "order_No",orderNo},
                { "game_extra", gameExtra ?? ""},
                { "fail_reason", failReason },
                { "retryCount", retryCount },
            });
        }

        // 客户端内购完成上报
        public static void IAPClientFinish(string productName, string productID, string orderID, 
            string orderNo,string currency, string price, 
            string gameExtra, string productType)
        {
            float fPrice;
            if (!float.TryParse(price, out fPrice))
            {
                fPrice = 0.0f;
            }
            int payTimes = SkyPrefs.GetInt("PAY_TIME", 0) + 1;
            SkyAnalyticsMgr.TrackEvent("Client_Finish_Channel", new Dictionary<string, object>
            {
                { "IAP", productName },
                { "product_id", productID },
                { "Currency", currency },
                { "Price", fPrice },
                { "game_extra", gameExtra ?? "" },
                { "order_id", orderID },
                { "is_first", SkyPrefs.GetInt("First_purchase", 0) == 0 },
                { "payment_method", GetPaymentPlatform() },
                { "product_type", productType },
                { "order_no",orderNo},
                { "payment_num", payTimes },
            });
        }

        // 上报缓存失败订单
        public static void IAPCacheOrder(Dictionary<string, string> args)
        {
            float fPrice;
            if (!float.TryParse(args["price"], out fPrice))
            {
                fPrice = 0.0f;
            }

            SkyAnalyticsMgr.TrackEvent("IAP_Cache_Order", new Dictionary<string, object>
            {
                { "price", fPrice },
                { "product_id", args["product_id"] },
                { "iap_name", args["iap_name"] },
                { "purchase_token", args["purchase_token"] },
                { "order_id", args["order_id"] },
                { "order_no", args["order_no"] },
                { "currency", args["currency"] },
                { "payment_method", GetPaymentPlatform() },
                { "product_type", args["product_type"] }
            });
        }

        // sdk 添加订单时上报
        public static void SDKSaveOrder(Dictionary<string, string> args)
        {
            float fPrice;
            if (!float.TryParse(args["price"], out fPrice))
            {
                fPrice = 0.0f;
            }

            SkyAnalyticsMgr.TrackEvent("IAP_SDK_Save_Order", new Dictionary<string, object>
            {
                { "price", fPrice },
                { "product_id", args["product_id"] },
                { "iap_name", args["iap_name"] },
                { "purchase_token", args["purchase_token"] },
                { "order_id", args["order_id"] },
                { "order_no", args["order_no"] },
                { "currency", args["currency"] },
                { "payment_method", GetPaymentPlatform() },
                { "product_type", args["product_type"] }
            });
        }

        // sdk 移除订单时上报
        public static void SDKRemoveOrder(Dictionary<string, string> args)
        {
            float fPrice;
            if (!float.TryParse(args["price"], out fPrice))
            {
                fPrice = 0.0f;
            }

            SkyAnalyticsMgr.TrackEvent("IAP_SDK_Remove_Order", new Dictionary<string, object>
            {
                { "price", fPrice },
                { "product_id", args["product_id"] },
                { "iap_name", args["iap_name"] },
                { "purchase_token", args["purchase_token"] },
                { "order_id", args["order_id"] },
                { "order_no", args["order_no"] },
                { "currency", args["currency"] },
                { "payment_method", GetPaymentPlatform() },
                { "product_type", args["product_type"] }
            });
        }

        private static string GetPaymentPlatform()
        {
#if UNITY_IOS
            return "Appstore";
#elif UNITY_ANDROID
            return "GooglePlay";
#else
            return "Other";
#endif
        }

    }
}

