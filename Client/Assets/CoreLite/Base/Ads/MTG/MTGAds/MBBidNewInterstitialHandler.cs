
using CoreLite;
using UnityEngine;
using UnityEngine.Scripting;

namespace MTGAds
{
    public class MBBidNewInterstitialHandler
    {
        private AndroidJavaObject _androidJavaObject;

        public MBBidNewInterstitialHandler(string adPlacementId, string adUnitId)
        {
            _androidJavaObject = CoreReflectionUtils.CreateInstance("com.mbridge.msdk.newinterstitial.out.MBBidNewInterstitialHandler", CoreReflectionUtils.GetApplicationContext(), adPlacementId, adUnitId);
        }


        public void setInterstitialVideoListener(NewInterstitialListener listener)
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setInterstitialVideoListener", new AdnroidSDKNewInterstitialListener(listener)); });
        }

        public void loadFromBid(string bidToken)
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "loadFromBid", bidToken); });
        }

        public void showFromBid()
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "showFromBid"); });
        }

        public void loadFormSelfFilling()
        {
            CoreReflectionUtils.RunOnUiThread(() => { CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "loadFormSelfFilling"); });
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

        public void playVideoMute(int mute)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "playVideoMute", mute);
        }

        public void setIVRewardEnable(int var1, int var2)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setIVRewardEnable", var1, var2);
        }

        public void setIVRewardEnable(int var1, double var2)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setIVRewardEnable", var1, var2);
        }

        public void setAlertDialogText(string var1, string var2, string var3, string var4)
        {
            CoreReflectionUtils.CallInstanceMethod(_androidJavaObject, "setAlertDialogText", var1, var2, var3, var4);
        }
    }

    public interface NewInterstitialListener
    {
        void onLoadCampaignSuccess(MBridgeIds var1);

        void onResourceLoadSuccess(MBridgeIds var1);

        void onResourceLoadFail(MBridgeIds var1, string var2);

        void onAdShow(MBridgeIds var1);

        void onAdClose(MBridgeIds var1, RewardInfo var2);

        void onShowFail(MBridgeIds var1, string var2);

        void onAdClicked(MBridgeIds var1);

        void onVideoComplete(MBridgeIds var1);

        void onAdCloseWithNIReward(MBridgeIds var1, RewardInfo var2);

        void onEndcardShow(MBridgeIds var1);
    }

    #region android的回调

    class AdnroidSDKNewInterstitialListener : AndroidJavaProxy
    {
        private NewInterstitialListener newInterstitialListener;

        public AdnroidSDKNewInterstitialListener(NewInterstitialListener newInterstitialListener) : base("com.mbridge.msdk.newinterstitial.out.NewInterstitialListener")
        {
            this.newInterstitialListener = newInterstitialListener;
        }

        [Preserve]
        void onLoadCampaignSuccess(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onLoadCampaignSuccess(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onResourceLoadSuccess(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onResourceLoadSuccess(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onResourceLoadFail(AndroidJavaObject var1, string var2)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onResourceLoadFail(new MBridgeIds(var1), var2); });
        }

        [Preserve]
        void onAdShow(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onAdShow(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onAdClose(AndroidJavaObject var1, AndroidJavaObject var2)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onAdClose(new MBridgeIds(var1), new RewardInfo(var2)); });
        }

        [Preserve]
        void onShowFail(AndroidJavaObject var1, string var2)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onShowFail(new MBridgeIds(var1), var2); });
        }

        [Preserve]
        void onAdClicked(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onAdClicked(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onVideoComplete(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onVideoComplete(new MBridgeIds(var1)); });
        }

        [Preserve]
        void onAdCloseWithNIReward(AndroidJavaObject var1, AndroidJavaObject var2)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onAdCloseWithNIReward(new MBridgeIds(var1), new RewardInfo(var2)); });
        }

        [Preserve]
        void onEndcardShow(AndroidJavaObject var1)
        {
            UITools.RunUnityUI(() => { newInterstitialListener.onEndcardShow(new MBridgeIds(var1)); });
        }
    }

    #endregion
}