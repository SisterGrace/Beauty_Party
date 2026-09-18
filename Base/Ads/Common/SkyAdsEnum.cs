using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    [System.Serializable]
    public struct HandCallbackInfo
    {
        public string msg;
        /// <summary>
        /// 插屏广告是否已准备好（加载完成且可展示）
        /// </summary>
        public bool isReady;
        /// <summary>
        /// 在插屏规则下是否允许展示（频次等规则允许），与 isReady 无关。
        /// 当 isReady 为 false 但 frequencyAllowsShow 为 true 时，可用激励视频代替插屏（插屏/激励视频竞价场景）。
        /// </summary>
        public bool frequencyAllowsShow;
    }
    
    public enum SkyAdsType
    {
        Rewarded,
        Interstitial,
        Banner,
        Native,
        Splash,
        /// <summary> 全屏 Native（多广告位竞价），在线参数 admob_native_fullscreen_units。 </summary>
        NativeFullscreen,
        /// <summary> Small 模板 Native（多广告位竞价），在线参数 admob_native_small_units。 </summary>
        NativeSmall,
    }

    public enum IvType
    {
        NULL = -1,
        IV1 = 1,
        IV2 = 2,
        IV3 = 3,
        IV4 = 4,
        IV5 = 5,
        IV6 = 6,
        IV7 = 7,
        IV8 = 8
    }

    /// <summary>
    /// H5 广告展示上报使用的平台枚举，用于 Adjust AdRevenueNetwork、Firebase/ThinkingData 的 ad_platform 与 ad_source。
    /// </summary>
    public enum SkyH5AdPlatform
    {
        SpinGO,
        Okspin
    }
}
