#if TPN_AD
#if UNITY_ANDROID || UNITY_IOS
using AnyThinkAds.Api;
using Sky;

namespace Sky
{
    /// <summary>
    /// GDPR 弹窗关闭监听器
    /// 文档：https://docs.toponad.com/zh-cn/unity/GoogleUMP
    /// </summary>
    public class ConsentDismissListener : ATConsentDismissListener
    {
        private string _appId;
        private string _appKey;
        private System.Action _onSuccess;
        private System.Action<string> _onFail;

        public ConsentDismissListener(string appId, string appKey, System.Action onSuccess, System.Action<string> onFail)
        {
            _appId = appId;
            _appKey = appKey;
            _onSuccess = onSuccess;
            _onFail = onFail;
        }

        /// <summary>
        /// 用户关闭 GDPR 弹窗后自动回调
        /// 此时可以安全地初始化 TopOn SDK
        /// </summary>
        public void onConsentDismiss(ATConsentDismissInfo consentDismissInfo)

		{
            SkyLogUtils.Debug("[SkyTpn] onConsentDismiss() - GDPR consent handled, initializing SDK...");
            var listener = new SkyTpnInitListener(_appId, _onSuccess, _onFail);
            AnyThinkAds.Api.ATSDKAPI.initSDK(_appId, _appKey, listener);
        }
    }
}

#endif
#endif
