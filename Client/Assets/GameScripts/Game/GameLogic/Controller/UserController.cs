using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using UnityEngine;
using GameBase;
using TK_Engine;
using System;

namespace GameLogic
{
    public class UserController : Singleton<UserController>
    {
        private UserModel _userModel;
        private float _remakeTaskValue;
        public bool IsInit => _userModel != null;
        private float _userDatatimer = 0;
        private float _userDatainterval = 3;

        public void InitUser()
        {
            _userDatatimer = 0;
            _userDatainterval = 3;
            _userModel = ES3.Load("UserModel", new UserModel(true));
            _userModel.ActivityDataDic.Remove(ActivityType.H5Check);
            OnlineparametersAB();
            //SDKManager.Instance.LogCommonEvent( _userModel.Level,0,_userModel.DayGameCount,_userModel.CurSceneId,_userModel.LifeData.Life);
            EveryDayUpdatePlayerData();
            GmController.Instance.Init();
            GuideController.Instance.Init();
            InitPhotoSeenConfigIfEmpty();
            //注册数据轮询
            Utility.Unity.AddUpdateListener(PollotUserData);
           
        }

        public void Save()
        {
            ES3.Save("UserModel", _userModel);
        }

        #region 用户属性

        public int ID
        {
            get => _userModel.Id;
            set
            {
                _userModel.Id = value;
                Save();
            }
        }

        //玩家钻石
        public int Diamond
        {
            get => _userModel.Diamond;
            set
            {
                _userModel.Diamond = value;
                GameEvent.Send(EventType.RefreshCurrency, 0);
                SDKManager.Instance.LogUserEvent(_userModel.Diamond);
                GameEvent.Send(EventType.SetGachaRedPoint, CheckGachaAdTime(), "");
                Save();
            }
        }

        //玩家关卡数
        public int Level
        {
            get => _userModel.Level;
            set
            {
                //当关卡数大于配置的时候，直接进入关卡循环模式
                if (value > LevelController.Instance.MapConfigs.Count)
                {
                    IsLoopLevel = true;
                    LoopLevelId_1 = value - LevelController.Instance.MapConfigs.Count - 1;
                }
                else
                {
                    IsLoopLevel = false;
                    LoopLevelId_1 = -1;
                }
                bool isUp = _userModel.Level < value;
                IsLevelUp = isUp;
                _userModel.Level = value;
                CheckUpLevel(isUp);
                //关卡数改变，则重置关卡刷新状态
                CurAdLevelRefreshState = 0;
                GameEvent.Send(EventType.RefreshLevel);
                Save();

            }
        }
        //关卡广告刷新状态，0-免费刷新，1-广告刷新，2-无刷新
        public int CurAdLevelRefreshState
        {
            get => _userModel.CurLevelIsAdRefresh;
            set
            {
                _userModel.CurLevelIsAdRefresh = value;
                Save();
            }
        }
        //游戏是否关卡循环模式
        public bool IsLoopLevel
        {
            get => _userModel.IsLevelloop;
            set
            {
                _userModel.IsLevelloop = value;
                Save();
            }
        }
        //当前关卡大循环id
        public int LoopLevelId_1
        {
            get => _userModel.LevelloopId_1;
            set
            {
                _userModel.LevelloopId_1 = value;
                Save();
            }
        }
        public List<PropInfo> GetAllPropsInfo
        {
            get => _userModel.Props;
        }
        #endregion

        #region 商店相关

        #region 照片商品工具方法

        private static bool ArePhotoIdsEqual(List<int> a, List<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!b.Contains(a[i])) return false;
            }
            return true;
        }

        public static List<int> GetPhotoIdsFromRewardList(List<TBRewardData> rewardList)
        {
            List<int> photoIds = new List<int>();
            foreach (var reward in rewardList)
            {
                if (reward.Type == ERewardType.Photo)
                {
                    if (reward.ID > 0) photoIds.Add(reward.ID);
                    if (reward.OtherID > 0) photoIds.Add(reward.OtherID);
                }
                else if (reward.Type == ERewardType.PhotoGroup)
                {
                    var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(reward.ID);
                    photoIds.AddRange(group.APhotoRews);
                    photoIds.AddRange(group.BPhotoRews);
                }
            }
            return photoIds;
        }

        /// <summary>
        /// 一次性获取所有照片商品的 giftId -> photoIds 映射
        /// </summary>
        public static Dictionary<int, List<int>> GetAllPhotoProductConfigs()
        {
            var result = new Dictionary<int, List<int>>();
            var allShopCfgs = ConfigLoader.Instance.Tables.TbCfgShop.DataList;
            var photoCfgs = allShopCfgs.FindAll(x => x.GoodType == mProductType.PhotoPack);
            for (int i = 0; i < photoCfgs.Count; i++)
            {
                var rechargeInfo = RechargeController.GetRechargeInfoByShop(photoCfgs[i].Id);
                var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId).Reward;
                result[rechargeInfo.Giftid] = GetPhotoIdsFromRewardList(rewardList);
            }
            return result;
        }

        #endregion

        public List<int> GetShopPackIds
        {
            get => _userModel.CurShopPackId;
        }
        public void AddeShopPackId(int id)
        {
            if (GetShopPackIds.Contains(id)) return;
            GetShopPackIds.Add(id);
            Save();
        }
        //重置照片购买项
        public void ResetResetShopPhotoId()
        {
            GetShopPhotoConfigDic.Clear();
            Save();
        }
        //获取商城照片配置字典（key=GiftId表示已购买，value=配置的照片ID列表）
        public Dictionary<int, List<int>> GetShopPhotoConfigDic
        {
            get
            {
                if (_userModel.ShopPhotoConfigDic == null)
                {
                    _userModel.ShopPhotoConfigDic = new Dictionary<int, List<int>>();
                }
                return _userModel.ShopPhotoConfigDic;
            }
        }
        /// <summary>
        /// 检查单个照片商品配置是否变化
        /// </summary>
        /// <returns>true表示配置未变化且已购买，false表示配置变化或未购买</returns>
        public bool CheckShopPhotoConfig(int giftId, List<int> photoIds)
        {
            if (!GetShopPhotoConfigDic.TryGetValue(giftId, out var oldPhotoIds))
            {
                return false;
            }

            if (ArePhotoIdsEqual(oldPhotoIds, photoIds))
            {
                return true;
            }

            GetShopPhotoConfigDic.Remove(giftId);
            Save();
            return false;
        }
        /// <summary>
        /// 添加商城照片购买记录（同时记录配置）
        /// </summary>
        public void AddShopPhotoWithConfig(int giftId, List<int> photoIds)
        {
            GetShopPhotoConfigDic[giftId] = new List<int>(photoIds);
            Save();
        }

        #region 照片商品更新检测

        public Dictionary<int, List<int>> GetShopPhotoSeenConfigDic
        {
            get
            {
                if (_userModel.ShopPhotoSeenConfigDic == null)
                {
                    _userModel.ShopPhotoSeenConfigDic = new Dictionary<int, List<int>>();
                }
                return _userModel.ShopPhotoSeenConfigDic;
            }
        }

        /// <summary>
        /// 游戏初始化时调用，若SeenConfig为空则写入当前所有照片商品配置作为基线
        /// </summary>
        public void InitPhotoSeenConfigIfEmpty()
        {
            if (GetShopPhotoSeenConfigDic.Count > 0) return;

            var allConfigs = GetAllPhotoProductConfigs();
            foreach (var kvp in allConfigs)
            {
                GetShopPhotoSeenConfigDic[kvp.Key] = kvp.Value;
            }
            Save();
        }

        /// <summary>
        /// 检测单个照片商品配置是否相对于已见配置发生了变化
        /// </summary>
        /// <returns>true表示配置已更新，false表示未更新或首次</returns>
        public bool IsPhotoProductUpdated(int giftId, List<int> currentPhotoIds)
        {
            if (!GetShopPhotoSeenConfigDic.TryGetValue(giftId, out var seenPhotoIds))
            {
                //找不到 Seen 记录视为新礼包，需要红点提示
                return true;
            }
            return !ArePhotoIdsEqual(seenPhotoIds, currentPhotoIds);
        }

        /// <summary>
        /// 批量保存当前配置为已见
        /// </summary>
        public void MarkPhotoProductsSeen(Dictionary<int, List<int>> allCurrentConfigs)
        {
            foreach (var kvp in allCurrentConfigs)
            {
                GetShopPhotoSeenConfigDic[kvp.Key] = new List<int>(kvp.Value);
            }
            Save();
            GameEvent.Send(EventType.ShopRedPoint);
        }

        /// <summary>
        /// 是否有任意照片商品配置发生了变化（供主界面红点使用）
        /// </summary>
        public bool HasAnyPhotoProductUpdated()
        {
            var allConfigs = GetAllPhotoProductConfigs();
            foreach (var kvp in allConfigs)
            {
                if (IsPhotoProductUpdated(kvp.Key, kvp.Value)) return true;
            }
            return false;
        }

        #endregion
        public long GetFreeGetGoldTime
        {
            get => _userModel.FreeGetGoldTime;
            set
            {
                _userModel.FreeGetGoldTime = value;
                //发送商城红点事件
                GameEvent.Send(EventType.ShopRedPoint);
                Save();
            }
        }
        //检测是否有免费领取钻石，false-有免费的，True-没有
        public bool CheckFreeGetGoldTime()
        {
            if (GetFreeGetGoldTime > 0)
            {
                long curTime = TimeUtil.GetTimeStamp();
                var diffTime = GetFreeGetGoldTime - curTime;
                if (diffTime <= 0)
                {
                    GetFreeGetGoldTime = 0;
                    return false;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region 活动相关

        public bool AdFree
        {
            get => _userModel.AdFree;
            set
            {
                _userModel.AdFree = value;
                Save();
            }
        }

        public bool GetOpenDreamPass
        {
            get => _userModel.ISOpenDreampass;
            set
            {
                _userModel.ISOpenDreampass = value;
                Save();
            }
        }

        public Dictionary<ActivityType, ActityData> CurActivityData
        {
            get => _userModel.ActivityDataDic;
        }
        public Dictionary<ActivityType, bool> ActivityEndDic
        {
            get => _userModel.ActivityEndDic;
        }
        public List<ActivityType> CurRemoveActivityList
        {
            get => _userModel.RemoveActivityList;
        }

        public List<ActityData> GetAllActivityData()
        {
            var list = new List<ActityData>();
            var curTime = TimeUtil.GetTimeStamp();
            foreach (var info in CurActivityData)
            {
                if (info.Value.IsTimeEnded)
                {
                    var endTime = info.Value.EndTime;
                    if (endTime == 0 || curTime < endTime)
                    {
                        list.Add(info.Value);
                    }
                }
                else
                {
                    list.Add(info.Value);
                }

            }
            return list;
        }

        public ActityData GetOneActivityData(ActivityType type)
        {
            if (CurActivityData.ContainsKey(type))
            {
                return CurActivityData[type];
            }
            return null;
        }
        //获取某活动是否永久结束
        public bool GetActivityEnd(ActivityType type)
        {
            if (ActivityEndDic.ContainsKey(type))
            {
                return ActivityEndDic[type];
            }
            return false;
        }
        public void CheckOneActivityTime(ActivityType type)
        {
            var data = GetOneActivityData(type);
            if (data == null) return;
            data.CheckTime();
            Save();
        }
        //设置活动首次进入
        public void SetActivityIsEnter(ActivityType type)
        {
            var data = GetOneActivityData(type);
            if (data == null || data.IsOpen) return;
            data.IsOpen = true;
            Save();
        }
        //设置活动是否永久结束
        public void SetActivityEnd(ActivityType type, bool isEnd)
        {
            if (ActivityEndDic.ContainsKey(type))
            {
                ActivityEndDic[type] = isEnd;
            }
            else
            {
                ActivityEndDic.Add(type, isEnd);
            }
            Save();
        }
        /// <summary>
        /// 添加活动循环次数
        /// </summary>
        /// <param name="type"></param>
        public void AddActivityLoopCount(ActivityType type, bool isClear = false)
        {
            var data = GetOneActivityData(type);
            if (data == null) return;
            if (isClear)
            {
                data.LoopCount = 0;
            }
            else
            {
                data.LoopCount++;
            }
            Save();
        }
        /// <summary>
        /// 检测通行证某一等级奖励领取情况
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CheckGetDreamPassReward(int id, bool isNormal)
        {
            var data = GetOneActivityData(ActivityType.DreamPass);
            if (data == null) return false;
            if (isNormal)
            {
                return data.GetPassReward.ContainsKey(id);
            }
            else
            {
                if (data.GetPassReward.ContainsKey(id))
                {
                    return data.GetPassReward[id];
                }
                return false;
            }

        }
        /// <summary>
        /// 添加活动数据
        /// </summary>
        /// <param name="type">活动类型</param>
        /// <param name="data">活动数据</param>
        public void AddActivityData(ActivityType type, ActityData data)
        {
            if (CurActivityData.ContainsKey(type)) return;
            CurActivityData.TryAdd(type, data);
            SetActivityEnd(type, false);
            switch (type)
            {
                case ActivityType.UntoldTreasure:
                    SDKManager.Instance.LogEvent("Endless_Unlock");
                    break;
                case ActivityType.DreamPass:
                    SDKManager.Instance.LogEvent("Pass_Unlock");
                    break;
                case ActivityType.Gacha:
                    SDKManager.Instance.LogEvent("Draw_Unlock");
                    break;
                case ActivityType.Discard:
                    SDKManager.Instance.LogEvent("Discord_Unlock");
                    break;
                case ActivityType.DayCheckIn:
                    SDKManager.Instance.LogEvent("Sign_Unlock");
                    break;
            }
            //设置活动随机奖励
            PhotoController.Instance.SetCurActivityPhotoIds(type);
            Save();
        }
        /// <summary>
        /// 移除活动数据
        /// </summary>
        /// <param name="type"></param>
        public void RemoveOneActivityData(ActivityType type)
        {
            CurActivityData.RemoveSafe(type);
            Save();
        }
        /// <summary>
        /// 重置活动时间
        /// </summary>
        /// <param name="type"></param>
        public void ReSetActivityTime(ActivityType type)
        {
            var data = GetOneActivityData(type);
            var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(type);
            if (cfg.ActTime <= 0) return;
            if (data.EndTime - TimeUtil.GetTimeStamp() <= 0)
            {
                data.EndTime = TimeUtil.GetTimeStamp() + cfg.ActTime;
                Save();
            }

        }
        /// <summary>
        /// 解锁通行证
        /// </summary>
        /// <param name="levelUp">是否升级</param>
        /// <param name="count">当前关卡数量</param>
        /// <param name="isClear">是否重置</param>
        public void AddAccruePassLevel(bool levelUp, int count, bool isClear = false)
        {
            if (!levelUp) return;
            if (CurActivityData.ContainsKey(ActivityType.DreamPass))
            {
                if (!isClear)
                {
                    CurActivityData[ActivityType.DreamPass]?.AddAccruePassLevel(count);
                }
                Save();
            }
        }
        /// <summary>
        /// 添加通行证领取的奖励
        /// </summary>
        public void AddDreamPassReward(int id)
        {
            if (CurActivityData.ContainsKey(ActivityType.DreamPass))
            {
                var recDic = CurActivityData[ActivityType.DreamPass].GetPassReward;
                if (recDic.ContainsKey(id))
                {
                    recDic[id] = true;
                }
                else
                {
                    recDic.Add(id, CurActivityData[ActivityType.DreamPass].IsOpenDreamPass);
                }
                CurActivityData[ActivityType.DreamPass].CheckDreamPassGetRew(true, out int count);
                Save();
            }
        }
        /// <summary>
        /// 激活通行证
        /// </summary>
        private void OpenDreamPass()
        {
            GetOpenDreamPass = true;
            Save();
        }

        /// <summary>
        /// 全局激励广告成功 hook：累加梦想通行证进度，达标自动开通。
        /// </summary>
        public void AddDreamPassAdProgress()
        {
            if (GetOpenDreamPass) return;

            var actCfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.DreamPass);
            if (actCfg == null) return;

            int rechargeId = actCfg.RechargeId;
            int target = ConfigLoader.Constant.DreamPassAdTimes;
            int cur = GetAcItemUnLock(rechargeId) + 1;
            SetAcItemUnLock(rechargeId, cur);

            if (cur >= target)
            {
                var recharge = RechargeController.GetRechargeInfo(rechargeId);
                if (recharge != null)
                {
                    PropsController.Instance.SendRewardData(recharge.RewardId);
                    GameEvent.Send(EventType.OpenDreamPass, recharge);
                }
            }
            else
            {
                GameEvent.Send(EventType.RefreshDreamPassAdCount);
            }
        }

        public List<int> GetCurRewList(ActivityType type)
        {
            var acData = GetOneActivityData(type);
            if (acData != null)
            {
                return acData.RewGetList;
            }

            return null;
        }
        /// <summary>
        /// 给无限领取的奖励添加id
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="isClear"></param>
        public void AddRewGetList(ActivityType type, int id, bool isClear = false)
        {
            var data = GetCurRewList(type);
            if (data != null)
            {
                if (!isClear)
                {
                    data.Add(id);
                }
                else
                {
                    data.Clear();
                }
                Save();
            }
        }
        /// <summary>
        /// 检测无尽宝藏或者礼品包的第一个奖励是不是免费
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool CheckRewIsFree(ActivityType type)
        {
            var data = GetCurRewList(type);
            if (data != null)
            {
                switch (type)
                {
                    case ActivityType.UntoldTreasure:
                        var endlessCfgs = ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.DataList;
                        if (data.Count >= endlessCfgs.Count)
                        {
                            return RechargeController.GetRechargeInfo(endlessCfgs[0].Price.ID).Price <= 0;
                        }
                        else
                        {
                            var newId = data.Count == 0 ? endlessCfgs[0].Id : data[data.Count - 1] + 1;
                            var cfg = ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.Get(newId);
                            return RechargeController.GetRechargeInfo(cfg.Price.ID).Price <= 0;
                        }
                }
            }
            return false;
        }

        public void SetNoAdsEndTime(int day)
        {
            if (day == -1 && !AdFree)
            {
                AdFree = true;
                if (AdFree)
                {
                    ActivityController.Instance.RemoveActivity(ActivityType.Adfree);
                    GameEvent.Send(EventType.SetAdEnter);
                    SDKManager.SetRemoveAds();
                    //隐藏一下banner广告
                    SDKManager.Instance.Button_HideBanner();
                }
            }
            else
            {
                AdFree = false;
            }

        }
        //发送新手礼包，主要用于补单发送
        public void SendStarterPack(int id)
        {
            if (GetBugStarterPack) return;
            //更新新手礼包奖励
            List<TBRewardData> _allRewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(id).Reward;
            PropsController.Instance.AddRewardData(_allRewardList);
            GetBugStarterPack = true;
            ActivityController.Instance.RemoveActivity(ActivityType.NewPlayerPack);
        }
        /// <summary>
        /// 得到活动解锁次数
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetAcItemUnLock(int id)
        {
            if (_userModel.ActivtyItemUnlockDic.ContainsKey(id))
            {
                return _userModel.ActivtyItemUnlockDic[id];
            }

            return 0;
        }
        /// <summary>
        /// 设置活动解锁次数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        public void SetAcItemUnLock(int id, int count)
        {
            if (_userModel.ActivtyItemUnlockDic == null)
            {
                _userModel.ActivtyItemUnlockDic = new Dictionary<int, int>();
            }
            _userModel.ActivtyItemUnlockDic[id] = count;
            Save();
        }

        public int GetVIPLevel
        {
            get
            {
                if ((_userModel.VIPLevel == 1 || _userModel.VIPLevel == 2) && TimeUtil.TimeStampNow >= _userModel.VIPEndTime)
                {
                    // 时间到了，重置VIP等级。这里不需要特意去保存。
                    _userModel.VIPLevel = 0;
                }

                return _userModel.VIPLevel;
            }
        }

        public long GetVIPEndTime
        {
            get
            {
                if (GetVIPLevel == 3 || GetVIPLevel == 0) return 0;
                return _userModel.VIPEndTime;
            }
        }
        public void SetVIPLevel(int level)
        {
            var now = TimeUtil.GetTimeStamp();
            var originalVip = GetVIPLevel;
            var tbConstant = ConfigLoader.Constant;
            //如果此时是最高档位的直接返回
            if (originalVip == 3) return;
            switch (level)
            {
                case 1:
                    _userModel.VIPLevel = 1;
                    if (now >= _userModel.VIPEndTime)
                    {
                        _userModel.VIPEndTime = now + 86400 * 7;
                    }
                    else
                    {
                        _userModel.VIPEndTime += 86400 * 7;
                    }

                    // 设置免费下载次数和刷新时间
                    if (originalVip == 0)
                    {
                        _userModel.VIPFreeDownloadCount = tbConstant.VIPWeeklyFreeDownload;
                        _userModel.VIPFreeDownloadCountRefreshTime = TimeUtil.GetTimeStampToNextZero();
                    }

                    break;
                case 2:
                    _userModel.VIPLevel = 2;
                    if (now >= _userModel.VIPEndTime)
                    {
                        _userModel.VIPEndTime = now + 86400 * 30;
                    }
                    else
                    {
                        _userModel.VIPEndTime += 86400 * 30;
                    }
                    _userModel.VIPFreeDownloadCount = tbConstant.VIPMonthlyFreeDownload;
                    _userModel.VIPFreeDownloadCountRefreshTime = TimeUtil.GetTimeStampToNextZero();
                    break;
                case 3:
                    _userModel.VIPLevel = 3;
                    _userModel.VIPEndTime = -1;
                    // 无限次免费下载
                    _userModel.VIPFreeDownloadCount = -1;
                    //设置永久免广告
                    SetNoAdsEndTime(-1);
                    break;
                default:
                    _userModel.VIPLevel = 0;
                    _userModel.VIPEndTime = 0;
                    _userModel.VIPFreeDownloadCount = 0;
                    break;
            }
            Save();
        }

        /// <summary>
        /// 获取当前vip免费下载次数。
        /// </summary>
        /// <returns>0和正数表示次数，-1表示无限次</returns>
        public int VIPFreeDownloadCount()
        {
            var vipLevel = GetVIPLevel;
            if (vipLevel <= 0) return 0;
            else if (vipLevel >= 3) return -1;
            else
            {
                var now = TimeUtil.TimeStampNow;
                if (now >= _userModel.VIPFreeDownloadCountRefreshTime)
                {
                    _userModel.VIPFreeDownloadCount = vipLevel == 1 ? 2 : 5;
                    _userModel.VIPFreeDownloadCountRefreshTime = TimeUtil.GetTimeStampToNextZero();
                    Save();
                }
                return _userModel.VIPFreeDownloadCount;
            }
        }

        /// <summary>
        /// 扣除vip免费下载次数。
        /// 一定要调用 VIPFreeDownloadCount() 获取次数，判断次数大于0再调用这个方法。
        /// </summary>
        public void UseVIPFreeDownloadCount()
        {
            if (VIPFreeDownloadCount() <= 0) return;
            var vipLevel = GetVIPLevel;
            if (vipLevel == 1 || vipLevel == 2)
            {
                _userModel.VIPFreeDownloadCount -= 1;
                if(_userModel.VIPFreeDownloadCount < 0)
                {
                    _userModel.VIPFreeDownloadCount = 0;
                }
                Save();
            }
        }
        /// <summary>
        /// 检测七日签到是否有可领取奖励（用于红点）
        /// </summary>
        public bool CheckDayCheckInRedPoint()
        {
            var daycheckData = GetOneActivityData(ActivityType.DayCheckIn);
            if (daycheckData == null) return false;
            return daycheckData.CheckDayCheckInRedPoint();
        }
        //执行签到,返回0表示签到失败
        public int DoCheckIn()
        {
            var daycheckData = GetOneActivityData(ActivityType.DayCheckIn);
            if (daycheckData == null) return 0;
            int daystate = daycheckData.DoCheckIn();
            if (daystate == 0) return 0;
            Save();
            return daystate;
        }
        //设置是否进入discard
        public void SetDiscardOpen(bool isOpen)
        {
            var discardData = GetOneActivityData(ActivityType.Discard);
            if (discardData == null) return;
            discardData.IsDiscardOpen = isOpen;
            if (isOpen)
            {
                //关闭该活动
                ActivityController.Instance.RemoveActivity(ActivityType.Discard);
            }
            Save();
        }
        #endregion
        #region 通用方法

        #region 设置用户数据的方法

        //设置一个道具数量
        public void SetOneProp(PropType type, long num, bool isAddTime = false, bool isFree = false,
            bool isUpdateUI = false)
        {
            PropInfo propInfo = _userModel.Props.Find(p => p.mType == type);
            if (propInfo != null)
            {
                if (propInfo.IsFirst&&num<0)
                {
                    propInfo.IsFirst = false;
                }
                //对于无限畅用的就不用减次数
                if (num < 0 && propInfo.EndTime > 0)
                {
                    propInfo.IsFree = false;
                    Save();
                    return;
                }

                if (!isAddTime)//加个数
                {
                    if (num < 0 && propInfo.IsFree)//免费使用
                    {
                        num = 0;
                        propInfo.IsFree = false;
                    }
                    propInfo.Num += (int)num;
                    if (propInfo.Num <= 0)
                    {
                        propInfo.Num = 0;
                    }
                }
                else//加无限时间
                {
                    propInfo.AddTime(num);
                }
                propInfo.IsFree = isFree;

                //更新UI界面状态
                if (isUpdateUI)
                {
                    GameEvent.Send(EventType.UpdatePropBtnState, type);
                }
            }
            Save();
        }
        //清空某道具数量
        public void ResetOneProp(PropType type)
        {
            PropInfo propInfo = _userModel.Props.Find(p => p.mType == type);
            if (propInfo != null)
            {
                propInfo.Num = 0;
            }
            Save();
        }
        //使用货币
        public void UserConsume(ERewardType type, int id, int num)
        {
            if (type == ERewardType.Currency)
            {
                switch (id)
                {
                    case 0:
                        Diamond -= num;
                        return;
                }
            }
        }
        public void UserConsume(TBConsumeData data)
        {
            UserConsume(data.Type, data.ID, data.Count);
        }

        #endregion

        /// <summary>
        /// 发送奖励，（区分是增加个数还是增加无限时间）
        /// </summary>
        /// <param name="type">奖励类型</param>
        /// <param name="id"></param>
        /// <param name="num"></param>
        public void SendReward(ERewardType type, int id, long num, bool isAddTime)
        {
            if (type == ERewardType.Currency)
            {
                switch (id)
                {
                    case 0://金币
                        _userModel.Diamond += (int)num;
                        SDKManager.Instance.LogUserEvent(_userModel.Diamond);
                        GameEvent.Send(EventType.SetGachaRedPoint, CheckGachaAdTime(), "");
                        Save();
                        break;
                }
            }
            else if (type == ERewardType.Prop)
            {
                PropType propType = (PropType)id;
                SetOneProp(propType, num, isAddTime);
            }
            else if (type == ERewardType.NoAd)
            {
                SetNoAdsEndTime(-1);
            }
            else if (type == ERewardType.DreamPass)
            {
                OpenDreamPass();
            }
            else if (type == ERewardType.Photo)//发送照片奖励，通常为活动奖励发送
            {
                AddUserPhotoData(id);
            }
            else if (type == ERewardType.NewPlayerPack)//新手礼包发送
            {
                SendStarterPack(id);
            }
            else if (type == ERewardType.VIP)//设置VIP
            {
                SetVIPLevel(id);
            }
        }


        #region 检测相关的方法

        public bool IsLevelUp;
        /// <summary>
        /// 等级提升的检测方法
        /// </summary>
        public void CheckUpLevel(bool isUp)
        {
            /* if (isUp)
            {
                int abControl = SDKManager.GetRemoteConfigInt("AB_control", 1);
                if (abControl == 2)
                {
                    //当此时为A用户时，若此时关卡数大于设定的关卡数包括此时是非提审环境，则升级为B用户,且为非ASN用户
                    if (Level > ConfigLoader.Constant.AUnlockLevel && GetCurUserType != 1 && !SDKManager.Instance.IsASN)
                    {
                        GetCurUserType = 1;
                    }
                }
                IsLevelUp = false;
            } */
            CheckPropUnlock(isUp);
            CheckActivityOpen(isUp);
            AddAccruePassLevel(isUp, Level);

        }
        //检查玩家是否拥有足够的货币（金币、生命）
        public bool CheckConsume(ERewardType type, int id, int num)
        {
            if (type == ERewardType.Currency)
            {
                switch (id)
                {
                    case 0:
                        return Diamond >= num;
                    default:
                        return false;
                }
            }

            return false;
        }
        public bool CheckConsume(TBConsumeData data)
        {
            return CheckConsume(data.Type, data.ID, data.Count);
        }
        /// <summary>
        /// 检查道具是否解锁
        /// </summary>
        private void CheckPropUnlock(bool isUp)
        {
            foreach (var item in _userModel.Props)
            {
                var cfg = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetByType(item.mType);
                if (cfg == null) continue;
                if (isUp && !item.IsUnlock)
                {
                    if (_userModel.Level >= cfg.UnLcokLevel)
                    {
                        item.IsUnlock = true;
                        if (item.mType != PropType.Prompt && item.mType != PropType.DrawCard &&
                            item.mType != PropType.AddSolt)
                        {
                            /*//打开道具解锁弹窗
                            GameModule.UI.AddOrderPanel<Panel_UnlockOneProp>(2,false,item.mType);*/
                            //现在改为直接增加数量
                            SetOneProp(item.mType, cfg.GetCount);
                        }
                    }
                }
                else
                {
                    if (_userModel.Level < cfg.UnLcokLevel)
                    {
                        item.IsUnlock = false;
                        item.IsFree = false;
                        item.Num = 0;
                        item.EndTime = 0;
                    }
                }

            }

        }
        /// <summary>
        /// 检查活动是否开启
        /// </summary>
        private void CheckActivityOpen(bool isUp)
        {
            if (isUp)
            {
                ActivityController.Instance.UpgradeCheckActivity();
            }
        }
        /// <summary>
        /// 检测关卡助推器是否过期是否免费使用
        /// </summary>
        /// <param name="propInfo"></param>
        public void CheckBoosterTime(PropInfo propInfo)
        {
            bool isSave = propInfo.CheckTime();
            if (isSave)
            {
                Save();
            }
        }
        #endregion
        #endregion

        #region SDK
        //是否购买新手礼包
        public bool GetBugStarterPack
        {
            get => _userModel.IsBuyStarterPack;
            set
            {
                _userModel.IsBuyStarterPack = value;
                Save();
            }
        }
        public long GetDayZeroTime
        {
            get => _userModel.CurDateTime;
            set
            {
                _userModel.CurDateTime = value;
                Save();
            }
        }


        public int GetDayGameCount
        {
            get => _userModel.DayGameCount;
            set
            {
                _userModel.DayGameCount = value;
                Save();
            }
        }

        public int GetAdLifeCount
        {
            get => _userModel.AdLifeCount;
            set
            {
                _userModel.AdLifeCount = value;
                Save();
            }
        }

        //设置生涯抽卡次数
        public int SetLuckyDrawCount
        {
            get => _userModel.LuckyDrawCount;
            set
            {
                _userModel.LuckyDrawCount = value;
                SDKManager.Instance.LogEvent("Draw_Click", "Draw_Num", _userModel.LuckyDrawCount);
                Save();
            }
        }
        #endregion

        #region 相册相关
        /// <summary>
        /// 得到当前用户类型，0-A，1-B
        /// </summary>
        public int GetCurUserType
        {
            get => _userModel.CurUserType;
            set
            {
                _userModel.CurUserType = value;
                //刷新关卡图
                PhotoController.Instance.PhotoPreloading(true, true);
                //刷新活动的随机奖励为A/B类型
                PhotoController.Instance.RefreshActivityRandomPhoto();
                //发送AB刷新事件
                GameEvent.Send(EventType.RefreshUserABType, _userModel.CurUserType);
                Save();
            }
        }

        public int GetCurUserPhotoID
        {
            get => _userModel.CurUserPhotoId;
            set
            {
                _userModel.CurUserPhotoId = value;
                //刷新主界面展示的图片
                GameEvent.Send(EventType.RefreshHomeVideo, value);
                Save();
            }
        }
        /// <summary>
        /// 在线参数控制AB
        /// </summary>
        private void OnlineparametersAB()
        {
            int abControl = SDKManager.GetRemoteConfigInt("AB_control", 1);
            //如果此时已经是B类用户，直接返回,或者是ASN用户直接返回
            if (GetCurUserType == 1 || SDKManager.Instance.IsASN) return;
            switch (abControl)
            {
                case 1:
                    GetCurUserType = 0;
                    break;
                case 3:
                    GetCurUserType = 1;
                    break;
            }
        }
        public UserPhotoData GetUserPhotoData(int id = -1)
        {
            if (id == -1)
            {
                id = GetCurUserPhotoID;
            }
            var data = _userModel.UserPhotosData.Find(p => p.Id == id);
            return data;
        }
        public List<UserPhotoData> GetUserPhotoDataList(int fromType, bool isLiked = false, bool isGetAll = false)
        {
            if (isGetAll)
            {
                return _userModel.UserPhotosData;
            }
            List<UserPhotoData> list = new List<UserPhotoData>();
            for (int i = 0; i < _userModel.UserPhotosData.Count; i++)
            {
                var data = _userModel.UserPhotosData[i];
                if (!isLiked)
                {
                    if (data.FromType == fromType)
                    {
                        list.Add(data);
                    }
                }
                else
                {
                    if (data.IsLike)
                    {
                        list.Add(data);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 获取当前已经拥有的照片id集合。
        /// </summary>
        /// <returns></returns>
        public HashSet<int> GetOwnedPhotoSet()
        {
            HashSet<int> photoSet = new HashSet<int>();
            for (int i = 0; i < _userModel.UserPhotosData.Count; ++i)
            {
                photoSet.Add(_userModel.UserPhotosData[i].Id);
            }

            return photoSet;
        }

        /// <summary>
        /// 添加照片数据
        /// </summary>
        /// <param name="id">照片id</param>
        /// <param name="fromType">来自类型,0所有来源</param>
        public void AddUserPhotoData(int id, int fromType = 0, bool isDownload = false)
        {
            //如果存在则不加入
            if (_userModel.UserPhotosData.Find(p => p.Id == id) != null) return;

            var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(id);
            if (cfg == null) return;
            //解锁一张图上报
            SDKManager.Instance.LogEvent("Picture_Unlock", CommonEvent.ComLevelEvent);
            int abType = cfg.ABGroup;
            UserPhotoData newData = new UserPhotoData(id, abType, fromType, false, isDownload);
            _userModel.UserPhotosData.AddWithoutRepetition(newData);
            //设置桌面红点更新
            GameEvent.Send(EventType.PhotoRedPoint, -1);
            Save();
        }
        /// <summary>
        /// 设置照片点赞状态
        /// </summary>
        /// <param name="id">照片id</param>
        /// <param name="isLike">是否喜欢</param>
        public void SetUserPhotoLike(int id, bool isLike,bool isUpdateLock = false)
        {
            var phData = _userModel.UserPhotosData.Find(p => p.Id == id);
            if (phData != null)
            {
                int picClick = isLike ? 1 : 2;
                var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(id);
                SDKManager.Instance.LogEvent("Picture_Preferences", "Picture_Click", picClick, CommonEvent.ComLevelEvent);
                phData.IsLike = isLike;
                if (isUpdateLock)
                {
                    phData.IsLook = isLike;
                }
                Save();
            }
        }
        /// <summary>
        /// 设置照片下载状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isCached"></param>
        public void SetUserPhotoCached(int id, bool isCached)
        {
            var phData = GetUserPhotoData(id);
            if (phData != null)
            {
                phData.IsDownload = isCached;
                Save();
            }
        }
        /// <summary>
        /// 设置照片是否查看状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isLook"></param>
        public void SetUserPhotoLook(int id, bool isLook)
        {
            var phData = GetUserPhotoData(id);
            if (phData != null)
            {
                phData.IsLook = isLook;
                //设置桌面红点更新
                GameEvent.Send(EventType.PhotoRedPoint, -1);
                Save();
            }
        }
        //是否已经获得该图片
        public bool IsGetPhoto(int id)
        {
            return _userModel.UserPhotosData.Find(p => p.Id == id) != null;
        }
        /// <summary>
        /// 获取当日剩余广告抽卡次数。
        /// </summary>
        /// <returns></returns>
        public int GetPhotoPackAdTimes(int id)
        {
            if (_userModel.PhotoPackAdTimes == null)
            {
                RefreshPhotoPackAdTimes();
            }

            if (!_userModel.PhotoPackAdTimes.ContainsKey(id))
            {
                _userModel.PhotoPackAdTimes.Add(id, ConfigLoader.Instance.Tables.TbConstant.GachaAdTimes);
            }

            return _userModel.PhotoPackAdTimes[id];
        }
        /// <summary>
        /// 检查是否可以抽卡（广告次数或钻石足够），用于抽卡红点
        /// </summary>
        /// <returns>任一卡包有广告次数或钻石足够消耗时返回true</returns>
        public bool CheckGachaAdTime()
        {
            var cfgs = ConfigLoader.Instance.Tables.TbCfgGacha.DataList;
            foreach (var cfg in cfgs)
            {
                //如果是当前卡包是需要VIP解锁的，则需要判定是否开启VIP
                if (cfg.VIPLock && GetVIPLevel == 0) continue;
                if (GetPhotoPackAdTimes(cfg.Id) > 0 || Diamond >= cfg.GachaCost)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 使用广告抽卡次数
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool UsePhotoPackAdTime(int id)
        {
            if (_userModel.PhotoPackAdTimes == null)
            {
                RefreshPhotoPackAdTimes();
            }

            // 跨天。刷新数据。
            var nowSecs = TimeUtil.GetTimeStamp();
            if (nowSecs >= _userModel.RefreshPhotoPackAdTimesTimestamp)
            {
                RefreshPhotoPackAdTimes();
            }

            if (!_userModel.PhotoPackAdTimes.ContainsKey(id))
            {
                _userModel.PhotoPackAdTimes.Add(id, ConfigLoader.Instance.Tables.TbConstant.GachaAdTimes);
            }

            var leftTimes = _userModel.PhotoPackAdTimes[id] - 1;
            if (leftTimes < 0) return false;

            _userModel.PhotoPackAdTimes[id] = leftTimes;
            bool isRed = CheckGachaAdTime();
            GameEvent.Send(EventType.SetGachaRedPoint, isRed, "");
            Save();
            return true;
        }

        /// <summary>
        /// 刷新广告抽卡次数。
        /// </summary>
        void RefreshPhotoPackAdTimes()
        {
            var tbGacha = ConfigLoader.Instance.Tables.TbCfgGacha;
            _userModel.PhotoPackAdTimes = new Dictionary<int, int>();
            for (int i = 0; i < tbGacha.DataList.Count; ++i)
            {
                _userModel.PhotoPackAdTimes.Add(tbGacha.DataList[i].Id, tbGacha.DataList[i].GachaAdTimes);
            }
            _userModel.RefreshPhotoPackAdTimesTimestamp = TimeUtil.GetTimeStampToNextZero();
            bool isGachaRed = CheckGachaAdTime();
            GameEvent.Send(EventType.SetGachaRedPoint, isGachaRed, "");
        }

        #region 抽卡保底相关
        /// <summary>
        /// 获取卡包当前保底计数
        /// </summary>
        /// <param name="packId">卡包ID</param>
        /// <returns>当前连续未出最高等级的次数</returns>
        public int GetGachaPityCount(int packId)
        {
            if (_userModel.GachaPityCounter == null)
                _userModel.GachaPityCounter = new Dictionary<int, int>();

            if (!_userModel.GachaPityCounter.ContainsKey(packId))
                _userModel.GachaPityCounter[packId] = 0;

            return _userModel.GachaPityCounter[packId];
        }

        /// <summary>
        /// 增加保底计数
        /// </summary>
        /// <param name="packId">卡包ID</param>
        public void AddGachaPityCount(int packId)
        {
            if (_userModel.GachaPityCounter == null)
                _userModel.GachaPityCounter = new Dictionary<int, int>();

            if (!_userModel.GachaPityCounter.ContainsKey(packId))
                _userModel.GachaPityCounter[packId] = 0;

            _userModel.GachaPityCounter[packId]++;
            Save();
        }

        /// <summary>
        /// 重置保底计数（抽到最高等级后调用）
        /// </summary>
        /// <param name="packId">卡包ID</param>
        public void ResetGachaPityCount(int packId)
        {
            if (_userModel.GachaPityCounter == null)
                _userModel.GachaPityCounter = new Dictionary<int, int>();

            _userModel.GachaPityCounter[packId] = 0;
            Save();
        }
        #endregion



        /// <summary>
        /// 检查是否有未查看的照片
        /// </summary>
        /// <param name="typeId">默认-1表示检查全部</param>
        /// <returns></returns>
        public bool IsPhototLook(int typeId = -1)
        {
            if (typeId == -1)
            {
                for (int i = 0; i < _userModel.UserPhotosData.Count; i++)
                {
                    if (!_userModel.UserPhotosData[i].IsLike)
                    {
                        if (!_userModel.UserPhotosData[i].IsLook)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                var curList = GetUserPhotoDataList(typeId);
                for (int i = 0; i < curList.Count; i++)
                {
                    if (!curList[i].IsLook)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion


        #region 处理每日更新玩家数据
        private void EveryDayUpdatePlayerData()
        {
            EveryDayChange();
            OpenGift();
            SDKManager.Instance.GetIsNewUser = false;
        }

        public void EveryDayChange()
        {
            var curTime = TimeUtil.GetTimeStampToZero();
            if (curTime != GetDayZeroTime)
            {
                //每日更新玩家数据
                GetDayZeroTime = curTime;
                GetDayGameCount = 0;
                GetAdLifeCount = 0;
                RefreshPhotoPackAdTimes();
                SDKManager.Instance.GetPayDailyTimes = 0;
            }
        }

        /// <summary>
        /// 零点时间轮询
        /// </summary>
        public void PollotUserData()
        {
            //每3秒轮询一次
            float deltaTime = Time.unscaledDeltaTime;
            _userDatatimer += deltaTime;
            if (_userDatatimer >= _userDatainterval)
            {
                //Log.Info("用户数据轮询");
                _userDatatimer = 0;
                EveryDayChange();
            }
        }
        private void OpenGift()
        {
            var removeList = new List<ActityData>();
            //检查活动是否过期
            foreach (var info in CurActivityData)
            {
                if (info.Value.IsRemove())
                {
                    removeList.Add(info.Value);
                } 
                //活动时间重置
                ReSetActivityTime(info.Key);
            }
            foreach (var data in removeList)
            {
                ActivityController.Instance.RemoveActivity(data.Id);
            }
            //默认开启商城
            ActivityController.Instance.CheckActivityOpen(ActivityType.Shop);
            //默认开启H5功能验证
            ActivityController.Instance.CheckActivityOpen(ActivityType.H5Check);
            var list = GetAllActivityData();
            foreach (var item in list)
            {
                var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(item.Id);
                if (cfg.OpenType == 1 || cfg.OpenType == 3)
                {
                    switch (item.Id)
                    {
                        case ActivityType.Adfree:
                            break;
                        case ActivityType.DreamPass:
                            break;
                        case ActivityType.UntoldTreasure:
                            break;
                        case ActivityType.NewPlayerPack:
                            //GameModule.UI.AddOrderPanel<Panel_StarterPack>(cfg.Order,false,item);
                            break;
                        default:
                            break;
                    }
                }

            }
        }

        #endregion
    }
}
