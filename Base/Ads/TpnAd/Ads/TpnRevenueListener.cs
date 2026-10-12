#if TPN_AD
using AnyThinkAds.Api;
using Sky;

namespace Sky
{
    /// <summary>
    /// 实现 ThinkUp 插件的 IATAdRevenueListener，
    /// 把 TopOn 专用收入回调转给 TopOnRevenueReporter。
    /// 通过 client.setAdRevenueListener(placementId, listener) 注册。
    /// </summary>
    public class TpnRevenueListener : IATAdRevenueListener
    {
        public void onAdRevenuePaid(string placementId, ATCallbackInfo callbackInfo)
        {
            if (callbackInfo == null) return;
            TopOnRevenueReporter.OnAdRevenuePaid(placementId, callbackInfo.toDictionary());
        }
    }
}
#endif