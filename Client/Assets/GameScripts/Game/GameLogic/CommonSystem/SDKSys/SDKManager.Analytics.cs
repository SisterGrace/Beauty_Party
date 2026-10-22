using System.Collections.Generic;
using Newtonsoft.Json;
using TK_Engine;
using Sky;

namespace GameLogic
{
    public partial class SDKManager
    {
        // ES3 存储的 Key
        private const string LevelKey = "SDK_Current_Level";

        // 内存缓存，避免每次打点都读 ES3
        private static int? _cachedLevel;

        /// <summary>
        /// 当前关卡（首次读取走 ES3，之后走内存缓存；写入时同时更新缓存、ES3 和公共属性）
        /// </summary>
        private static int CurrentLevel
        {
            get
            {
                if (_cachedLevel == null)
                {
                    _cachedLevel = ES3.Load(LevelKey, 0);
                }
                return _cachedLevel.Value;
            }
            set
            {
                _cachedLevel = value;
                ES3.Save(LevelKey, value);
                
                // 更新公共属性到 SkyAnalyticsMgr
                var superProperties = new Dictionary<string, object>
                {
                    { "Level", value }
                };
                SkyAnalyticsMgr.SetSuperProperties(superProperties);
            }
        }

        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, CommonEvent comEvent = default)
        {
            // 如果传入了有效的关卡，更新记录
            if (comEvent.level != 0)
            {
                CurrentLevel = comEvent.level;
            }

#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + "公共属性：" + json + " 当前记录关卡:" + CurrentLevel);
            return;
#endif

            SkyAnalyticsMgr.TrackEvent(_eventSort);
        }

        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, string _key, object _value, CommonEvent comEvent = default)
        {
            if (comEvent.level != 0)
            {
                CurrentLevel = comEvent.level;
            }

#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + ";Key:" + _key + ",value:" + _value + "公共属性：" + json + " 当前记录关卡:" + CurrentLevel);
            return;
#endif

            SkyAnalyticsMgr.TrackEvent(_eventSort, _key, _value);
        }

        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, string _key01, object _value01, string _key02, object _value02, CommonEvent comEvent = default)
        {
            if (comEvent.level != 0)
            {
                CurrentLevel = comEvent.level;
            }

#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + ";Key:" + _key01 + ",value:" + _value01 + ";Key2:" + _key02 +
                     ",value2:" + _value02 + "公共属性：" + json + " 当前记录关卡:" + CurrentLevel);
            return;
#endif

            var eventDic = new Dictionary<string, object>
            {
                { _key01, _value01 },
                { _key02, _value02 }
            };
            SkyAnalyticsMgr.TrackEvent(_eventSort, eventDic);
        }

        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, Dictionary<string, object> _eventDic, CommonEvent comEvent = default)
        {
            if (comEvent.level != 0)
            {
                CurrentLevel = comEvent.level;
            }

#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(_eventDic, Formatting.Indented);
            string json1 = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + ";" + "eventDic:" + json + "公共属性：" + json1 + " 当前记录关卡:" + CurrentLevel);
            return;
#endif

            SkyAnalyticsMgr.TrackEvent(_eventSort, _eventDic);
        }

        /// <summary>
        /// 设置用户属性
        /// </summary>
        /// <param name="_golds">当前金币数量</param>
        public void LogUserEvent(int _golds)
        {
#if UNITY_EDITOR
            Log.Info($"用户属性：当前金币数量={_golds}");
            return;
#endif
            var eventDic = new Dictionary<string, object>
            {
                { "User_Current_Gold", _golds.ToString() }
            };
            SkyAnalyticsMgr.UserSet(eventDic);
        }
    }

    /// <summary>
    /// 公共属性结构体
    /// </summary>
    public struct CommonEvent
    {
        public int level;

        private static readonly CommonEvent _noneEvent = new CommonEvent(0);
        public static CommonEvent NoneEvent
        {
            get => _noneEvent;
        }
        /// <summary>
        /// 关卡公共
        /// </summary>
        public static CommonEvent ComLevelEvent
        {
            get => new CommonEvent(UserController.Instance.Level);
        }
        public CommonEvent(int _level)
        {
            level = _level;
        }
    }
}