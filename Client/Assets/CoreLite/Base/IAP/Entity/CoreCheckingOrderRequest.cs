using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CoreLite.CoreIapH5Mgr;

namespace CoreLite
{
    public class CoreLiteCheckingOrder
    {
        [System.Serializable]
        public class Request
        {
            public string product_name;
            public string product_id;
            public string order_Id;
            public string game_extra01;
            public CoreLitePaymentType payment_type;

            public Request(string productName, string productId, string orderId, string gameExtra01, CoreLitePaymentType paymentType)
            {
                product_name = productName;
                product_id = productId;
                order_Id = orderId;
                game_extra01 = gameExtra01;
                payment_type = paymentType;
            }
        }
    }
}