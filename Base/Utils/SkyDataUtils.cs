using System.Collections;
using System.Collections.Generic;
using System.Globalization;
//using Google.MiniJSON;
using Newtonsoft.Json;
using UnityEngine;

namespace Sky
{
    public static class SkyDataUtils
    {
        public static double StringToDouble(string str)
        {
            double result = 0;
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                Debug.Log("转换成功: " + result);
            }
            else
            {
                Debug.Log("转换失败：字符串格式不正确");
            }
            return result;
        }
        
        public static string GetVersionCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
            using (var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", 
                currentActivity.Call<string>("getPackageName"), 0))
            {
                return packageInfo.Get<int>("versionCode").ToString();
            }
#else
            return "1";
#endif
        }
        public static string ConvertDictToJsonString(Dictionary<string, object> dictionary)
        {

            return JsonConvert.SerializeObject(dictionary);
        }
        public static T[] FromJsonArray<T>(string json)
        {
            string wrappedJson = $"{{\"items\":{json}}}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
            return wrapper.items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }
}

