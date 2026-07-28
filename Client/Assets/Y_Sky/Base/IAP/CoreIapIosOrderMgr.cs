#if IAP_ENABLE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreLite
{
    public class CoreIapIosOrderMgr : CoreSingleton<CoreIapIosOrderMgr>
    {
        
        private static string _orderMap = "iOSOrderMap";
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string GetCountryCode();

#endif
        public static string StoreCountryCode()
        {
#if UNITY_EDITOR || UNITY_ANDROID
            return "";
#else 
            return GetCountryCode();            
#endif
        }

        public static string GenerateGuid()
        {     
            return System.Guid.NewGuid().ToString();    
        }

        public static Dictionary<string, string> GetOrder(string sActorID)
        {
            if (ES3.KeyExists(_orderMap))
            {
                List<Dictionary<string,string>> list = ES3.Load(_orderMap) as List<Dictionary<string,string>>;
                var want = list.Find(e => e.ContainsValue(sActorID));
                if (want!= null) return want;
                return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 删除相同ID的全部数据
        /// </summary>
        /// <param name="productId"></param>
        public static void RemoveOrder(string productId)
        {
            if (ES3.KeyExists(_orderMap))
            {
                List<Dictionary<string,string>> list = ES3.Load(_orderMap) as List<Dictionary<string,string>>;
                var want = list.FindAll(e => e.ContainsValue(productId));
                if (want!= null)
                {
                    list.RemoveAll(want.Contains);
                    ES3.Save(_orderMap, list);
                    CoreLogUtils.Debug("[iap] [ios] RemoveOrder success"+productId);
                }
            }
        }

        public static void SaveOrder(Dictionary<string,string> _info)
        {
            if (ES3.KeyExists(_orderMap))
            {
                List<Dictionary<string,string>> list = ES3.Load(_orderMap) as List<Dictionary<string,string>>;

                var value = _info["actorID"];
                var want = list.Find(e => e.ContainsValue(value));
                if (want == null)
                {
                    list.Add(_info);
                    ES3.Save(_orderMap, list);
                    CoreLogUtils.Debug("[iap] [ios] 已经有表了，订单列表里没有这条订单，添加");
                }
                else
                {
                    CoreLogUtils.Debug("[iap] [ios] 已经有表了，订单列表里有这条订单，不再次添加");
                }
            }
            else
            {

                ES3.Save(_orderMap, new List<Dictionary<string,string>>{_info});
                CoreLogUtils.Debug("[iap] [ios] 首次添加订单到订单列表");
            }
        }

    }
}
#endif