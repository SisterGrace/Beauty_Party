#if IAP_ENABLE
using System.Collections;
using System.Collections.Generic;
using Sky;
using UnityEngine;

namespace Sky
{
    public class SkyCreateOrder
    {
        [System.Serializable]
        public class Request
        {
            public string product_name = "";
            public string product_id = "";
            public string product_desc = "";
            public string product_type = "Consumable"; // 产品类型，当前仅支持Consumable（消耗类产品）
            public float unit_price = 0;
            public int quantity = 1;
            public int order_type = 0;  // 订单类型：0:app应用
            public string user_id = "";
            public string currency_code = "";
            public string game_account_country; // 当前登录商店country code
            public string game_extra_param = "";
            public string game_server_channel = "0";  // 没有默认传0
            public string game_user_id = "0"; // 没有默认传0

            public Request(string productName, string productId, string productDesc, float unitPrice, int qty, string currencyCode, string gameExtraParam, string gameServerChannel)
            {
                product_name = productName;
                product_id = productId;
                product_desc = productDesc;
                product_type = "Consumable";
                unit_price = unitPrice;
                quantity = qty;
                order_type = 0;
                user_id = SkyPrefs.UserId;
                currency_code = currencyCode;
                game_account_country = SkyIapH5Mgr.Instance.storeCountryCode;
                game_extra_param = gameExtraParam;
                game_server_channel = gameServerChannel;
                game_user_id = SkyPrefs.GameUserId;
            }
        }
        
        // 支付信息结构体
        [System.Serializable]
        public class PaymentInfo
        {
            public string name;
            public int type;
            public string h5_url;
        }

        // 响应数据结构体
        [System.Serializable]
        public class ResponseData
        {
            public string order_no;
            public List<PaymentInfo> payment_list;
        }

    }

}
#endif
