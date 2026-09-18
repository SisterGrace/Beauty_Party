using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Sky
{
    public static class SkyPrefs 
    {
        private const string KeyPrefix = "Sky_";
        private const int TrueValue = 1;
        private const int FalseValue = 0;
        public static  async Task<string> GetOrCreateDeviceId()
        {
            if (!string.IsNullOrEmpty(SkyPrefs.UserId))
                return SkyPrefs.UserId;
            var deviceId = "";
#if UNITY_ANDROID
            if (!string.IsNullOrEmpty(SkyAdjustPrefs.Gaid))
            {
                return SkyAdjustPrefs.Gaid;
            }
            deviceId = await GetGaid();
            if (!string.IsNullOrEmpty(deviceId))
            {
                SkyAdjustPrefs.Gaid = deviceId; // 缓存结果
            }
#else
            deviceId = SkyAdjustPrefs.IDFA;
            if(deviceId.Contains("00000000"))deviceId = SkyAdjustPrefs.IDFV;

#endif
            SkyLogUtils.Debug("GetOrCreateDeviceId:"+deviceId);
            if(!string.IsNullOrEmpty(deviceId)) return deviceId;
            return GenerateNewDeviceId();
        }

        public static Task<string> GetGaid()
        {
            return Task.Run(() => GetGoogleAdvertisingId());
        }

        public static string GetGoogleAdvertisingId()
        {
            // 仅在Android平台上有效
            if (Application.platform != RuntimePlatform.Android)
            {
                SkyLogUtils.Warning("GAID only available on Android devices");
                return null;
            }

            try
            {
                // 使用AndroidJavaClass调用Google Play服务API
                using var advertisingIdClient = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                // 调用getAdvertisingIdInfo方法获取广告ID信息
                using var adInfo = advertisingIdClient.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
                // 获取GAID字符串
                var gaid = adInfo.Call<string>("getId");
                // 检查用户是否限制了广告跟踪
                var isLimitAdTrackingEnabled = adInfo.Call<bool>("isLimitAdTrackingEnabled");

                // 如果用户限制了广告跟踪，可能需要考虑不使用该ID
                return gaid;
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Debug($"Failed to get GAID: {e.Message}");
                return null;
            }
        }


        private static string GenerateNewDeviceId()
        {
            string newDeviceId = System.Guid.NewGuid().ToString();            
            return newDeviceId;
        }

        public static bool IsRemoveAds
        {
            get => SkyPrefs.GetBool("is_remove_ads",false);
            set => SkyPrefs.SetBool("is_remove_ads", value);
        }

        /// <summary>
        /// game user id
        /// </summary>
        public static string GameUserId
        {
            get => SkyPrefs.GetString("game_user_id","0");
            set => SkyPrefs.SetString("game_user_id", value);
        }
        
        /// <summary>
        /// user id
        /// </summary>
        public static string UserId
        {
            get => SkyPrefs.GetString("user_id");
            set => SkyPrefs.SetString("user_id", value);
        }


        public static int UserAtt
        {
            get => SkyPrefs.GetInt("user_att");
            set => SkyPrefs.SetInt("user_att", value);
        }
        /// <summary>
        /// 当前账号的类型
        /// </summary>
        public static int UserType
        {
            get => SkyPrefs.GetInt("use_type");
            set => SkyPrefs.SetInt("user_type", value);
        }

        /// <summary>
        /// 处理键名，添加前缀
        /// </summary>
        private static string ProcessKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            
            if (key.StartsWith(KeyPrefix))
                return key;
                
            return $"{KeyPrefix}{key}";
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public static bool HasKey(string key)
        {
            var processedKey = ProcessKey(key);
            return !string.IsNullOrEmpty(processedKey) && PlayerPrefs.HasKey(processedKey);
        }

        /// <summary>
        /// 设置整数值
        /// </summary>
        public static void SetInt(string key, int value)
        {
            var processedKey = ProcessKey(key);
            if (!string.IsNullOrEmpty(processedKey))
            {
                PlayerPrefs.SetInt(processedKey, value);
            }
        }

        /// <summary>
        /// 获取整数值
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            var processedKey = ProcessKey(key);
            return !string.IsNullOrEmpty(processedKey) && PlayerPrefs.HasKey(processedKey)
                ? PlayerPrefs.GetInt(processedKey)
                : defaultValue;
        }

        /// <summary>
        /// 设置浮点数值
        /// </summary>
        public static void SetFloat(string key, float value)
        {
            var processedKey = ProcessKey(key);
            if (!string.IsNullOrEmpty(processedKey))
            {
                PlayerPrefs.SetFloat(processedKey, value);
            }
        }

        /// <summary>
        /// 获取浮点数值
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            var processedKey = ProcessKey(key);
            return !string.IsNullOrEmpty(processedKey) && PlayerPrefs.HasKey(processedKey)
                ? PlayerPrefs.GetFloat(processedKey)
                : defaultValue;
        }

        /// <summary>
        /// 设置字符串值
        /// </summary>
        public static void SetString(string key, string value)
        {
            var processedKey = ProcessKey(key);
            if (!string.IsNullOrEmpty(processedKey))
            {
                PlayerPrefs.SetString(processedKey, value ?? string.Empty);
            }
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        public static string GetString(string key, string defaultValue = "")
        {
            var processedKey = ProcessKey(key);
            return !string.IsNullOrEmpty(processedKey) && PlayerPrefs.HasKey(processedKey)
                ? PlayerPrefs.GetString(processedKey)
                : defaultValue;
        }

        /// <summary>
        /// 设置布尔值
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            SetInt(key, value ? TrueValue : FalseValue);
        }

        /// <summary>
        /// 获取布尔值
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            var processedKey = ProcessKey(key);
            if (string.IsNullOrEmpty(processedKey) || !PlayerPrefs.HasKey(processedKey))
                return defaultValue;

            int value = PlayerPrefs.GetInt(processedKey);
            return value == TrueValue;
        }

        /// <summary>
        /// 删除指定键
        /// </summary>
        public static void DeleteKey(string key)
        {
            var processedKey = ProcessKey(key);
            if (!string.IsNullOrEmpty(processedKey))
            {
                PlayerPrefs.DeleteKey(processedKey);
            }
        }

        /// <summary>
        /// 删除所有Sky前缀的键（安全删除）
        /// </summary>
        public static void DeleteAllSkyKeys()
        {
            PlayerPrefs.DeleteAll();
        }

        /// <summary>
        /// 强制保存数据
        /// </summary>
        public static void Save()
        {
            try
            {
                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
            }
        }

        /// <summary>
        /// 检查是否是第一次（使用SkyPrefs自身的方法）
        /// </summary>
        public static bool IsFirstTime(string key)
        {
            var processedKey = ProcessKey(key);
            if (string.IsNullOrEmpty(processedKey))
                return true;

            bool isFirstTime = !HasKey(processedKey);
            if (isFirstTime)
            {
                SetBool(processedKey, true);
                Save();
            }
            
            return isFirstTime;
        }
    }
}