using CoreLite;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Sky
{ 
public class SkyAdsConfigParser : MonoBehaviour
{

    private static SkyConfigData GameConfigOrNull()
    {
        return SkyConfigMgr.Instance != null ? SkyConfigMgr.Instance.gameConfig : null;
    }

    private static List<string> SinglePlacementList(string placementId)
    {
        if (string.IsNullOrEmpty(placementId))
            return new List<string>();
        return new List<string> { placementId };
    }

    public static List<string> GetMaxAdUnits(SkyAdsType adType)
    {
        var t = GameConfigOrNull()?.maxLocalConfig;
        if (t == null)
            return new List<string>();

        switch (adType)
        {
            case SkyAdsType.Rewarded:
                return SinglePlacementList(t.rewardedPlacementId);
            case SkyAdsType.Interstitial:
                return SinglePlacementList(t.interstitialPlacementId);
            case SkyAdsType.Splash:
                return SinglePlacementList(t.splashPlacementId);
            case SkyAdsType.Banner:
                return SinglePlacementList(t.bannerPlacementId);
            default:
                return new List<string>();
        }
    }

    public static string GetMaxAppId()
    {
        return GameConfigOrNull()?.maxLocalConfig?.appId ?? "";
    }

    public static string GetMaxAppKey()
    {
        return GameConfigOrNull()?.maxLocalConfig?.appKey ?? "";
    }

    void Update()
    {

    }
}
}
