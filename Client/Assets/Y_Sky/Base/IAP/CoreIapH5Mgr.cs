using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CoreLite.CoreLiteCreateOrder;
using static CoreLite.CoreProductListRequest;
using static CoreLite.CoreLiteQueryOrder;

namespace CoreLite
{
    public class CoreIapH5Mgr : CoreSingleton<CoreIapH5Mgr>
    {
        [System.Serializable]
        public class RewardReportResponse
        {
            public string content;
        }

        public enum CoreLitePaymentType
        {
            None,          // 未知支付方式
            NativeApp = 1,  // 原生APP支付
            H5 = 2          // H5支付
        }
        public bool purchasePending = false; // 是否跳出应用支付
        public string currentOrderId; // 当前订单号
        public CoreLiteCreateOrder.Request currentOrderReq; //当前订单信息
        private int currentRetryCount = 0; // 当前重试次数
        private int maxRetryCount; // 最大重试次数
        private bool isQuerying = false; // 是否正在查询订单状态
        private float retryInterval = 2f; // 重试间隔秒数
        public string storeCountryCode = "";

        /// <summary>
        /// 获取Android商店代码
        /// </summary>
        public void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            storeCountryCode = "US";
            try
            {
                var country = new CoreBillingCountryFetcher();
                country.Init();
            }
            catch
            {
                
            }
            
#elif UNITY_IOS && !UNITY_EDITOR
            storeCountryCode = CoreIapIosOrderMgr.StoreCountryCode();
#endif
        }

        public void GetProductList(Action<bool, string, CoreProductListRequest.ProductListData> callback)
        {
            var args = new Dictionary<string, object>()
            {
                ["country_code"] = storeCountryCode
            };
            CoreServerMgr.Instance.ProductList(args,(code, message, data) =>
            {
                callback(code == 0, message, data);
            });
        }
        
        public void ReportIap()
        {
            
        }

        // 创建订单
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback">如果不使用SDK处理支付时，可根据回调信息自行处理</param>
        /// <param name="useSDKPayment">是否使用SDK处理支付逻辑，不使用时可根据回调自行处理</param>
        public void CreatePaymentOrder(CoreLiteCreateOrder.Request request, Action<bool, string, CoreLiteCreateOrder.ResponseData> callback = null, bool useSDKPayment = true)
        {
            
            CoreIapEvents.IAPBtnClick("IAP_Button_Click", 
                    request.product_name, 
                    request.product_id, 
                    request.currency_code, 
                    request.unit_price.ToString(), 
                    request.game_extra_param);
                    
            var args = new Dictionary<string, object>()
            {
                ["product_name"] = request.product_name,
                ["product_id"] = request.product_id,
                ["product_desc"] = request.product_desc,
                ["product_type"] = request.product_type,
                ["unit_price"] = request.unit_price,
                ["quantity"] = request.quantity,
                ["order_type"] = request.order_type,
                ["currency"] = request.currency_code,
                ["game_account_country"] = request.game_account_country,
                ["game_extra_param"] = request.game_extra_param,
                ["game_server_channel"] = request.game_server_channel,
                ["game_user_id"] = request.game_user_id
            };
            currentOrderReq = request;
            CoreServerMgr.Instance.CreatePaymentOrder(args, (code, message, data) =>
            {
                CoreLogUtils.Debug("iaph5mgr" + "CreatePaymentOrder code:" + code+ " msg:" + message + " data:" + JsonUtility.ToJson(data)+ "useSDKPayment:"+useSDKPayment);
                currentOrderId = data.order_no;
                ProcessPaymentOrderResult(request, callback, useSDKPayment, code, data,message);
            });
        }
        
        /// <summary>
        /// 订单奖励下发上报接口
        /// </summary>
        /// <param name="req">订单ID，用户ID</param>
        /// <param name="callback">回调: int int == 0 成功/失败; msg: 错误信息</param>
        public void ReportOrderReward(CoreLiteQueryOrder.Request req, Action<int, string> callback)
        {
            var args = new Dictionary<string, object>()
            {
                { "order_no", req.order_id },
                { "user_id", req.user_id }
            };

            CoreServerMgr.Instance.ReportOrderReward(args, (code, msg, content) =>
            {
                callback?.Invoke(code, msg);
            });
        }

        /// <summary>
        /// 订单查询接口
        /// </summary>
        /// <param name="req">订单ID，用户ID</param>
        /// <param name="callback">回调: bool 成功/失败; msg: 错误信息; ResponseData: 订单查询结果</param>
        private void QueryOrder(CoreLiteQueryOrder.Request req, Action<bool, string, CoreLiteQueryOrder.ResponseData> callback)
        {
            var args = new Dictionary<string, object>()
            {
                { "order_no", req.order_id },
                { "user_id", req.user_id }
            };

            CoreServerMgr.Instance.QueryOrder(args, (code, msg, order) =>
            {
                callback?.Invoke(code == 0, msg, order);
            });
        }

        /// <summary>
        /// 订单列表接口
        /// </summary>
        /// <param name="req">用户ID，支付状态，发货状态</param>
        /// <param name="callback">回调: bool 成功/失败; msg: 错误信息; List<OrderListData>: 订单列表</param>
        public void QueryOrderList(CoreLiteQueryOrderList.Request req, Action<bool, string, CoreLiteQueryOrder.OrderListData> callback)
        {
            var args = new Dictionary<string, object>()
            {
                { "user_id", req.user_id },
                { "pay_status", req.pay_status },
                { "shipping_status", req.shipping_status }
            };
            CoreServerMgr.Instance.QueryOrderList(args, (code, msg, orderList) =>
            {
                callback?.Invoke(code == 0, msg, orderList);
            });
        }

        private void ProcessPaymentOrderResult(CoreLiteCreateOrder.Request req, Action<bool, string, CoreLiteCreateOrder.ResponseData> callback, bool useSDKPayment, int code, CoreLiteCreateOrder.ResponseData data,string errorMsg)
        {
            // 游戏处理支付内容
            if (!useSDKPayment)
            {
                callback?.Invoke(code == 0, errorMsg, data);
                return;
            }
            // SDK处理支付内容
            if (code != 0 || data == null || data.payment_list == null || data.payment_list.Count == 0 || string.IsNullOrEmpty(data.order_no))
            {
                var res = new CoreLitePurchase.Result(req,CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.None);
                // 创建订单失败 
                CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
                return;
            }

            // 创建订单成功
            // 是走h5支付还是原生app支付
            PaymentInfo h5Payment = data.payment_list.FirstOrDefault(payment => payment.type == (int)CoreLitePaymentType.H5);

            if (h5Payment != null && !string.IsNullOrEmpty(h5Payment.h5_url))
            {
                // 走h5支付
                // 打开h5支付页面
                purchasePending = true;
                
                Application.OpenURL(h5Payment.h5_url);
            }
            else
            {
#if IAP_ENABLE
                // 走原生app支付
                CoreIapNativeMgr.Instance.BuyProductByID(new CoreLiteBuy.Request(req,currentOrderId,CoreLitePaymentType.NativeApp,""));
#endif
            }
        }

        #region 处理回到游戏时，校验订单
        void OnApplicationFocus(bool focus)
        {
            if (focus && purchasePending && !string.IsNullOrEmpty(currentOrderId))
            {
                // 校验订单
                StartOrderStatusCheck();
            }
            else if (!focus)
            {
                // 游戏切后台，停止订单校验
                StopOrderStatusCheck();
            }
        }

        /// <summary>
        /// 开始订单状态检查
        /// </summary>
        public void StartOrderStatusCheck()
        {
            if (isQuerying)
            {
                CoreLogUtils.Debug("iaph5mgr" + "订单查询正在进行中，跳过本次检查");
                return;
            }

            maxRetryCount = CoreFirebaseMgr.Instance.GetConfigInt("Purchase_MaxRetryCount", 5);
            currentRetryCount = 0;
            isQuerying = true;

            CoreLogUtils.Debug("iaph5mgr" + $"开始订单状态检查，订单号: {currentOrderId}, 最大重试次数: {maxRetryCount}");
            // 开始第一次查询
            QueryOrderStatus();
        }

        /// <summary>
        /// 查询订单状态
        /// </summary>
        private void QueryOrderStatus()
        {
            currentRetryCount++;
            CoreLogUtils.Debug("iaph5mgr" + $"第 {currentRetryCount} 次查询订单状态，订单号: {currentOrderId}");
            QueryOrder(new CoreLiteQueryOrder.Request(currentOrderId, CorePrefs.UserId), OnOrderQueryResult);
        }

        /// <summary>
        /// 订单查询结果回调
        /// </summary>
        private void OnOrderQueryResult(bool success, string message, CoreLiteQueryOrder.ResponseData orderData)
        {
            if (success && orderData != null)
            {
                // 查询成功，处理订单状态
                ProcessOrderQuerySuccess(orderData);
            }
            else
            {
                // 查询失败，处理重试逻辑
                ProcessOrderQueryFailure(message);
            }
        }

        /// <summary>
        /// 处理订单查询成功
        /// </summary>
        private void ProcessOrderQuerySuccess(CoreLiteQueryOrder.ResponseData orderData)
        {
            CoreLogUtils.Debug("iaph5mgr" + $"订单查询成功，订单号: {orderData.order_no}, 支付状态: {orderData.pay_status}, 发货状态: {orderData.shipping_status}，支付类型：{orderData.payment_method}");

            // 检查订单支付状态
            if (orderData.pay_status == CoreLiteQueryOrder.PayStatus.Success && orderData.shipping_status == CoreLiteQueryOrder.ShippingStatus.Pending) // 支付成功 & 未发货
            {
                // 支付成功处理
                HandlePaymentSuccess(orderData);
            }
            else 
            {
                // 未支付，继续重试（如果还有重试次数）
                if (currentRetryCount < maxRetryCount)
                {
                    CoreLogUtils.Debug("iaph5mgr" + $"订单未支付，{retryInterval}秒后重试...");
                    Invoke(nameof(QueryOrderStatus), retryInterval);
                }
                else
                {
                    // 重试次数用尽，订单仍为未支付状态
                    HandlePaymentFailure(orderData);
                }
            }
        }

        /// <summary>
        /// 处理订单查询失败
        /// </summary>
        private void ProcessOrderQueryFailure(string errorMessage)
        {
            CoreLogUtils.Debug("iaph5mgr" + $"订单查询失败: {errorMessage}");

            if (currentRetryCount < maxRetryCount)
            {
                CoreLogUtils.Debug("iaph5mgr" + $"查询失败，{retryInterval}秒后第 {currentRetryCount + 1} 次重试...");
                Invoke(nameof(QueryOrderStatus), retryInterval);
                CoreIapEvents.IAPFail(currentOrderReq?.product_name, 
                    currentOrderReq?.product_id, 
                    currentOrderId?? "", 
                    "",
                    currentOrderReq?.currency_code ?? "", 
                    currentOrderReq?.unit_price.ToString() ?? "", 
                    currentOrderReq?.game_extra_param ?? "", 
                    "订单查询失败", 
                    "stripe_h5", 
                    currentRetryCount);
            }
            else
            {
                // 重试次数用尽，查询仍然失败
                CoreLogUtils.Debug("iaph5mgr" + $"订单查询重试{maxRetryCount}次均失败");
                HandleQueryFailure(errorMessage);
            }
        }

        /// <summary>
        /// 处理支付成功
        /// </summary>
        private void HandlePaymentSuccess(CoreLiteQueryOrder.ResponseData orderData)
        {
            CoreLogUtils.Debug("iaph5mgr" + "支付成功处理开始"+JsonUtility.ToJson(orderData));
            CoreIapEvents.IAPClientFinish(orderData.product_name, 
                orderData.product_id, 
                orderData.order_no, 
                orderData.transaction_id, 
                orderData.currency, 
                orderData.price.ToString(), 
                orderData.game_extra_param,
                "stripe_h5");
            // 重置状态
            MarkPurchasePendingCleared();
            isQuerying = false;
            currentRetryCount = 0;

            var queryResult = new CoreLiteQueryOrder.Result(orderData.order_no, orderData.pay_status, orderData.shipping_status,
                orderData.product_name, orderData.transaction_id, orderData.pay_success_time,
                orderData.game_extra_param, orderData.product_id, orderData.price, orderData.currency, orderData.quantity,
                orderData.is_first_pay, orderData.payment_method);

            var res = new CoreLitePurchase.Result(currentOrderReq,queryResult,true,false,orderData.payment_method.Contains("stripe") ? CoreLitePaymentType.H5 : CoreLitePaymentType.NativeApp);
            CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
            CoreIapEvents.IAPSuccess(orderData?.product_name, 
                orderData?.product_id, 
                orderData?.order_no,
                orderData?.transaction_id,
                orderData?.currency,
                orderData?.price.ToString(),
                orderData?.game_extra_param,
                "consumable",
                "production",
                false,
                "stripe_h5");

            currentOrderId = "";
            currentOrderReq = null;

            CoreLogUtils.Debug("iaph5mgr" + "支付成功处理完成");
        }

        private void HandlePaymentFailure(CoreLiteQueryOrder.ResponseData orderData)
        {
            CoreLogUtils.Debug("iaph5mgr" + $"支付失败，状态: {orderData.pay_status}");
            CoreIapEvents.IAPFail(currentOrderReq?.product_name, 
                currentOrderReq?.product_id, 
                currentOrderId?? "", 
                "",
                currentOrderReq?.currency_code ?? "", 
                currentOrderReq?.unit_price.ToString() ?? "", 
                currentOrderReq?.game_extra_param ?? "", 
                $"支付失败，状态: {orderData.pay_status}", 
                "stripe_h5", 
                currentRetryCount);

            // 重置状态
            MarkPurchasePendingCleared();
            isQuerying = false;
            currentRetryCount = 0;

            var res = new CoreLitePurchase.Result(currentOrderReq,CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.H5);
            // 支付失败处理
            CoreSdkMgr.OnPurchaseComplete?.Invoke(res);

            currentOrderId = "";
            currentOrderReq = null;
        }

        /// <summary>
        /// 处理查询失败
        /// </summary>
        private void HandleQueryFailure(string errorMessage)
        {
            CoreLogUtils.Debug("iaph5mgr" + "订单查询完全失败");
            CoreIapEvents.IAPFail(currentOrderReq?.product_name, 
                currentOrderReq?.product_id, 
                currentOrderId?? "", 
                "",
                currentOrderReq?.currency_code ?? "", 
                currentOrderReq?.unit_price.ToString() ?? "", 
                currentOrderReq?.game_extra_param ?? "", 
                "订单查询失败", 
                "stripe_h5", 
                currentRetryCount);

            // 重置状态
            MarkPurchasePendingCleared();
            isQuerying = false;
            currentRetryCount = 0;
            var res = new CoreLitePurchase.Result(currentOrderReq,CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.H5);
            CoreSdkMgr.OnPurchaseComplete?.Invoke(res);

            currentOrderId = "";
            currentOrderReq = null;
            

        }

        /// <summary>
        /// 手动停止订单查询
        /// </summary>
        public void StopOrderStatusCheck()
        {
            // CoreLogUtils.Debug("iaph5mgr" + "手动停止订单状态检查");

            // 取消所有延迟调用
            CancelInvoke(nameof(QueryOrderStatus));

            // 重置状态
            isQuerying = false;
            currentRetryCount = 0;
        }
        
        /// <summary>
        /// 强制重置支付状态（用于异常情况恢复）
        /// </summary>
        public void ResetPaymentState()
        {
            CoreLogUtils.Debug("iaph5mgr" + "强制重置支付状态");

            StopOrderStatusCheck();
            
            MarkPurchasePendingCleared();
            currentOrderId = "";
            currentRetryCount = 0;
            currentOrderReq = null;
        }

        /// <summary>结束「跳出支付」态时调用，用于记录时间以供热开等逻辑做短暂避让。</summary>
        private void MarkPurchasePendingCleared()
        {
            purchasePending = false;
        }

        public CoreLiteQueryOrder.Result CreateFailQueryResultEntity(CoreLiteCreateOrder.Request request = null)
        {
            if(request != null)
            {
                return  new CoreLiteQueryOrder.Result("",PayStatus.All,ShippingStatus.All,
                    "","",request.product_name ?? "",
                    request.game_extra_param ?? "",request.product_id ?? "",
                    request.unit_price,request.currency_code ?? "",1,false,"");
            }else
            {
                if(currentOrderReq == null) currentOrderReq = new CoreLiteCreateOrder.Request("","","",0,1,"","","");
                return new CoreLiteQueryOrder.Result("",PayStatus.All,ShippingStatus.All,
                    "","",currentOrderReq.product_name ?? "",
                    currentOrderReq.game_extra_param ?? "",currentOrderReq.product_id ?? "",
                    0,currentOrderReq.currency_code ?? "",1,false,"");
            }
        }
        #endregion
    }
}

