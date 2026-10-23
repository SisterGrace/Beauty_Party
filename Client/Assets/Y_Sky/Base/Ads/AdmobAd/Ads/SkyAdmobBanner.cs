#if ADMOB_AD
using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob Banner 广告管理类。
    /// 
    /// 使用场景：应用底部或顶部的常驻横幅广告。
    /// 
    /// 关键逻辑：
    /// - 加载成功后在 OnAdPaid 中将 eCPM 写入 SkyAdmobEcpmCache（用于后续竞价）
    /// - 加载失败时进行指数退避重试，最多 6 次
    /// - 支持显示/隐藏，但不会自动销毁（除非主动调用 DestroyAd）
    /// - Banner 广告通常只使用单个广告位
    /// </summary>
    public class SkyAdmobBanner
    {
        private string _unitId;
        private BannerView _bannerView;
        private int _retryCount;
        private bool _isVisible;

        private const int MaxRetryCount = 6;

        public void Initialize(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                SkyLogUtils.Debug("[Admob Banner] 未配置广告位，跳过初始化");
                return;
            }

            _unitId = unitId;
            SkyLogUtils.Debug($"[Admob Banner] Initialize unit: {_unitId}");
            LoadAd();
        }

        private void LoadAd(int retryAttempt = 0)
        {
            if (retryAttempt > MaxRetryCount)
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 已达最大重试次数，放弃");
                return;
            }

            // 先销毁旧实例（如果存在）
            if (_bannerView != null)
            {
                SkyLogUtils.Debug($"[Admob Banner] 销毁旧实例后重新加载");
                _bannerView.Destroy();
                _bannerView = null;
            }

            SkyLogUtils.Debug($"[Admob Banner] 开始加载 {_unitId} (retry={retryAttempt})");

            // 创建 Banner（默认尺寸：320x50，位置：底部居中）
            _bannerView = new BannerView(_unitId, AdSize.Banner, AdPosition.Bottom);

            // 注册回调
            RegisterCallbacks();

            // 开始加载
            var request = new AdRequest();
            _bannerView.LoadAd(request);
        }

        private void RegisterCallbacks()
        {
            // 加载成功
            _bannerView.OnBannerAdLoaded += () =>
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 加载成功");
                _retryCount = 0; // 重置重试计数
                _isVisible = false; // 加载成功后默认不可见，等待主动调用 Show
            };

            // 加载失败
            _bannerView.OnBannerAdLoadFailed += error =>
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 加载失败: {error.GetMessage()}");

                int nextRetry = _retryCount + 1;
                _retryCount = nextRetry;

                if (nextRetry <= MaxRetryCount)
                {
                    float delay = Mathf.Pow(2, nextRetry); // 指数退避: 2^n 秒
                    SkyLogUtils.Debug($"[Admob Banner] {_unitId} 将在 {delay}s 后重试 (retry={nextRetry})");
                    SkyFuncDelayExecuteUtils.Instance.DelayExecute(delay, () => LoadAd(nextRetry));
                }
            };

            // 展示后收益回调（写入 eCPM 到缓存）
            _bannerView.OnAdPaid += adValue =>
            {
                HandleAdPaid(adValue);
            };

            // 展示成功（Banner 首次可见时触发）
            _bannerView.OnAdFullScreenContentOpened += () =>
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 全屏内容打开（点击后展开）");
            };

            // 全屏内容关闭（用户关闭点击后展开的落地页）
            _bannerView.OnAdFullScreenContentClosed += () =>
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 全屏内容关闭");
            };

            // 用户点击 Banner
            _bannerView.OnAdClicked += () =>
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 被点击");
            };

            // Banner 展示印象（实际展示到屏幕上时触发）
            _bannerView.OnAdImpressionRecorded += () =>
            {
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 印象已记录");
            };
        }

        private void HandleAdPaid(AdValue adValue)
        {
            double ecpm = adValue.Value / 1_000_000.0; // micro USD → USD
            string currency = adValue.CurrencyCode;

            SkyLogUtils.Debug($"[Admob Banner] {_unitId} OnAdPaid eCPM={ecpm:F4} {currency}");

            // 更新缓存（只保存 USD，其他货币丢弃）
            SkyAdmobEcpmCache.Instance.UpdateBannerLastShowEcpm(ecpm, currency, _unitId);

            // 上报广告展示事件
            SkyAnalyticsMgr.TrackAdImpressionEvent(
                ecpm, "admob", "", _unitId,
                SkyAdsType.Banner.ToString(), SkyPlatformType.Admob.ToString(),
                "" // Banner 通常没有 position 参数
            );
        }

        public bool IsReady()
        {
            return _bannerView != null;
        }

        public void Show()
        {
            if (_bannerView == null)
            {
                SkyLogUtils.Debug("[Admob Banner] Banner 未加载，无法显示");
                LoadAd(); // 尝试重新加载
                return;
            }

            if (!_isVisible)
            {
                _bannerView.Show();
                _isVisible = true;
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 已显示");
            }
        }

        public void Hide()
        {
            if (_bannerView == null)
            {
                SkyLogUtils.Debug("[Admob Banner] Banner 未加载，无需隐藏");
                return;
            }

            if (_isVisible)
            {
                _bannerView.Hide();
                _isVisible = false;
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 已隐藏");
            }
        }

        public void ClearAd()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
                _isVisible = false;
                SkyLogUtils.Debug($"[Admob Banner] {_unitId} 已销毁");
            }
        }
    }
}
#endif
