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
        private const string PictureIdKey = "SDK_Picture_ID";
        private const string PictureTypeKey = "SDK_Picture_Type";
        private const string GetTypeKey = "SDK_Get_Type";
        private const string CardIdKey = "SDK_Card_ID";

        // 内存缓存，避免每次打点都读 ES3
        private static int? _cachedLevel;
        private static int? _cachedPictureId;
        private static int? _cachedPictureType;
        private static int? _cachedGetType;
        private static int? _cachedCardId;

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
                UpdateAllSuperProperties();
            }
        }

        /// <summary>
        /// 图片ID（Picture_ID）
        /// </summary>
        private static int CurrentPictureId
        {
            get
            {
                if (_cachedPictureId == null)
                {
                    _cachedPictureId = ES3.Load(PictureIdKey, 0);
                }
                return _cachedPictureId.Value;
            }
            set
            {
                _cachedPictureId = value;
                ES3.Save(PictureIdKey, value);
                UpdateAllSuperProperties();
            }
        }

        /// <summary>
        /// 图片类型（Picture_Type）：1=免费图片, 2=激励视频图片, 3=VIP图片
        /// </summary>
        private static int CurrentPictureType
        {
            get
            {
                if (_cachedPictureType == null)
                {
                    _cachedPictureType = ES3.Load(PictureTypeKey, 0);
                }
                return _cachedPictureType.Value;
            }
            set
            {
                _cachedPictureType = value;
                ES3.Save(PictureTypeKey, value);
                UpdateAllSuperProperties();
            }
        }

        /// <summary>
        /// 获得类型（Get_Type）：1=普通关卡1号位, 2=普通关卡2号位, 3=普通关卡3号位/Boss关, 4=金币卡包, 6=付费卡包, 7=无尽宝藏, 8=礼品包, 9=黄金狂潮, 10=梦想通行证, 11=新手礼包, 12=商城礼包
        /// </summary>
        private static int CurrentGetType
        {
            get
            {
                if (_cachedGetType == null)
                {
                    _cachedGetType = ES3.Load(GetTypeKey, 0);
                }
                return _cachedGetType.Value;
            }
            set
            {
                _cachedGetType = value;
                ES3.Save(GetTypeKey, value);
                UpdateAllSuperProperties();
            }
        }

        /// <summary>
        /// 卡包ID（Card_ID）：如果没有卡包ID，则为0
        /// </summary>
        private static int CurrentCardId
        {
            get
            {
                if (_cachedCardId == null)
                {
                    _cachedCardId = ES3.Load(CardIdKey, 0);
                }
                return _cachedCardId.Value;
            }
            set
            {
                _cachedCardId = value;
                ES3.Save(CardIdKey, value);
                UpdateAllSuperProperties();
            }
        }

        /// <summary>
        /// 统一更新所有公共属性到 SkyAnalyticsMgr
        /// </summary>
        private static void UpdateAllSuperProperties()
        {
            var superProperties = new Dictionary<string, object>
            {
                { "Level", CurrentLevel }
            };

            // Picture_ID: 0 视为无效值，不上报
            if (CurrentPictureId != 0)
            {
                superProperties["Picture_ID"] = CurrentPictureId;
            }

            // Picture_Type: 0 视为无效值，不上报
            if (CurrentPictureType != 0)
            {
                superProperties["Picture_Type"] = CurrentPictureType;
            }

            // Get_Type: 0 视为无效值，不上报
            if (CurrentGetType != 0)
            {
                superProperties["Get_Type"] = CurrentGetType;
            }

            // Card_ID: 0 视为无卡包ID，上报为 null
            if (CurrentCardId != 0)
            {
                superProperties["Card_ID"] = CurrentCardId;
            }
            else
            {
                superProperties["Card_ID"] = null;
            }

            SkyAnalyticsMgr.SetSuperProperties(superProperties);
        }

        /// <summary>
        /// 设置图片相关公共属性
        /// </summary>
        /// <param name="pictureId">图片ID</param>
        /// <param name="pictureType">图片类型：1=免费图片, 2=激励视频图片, 3=VIP图片</param>
        /// <param name="getType">获得类型：1=普通关卡1号位, 2=普通关卡2号位, 3=普通关卡3号位/Boss关, 4=金币卡包, 6=付费卡包, 7=无尽宝藏, 8=礼品包, 9=黄金狂潮, 10=梦想通行证, 11=新手礼包, 12=商城礼包</param>
        /// <param name="cardId">卡包ID，如果没有卡包ID传0</param>
        public void SetPictureProperties(int pictureId, int pictureType, int getType, int cardId = 0)
        {
            CurrentPictureId = pictureId;
            CurrentPictureType = pictureType;
            CurrentGetType = getType;
            CurrentCardId = cardId;
        }

        /// <summary>
        /// 清除图片相关公共属性（用于图片事件结束后重置）
        /// </summary>
        public void ClearPictureProperties()
        {
            CurrentPictureId = 0;
            CurrentPictureType = 0;
            CurrentGetType = 0;
            CurrentCardId = 0;
        }

        /// <summary>
        /// 根据关卡选择的图片信息设置图片公共属性（Picture_ID / Picture_Type / Get_Type / Card_ID）
        /// 在 Level_Enter / Level_Choose / Level_Success 等关卡事件上报前调用，
        /// 保证这几个事件带上当前关卡所选的图片信息。
        /// </summary>
        /// <param name="photoInfo">当前关卡选择的图片信息</param>
        public void SetLevelPictureProperties(OneSelectPhotoInfo photoInfo)
        {
            if (photoInfo.Id == 0) return;

            // Boss关固定为 3 号位，普通关卡按索引为 1/2/3 号位
            int getType = photoInfo.BoosLevel ? 3 : (photoInfo.Index + 1);
            //SetPictureProperties(photoInfo.Id, photoInfo.Picture_Type, getType, 0);
            SetPictureProperties(photoInfo.Id, 0, 0, 0);
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