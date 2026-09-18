#if TPN_AD
using System;
using AnyThinkAds.Api;

namespace Sky
{
    /// <summary>
    /// TopOn ATSDKAPI.initSDK 的初始化回调适配器。把 SDK 异步回调桥接到 SkyTpnMgr 的成功/失败处理上。
    /// </summary>
    public class SkyTpnInitListener : ATSDKInitListener
    {
        private readonly Action _onSuccess;
        private readonly Action<string> _onFail;
        private readonly string _appId;

        public SkyTpnInitListener(string appId, Action onSuccess, Action<string> onFail)
        {
            _appId = appId ?? "";
            _onSuccess = onSuccess;
            _onFail = onFail;
        }

        public void initSuccess()
        {
            // TopOn SDK 回调线程在不同 Network/平台下可能不一致，统一调度回主线程再执行业务逻辑。
            Loom.QueueOnMainThread(_ =>
            {
                SkyLogUtils.Debug("[SkyTpn] ATSDKInitListener.initSuccess appId:" + _appId);
                try
                {
                    _onSuccess?.Invoke();
                }
                catch (Exception e)
                {
                    SkyLogUtils.Error("[SkyTpn] initSuccess handler exception: " + e.Message);
                }
            }, null);
        }

        public void initFail(string message)
        {
            Loom.QueueOnMainThread(_ =>
            {
                SkyLogUtils.Error("[SkyTpn] ATSDKInitListener.initFail appId:" + _appId + " message:" + (message ?? ""));
                try
                {
                    _onFail?.Invoke(message);
                }
                catch (Exception e)
                {
                    SkyLogUtils.Error("[SkyTpn] initFail handler exception: " + e.Message);
                }
            }, null);
        }
    }
}
#endif