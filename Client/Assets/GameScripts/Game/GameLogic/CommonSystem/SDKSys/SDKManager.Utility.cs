using Config;
using Config.common;
using Newtonsoft.Json;
using TK_Engine;
using UnityEngine;
#if USE_UP2SDK
using CoreLite;
#endif

namespace GameLogic
{
    public partial class SDKManager
    {
        #region 在线参数

        /// <summary>
        /// 在线参数激活回调
        /// </summary>
        private void GetRemoteConfigCall()
        {
            Log.Info($"执行在线参数回调");
            var tIsPromote = IsPromote;
            ActivityPhotoRemoteHelper.ApplyAllOverrides(ConfigLoader.Constant.LEVELPHOTOKEY, ConfigLoader.Constant.IAPSwitchKey);

            //获取H5CheckInfo
            string h5CheckInfoJson = GetRemoteConfigString("H5_Activity_Show", "");
            H5CheckInfo = H5CheckInfo.FromJson(h5CheckInfoJson);
            Log.Info($"H5CheckInfo: {H5CheckInfo.H5CheckOpen} {H5CheckInfo.H5CheckType}");
            SyncH5CheckActivity();
        }

        /// <summary>
        /// 获取在线参数数据
        /// </summary>
        public static int GetRemoteConfigInt(string key, int dValue)
        {
#if USE_UP2SDK
            return CoreSdkMgr.GetConfigInt(key, dValue);
#else
            return dValue;
#endif
        }

        /// <summary>
        /// 获取在线参数数据
        /// </summary>
        public static string GetRemoteConfigString(string key, string dValue)
        {
#if USE_UP2SDK
            return CoreSdkMgr.GetConfigStr(key, dValue);
#else
            return dValue;
#endif
        }

        /// <summary>
        /// 获取在线参数数据
        /// </summary>
        public static bool GetRemoteConfigBool(string key, bool dValue)
        {
#if USE_UP2SDK
            return CoreSdkMgr.GetConfigBool(key, dValue);
#else
            return dValue;
#endif
        }

        /// <summary>
        /// 在线参数到达/变化时同步 H5Check 活动状态
        /// </summary>
        private void SyncH5CheckActivity()
        {
            if (!UserController.Instance.IsInit) return;

            var actDic = UserController.Instance.CurActivityData;
            if (H5CheckInfo.H5CheckOpen)
            {
                if (!actDic.ContainsKey(ActivityType.H5Check))
                {
                    ActivityController.Instance.CheckActivityOpen(ActivityType.H5Check);
                }
            }
            else
            {
                if (actDic.ContainsKey(ActivityType.H5Check))
                {
                    ActivityController.Instance.RemoveActivity(ActivityType.H5Check);
                }
            }
        }

        #endregion

        #region 杂项工具

        /// <summary>
        /// 游戏震动
        /// </summary>
        /// <param name="shakeType">震动强弱：0:轻微 1:中度 2:重度</param>
        /// <param name="intensity">震动时间: 0 ~ 1</param>
        public static void GameShake(int shakeType = 0, int intensity = 1)
        {
            bool isShake = GameModule.Setting.GetBool(Constant.Setting.Vibrate, true);
            if (!isShake) return;
            if (shakeType == 0)
            {
                Vibration.VibratePeek();
            }
            else if (shakeType == 1)
            {
                Vibration.VibratePop();
            }
        }

        /// <summary>
        /// 打开隐私政策
        /// </summary>
        public static void OpenPrivacy()
        {
            //Debug.Log("Application.OpenURL(ConfigLoader.Constant.PrivacyUrl)");
            Debug.Log(ConfigLoader.Constant.PrivacyUrl);
            //Application.OpenURL(ConfigLoader.Constant.PrivacyUrl);
            Application.OpenURL("https://powerxg-yl.github.io/privacy-policy-GlamTileBeautyParty/");
        }

        /// 买量用户归因回调
        private void UserSourceListenerCallback(bool promote, string network, string clickLabe)
        {
            _isPromote = promote;
            Log.Info("是否为买量用户：" + promote + " network：" + network + " clickLable：" + clickLabe);
            /*if(promote)
            {
                GameEvent.Send(EventType.SetUserType, 1);
            }
            else
            {
                GameEvent.Send(EventType.SetUserType, 0);
            }*/
            
        }

#if USE_UP2SDK
        /// <summary>
        /// 返回APP是否下架
        /// </summary>
        public void GetAPPState(CoreGameConfig.AppStatus state, string msg)
        {
            Log.Info("CoreLite get game config appState:" + state + " msg:" + msg);
            IsRemovedApp = (int)state == 3;
            if (IsRemovedApp)
            {
                GetProductListData();
            }
        }
#endif

        public static string GetSDKVersion()
        {
#if USE_UP2SDK
            return CoreSdkMgr.GetSDKVersion();
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// 设置用户ASN
        /// </summary>
        /// <param name="isASN">true：是asn用户；false：不是asn用户</param>
        public void SetUserASN(bool isASN)
        {
            Log.Info($"设置用户ASN: {isASN}");
            _isASN = isASN;
            if (isASN && UserController.Instance.IsInit)
            {
                UserController.Instance.GetCurUserType = 0;
            }
        }

        /// <summary>
        /// 获取商店国家代码
        /// </summary>
        public static string GetStoreCountryCode()
        {
#if USE_UP2SDK
            return CoreSdkMgr.GetStoreCountryCode();
#else
            return string.Empty;
#endif
        }

        #endregion
    }
    public struct H5CheckInfo
    {
        private const bool DefaultH5Check = false;
        private const int DefaultH5CheckType = 0;

        [JsonProperty("h5_check")]
        public bool H5CheckOpen;

        [JsonProperty("h5_type")]
        public int H5CheckType;
        
        public H5CheckInfo(bool h5CheckOpen, int h5CheckType)
        {
            H5CheckOpen = h5CheckOpen;
            H5CheckType = h5CheckType;
        }

        public static H5CheckInfo FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new H5CheckInfo(DefaultH5Check, DefaultH5CheckType);
            }
            return JsonConvert.DeserializeObject<H5CheckInfo>(json);
        }
    }
}
