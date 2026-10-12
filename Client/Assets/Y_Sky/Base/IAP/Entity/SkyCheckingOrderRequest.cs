#if IAP_ENABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Sky.SkyIapH5Mgr;

namespace Sky
{
    public class SkyCheckingOrder
    {
        [System.Serializable]
        public class Request
        {
            public string product_name;
            public string product_id;
            public string order_Id;
            public string game_extra01;
            public SkyPaymentType payment_type;

            public Request(string productName, string productId, string orderId, string gameExtra01, SkyPaymentType paymentType)
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
#endif