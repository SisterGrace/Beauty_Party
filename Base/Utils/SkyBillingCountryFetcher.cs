#if IAP_ENABLE
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Sky
{
    public class SkyBillingCountryFetcher : MonoBehaviour,BillingClientStateListener, BillingConfigResponseListener
    {
        async void Start()
        {
            SkyLogUtils.Debug("SkyBillingCountryFetcher start");
        }

        public void Init()
        {
            SkyLogUtils.Debug("SkyBillingCountryFetcher init");
            googleBillingClint = GoogleBillingClint.NewBuilder().Build();
            googleBillingClint.StartConnection(this);
        }
        private GoogleBillingClint googleBillingClint;

        public void OnBillingConfigResponse(BillingResult billingResult, BillingConfig billingConfig)
        {
            SkyLogUtils.Debug($"SkyBillingCountryFetcher OnBillingConfigResponse {billingResult.GetDebugMessage()},"+
            $"{billingResult.GetResponseCode()} , " +
            $"billingConfig.GetCountryCode = {billingConfig.GetCountryCode()}");
            SkyIapH5Mgr.Instance.storeCountryCode = billingConfig.GetCountryCode();
        }

        public void OnBillingServiceDisconnected()
        {
            SkyLogUtils.Debug("SkyBillingCountryFetcher OnBillingServiceDisconnected");
        }

        public void OnBillingSetupFinished(BillingResult billingResult)
        {
            SkyLogUtils.Debug($"SkyBillingCountryFetcher OnBillingSetupFinished {billingResult.GetDebugMessage()} , {billingResult.GetResponseCode()} ");
            googleBillingClint.GetBillingConfigAsync(this);
        }
    }
}
#endif