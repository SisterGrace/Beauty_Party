using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace CoreLite
{
    public class CoreBillingCountryFetcher : MonoBehaviour,BillingClientStateListener, BillingConfigResponseListener
    {
        async void Start()
        {
            CoreLogUtils.Debug("CoreBillingCountryFetcher start");
        }

        public void Init()
        {
            CoreLogUtils.Debug("CoreBillingCountryFetcher init");
            googleBillingClint = GoogleBillingClint.NewBuilder().Build();
            googleBillingClint.StartConnection(this);
        }
        private GoogleBillingClint googleBillingClint;

        public void OnBillingConfigResponse(BillingResult billingResult, BillingConfig billingConfig)
        {
            CoreLogUtils.Debug($"CoreBillingCountryFetcher OnBillingConfigResponse {billingResult.GetDebugMessage()},"+
            $"{billingResult.GetResponseCode()} , " +
            $"billingConfig.GetCountryCode = {billingConfig.GetCountryCode()}");
            CoreIapH5Mgr.Instance.storeCountryCode = billingConfig.GetCountryCode();
        }

        public void OnBillingServiceDisconnected()
        {
            CoreLogUtils.Debug("CoreBillingCountryFetcher OnBillingServiceDisconnected");
        }

        public void OnBillingSetupFinished(BillingResult billingResult)
        {
            CoreLogUtils.Debug($"CoreBillingCountryFetcher OnBillingSetupFinished {billingResult.GetDebugMessage()} , {billingResult.GetResponseCode()} ");
            googleBillingClint.GetBillingConfigAsync(this);
        }
    }
}
