using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CoreLite
{
    public class CoreLiteQueryOrder
    {
        [System.Serializable]
        public class Request
        {
            public string order_id;
            public string user_id;

            public Request(string orderId, string userId)
            {
                order_id = orderId;
                user_id = userId;
            }
        }

        [System.Serializable]
        public class Result
        {
            public string order_no;
            public PayStatus pay_status;
            public ShippingStatus shipping_status;
            public string transaction_id;
            public string pay_success_time;
            public string game_extra_param;
            public string product_name;
            public string product_id;
            public float price;
            public string currency;
            public int quantity;
            public bool is_first_pay;
            public string payment_method;

            public Result(string orderNo, PayStatus payStatus, ShippingStatus shippingStatus, string transactionId, string paySuccessTime, string gameExtraParam, string productName, string productId, float price, string currency, int quantity, bool isFirstPay, string paymentMethod)
            {
                order_no = orderNo;
                pay_status = payStatus;
                shipping_status = shippingStatus;
                transaction_id = transactionId;
                pay_success_time = paySuccessTime;
                game_extra_param = gameExtraParam;
                product_name = productName;
                product_id = productId;
                this.price = price;
                this.currency = currency;
                this.quantity = quantity;
                is_first_pay = isFirstPay;
                payment_method = paymentMethod;
            }
        }
        

        public enum PayStatus
        {
            All = -1,       // 全部
            Pending = 0,    // 待支付
            Success = 1,    // 支付成功
            Failed = 2,     // 支付失败
        }

        public enum ShippingStatus
        {
            All = -1,       // 全部
            Pending = 0,    // 待发货
            Shipped = 1,    // 已发货
        }

        [System.Serializable]
        public class OrderListData
        {
            public List<ResponseData> order_list;
        }

        
        [System.Serializable]
        public class ResponseData
        {
            public string order_no;
            public PayStatus pay_status;
            public ShippingStatus shipping_status;
            public string transaction_id;
            public string pay_success_time;
            public string game_extra_param;
            public string product_name;
            public string product_id;
            public float price;
            public string currency;
            public int quantity;
            public bool is_first_pay;
            public string payment_method;
        }
    }

}

