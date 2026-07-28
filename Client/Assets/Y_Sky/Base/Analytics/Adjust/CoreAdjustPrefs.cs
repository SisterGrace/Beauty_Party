using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace CoreLite
{
    public static class CoreAdjustPrefs
    {
        public static string DeviceId
        {

            get => CorePrefs.GetString("device_id");
            set => CorePrefs.SetString("device_id", value);
        }
        /// <summary>
        /// adjust id
        /// </summary>
        public static string Adid
        {
            get => CorePrefs.GetString("adid");
            set => CorePrefs.SetString("adid", value);
        }

        /// <summary>
        /// google ad id 
        /// </summary>
        public static string Gaid
        {
            get => CorePrefs.GetString("gaid");
            set => CorePrefs.SetString("gaid", value);
        }

        /// <summary>
        /// ios idfa
        /// </summary>
        public static string IDFA
        {
            get => CorePrefs.GetString("idfa");
            set => CorePrefs.SetString("idfa", value);
        }

        /// <summary>
        /// ios idfv
        /// </summary>
        public static string IDFV
        {
            get => CorePrefs.GetString("idfv");
            set => CorePrefs.SetString("idfv", value);
        }

        /// <summary>
        /// adjust 识别的买量类型
        /// </summary>
        public static string AdjustNetwork
        {
            get => CorePrefs.GetString("adjust_network");
            set => CorePrefs.SetString("adjust_network", value);
        }

        public static string AdjustCampaign
        {
            get => CorePrefs.GetString("adjust_campaign");
            set => CorePrefs.SetString("adjust_campaign", value);
        }

        public static string AdjustAdgroup
        {
            get => CorePrefs.GetString("adjust_adgroup");
            set => CorePrefs.SetString("adjust_adgroup", value);
        }

        public static string AdjustCreative
        {
            get => CorePrefs.GetString("adjust_creative");
            set => CorePrefs.SetString("adjust_creative", value);
        }

        public static string AdjustClickLabel
        {
            get => CorePrefs.GetString("adjust_click_label");
            set => CorePrefs.SetString("adjust_click_label", value);
        }

        /// <summary>
        /// 是否已在某次启动中成功初始化过 Adjust，用于下次启动时走直接初始化逻辑
        /// </summary>
        public static bool HasInitialized
        {
            get => CorePrefs.GetBool("adjust_has_initialized", false);
            set => CorePrefs.SetBool("adjust_has_initialized", value);
        }
    }
}

