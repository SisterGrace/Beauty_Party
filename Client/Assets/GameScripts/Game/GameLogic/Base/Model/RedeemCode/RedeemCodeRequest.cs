using System.Collections;
using System.Collections.Generic;
using Y_Base;
using UnityEngine;

namespace GameLogic
{
    public class RedeemCodeRequest : APIBase
    {
        public override string RequestURL => ConfigLoader.Constant.RedeemUrl;
        public override HttpMethod Method => HttpMethod.GET;
        
        /// <summary>
        /// 兑换码接口直接返回数据，不使用 code/msg/data 包装
        /// </summary>
        public override bool UseRawResponse => true;

        public string code;
        public string device_id;
        public string app_id;
        public string country;
        public RedeemCodeRequest(string code, string deviceId, string appId, string country)
        {
            this.code = code;
            this.device_id = deviceId;
            this.app_id = appId;
            this.country = country;
        }
    }
}
