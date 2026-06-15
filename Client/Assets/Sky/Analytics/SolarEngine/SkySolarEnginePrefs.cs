//using CoreLite;

namespace Sky
{
    // 【新增】SolarEngine 归因结果本地缓存。
    // 设计参照 CoreLite.CoreAdjustPrefs：把 SDK 回调的归因字段落到 PlayerPrefs，
    // App 第二次启动可在 SDK 回调到达前直接读缓存，给业务层 0 延迟的归因结果。
    //
    // 复用 CoreLite.CryptoPrefs（已在 Sky.asmdef 引用列表中），不重复实现 KV 存储。
    public static class SkySolarEnginePrefs
    {
        // 是否完成过一次成功的归因回调，用于热启动判断
        public static bool HasAttribution
        {
            get => CryptoPrefs.GetBool("se_has_attribution", false);
            set => CryptoPrefs.SetBool("se_has_attribution", value);
        }

        // 是否买量用户（!= "Organic" 视为买量），方便业务侧直接读
        public static bool IsPaidUser
        {
            get => CryptoPrefs.GetBool("se_is_paid_user", false);
            set => CryptoPrefs.SetBool("se_is_paid_user", value);
        }

        // 渠道名（自然量为 "Organic"），对齐 CoreAdjustPrefs.AdjustNetwork
        public static string Network
        {
            get => CryptoPrefs.GetString("se_network");
            set => CryptoPrefs.SetString("se_network", value);
        }

        public static string Campaign
        {
            get => CryptoPrefs.GetString("se_campaign");
            set => CryptoPrefs.SetString("se_campaign", value);
        }

        public static string Adgroup
        {
            get => CryptoPrefs.GetString("se_adgroup");
            set => CryptoPrefs.SetString("se_adgroup", value);
        }

        public static string Creative
        {
            get => CryptoPrefs.GetString("se_creative");
            set => CryptoPrefs.SetString("se_creative", value);
        }

        public static string ClickLabel
        {
            get => CryptoPrefs.GetString("se_click_label");
            set => CryptoPrefs.SetString("se_click_label", value);
        }
    }
}
