using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sky
{
    public static class SkyAdjustPrefs
    {
        public static string DeviceId
        {

            get => SkyPrefs.GetString("device_id");
            set => SkyPrefs.SetString("device_id", value);
        }
        /// <summary>
        /// adjust id
        /// </summary>
        public static string Adid
        {
            get => SkyPrefs.GetString("adid");
            set => SkyPrefs.SetString("adid", value);
        }

        /// <summary>
        /// google ad id 
        /// </summary>
        public static string Gaid
        {
            get => SkyPrefs.GetString("gaid");
            set => SkyPrefs.SetString("gaid", value);
        }

        /// <summary>
        /// ios idfa
        /// </summary>
        public static string IDFA
        {
            get => SkyPrefs.GetString("idfa");
            set => SkyPrefs.SetString("idfa", value);
        }

        /// <summary>
        /// ios idfv
        /// </summary>
        public static string IDFV
        {
            get => SkyPrefs.GetString("idfv");
            set => SkyPrefs.SetString("idfv", value);
        }

        /// <summary>
        /// adjust 识别的买量类型
        /// </summary>
        public static string AdjustNetwork
        {
            get => SkyPrefs.GetString("adjust_network");
            set => SkyPrefs.SetString("adjust_network", value);
        }

        public static string AdjustCampaign
        {
            get => SkyPrefs.GetString("adjust_campaign");
            set => SkyPrefs.SetString("adjust_campaign", value);
        }

        public static string AdjustAdgroup
        {
            get => SkyPrefs.GetString("adjust_adgroup");
            set => SkyPrefs.SetString("adjust_adgroup", value);
        }

        public static string AdjustCreative
        {
            get => SkyPrefs.GetString("adjust_creative");
            set => SkyPrefs.SetString("adjust_creative", value);
        }

        public static string AdjustClickLabel
        {
            get => SkyPrefs.GetString("adjust_click_label");
            set => SkyPrefs.SetString("adjust_click_label", value);
        }

        /// <summary>
        /// 是否已在某次启动中成功初始化过 Adjust，用于下次启动时走直接初始化逻辑
        /// </summary>
        public static bool HasInitialized
        {
            get => SkyPrefs.GetBool("adjust_has_initialized", false);
            set => SkyPrefs.SetBool("adjust_has_initialized", value);
        }
    }
}

