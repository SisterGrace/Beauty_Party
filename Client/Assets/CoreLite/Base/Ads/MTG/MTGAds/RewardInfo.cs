using CoreLite;
using UnityEngine;

namespace MTGAds
{
    public class RewardInfo
    {
        private AndroidJavaObject obj;

        public bool isCompleteView;
        public int rewardAlertStatus;
        public string rewardAmount;
        public string rewardName;

        public RewardInfo(AndroidJavaObject obj)
        {
            this.obj = obj;
            isCompleteView = CoreReflectionUtils.CallInstanceMethod<bool>(obj, "isCompleteView");
            rewardAlertStatus = CoreReflectionUtils.CallInstanceMethod<int>(obj, "getRewardAlertStatus");
            rewardAmount = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getRewardAmount");
            rewardName = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getRewardName");
        }
    }
}