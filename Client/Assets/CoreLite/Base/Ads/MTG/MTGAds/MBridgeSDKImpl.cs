
using CoreLite;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace MTGAds
{
    public class MBridgeSDKImpl
    {
        private AndroidJavaObject mBridgeSDK;

        public MBridgeSDKImpl()
        {
            mBridgeSDK = CoreReflectionUtils.CallStaticMethod<AndroidJavaObject>("com.mbridge.msdk.out.MBridgeSDKFactory", "getMBridgeSDK");
        }

        private AndroidJavaObject getMBConfigurationMap(string appId, string appKey)
        {
            return CoreReflectionUtils.CallInstanceMethod<AndroidJavaObject>(mBridgeSDK, "getMBConfigurationMap", appId, appKey);
        }

        private void init(AndroidJavaObject configurationMap, AndroidJavaObject application, SDKInitStatusListener statusListener)
        {
            CoreReflectionUtils.CallInstanceMethod(mBridgeSDK, "init", configurationMap, application, new AndroidSDKInitStatusListener(statusListener));
        }

        public void Init(string appId, string appKey, SDKInitStatusListener statusListener)
        {
            init(getMBConfigurationMap(appId, appKey), CoreReflectionUtils.GetApplicationContext(), statusListener);
        }
    }

    public interface SDKInitStatusListener
    {
        void onInitFail(string msg);

        void onInitSuccess();
    }

    #region android的回调

    class AndroidSDKInitStatusListener : AndroidJavaProxy
    {
        private SDKInitStatusListener sdkInitStatusListener;

        public AndroidSDKInitStatusListener(SDKInitStatusListener sdkInitStatusListener) : base("com.mbridge.msdk.out.SDKInitStatusListener")
        {
            this.sdkInitStatusListener = sdkInitStatusListener;
        }

        [Preserve]
        void onInitFail(string msg)
        {
            UITools.RunUnityUI(() => { sdkInitStatusListener.onInitFail(msg); });
        }

        [Preserve]
        void onInitSuccess()
        {
            UITools.RunUnityUI(() => { sdkInitStatusListener.onInitSuccess(); });
        }
    }

    #endregion
}