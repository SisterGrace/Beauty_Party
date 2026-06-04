using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class SDKModel
    {
        //是否是新用户
        public bool IsNewUser;
        //每日累计支付失败次数
        public int PaydailyTimes;
        //支付失败累计总次数
        public int PaytotalTimes;
        //插屏成功播放次数
        public int InterstitialPlayTimes;
        //IAP花费总额
        public float IAPTotalCost;
        //Level_Choose是否上报
        public bool IsLevelChooseReported;
        public SDKModel()
        {
            
        }
        public SDKModel(bool isNew)
        {
            if (isNew)
            {
                IsNewUser = true;
                PaydailyTimes = 0;
                PaytotalTimes = 0;
                InterstitialPlayTimes = 0;
                IAPTotalCost = 0;
                IsLevelChooseReported = false;
            }
        }
    }
}
