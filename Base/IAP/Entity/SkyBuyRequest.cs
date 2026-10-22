#if IAP_ENABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Sky.SkyIapH5Mgr;


namespace Sky
{
    public class SkyBuy
    {
        [System.Serializable]
        public class Request
        {
            public SkyCreateOrder.Request create_order_request;
            public string order_Id;
            public SkyPaymentType payment_type;
            public string h5_url;

            public Request(SkyCreateOrder.Request req, string orderId, SkyPaymentType payType, string h5Url)
            {
                create_order_request = req;
                order_Id = orderId;
                payment_type = payType;
                h5_url = h5Url;
            }
        }
    }
}
#endif
