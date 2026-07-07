/*using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;

namespace CoreLite
{
    public class FirebaseManager : MonoBehaviour
    {
        private static bool _isInitialized = false;

        void Start()
        {
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _isInitialized = true;
                    Debug.Log("[FirebaseManager] Firebase Analytics initialized.");

                    // ✅ 初始化成功后自动调用示例打点（无需手动触发）
                    AutoSendTestEvents();
                }
                else
                {
                    Debug.LogError($"[FirebaseManager] Failed: {task.Result}");
                }
            });
        }

        /// <summary>
        /// 自动发送一些测试事件，验证接入是否正常
        /// </summary>
        private void AutoSendTestEvents()
        {
            // 1. 简单事件
            LogEvent("firebase_initialized");

            // 2. 带参数的事件
            LogEvent("test_custom_event", "test_param", "hello_firebase");

            // 3. 解锁成就（使用字符串常量 "achievement_id"）
            LogAchievementUnlocked("test_achievement_001");

            // 4. 虚拟货币消费（使用字符串常量 "item_name", "virtual_currency_name", "value"）
            LogSpendVirtualCurrency("magic_sword", "gold", 100);

            // 5. 广告收入
            LogAdRevenue("TestAdPlatform", "TestNetwork", "rewarded_video", "rewarded", 0.01);

            Debug.Log("[FirebaseManager] Test events sent. Check debug logs or wait 4-6 hours for console.");
        }

        // ==================== 核心打点方法（公开静态）====================

        public static bool IsInitialized => _isInitialized;

        public static void LogEvent(string eventName)
        {
            if (!_isInitialized) return;
            FirebaseAnalytics.LogEvent(eventName);
        }

        public static void LogEvent(string eventName, string paramName, string paramValue)
        {
            if (!_isInitialized) return;
            FirebaseAnalytics.LogEvent(eventName, paramName, paramValue);
        }

        public static void LogEvent(string eventName, string paramName, long paramValue)
        {
            if (!_isInitialized) return;
            FirebaseAnalytics.LogEvent(eventName, paramName, paramValue);
        }

        public static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!_isInitialized || parameters == null) return;
            var paramList = new List<Parameter>();
            foreach (var kvp in parameters)
            {
                if (kvp.Value is string s)
                    paramList.Add(new Parameter(kvp.Key, s));
                else if (kvp.Value is long l)
                    paramList.Add(new Parameter(kvp.Key, l));
                else if (kvp.Value is int i)
                    paramList.Add(new Parameter(kvp.Key, i));
                else if (kvp.Value is double d)
                    paramList.Add(new Parameter(kvp.Key, d));
                else if (kvp.Value is float f)
                    paramList.Add(new Parameter(kvp.Key, f));
                else if (kvp.Value is bool b)
                    paramList.Add(new Parameter(kvp.Key, b ? 1 : 0));
                else if (kvp.Value != null)
                    paramList.Add(new Parameter(kvp.Key, kvp.Value.ToString()));
            }
            FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
        }

        // ==================== 游戏常用事件封装（全部使用字符串常量，避免编译错误）====================

        /// <summary>
        /// 广告收入
        /// </summary>
        public static void LogAdRevenue(string adPlatform, string adSource, string adUnit, string adFormat, double revenue)
        {
            if (!_isInitialized) return;
            var parameters = new Parameter[]
            {
                new Parameter("ad_platform", adPlatform),
                new Parameter("ad_source", adSource),
                new Parameter("ad_unit_name", adUnit),
                new Parameter("ad_format", adFormat),
                new Parameter("value", revenue),
                new Parameter("currency", "USD")
            };
            FirebaseAnalytics.LogEvent("ad_impression", parameters);
        }

        /// <summary>
        /// 解锁成就
        /// </summary>
        public static void LogAchievementUnlocked(string achievementId)
        {
            if (!_isInitialized) return;
            // 直接使用字符串 "achievement_id" 而不是不存在的常量
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventUnlockAchievement, "achievement_id", achievementId);
        }

        /// <summary>
        /// 虚拟货币消费
        /// </summary>
        public static void LogSpendVirtualCurrency(string itemName, string currencyName, long amount)
        {
            if (!_isInitialized) return;
            var parameters = new Parameter[]
            {
                new Parameter("item_name", itemName),
                new Parameter("virtual_currency_name", currencyName),
                new Parameter("value", amount)
            };
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSpendVirtualCurrency, parameters);
        }

        /// <summary>
        /// 登录事件
        /// </summary>
        public static void LogLogin(string method)
        {
            if (!_isInitialized) return;
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLogin, "method", method);
        }

        /// <summary>
        /// 通关/分数事件
        /// </summary>
        public static void LogPostScore(long score, string levelName = null)
        {
            if (!_isInitialized) return;
            if (!string.IsNullOrEmpty(levelName))
            {
                var parameters = new Parameter[]
                {
                    new Parameter("score", score),
                    new Parameter("level_name", levelName)
                };
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPostScore, parameters);
            }
            else
            {
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPostScore, "score", score);
            }
        }

        /// <summary>
        /// 分享事件
        /// </summary>
        public static void LogShare(string contentType = null, string itemId = null, string method = null)
        {
            if (!_isInitialized) return;
            var parameters = new List<Parameter>();
            if (!string.IsNullOrEmpty(contentType))
                parameters.Add(new Parameter("content_type", contentType));
            if (!string.IsNullOrEmpty(itemId))
                parameters.Add(new Parameter("item_id", itemId));
            if (!string.IsNullOrEmpty(method))
                parameters.Add(new Parameter("method", method));
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventShare, parameters.ToArray());
        }
    }
}*/