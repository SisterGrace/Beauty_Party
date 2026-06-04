using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class RedeemCodeResponse
    {
        /// <summary>
        /// 0: 成功，1：无效，2：已使用
        /// </summary>
        public int result;

        /// <summary>
        /// 0: 内购项 1：图片
        /// </summary>
        public int type;

        /// <summary>
        /// 内购项ID 或 图片ID
        /// </summary>
        public string content;
    }
}
