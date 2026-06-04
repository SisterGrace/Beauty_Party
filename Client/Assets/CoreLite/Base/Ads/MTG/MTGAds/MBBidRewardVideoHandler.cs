using CoreLite;
using UnityEngine;
using UnityEngine.Scripting;

namespace MTGAds
{
    public class MBBidRewardVideoHandler
    {
        private AndroidJavaObject _androidJavaObject;

        public MBBidRewardVideoHandler(string adPlacementId, string adUnitId)
        {
            _androidJavaObject = CoreReflectionUtils.CreateInstance("com.mbridge.msdk.out.MBBidRewardVideoHandler", CoreReflectionUtils.GetApplicationContext(), adPlacementId, adUnitId);
        }


        public void setRewardVideoListener(RewardVideoListener listener)
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setRewardVideoListener", new AdnroidSDKRewardVideoListener(listener)); });
        }

        public void loadFromBid(string bidToken)
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "loadFromBid", bidToken); });
        }

        public void showFromBid()
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "showFromBid"); });
        }

        public bool isBidReady()
        {
            return CoreReflectionUtils.CallInstanceMethod<bool>(_androidJavaObject, "isBidReady");
        }

        public string getRequestId()
        {
            return CoreReflectionUtils.CallInstanceMethod<string>(_androidJavaObject, "getRequestId");
        }

        public string getCreativeIdWithUnitId()
        {
            return CoreReflectionUtils.CallInstanceMethod<string>(_androidJavaObject, "getCreativeIdWithUnitId");
        }

        public void clearVideoCache()
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "clearVideoCache");
        }

        public void playVideoMute(int var1)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "playVideoMute", var1);
        }

        public void setAlertDialogText(string var1, string var2, string var3, string var4)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setAlertDialogText", var1, var2, var3, var4);
        }

        public void setRewardPlus(bool var1)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setRewardPlus", var1);
        }
    }

    public interface RewardVideoListener
    {
        void onVideoLoadSuccess(MBridgeIds var1);

        void onLoadSuccess(MBridgeIds var1);

        void onVideoLoadFail(MBridgeIds var1, string var2);

        void onAdShow(MBridgeIds var1);

        void onAdClose(MBridgeIds var1, RewardInfo var2);

        void onShowFail(MBridgeIds var1, string var2);

        void onVideoAdClicked(MBridgeIds var1);

        void onVideoComplete(MBridgeIds var1);

        void onEndcardShow(MBridgeIds var1);
    }


    #region android的回调

    class AdnroidSDKRewardVideoListener : AndroidJavaProxy
    {
        private RewardVideoListener rewardVideoListener;

        public AdnroidSDKRewardVideoListener(RewardVideoListener rewardVideoListener) : base("com.mbridge.msdk.video.bt.module.orglistener.g")
        {
            this.rewardVideoListener = rewardVideoListener;
        }

        [Preserve]
        void onVideoLoadSuccess(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onVideoLoadSuccess(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onLoadSuccess(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onLoadSuccess(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onVideoLoadFail(AndroidJavaObject var1, string var2)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onVideoLoadFail(new MBridgeIds(var1), var2); });
        }

        [Preserve]
        void onAdShow(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onAdShow(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onAdClose(AndroidJavaObject var1, AndroidJavaObject var2)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onAdClose(new MBridgeIds(var1), new RewardInfo(var2)); });
        }

        [Preserve]
        void onShowFail(AndroidJavaObject var1, string var2)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onShowFail(new MBridgeIds(var1), var2); });
        }

        [Preserve]
        void onVideoAdClicked(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onVideoAdClicked(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onVideoComplete(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onVideoComplete(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onEndcardShow(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { rewardVideoListener.onEndcardShow(new MBridgeIds(var1)); });
        }
    }

    #endregion
}