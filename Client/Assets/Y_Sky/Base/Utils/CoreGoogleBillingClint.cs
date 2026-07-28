using UnityEngine;
using UnityEngine.Scripting;

namespace CoreLite
{
    public class GoogleBillingClint
    {
        private AndroidJavaObject billingClient = null;

        private static string GoogleBillingClintClass = "com.android.billingclient.api.BillingClient";

        private GoogleBillingClint()
        {
            
            // billingClient = new AndroidJavaObject("com.google.android.gms.BillingClient");
        }

        public class Builder
        {
            private readonly AndroidJavaObject _builder;

            public Builder()
            {
                var androidJavaClass = new AndroidJavaClass(GoogleBillingClintClass);
                _builder = androidJavaClass.CallStatic<AndroidJavaObject>("newBuilder", GetActivity());
                _builder.Call<AndroidJavaObject>("setListener", new PurchasesUpdatedListenerJava());
                var cls = new AndroidJavaClass("com.android.billingclient.api.PendingPurchasesParams");
                var pendingPurchasesParams = cls.CallStatic<AndroidJavaObject>("newBuilder").Call<AndroidJavaObject>("enableOneTimeProducts").Call<AndroidJavaObject>("build");
                _builder.Call<AndroidJavaObject>("enablePendingPurchases", pendingPurchasesParams);
            }

            public GoogleBillingClint Build()
            {
                var googleBillingClint = new GoogleBillingClint
                {
                    billingClient = _builder.Call<AndroidJavaObject>("build")
                };
                return googleBillingClint;
            }
        }

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        public void StartConnection(BillingClientStateListener billingClientStateListener)
        {
            billingClient.Call("startConnection", new BillingClientStateListenerJava(billingClientStateListener));
        }

        public void GetBillingConfigAsync(BillingConfigResponseListener billingConfigResponseListener)
        {
            var getBillingConfigParams = new AndroidJavaClass("com.android.billingclient.api.GetBillingConfigParams").CallStatic<AndroidJavaObject>("newBuilder").Call<AndroidJavaObject>("build");
            billingClient.Call("getBillingConfigAsync", getBillingConfigParams, new BillingConfigResponseListenerJava(billingConfigResponseListener));
        }

        private static AndroidJavaObject GetActivity()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return activity;
        }
    }

    public interface BillingConfigResponseListener
    {
        void OnBillingConfigResponse(BillingResult billingResult, BillingConfig billingConfig);
    }

    class PurchasesUpdatedListenerJava : AndroidJavaProxy
    {
        public PurchasesUpdatedListenerJava() : base("com.android.billingclient.api.PurchasesUpdatedListener")
        {
        }

        [Preserve]
        public void onPurchasesUpdated(AndroidJavaObject billingResult, AndroidJavaObject purchaseList)
        {
        }
    }

    class BillingConfigResponseListenerJava : AndroidJavaProxy
    {
        private BillingConfigResponseListener _billingConfigResponseListener;

        public BillingConfigResponseListenerJava(BillingConfigResponseListener billingConfigResponseListener) : base("com.android.billingclient.api.BillingConfigResponseListener")
        {
            _billingConfigResponseListener = billingConfigResponseListener;
        }

        [Preserve]
        public void onBillingConfigResponse(AndroidJavaObject billingResult, AndroidJavaObject billingConfig)
        {
            _billingConfigResponseListener.OnBillingConfigResponse(new BillingResult(billingResult), new BillingConfig(billingConfig));
        }
    }

    class BillingClientStateListenerJava : AndroidJavaProxy
    {
        private BillingClientStateListener _billingClientStateListener;

        public BillingClientStateListenerJava(BillingClientStateListener billingClientStateListener) : base("com.android.billingclient.api.BillingClientStateListener")
        {
            _billingClientStateListener = billingClientStateListener;
        }

        [Preserve]
        public void onBillingSetupFinished(AndroidJavaObject billingResult)
        {
            _billingClientStateListener.OnBillingSetupFinished(new BillingResult(billingResult));
        }


        [Preserve]
        public void onBillingServiceDisconnected()
        {
            _billingClientStateListener.OnBillingServiceDisconnected();
        }
    }

    public interface BillingClientStateListener
    {
        public void OnBillingServiceDisconnected();
        public void OnBillingSetupFinished(BillingResult billingResult);
    }

    public class BillingResult
    {
        AndroidJavaObject billingResult;

        public BillingResult(AndroidJavaObject billingResult)
        {
            this.billingResult = billingResult;
        }

        public int GetResponseCode()
        {
            return billingResult.Call<int>("getResponseCode");
        }

        public string GetDebugMessage()
        {
            return billingResult.Call<string>("getDebugMessage");
        }
    }

    public class BillingConfig
    {
        AndroidJavaObject billingConfig;

        public BillingConfig(AndroidJavaObject billingConfig)
        {
            this.billingConfig = billingConfig;
        }

        public string GetCountryCode()
        {
            return billingConfig.Call<string>("getCountryCode");
        }
    }
}