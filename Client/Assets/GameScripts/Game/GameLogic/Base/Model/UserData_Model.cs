using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Config.common;
using Config.config;
using TK_Engine;
using Y_Base;

namespace GameLogic
{
    public partial class UserModel
    {
    }

    /// <summary>
    /// 道具信息
    /// </summary>
    [Serializable]
    public class PropInfo
    {
        public PropType mType;
        public bool IsUnlock; //是否解锁
        public int Num; //道具数量
        public long EndTime; //无限畅用过期时间
        public bool IsFree; //是否免费使用
        public bool IsFirst; //是否是首次使用
        public PropInfo()
        {
        }

        public PropInfo(PropType type, bool Unlock, int num,bool isFree)
        {
            mType = type;
            IsUnlock = Unlock;
            Num = num;
            IsFree = isFree;
            IsFirst = true;
        }

        public void AddTime(long time)
        {
            if (EndTime > 0)
            {
                EndTime += time;
            }
            else
            {
                EndTime = TimeUtil.GetTimeStamp() + time;
            }
        }

        public bool CheckTime()
        {
            if (IsFree)
            {
                PropsController.Instance.SetConsumelist(this, true);
                return false;
            }
            if (EndTime > 0)
            {
                var t = EndTime - TimeUtil.GetTimeStamp();
                if (t <= 0)
                {
                    EndTime = 0;
                    PropsController.Instance.SetConsumelist(this, false);
                    return true;
                }
                else
                {
                    if (IsUnlock)
                    {
                        PropsController.Instance.SetConsumelist(this, true);
                    }
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 活动数据
    /// </summary>
    [Serializable]
    public class ActityData
    {
        public ActivityType Id;
        public long EndTime;
        public List<int> BuyCount;
        public bool IsTimeEnded;//是否时间到期活动结束
        public List<int> RewGetList;  //无尽宝藏或者礼品包领取领取列表
        public bool IsOpen;
        //是否进入disCord活动
        public bool IsDiscardOpen;
        
        #region 梦想通行证数据
        public bool IsOpenDreamPass => UserController.Instance.GetOpenDreamPass;//是否开启梦想通行证
        public int CurrDreamPassLevel;//当前梦想通行证等级
        // 普通奖励领取记录：key存在即表示普通已领
        public Dictionary<int, bool> NormalGotten;
        // 高级奖励领取记录：key存在且value==true表示高级已领
        public Dictionary<int, bool> SpecialGotten;
        #endregion

        #region 七日签到数据
        /// <summary>
        /// 上次签到时的零点时间戳（用于判断今天是否已签到）
        /// </summary>
        public long LastCheckInZeroTime;
        /// <summary>
        /// 当前签到进度（1-7，表示下一次要签的是第几天）
        /// </summary>
        public int CurrentCheckInDay;
        /// <summary>
        /// 是否已购买高级签到(暂时用不上)
        /// </summary>
        public bool IsPremiumCheckIn;
        #endregion

      
        
        //对于无限循环活动，记录开启的第几轮
        public int LoopCount;
        
        public ActityData()
        {
            
        }

        public ActityData(CfgActivity cfg)
        {
            Id = cfg.ActType;
            BuyCount = new List<int>();
            IsTimeEnded = cfg.TimeClose;
            LoopCount = 0;
            IsOpen = false;
            IsDiscardOpen = false;
            if (cfg.ActTime > 0)
            {
                EndTime = TimeUtil.GetTimeStamp() + cfg.ActTime;
            }
            switch (cfg.ActType)
            {
                case ActivityType.DreamPass:
                    CurrDreamPassLevel = 1;
                    NormalGotten = new Dictionary<int, bool>();
                    SpecialGotten = new Dictionary<int, bool>();
                    break;
                case ActivityType.UntoldTreasure:
                    RewGetList = new List<int>();
                    break;
                case ActivityType.DayCheckIn:
                    CurrentCheckInDay = 1;
                    LastCheckInZeroTime = 0;
                    IsPremiumCheckIn = false;
                    break;
            }
        }

        public void CheckTime()
        {
            if (EndTime > 0)
            {
                var t = EndTime - TimeUtil.GetTimeStamp();
                if (t <= 0)
                {
                    EndTime = 0;
                    var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(Id);
                    if(cfg != null)
                    {
                        if(cfg.TimeClose)
                        {
                           ActivityController.Instance.RemoveActivity(Id);
                        }
                    }
                    //ActivityController.Instance.RemoveActivity(Id);
                }
            }
        }

        public bool IsRemove()
        {
            //如果不是时间到期活动，则判断是否达到条件而关闭
            if (!IsTimeEnded)
            {
                switch (Id)
                {
                    case ActivityType.DreamPass:
                        // 防御旧存档：字典可能为null
                        if (NormalGotten == null) NormalGotten = new Dictionary<int, bool>();
                        if (SpecialGotten == null) SpecialGotten = new Dictionary<int, bool>();
                        var allCfgs = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
                        bool allGotten = true;
                        foreach (var cfg in allCfgs)
                        {
                            bool normalGotten = NormalGotten.ContainsKey(cfg.Id);
                            bool specialGotten = SpecialGotten.ContainsKey(cfg.Id) && SpecialGotten[cfg.Id];
                            if (!normalGotten || (IsOpenDreamPass && !specialGotten))
                            {
                                allGotten = false;
                                break;
                            }
                        }
                        return allGotten && NormalGotten.Count == allCfgs.Count && IsOpenDreamPass;
                    case ActivityType.Adfree:
                        return UserController.Instance.AdFree;
                    case ActivityType.UntoldTreasure:
                        return false;
                    case ActivityType.NewPlayerPack:
                        return UserController.Instance.GetBugStarterPack;
                    case ActivityType.DayCheckIn:
                        return IsAllCheckInClaimed();
                    case ActivityType.Discard:
                        return IsDiscardOpen;
                }
            }
            if (EndTime > 0&&IsTimeEnded)
            {
                var t = EndTime - TimeUtil.GetTimeStamp();
                if (t <= 0)
                {
                    return true;
                }
            }
            return false;
        }
        public void AddAccruePassLevel(int level)
        {
            //如果当前关卡数大于当前通行证奖励
            var passCfgs = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
            if (BuyCount.Count == passCfgs.Count) return; //如果已经解锁了所有奖励，则不进行解锁
            bool found = false;
            for (int i = 0; i < passCfgs.Count; i++)
            {
                if (level > passCfgs[i].NeedKeyCount)
                {
                    if (!BuyCount.Contains(passCfgs[i].Id))
                    {
                        BuyCount.Add(passCfgs[i].Id);
                        SDKManager.Instance.LogEvent("Pass_Level_Unlock", new Dictionary<string, object>()
                        {
                            { "Pass_Level", passCfgs[i].Id }
                        });
                    }
                }
                if (level <= passCfgs[i].NeedKeyCount)
                {
                    CurrDreamPassLevel = passCfgs[i].Id;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                CurrDreamPassLevel = passCfgs[passCfgs.Count - 1].Id + 1;
            }
            CheckDreamPassGetRew(true,out int mcount);
        }
        public bool CheckDreamPassGetRew(bool DoEvent,out int Count)
        {
            Count = 0;
            // 防御旧存档：字典可能为null
            if (NormalGotten == null) NormalGotten = new Dictionary<int, bool>();
            if (SpecialGotten == null) SpecialGotten = new Dictionary<int, bool>();
            var _allPassList = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
            //检测未领取的奖励数量
            for (int i = 0; i < _allPassList.Count; i++)
            {
                var cfg = _allPassList[i];
                bool normalCanGet = BuyCount.Contains(cfg.Id) && !NormalGotten.ContainsKey(cfg.Id);
                bool specialCanGet = IsOpenDreamPass && (!SpecialGotten.ContainsKey(cfg.Id) || !SpecialGotten[cfg.Id]);
                if (normalCanGet || specialCanGet)
                {
                    Count++;
                }
            }
            if (DoEvent)
            {
                GameEvent.Send(EventType.DreamRedPoint,Count > 0,$"{Count}");
            }
            return false;
        }

        #region 七日签到方法
        /// <summary>
        /// 检查今天是否可以签到
        /// </summary>
        public bool CanCheckInToday()
        {
            long todayZero = TimeUtil.GetTimeStampToZero();
            // 今天零点 != 上次签到零点，且还没签满7天
            return LastCheckInZeroTime != todayZero && CurrentCheckInDay <= 7;
        }

        /// <summary>
        /// 执行签到，返回签到的天数（用于发奖励），返回0表示签到失败
        /// </summary>
        public int DoCheckIn()
        {
            if (!CanCheckInToday()) return 0;

            int claimedDay = CurrentCheckInDay;
            LastCheckInZeroTime = TimeUtil.GetTimeStampToZero();
            CurrentCheckInDay++;
            return claimedDay;
        }

        /// <summary>
        /// 获取指定天数的签到状态
        /// </summary>
        public DayCheckState GetDayState(int day)
        {
            if (day < CurrentCheckInDay)
                return DayCheckState.Claimed;      // 已领取

            if (day == CurrentCheckInDay && CanCheckInToday())
                return DayCheckState.Available;    // 可领取

            return DayCheckState.Locked;           // 未解锁
        }

        /// <summary>
        /// 是否完成7天签到
        /// </summary>
        public bool IsAllCheckInClaimed()
        {
            return CurrentCheckInDay > 7;
        }

        /// <summary>
        /// 重置签到周期（7天结束后开始新一轮）
        /// </summary>
        public void ResetCheckInCycle()
        {
            CurrentCheckInDay = 1;
            LastCheckInZeroTime = 0;
            LoopCount++;
        }

        /// <summary>
        /// 获取距离明天零点的剩余秒数（用于UI倒计时）
        /// </summary>
        public long GetSecondsToNextDay()
        {
            return TimeUtil.GetTimeStampToNextZero(1) - TimeUtil.GetTimeStamp();
        }

        /// <summary>
        /// 检查七日签到是否有可领取奖励（用于红点）
        /// </summary>
        public bool CheckDayCheckInRedPoint()
        {
            return CanCheckInToday();
        }
        #endregion
    }

    /// <summary>
    /// 签到状态枚举
    /// </summary>
    public enum DayCheckState
    {
        Locked = 0,     // 未解锁（还没轮到）
        Available = 1,  // 可领取（当前天）
        Claimed = 2     // 已领取
    }

    /// <summary>
    /// 用户相册数据
    /// </summary>
    [Serializable]
    public class UserPhotoData
    {
        //照片id
        public int Id;
        //图片AB面类型，0-A面，1-B面
        public int ABType;
        //照片来自，0-关卡，1-付费，活动，抽卡,2-付费
        public int FromType;
        //照片是否喜欢
        public bool IsLike;

        //是否AVPro缓存视频
        public bool IsAvProCache;
        //是否下载到相册
        public bool IsDownload;
        //是否查看过该照片
        public bool IsLook;
        public UserPhotoData()
        {
            
        }

        public UserPhotoData(int id, int abType, int fromType, bool isLike,bool isDownload =false)
        {
            Id = id;
            ABType = abType;
            FromType = fromType;
            IsLike = isLike;
            IsAvProCache = false;
            IsDownload = isDownload;
            IsLook = false;
        }
    }
}