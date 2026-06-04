using System.Collections.Generic;
using Newtonsoft.Json;
using TK_Engine;
#if USE_UP2SDK
using CoreLite;
#endif

namespace GameLogic
{
    public partial class SDKManager
    {
        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, CommonEvent comEvent = default)
        {
#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + "公共属性：" + json);
            return;
#endif
            if (comEvent.level != 0)
            {
                LogCommonEvent(comEvent);
            }
#if USE_UP2SDK
            CoreSdkMgr.TrackEvent(_eventSort);
#endif
        }

        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, string _key, object _value, CommonEvent comEvent = default)
        {
#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + ";Key:" + _key + ",value:" + _value + "公共属性：" + json);
            return;
#endif
            if (comEvent.level != 0)
            {
                LogCommonEvent(comEvent);
            }
#if USE_UP2SDK
            CoreSdkMgr.TrackEvent(_eventSort, _key, _value);
#endif
        }

        /// <summary>
        /// Log Event
        /// </summary>
        public void LogEvent(string _eventSort, string _key01, object _value01, string _key02, object _value02, CommonEvent comEvent = default)
        {
#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + ";Key:" + _key01 + ",value:" + _value01 + ";Key2:" + _key02 +
                     ",value2:" + _value02 + "公共属性：" + json);
            return;
#endif
            LogEvent(_eventSort, new Dictionary<string, object>()
            {
                { _key01, _value01 },
                { _key02, _value02 }
            }, comEvent);
        }

        /// <summary>
        ///  Log Event
        /// </summary>
        public void LogEvent(string _eventSort, Dictionary<string, object> _eventDic, CommonEvent comEvent = default)
        {
#if UNITY_EDITOR
            string json = JsonConvert.SerializeObject(_eventDic, Formatting.Indented);
            string json1 = JsonConvert.SerializeObject(comEvent, Formatting.Indented);
            Log.Info("SDKEvent:" + _eventSort + ";" + "eventDic:" + json + "公共属性：" + json1);
            return;
#endif
            if (comEvent.level != 0)
            {
                LogCommonEvent(comEvent);
            }
#if USE_UP2SDK
            CoreSdkMgr.TrackEvent(_eventSort, _eventDic);
#endif
        }

        /// <summary>
        /// 设置公共属性
        /// </summary>
        public void LogCommonEvent(CommonEvent comEvent)
        {
            int _level = comEvent.level;
#if UNITY_EDITOR
            Log.Info($"公共属性：关卡序员={_level}");
            return;
#endif
            Dictionary<string, object> eventDic = new();
            eventDic.Add("Level", _level);
#if USE_UP2SDK
            CoreSdkMgr.SetSuperProperties(eventDic);
#endif
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
            Dictionary<string, object> eventDic = new()
            {
                { "User_Current_Gold", _golds.ToString() }
            };
#if USE_UP2SDK
            CoreSdkMgr.UserSet(eventDic);
#endif
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
