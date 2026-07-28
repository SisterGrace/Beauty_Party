using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static CoreLite.CoreIapH5Mgr;



namespace CoreLite
{
    public class CoreLitePurchase
    {
        [System.Serializable]
        public class Result
        {
            public CoreLiteCreateOrder.Request create_order_request;
            public CoreLiteQueryOrder.Result query_order_reuslt; 
            public bool purchase_result;
            public bool order_already_exists;
            public CoreLitePaymentType payment_type;
            

            public Result(CoreLiteCreateOrder.Request orderRequest, CoreLiteQueryOrder.Result orderResult, bool purchaseResult, bool orderAlreadyExists, CoreLitePaymentType paymentType)
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
