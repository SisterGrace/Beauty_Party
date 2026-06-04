using System;
using CoreLite;
using UnityEngine;
using UnityEngine.Scripting;

namespace MTGAds
{
    public class BidManager
    {
        private AndroidJavaObject bidManager;

        public BidManager(string adPlacementId, string adUnitId)
        {
            bidManager = CoreReflectionUtils.CreateInstance("com.mbridge.msdk.mbbid.out.BidManager", adPlacementId, adUnitId);
        }

        public void setBidListener(BidListennning listen)
        {
            CoreReflectionUtils.CallInstanceMethod(bidManager, "setBidListener", new AndroidSDKBidResponsed(listen));
        }

        public void bid()
        {
            CoreReflectionUtils.CallInstanceMethod(bidManager, "bid");
        }

        public void setRewardPlus(bool rewardPlus)
        {
            CoreReflectionUtils.CallInstanceMethod(bidManager, "setRewardPlus", rewardPlus);
        }
    }

    public interface BidListennning
    {
        void onFailed(String msg);

        void onSuccessed(BidResponsed responsed);
    }

    public class BidLossCode
    {
        private AndroidJavaObject obj;

        public BidLossCode(int code)
        {
            obj = CoreReflectionUtils.CreateInstance("com.mbridge.msdk.mbbid.out.BidLossCode", code);
        }

        public static BidLossCode bidPriceNotHighest()
        {
            return new BidLossCode(102);
        }

        public static BidLossCode bidWinButNotShow()
        {
            return new BidLossCode(3001);
        }

        public AndroidJavaObject GetBidLossCode()
        {
            return obj;
        }
    }

    public class BidResponsed
    {
        private AndroidJavaObject obj;

        public string bidToken;
        public string price;

        public BidResponsed(AndroidJavaObject obj)
        {
            this.obj = obj;
            bidToken = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getBidToken");
            price = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getPrice");
        }

        public void sendWinNotice()
        {
            CoreReflectionUtils.CallInstanceMethod(obj, "sendWinNotice", CoreReflectionUtils.GetApplicationContext());
        }

        public void sendLossNotice(BidLossCode bidLossCode)
        {
            CoreReflectionUtils.CallInstanceMethod(obj, "sendLossNotice", CoreReflectionUtils.GetApplicationContext(), bidLossCode.GetBidLossCode());
        }
    }

    #region android的回调

    public class AndroidSDKBidResponsed : AndroidJavaProxy
    {
        private BidListennning listen;

        public AndroidSDKBidResponsed(BidListennning listen) : base("com.mbridge.msdk.mbbid.out.BidListennning")
        {
            this.listen = listen;
        }

        [Preserve]
        void onFailed(String var1)
        {
            UITools.RunUnityUI(() => { listen.onFailed(var1); });
        }

        [Preserve]
        void onSuccessed(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { listen.onSuccessed(new BidResponsed(var1)); });
        }
    }

    #endregion
}