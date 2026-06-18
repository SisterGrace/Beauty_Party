using System;
using UnityEngine;
using TK_Engine;

namespace Y_Base
{
    public enum HttpMethod
    {
        GET,
        POST
    }
    [Serializable]
    public abstract class APIBase
    {
        public virtual string RequestURL => "";
        public virtual HttpMethod Method => HttpMethod.POST;
        
        /// <summary>
        /// 是否使用原始响应格式（直接返回数据，而不是 code/msg/data 包装）
        /// </summary>
        public virtual bool UseRawResponse => false;
        
        public string RequestJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
