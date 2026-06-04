using System.Collections;
using System.Collections.Generic;
using MTGAds;
using UnityEngine;

namespace CoreLite
{
    public class CoreMtgInitListener : SDKInitStatusListener
    {
        public void onInitFail(string msg)
        {
            CoreLogUtils.Debug(" MTG Init Fail: " + msg);
        }

        public void onInitSuccess()
        {
            CoreLogUtils.Debug(" MTG Init Success");
            CoreMtgMgr.Instance.LoadAllBidRewardAds();
            CoreMtgMgr.Instance.LoadAllBidInterstitialAds();
        }
    }
}

