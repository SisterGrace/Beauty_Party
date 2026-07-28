//using CoreLite;
using System;

namespace Sky
{
    public static class SkySolarEnginePrefs
    {
        public static bool HasAttribution
        {
            get => CryptoPrefs.GetBool("se_has_attribution", false);
            set => CryptoPrefs.SetBool("se_has_attribution", value);
        }

        public static bool IsPaidUser
        {
            get => CryptoPrefs.GetBool("se_is_paid_user", false);
            set => CryptoPrefs.SetBool("se_is_paid_user", value);
        }

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

        // ---- 新增 ----
        public static bool IsFinalized
        {
            get => CryptoPrefs.GetBool("se_is_finalized", false);
            set => CryptoPrefs.SetBool("se_is_finalized", value);
        }

        public static long FirstStartTicks
        {
            get => CryptoPrefs.GetLong("se_first_start_ticks", 0);
            set => CryptoPrefs.SetLong("se_first_start_ticks", value);
        }

        public static int ElapsedSeconds
        {
            get => CryptoPrefs.GetInt("se_elapsed_seconds", 0);
            set => CryptoPrefs.SetInt("se_elapsed_seconds", value);
        }
    }
}