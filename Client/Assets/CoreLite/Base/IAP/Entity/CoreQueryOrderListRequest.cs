using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{
    public class CoreLiteQueryOrderList
    {
        [System.Serializable]
        public class Request
        {
            public string user_id; 
            public int pay_status = 1;         // -1：全部，0:未支付, 1:已支付，2：支付失败
            public int shipping_status = 0;    // -1：全部，0:未下发，1：已下发
            
            public Request(string userId, int payStatus = 1, int shippingStatus = 0)
            {
                user_id = userId;
                pay_status = payStatus;
                shipping_status = shippingStatus;
            }
        }
    }

}

