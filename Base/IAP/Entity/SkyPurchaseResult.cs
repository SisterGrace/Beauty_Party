#if IAP_ENABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static Sky.SkyIapH5Mgr;



namespace Sky
{
    public class SkyPurchase
    {
        [System.Serializable]
        public class Result
        {
            public SkyCreateOrder.Request create_order_request;
            public SkyQueryOrder.Result query_order_reuslt; 
            public bool purchase_result;
            public bool order_already_exists;
            public SkyPaymentType payment_type;
            

            public Result(SkyCreateOrder.Request orderRequest, SkyQueryOrder.Result orderResult, bool purchaseResult, bool orderAlreadyExists, SkyPaymentType paymentType)
            {
                create_order_request = orderRequest;
                query_order_reuslt = orderResult;
                purchase_result = purchaseResult;
                order_already_exists = orderAlreadyExists;
                payment_type = paymentType;
            }
        }
    }
}
#endif