using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CoreLite.CoreIapH5Mgr;


namespace CoreLite
{
    public class CoreLiteBuy
    {
        [System.Serializable]
        public class Request
        {
            public CoreLiteCreateOrder.Request create_order_request;
            public string order_Id;
            public CoreLitePaymentType payment_type;
            public string h5_url;

            public Request(CoreLiteCreateOrder.Request req, string orderId, CoreLitePaymentType payType, string h5Url)
            {
                create_order_request = req;
                order_Id = orderId;
                payment_type = payType;
                h5_url = h5Url;
            }
        }
    }
}
