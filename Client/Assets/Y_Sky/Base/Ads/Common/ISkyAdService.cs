using System;
using UnityEngine;

// using UnityEditor.Media;

namespace Sky
{
    /// <summary>
    /// 提供广告SDK功能的统一接口
    /// </summary>
    public interface ISkyAdService
    {
        /// <summary>
        /// 获取广告客户端名称
        /// </summary>
        string ClientName { get; }

        SkyPlatformType PlatformType { get; }

        /// <summary>
        /// 初始化广告SDK：内部应基于本地配置一次性完成数据读取、广告位设置与 SDK init。
        /// 由 SkyAdsMgr 在启动时统一调用一次；广告 ID 已改为本地读取，运行期不再需要刷新。
        /// </summary>
        void Initialize();

        void ClearAllAds(); // 立即停止并清除该平台所有广告（配置变为空时调用）

        public (string adId, string adSource, double adEcpm) GetInterstitialAdInfo();

        #region 横幅广告功能

        /// <summary>
        /// 加载横幅广告
        /// </summary>
        void LoadBanner();

        /// <summary>
        /// 检查横幅广告是否可用
        /// </summary>
        bool IsBannerReady();

        /// <summary>
        /// 显示横幅广告
        /// </summary>
        void ShowBanner();

        /// <summary>
        /// 隐藏横幅广告
        /// </summary>
        void HideBanner();

        /// <summary>
        /// 获取横幅广告收益信息
        /// </summary>
        double GetBannerRevenue();

        #endregion

        #region 插页广告功能

        /// <summary>
        /// 加载插页广告
        /// </summary>
        void LoadInterstitial();

        /// <summary>
        /// 检查插页广告是否可用
        /// </summary>
        bool IsInterstitialAvailable();

        /// <summary>
        /// 显示插页广告
        /// </summary>
        void ShowInterstitial(string position=null);

        /// <summary>
        /// 获取插页广告收益信息
        /// </summary>
        double GetInterstitialRevenue();

        #endregion

        #region 激励广告功能

        /// <summary>
        /// 加载激励广告
        /// </summary>
        void LoadRewarded();

        /// <summary>
        /// 检查激励广告是否可用
        /// </summary>
        bool IsRewardedAvailable();

        /// <summary>
        /// 显示激励广告
        /// </summary>
        void ShowRewarded(string positon=null);

        /// <summary>
        /// 获取激励广告收益信息
        /// </summary>
        double GetRewardedRevenue();

        #endregion

        #region 开屏广告功能

        /// <summary>
        /// 加载开屏广告
        /// </summary>
        void LoadSplash();

        /// <summary>
        /// 检查开屏广告是否可用
        /// </summary>
        bool IsSplashAvailable();

        /// <summary>
        /// 显示开屏广告
        /// </summary>
        void ShowSplash();

        /// <summary>
        /// 获取开屏广告收益信息
        /// </summary>
        double GetSplashRevenue();

        #endregion

        #region 原生广告功能

        /// <summary>
        /// 加载原生广告
        /// </summary>
        void LoadNative();

        /// <summary>
        /// 检查原生广告是否可用
        /// </summary>
        bool IsNativeAvailable();

        /// <summary>
        /// 显示原生广告
        /// </summary>
        /// <param name="position">广告位置和尺寸信息</param>
        void ShowNative();

        /// <summary>
        /// 移除原生广告
        /// </summary>
        void RemoveNative();

        /// <summary>
        /// 获取原生广告收益信息
        /// </summary>
        double GetNativeRevenue();

        #endregion

        #region 原生全屏广告功能

        bool IsNativeFullscreenAvailable();
        void ShowNativeFullscreen();
        void RemoveNativeFullscreen();
        double GetNativeFullscreenRevenue();

        #endregion

        #region 原生小广告功能

        bool IsNativeSmallAvailable();
        void ShowNativeSmall();
        void RemoveNativeSmall();
        double GetNativeSmallRevenue();

        #endregion
    }
}
