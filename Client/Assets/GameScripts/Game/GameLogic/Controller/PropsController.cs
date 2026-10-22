using System.Collections;
using System.Collections.Generic;
using Config.common;
using UnityEngine;
using Y_Base;
using System;
using Config.config;
using DG.Tweening;
using TK_Engine;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
    public class PropsController : Singleton<PropsController>
    {
        private List<PropInfo> _propInfos => UserController.Instance.GetAllPropsInfo;

        private List<GameBooster> _boosterProps = new List<GameBooster>();

        //等待发送的奖励
        private List<OneRewardData> _waitRewardList = new List<OneRewardData>();
        private List<OneRewardData> _waitPropRes = new List<OneRewardData>();
        private List<OneRewardData> _waitPhotoRes = new List<OneRewardData>();
        public List<OneRewardData> WaitRewardList => _waitRewardList;
        public List<GameBooster> BoosterProps => _boosterProps;

        // 待显示的钻石奖励数据（用于收集面板）
        private OneRewardData _pendingDiamondReward;
        // 待显示的照片奖励数据（用于收集面板）
        private List<OneRewardData> _pendingPhotoRewards = new List<OneRewardData>();
        public PropInfo GetCurPropsInfo(PropType type)
        {
            return _propInfos.Find(info => info.mType == type);
        }

        #region 游戏玩法道具
        /// <summary>
        /// 获取临时道具信息列表
        /// </summary>
        /// <param name="isBoosters">是否是助推器道具</param>
        /// <returns></returns>
        public List<PropInfo> GetTempPropsInfos(bool isBoosters)
        {
            if (isBoosters)
            {
                return _propInfos.FindAll(i =>
                    i.mType == PropType.Prompt || i.mType == PropType.AddSolt);
            }
            else
            {
                return _propInfos.FindAll(i =>
                    i.mType != PropType.Prompt && i.mType != PropType.DrawCard && i.mType != PropType.AddSolt);
            }
        }

        public void PropBtnClick(PropType type, UIWindow window = null, Action callback = null, Transform btnTr = null, bool isFree = false)
        {
            PropInfo propInfo = GetCurPropsInfo(type);
            var cfg = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetByType(type);
            //检测是否解锁
            if (cfg.UnLcokLevel > UserController.Instance.Level)
            {
                //播放道具未解锁声音
                AudioManager.Play(AudioType.Sound, AudioDef.LockPropClick);
                //提示道具到达等级要求
                if (type == PropType.Prompt || type == PropType.DrawCard || type == PropType.AddSolt)
                {
                    GameEvent.Send(EventType.ShowTips3, "uitps_levelnfo_lockprop", btnTr, 1,
                        new string[] { $"{cfg.UnLcokLevel}" });
                }
                else
                {
                    GameEvent.Send(EventType.ShowTips2, "uitps_levelnfo_lockprop", true,
                        new string[] { $"{cfg.UnLcokLevel}" });
                }

                return;
            }

            AudioManager.Play(AudioType.Sound, AudioDef.BtnClick);

            if (propInfo.Num > 0 || propInfo.IsFree)
            {
                bool isConsume = false;
                CardsController.Instance.CurPlayCardType = playCardType.useprop;
                switch (type)
                {
                    case PropType.Revoke:
                        isConsume = CardsController.Instance.Revoke();
                        break;
                    case PropType.MessUp:
                        isConsume = CardsController.Instance.MessUp();
                        break;
                    case PropType.DestoryGruop:
                        isConsume = CardsController.Instance.DestoryOneGruop();
                        break;
                    case PropType.MoveOut:
                        isConsume = CardsController.Instance.MoveOut();
                        break;
                }

                if (isConsume)
                {
                    //SDKManager.Instance.LogEvent("Use_Prop", "Prop_Name", type.ToString(), "IS_Frist_Prop", propInfo.IsFirst);
                    callback?.Invoke();
                    //对于非助推器道具，次数减一
                    int reduceNum = -1;
                    UserController.Instance.SetOneProp(type, reduceNum, isUpdateUI: true);
                }
                GuideController.Instance.NextTriggerGuide();
            }
            else
            {
                /* //展示道具购买界面
                GameModule.UI.ShowUI<Panel_BuyOneProp>(type, window); */
                //直接弹出AD，广告成功之后，直接发送道具数量
                PropShowRVEvent(type, out HCRVPositionName adPos,false);
                SDKManager.Instance.Button_ShowRV(adPos, (success) =>
                {
                    if (success)
                    {
                        UserController.Instance.SetOneProp(type, cfg.AdgetCount, isUpdateUI: true);
                    }
                });
            }
        }
        public static void PropShowRVEvent(PropType type, out HCRVPositionName adPos,bool isLog = true)
        {
            switch (type)
            {
                case PropType.Revoke:
                    adPos = HCRVPositionName.RV_PropA;
                    if (isLog)
                    {
                        SDKManager.Instance.LogEvent("RV_Button_Show", "Position", adPos.ToString());
                    }
                    break;
                case PropType.MessUp:
                    adPos = HCRVPositionName.RV_PropB;
                    if (isLog)
                    {
                        SDKManager.Instance.LogEvent("RV_Button_Show", "Position", adPos.ToString());
                    }
                    break;
                case PropType.DestoryGruop:
                    adPos = HCRVPositionName.RV_PropC;
                    if (isLog)
                    {
                        SDKManager.Instance.LogEvent("RV_Button_Show", "Position", adPos.ToString());
                    }
                    break;
                case PropType.MoveOut:
                    adPos = HCRVPositionName.RV_PropD;
                    if (isLog)
                    {
                        SDKManager.Instance.LogEvent("RV_Button_Show", "Position", adPos.ToString());
                    }
                    break;
                    default:
                    adPos = HCRVPositionName.RV_PropA;
                    break;
            }
        }
        public void SetConsumelist(PropInfo info, bool isAdd)
        {
            int reduceNum = 1;
            /*if (info.mType == PropType.Prompt)
            {
                PropInfo propInfo = GetCurPropsInfo(PropType.Prompt);
                reduceNum = propInfo.Num > 3 ? 3 : propInfo.Num;
            }*/

            GameBooster booster = new GameBooster(info.mType, reduceNum);
            if (isAdd)
            {
                AddAndSort(booster);
            }
            else
            {
                _boosterProps.RemoveSafe(booster);
            }
        }

        //加入助推器列表，并排序
        private void AddAndSort(GameBooster booster)
        {
            _boosterProps.AddWithoutRepetition(booster);
            _boosterProps.Sort((a, b) => (int)b.Type - (int)a.Type);
        }

        /// <summary>
        /// 消耗助推器道具
        /// </summary>
        public void ConsumeBooster()
        {
            int reduceNum = -1;
            foreach (var item in _boosterProps)
            {
                reduceNum = -item.Num;
                //SDKManager.Instance.LogEvent("Use_Prop", "Prop_Name", item.Type.ToString(), "IS_Frist_Prop", GetCurPropsInfo(item.Type).IsFree);
                UserController.Instance.SetOneProp(item.Type, reduceNum);
            }
        }
        /// <summary>
        /// 是否已经添加某助推器道具
        /// </summary>
        /// <param name="oneType"></param>
        /// <returns></returns>
        public static bool IsGetOneBooster(PropType oneType)
        {
            var boosters = Instance._boosterProps;
            if (boosters.Count == 0) return false;
            for (int i = 0; i < boosters.Count; i++)
            {
                var oneBooster = boosters[i];
                if (oneBooster.Type == oneType) return true;
            }

            return false;
        }
        #endregion

        #region 奖励收集面板管理

        /// <summary>
        /// 显示奖励收集面板（钻石和照片）
        /// 使用 UIModule 的 OrderPanel 队列系统按顺序显示
        /// 数据会先写入，UI只是提示作用
        /// </summary>
        /// <param name="rewardList">奖励列表</param>
        public void ShowRewardCollectPanels(List<TBRewardData> rewardList, bool isMultiple = true)
        {
            // 清空之前的数据
            _pendingDiamondReward = OneRewardData.NoneData;
            _pendingPhotoRewards.Clear();

            // 整理奖励数据
            List<OneRewardData> collateList = CollateReward(rewardList);

            // 分离钻石和照片奖励
            OneRewardData diamondReward = OneRewardData.NoneData;
            List<OneRewardData> photoRewards = new List<OneRewardData>();
            List<OneRewardData> otherRewards = new List<OneRewardData>();

            foreach (var item in collateList)
            {
                if (item.Type == ERewardType.Currency && item.Id == 0)
                {
                    // 钻石奖励（id=0）
                    diamondReward = item;
                }
                else if (item.Type == ERewardType.Photo)
                {
                    // 照片奖励
                    photoRewards.Add(item);
                }
                else
                {
                    // 其他奖励
                    otherRewards.Add(item);
                }
            }

            // 先写入所有基础奖励数据（保证数据安全）
            if (diamondReward.Type != ERewardType.None)
            {
                UserController.Instance.SendReward(diamondReward.Type, diamondReward.Id, diamondReward.Count,
                    diamondReward.flyType == itemFlyType.AddTimeFly);
                _pendingDiamondReward = diamondReward;
            }

            foreach (var photo in photoRewards)
            {
                UserController.Instance.SendReward(photo.Type, photo.Id, photo.Count,
                    photo.flyType == itemFlyType.AddTimeFly);
                _pendingPhotoRewards.Add(photo);
            }

            foreach (var other in otherRewards)
            {
                UserController.Instance.SendReward(other.Type, other.Id, other.Count,
                    other.flyType == itemFlyType.AddTimeFly);
            }

            // 使用 OrderPanel 队列系统按顺序添加面板（order 越小越优先）
            // 钻石面板优先级 0，照片面板优先级 1
            if (_pendingDiamondReward.Type != ERewardType.None)
            {
                GameModule.UI.AddOrderPanel<Panel_CollectDiamond>(0, true, (int)_pendingDiamondReward.Count,isMultiple);
            }

            if (_pendingPhotoRewards.Count > 0)
            {
                // 如果没有钻石面板，照片面板立即显示；否则等待钻石面板关闭后显示
                bool isOpen = _pendingDiamondReward.Type == ERewardType.None;
                GameModule.UI.AddOrderPanel<Panel_Collectphotos>(1, isOpen, _pendingPhotoRewards[0].Id, _pendingPhotoRewards.Count);
            }
        }

        /// <summary>
        /// 显示奖励收集面板（使用OneRewardData列表）
        /// </summary>
        public void ShowRewardCollectPanels(List<OneRewardData> rewardList, bool isMultiple = true)
        {
            // 清空之前的数据
            _pendingDiamondReward = OneRewardData.NoneData;
            _pendingPhotoRewards.Clear();

            // 分离钻石和照片奖励
            OneRewardData diamondReward = OneRewardData.NoneData;
            List<OneRewardData> photoRewards = new List<OneRewardData>();
            List<OneRewardData> otherRewards = new List<OneRewardData>();

            foreach (var item in rewardList)
            {
                if (item.Type == ERewardType.Currency && item.Id == 0)
                {
                    diamondReward = item;
                }
                else if (item.Type == ERewardType.Photo)
                {
                    photoRewards.Add(item);
                }
                else
                {
                    otherRewards.Add(item);
                }
            }

            // 先写入所有基础奖励数据
            if (diamondReward.Type != ERewardType.None)
            {
                UserController.Instance.SendReward(diamondReward.Type, diamondReward.Id, diamondReward.Count,
                    diamondReward.flyType == itemFlyType.AddTimeFly);
                _pendingDiamondReward = diamondReward;
            }

            foreach (var photo in photoRewards)
            {
                UserController.Instance.SendReward(photo.Type, photo.Id, photo.Count,
                    photo.flyType == itemFlyType.AddTimeFly);
                _pendingPhotoRewards.Add(photo);
            }

            foreach (var other in otherRewards)
            {
                UserController.Instance.SendReward(other.Type, other.Id, other.Count,
                    other.flyType == itemFlyType.AddTimeFly);
            }

            // 使用 OrderPanel 队列系统按顺序添加面板
            if (_pendingDiamondReward.Type != ERewardType.None)
            {
                GameModule.UI.AddOrderPanel<Panel_CollectDiamond>(0, true, (int)_pendingDiamondReward.Count,isMultiple);
            }

            if (_pendingPhotoRewards.Count > 0)
            {
                bool isOpen = _pendingDiamondReward.Type == ERewardType.None;
                GameModule.UI.AddOrderPanel<Panel_Collectphotos>(1, isOpen, _pendingPhotoRewards[0].Id, _pendingPhotoRewards.Count);
            }
        }

        /// <summary>
        /// 发送广告倍数奖励（钻石）
        /// 额外发送 (倍数-1) * 基础奖励
        /// 注意：UI刷新在面板关闭时统一处理
        /// </summary>
        /// <param name="baseCount">基础奖励数量</param>
        /// <param name="multiple">广告倍数</param>
        public void SendAdMultipleDiamondReward(int baseCount, int multiple)
        {
            int extraCount = baseCount * (multiple - 1);
            SendRewardData(ERewardType.Currency, 0, extraCount, itemFlyType.None);
        }

        #endregion

        #region 奖励的发送或者消耗

        /// <summary>
        /// 添加奖励数据（先进行奖励整理，在内部排序处理）
        /// </summary>
        /// <param name="rewardList"></param>
        public void AddRewardData(List<TBRewardData> rewardList, bool multiple = false, int mulId = -1, bool isRefresh = false)
        {
            _waitRewardList.Clear();
            _waitPropRes.Clear();
            _waitPhotoRes.Clear();
            List<OneRewardData> collateList = CollateReward(rewardList);
            //排序处理
            List<OneRewardData> oneRewardList = new List<OneRewardData>();
            foreach (var item in collateList)
            {
                //只有钻石能叠加
                if (multiple && item.Type == ERewardType.Currency && item.Id == mulId)
                {
                    int EndCount = (int)item.Count * (item.Id == 0 ? ConfigLoader.Constant.AdMultiple : ConfigLoader.Constant.MMOIntegralMultiple);

                    oneRewardList.Add(new OneRewardData(item.Type, item.Id, EndCount, item.flyType));
                }
                else
                {
                    if (item.Type == ERewardType.Prop)
                    {
                        _waitPropRes.Add(item);
                    }
                    else if (item.Type == ERewardType.Photo)
                    {
                        _waitPhotoRes.Add(item);
                    }
                    else
                    {
                        oneRewardList.Add(item);
                    }
                }

            }
            var currencyList = oneRewardList.FindAll(i => i.Type == ERewardType.Currency);
            currencyList.Sort(SortCurrency);
            _waitRewardList.AddRange(currencyList);
            var otherList = oneRewardList.FindAll(i => i.Type != ERewardType.Currency);
            otherList.Sort(SortReward);
            _waitRewardList.AddRange(otherList);
            SendRewardData();
            if (isRefresh)
            {
                SendRewardEvent(currencyList);
            }
        }

        /// <summary>
        /// 添加奖励数据(内部排序处理)
        /// </summary>
        /// <param name="rewardList">等待领取的奖励</param>
        public void AddRewardData(List<TBRewardData> rewardList, OneRewardData goldData, bool multiple = false, int mulId = -1, bool isRefresh = false)
        {
            _waitRewardList.Clear();
            _waitPropRes.Clear();
            _waitPhotoRes.Clear();
            List<OneRewardData> oneRewardList = new List<OneRewardData>();

            foreach (var item in rewardList)
            {
                //只有钻石能叠加
                if (multiple && item.Type == ERewardType.Currency && item.ID == mulId)
                {
                    int EndCount = (int)item.Count * (item.ID == 0 ? ConfigLoader.Constant.AdMultiple : ConfigLoader.Constant.MMOIntegralMultiple);
                    oneRewardList.Add(new OneRewardData(item.Type, item.ID, EndCount, item.FlyType));

                }
                else
                {
                    if (item.Type == ERewardType.Prop)
                    {
                        _waitPropRes.Add(new OneRewardData(item.Type, item.ID, (int)item.Count, item.FlyType));
                    }
                    else if (item.Type == ERewardType.Photo)
                    {
                        int reId = item.ID;
                        if (UserController.Instance.GetCurUserType == 1)
                        {
                            reId = item.OtherID;
                        }
                        _waitPhotoRes.Add(new OneRewardData(item.Type, reId, (int)item.Count, item.FlyType));
                    }
                    else
                    {
                        oneRewardList.Add(new OneRewardData(item.Type, item.ID, (int)item.Count, item.FlyType));
                    }
                }
            }

            //排序：金币->生命->金砖->关卡钥匙->道具->照片
            var currencyList = oneRewardList.FindAll(i => i.Type == ERewardType.Currency);
            currencyList.Sort(SortCurrency);
            _waitRewardList.AddRange(currencyList);
            var otherList = oneRewardList.FindAll(i => i.Type != ERewardType.Currency);
            otherList.Sort(SortReward);
            _waitRewardList.AddRange(otherList);
            SendRewardData();
            if (isRefresh)
            {
                SendRewardEvent(currencyList);
            }
        }

        #region 排序处理

        private int SortReward(OneRewardData a, OneRewardData b)
        {
            if ((int)a.Type > (int)b.Type) return 1;
            else if ((int)a.Type < (int)b.Type) return -1;
            return 0;
        }

        private int SortCurrency(OneRewardData a, OneRewardData b)
        {
            if (a.Id > b.Id) return 1;
            else if (a.Id < b.Id) return -1;
            return 0;
        }

        #endregion

        /// <summary>
        /// 奖励整理（同类型合并）
        /// </summary>
        /// <returns></returns>
        public List<OneRewardData> CollateReward(List<TBRewardData> rewardList)
        {
            List<OneRewardData> collateList = new List<OneRewardData>();
            foreach (var item in rewardList)
            {
                int reId = item.ID;
                int userType = UserController.Instance.GetCurUserType;
                //如果是照片组奖励
                if (item.Type == ERewardType.PhotoGroup)
                {
                    var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(reId);
                    var allPhotoIds = userType == 0 ? group.APhotoRews : group.BPhotoRews;
                    foreach (var photoId in allPhotoIds)
                    {
                        OneRewardData onePicReward = new OneRewardData(ERewardType.Photo, photoId, item.Count, item.FlyType);
                        collateList.Add(onePicReward);
                    }
                    continue;
                }
                //如果是照片奖励，且此时是B用户，则设置为B类ID
                if (item.Type == ERewardType.Photo && userType == 1)
                {
                    reId = item.OtherID;
                }
                OneRewardData oneReward = new OneRewardData(item.Type, reId, item.Count, item.FlyType);
                int oneIndex = collateList.FindIndex(i =>
                    i.Type == oneReward.Type && i.Id == oneReward.Id && i.flyType == oneReward.flyType);
                if (oneIndex >= 0)
                {
                    var existOne = collateList[oneIndex];
                    existOne.Count += oneReward.Count;
                    collateList[oneIndex] = existOne;
                }
                else
                {
                    collateList.Add(oneReward);
                }
            }
            return collateList;
        }
        /// <summary>
        /// 写入奖励数据到用户
        /// </summary>
        private void SendRewardData()
        {
            if (WaitRewardList.Count > 0 || _waitPropRes.Count > 0 || _waitPhotoRes.Count > 0)
            {
                for (int i = 0; i < WaitRewardList.Count; i++)
                {
                    var reward = WaitRewardList[i];
                    UserController.Instance.SendReward(reward.Type, reward.Id, reward.Count, reward.flyType == itemFlyType.AddTimeFly);
                }

                if (_waitPropRes.Count > 0)
                {
                    for (int j = 0; j < _waitPropRes.Count; j++)
                    {
                        var propRes = _waitPropRes[j];
                        UserController.Instance.SendReward(propRes.Type, propRes.Id, propRes.Count, propRes.flyType == itemFlyType.AddTimeFly);
                    }
                }

                if (_waitPhotoRes.Count > 0)
                {
                    for (int k = 0; k < _waitPhotoRes.Count; k++)
                    {
                        var photo = _waitPhotoRes[k];
                        UserController.Instance.SendReward(photo.Type, photo.Id, photo.Count, photo.flyType == itemFlyType.AddTimeFly);
                    }
                }
            }
        }
        /// <summary>
        /// 写入奖励数据到用户
        /// </summary>
        /// <param name="reward">奖励id</param>
        public void SendRewardData(int rewardId)
        {
            var allReward = ConfigLoader.Instance.Tables.TbCfgReward.Get(rewardId).Reward;
            int userType = UserController.Instance.GetCurUserType;
            Log.Info($"此时发送奖励的时候，用户类型{userType}");
            foreach (var data in allReward)
            {
                int reId = data.ID;
                if (data.Type == ERewardType.PhotoGroup)
                {
                    SendPhotoGroupReward(reId, userType);
                    continue;
                }
                //如果是照片奖励，且此时是B用户，则设置为B类奖励ID
                if (data.Type == ERewardType.Photo && userType == 1)
                {
                    reId = data.OtherID;
                }
                UserController.Instance.SendReward(data.Type, reId, data.Count,
                    data.FlyType == itemFlyType.AddTimeFly);
                UpdateRewardEvent(data.Type, reId);
            }
        }
        //发照片组奖励
        private void SendPhotoGroupReward(int photoGroupId, int ABType)
        {
            var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(photoGroupId);
            var allPhotoIds = ABType == 0 ? group.APhotoRews : group.BPhotoRews;
            foreach (var photoId in allPhotoIds)
            {
                UserController.Instance.SendReward(ERewardType.Photo, photoId, 1, false);
            }
        }
        /// <summary>
        /// 写入单个奖励数据到用户
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <param name="flyType"></param>
        public void SendRewardData(ERewardType type, int id, long count, itemFlyType flyType)
        {
            UserController.Instance.SendReward(type, id, count, flyType == itemFlyType.AddTimeFly);
        }
        public void FlyReward(Vector3 startPos, Action sendEnd = null)
        {
            //这里就需要先保存一下奖励数据，然后进行奖励的动画
            Utility.Unity.StartCoroutine(CheckReward(startPos, sendEnd));
        }

        /// <summary>
        /// 依次发送奖励
        /// </summary>
        /// <param name="startPos"></param>
        /// <returns></returns>
        private IEnumerator CheckReward(Vector3 startPos, Action sendEnd = null)
        {
            if (WaitRewardList.Count > 0 || _waitPropRes.Count > 0 || _waitPhotoRes.Count > 0)
            {
                GameEvent.Send(EventType.SetAnimMask, true);
                for (int i = 0; i < WaitRewardList.Count; i++)
                {
                    bool isCompleted = false;
                    var reward = WaitRewardList[i];
                    FlyReward(reward.Type, reward.Id, reward.Count, reward.flyType, startPos, null, null, null,
                        () =>
                        {
                            isCompleted = true;
                        });

                    yield return new WaitUntil(() => isCompleted);
                }
                if (_waitPropRes.Count > 0)
                {
                    bool isCompleted = false;
                    FlyReward(ERewardType.Prop, 1, 0, itemFlyType.MultipleFly, startPos, null, _waitPropRes, null,
                        () =>
                        {
                            isCompleted = true;

                        });
                    yield return new WaitUntil(() => isCompleted);
                }

                if (_waitPhotoRes.Count > 0)
                {
                    FlyReward(ERewardType.Photo, 1, 0, itemFlyType.None, startPos, null, _waitPhotoRes, null, () =>
                    {

                    });
                }
                sendEnd?.Invoke();
                GameEvent.Send(EventType.SetAnimMask, false);
            }
        }

        /// <summary>
        /// 发送奖励(数据已经写入到用户)，实现动画与数据分离
        /// </summary>
        /// <param name="type">奖励类型</param>
        /// <param name="id">奖励id</param>
        /// <param name="count">奖励数量</param>
        /// <param name="isAddTime">是否是增加时间</param>
        /// <param name="startPos">开始位置</param>
        /// <param name="endTr">目标位置</param>
        /// <param name="window">奖励生成的窗口</param>
        /// <param name="callback">回调</param>      
        public static void FlyReward(ERewardType type, int id, long count, itemFlyType flyType, Vector3 startPos,
            Transform endTr, List<OneRewardData> propRews = null, UIWindow window = null, Action callback = null, bool isWriteData = false)
        {
            if (isWriteData)
            {
                Instance.SendRewardData(type, id, count, flyType);
            }
            //打开遮罩层
            GameEvent.Send(EventType.SetTipsMask, true);
            switch (type)
            {
                case ERewardType.Currency: //货币奖励
                    FlySystem.Instance.GetFlyCurrencyN(type, id, flyType, startPos, count, (Tr) =>
                    {
                        GameEvent.Send(EventType.RefreshCurrency, id);
                        callback?.Invoke();
                        Sequence seq = DOTween.Sequence();
                        seq.Append(Tr?.DOScale(1.1f, 0.15f));
                        seq.Append(Tr?.DOScale(0.9f, 0.15f));
                        seq.Append(Tr?.DOScale(1f, 0.15f));
                        //关闭遮罩层
                        GameEvent.Send(EventType.SetTipsMask, false);
                    });
                    break;
                case ERewardType.Prop: //道具奖励
                    window ??= GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName);
                    if (propRews == null)
                    {
                        propRews = new List<OneRewardData>()
                        {
                            new OneRewardData(type, id, count, flyType)
                        };
                    }
                    FlySystem.Instance.GetFlyProp(window.transform, propRews, startPos, endTr, (Tr) =>
                    {
                        for (int i = 0; i < propRews.Count; i++)
                        {
                            GameEvent.Send(EventType.UpdatePropBtnState, (PropType)propRews[i].Id);
                        }
                        callback?.Invoke();
                        float scale = Tr.localScale.x;
                        Sequence seq = DOTween.Sequence();
                        seq.Append(Tr?.DOScale(scale * 1.1f, 0.15f));
                        seq.Append(Tr?.DOScale(scale * 0.9f, 0.15f));
                        seq.Append(Tr?.DOScale(scale, 0.15f));
                        GameEvent.Send(EventType.SetTipsMask, false);
                    });
                    break;
                case ERewardType.NoAd:
                    GameEvent.Send(EventType.SetTipsMask, false);
                    break;
                case ERewardType.Photo:
                    GameEvent.Send(EventType.SetTipsMask, false);
                    //打开照片收集面板,作战照片，只展示第一张，但是数量却是总和
                    GameModule.UI.ShowUI<Panel_Collectphotos>(propRews[0].Id, propRews.Count);
                    break;
                default:
                    GameEvent.Send(EventType.SetTipsMask, false);
                    break;
            }
        }

        /// <summary>
        /// 发送奖励（不需要飞向目标）
        /// </summary>
        /// <param name="startPosList">奖励所有初始位置</param>
        /// <param name="reward">奖励数据</param>
        /// <param name="activityType">活动类型</param>
        public static void FlyReward(List<Vector3> startPosList, CfgReward reward, ActivityType activityType, int _curLoopTimes)
        {
            Transform parent = GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName).transform;
            List<OneRewardData> oneRewardList = new List<OneRewardData>();
            //bool isHaveRandom = PhotoController.Instance.IsHaveRandomPhotos(activityType) && PhotoController.Instance.IsHaveLastRandomPhotos(activityType);
            //如果当前活动奖励是随机的，
            if (reward.IsRandom)
            {
                var acLoopCount = UserController.Instance.GetOneActivityData(activityType).LoopCount;
                bool isHaveCurRandom = PhotoController.Instance.IsHaveRandomPhotos(activityType);
                bool isHaveLastRandom = PhotoController.Instance.IsHaveLastRandomPhotos(activityType);
                //如果此时存在随机奖励记录，就发送这个奖励，否则发送默认奖励
                int curIndex = (isHaveCurRandom || isHaveLastRandom) ? 0 : 1;
                var rewCfg = reward.Reward[curIndex];
                OneRewardData oneReward = new OneRewardData();
                if (isHaveCurRandom || isHaveLastRandom)
                {
                    var randomPhotoId = (_curLoopTimes == acLoopCount) ? PhotoController.Instance.GetCurActivityPhotoIds(activityType)[Mathf.Abs(rewCfg.ID)] :
                        PhotoController.Instance.GetLastActivityPhotoIds(activityType)[Mathf.Abs(rewCfg.ID)];
                    oneReward = new OneRewardData(ERewardType.Photo, randomPhotoId, rewCfg.Count, rewCfg.FlyType);
                }
                else
                {
                    oneReward = new OneRewardData(rewCfg.Type, rewCfg.ID, rewCfg.Count, rewCfg.FlyType);
                }
                oneRewardList.Add(oneReward);
                UserController.Instance.SendReward(rewCfg.Type, oneReward.Id, rewCfg.Count,
                    rewCfg.FlyType == itemFlyType.AddTimeFly);
            }
            else
            {
                foreach (var item in reward.Reward)
                {
                    OneRewardData oneReward = new OneRewardData(item.Type, item.ID, item.Count, item.FlyType);
                    oneRewardList.Add(oneReward);
                    UserController.Instance.SendReward(item.Type, item.ID, item.Count, item.FlyType == itemFlyType.AddTimeFly);
                }
            }

            FlySystem.Instance.GetFlyPropNoTarget(parent, oneRewardList, startPosList, () =>
            {
                Instance.SendRewardEvent(oneRewardList);
            });
        }
        /// <summary>
        /// 发送奖励（不需要飞向目标）
        /// </summary>
        /// <param name="startPosList">奖励所有初始位置</param>
        /// <param name="reward">奖励数据列表</param>
        /// <param name="activityType">活动类型</param>
        public static void FlyReward(List<Vector3> startPosList, List<TBRewardData> reward)
        {
            Transform parent = GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName).transform;
            List<OneRewardData> oneRewardList = new List<OneRewardData>();
            int abType = UserController.Instance.GetCurUserType;
            foreach (var item in reward)
            {
                int rewId = item.ID;
                //对于图片奖励，如果是B用户，则设置为B类ID
                if (item.Type == ERewardType.Photo)
                {
                    rewId = abType == 1 ? item.OtherID : item.ID;
                }
                OneRewardData oneReward = new OneRewardData(item.Type, rewId, item.Count, item.FlyType);
                oneRewardList.Add(oneReward);
                UserController.Instance.SendReward(item.Type, rewId, item.Count, item.FlyType == itemFlyType.AddTimeFly);
            }
            FlySystem.Instance.GetFlyPropNoTarget(parent, oneRewardList, startPosList, () =>
            {
                Instance.SendRewardEvent(oneRewardList);
            });
        }
        public static void FlyPropReward(Transform startPos)
        {
            //打开遮罩层
            GameEvent.Send(EventType.SetTipsMask, true);
            var waitPropRes = Instance._waitPropRes;
            FlySystem.Instance.GameGetFlyProps(waitPropRes, startPos.position, (tr, pt) =>
            {
                GameEvent.Send(EventType.UpdatePropBtnState, (PropType)pt.Id);
            }, () =>
            {
                GameEvent.Send(EventType.SetTipsMask, false);
                Instance._waitPropRes.Clear();
            });

        }

        //时间冻结特效飞行
        public static void FreezeTime(Vector3 startPos, Action<Transform> call)
        {
            Transform parent = GameModule.UI.GetWindow(typeof(Panel_Game).FullName).transform;
            var targetPos = FlySystem.Instance.GetPos(FlyReType.FreezeTime);
            FlySystem.Instance.GetNormalFly("prefab_FlyFreezeTime", parent, startPos, targetPos, call);
        }

        //金块收集
        public static void CollectGold(Vector3 startPos, Action call)
        {
            Transform parent = GameModule.UI.GetWindow(typeof(Panel_Game).FullName).transform;
            FlySystem.Instance.GetGoldFly1(parent, startPos, call, true);
        }
        private void SendRewardEvent(List<OneRewardData> datas)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                switch (data.Type)
                {
                    case ERewardType.Currency:
                        if (data.Id == 1)
                        {
                            GameEvent.Send(EventType.RefreshLife);
                        }
                        else
                        {
                            GameEvent.Send(EventType.RefreshCurrency, data.Id);
                        }
                        break;
                    case ERewardType.Prop:
                        PropType curT = (PropType)data.Id;
                        GameEvent.Send(EventType.UpdatePropBtnState, curT);
                        break;
                    case ERewardType.GoldCube:
                        break;
                    case ERewardType.LevelKey:
                        break;
                }
            }
        }
        public void UpdateRewardEvent(ERewardType type, int id)
        {
            switch (type)
            {
                case ERewardType.Currency:
                    if (id == 1)
                    {
                        GameEvent.Send(EventType.RefreshLife);
                    }
                    else
                    {
                        GameEvent.Send(EventType.RefreshCurrency, id);
                    }
                    break;
                case ERewardType.Prop:
                    PropType curT = (PropType)id;
                    GameEvent.Send(EventType.UpdatePropBtnState, curT);
                    break;
                case ERewardType.GoldCube:
                    break;
                case ERewardType.LevelKey:
                    break;
                case ERewardType.NewPlayerPack:
                    GameEvent.Send(EventType.RefreshCurrency, 0);
                    break;
            }
        }
        #endregion

        #region 道具数量，名称
        public static string GetRewardIcon(TBRewardData cfg)
        {
            if (cfg.Type == ERewardType.Currency)
            {
                switch (cfg.ID)
                {
                    case 0: return "icon_sc_zs02";
                }
            }
            else if (cfg.Type == ERewardType.Prop)
            {
                return ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetById(cfg.ID).Icon;
            }
            return "icon_sc_zs02";
        }
        public static string GetRewardIcon(OneRewardData data)
        {
            if (data.Type == ERewardType.Currency)
            {
                switch (data.Id)
                {
                    case 0: return "icon_sc_zs02";
                }
            }
            else if (data.Type == ERewardType.Prop)
            {
                return ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetById(data.Id).Icon;
            }
            return "icon_sc_zs02";
        }
        public static string GetFlyItemIcon(OneRewardData cfg)
        {
            if (cfg.Type == ERewardType.Currency)
            {
                switch (cfg.Id)
                {
                    case 0: return "icon_sc_zs02";
                }

            }
            else if (cfg.Type == ERewardType.Prop)
            {
                return ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetById(cfg.Id).Icon;
            }
            return "icon_sc_zs02";
        }

        public static string GetRewardCount(TBRewardData cfg, bool isInfinitely = false)
        {
            switch (cfg.FlyType)
            {
                case itemFlyType.None:
                case itemFlyType.EachFly:
                case itemFlyType.MultipleFly:
                    return $"{cfg.Count}";
                case itemFlyType.AddTimeFly:
                    return $"{(!isInfinitely ? "" : "∞")}{(cfg.Count <= 3600 ? $"{TimeUtil.GetSecToMin(cfg.Count)}m" : $"{TimeUtil.GetSecToHour(cfg.Count, true)}")}";
                default:
                    return $"{cfg.Count}";
            }
        }
        public static string GetRewardCount(OneRewardData cfg, bool isInfinitely = false)
        {
            switch (cfg.flyType)
            {
                case itemFlyType.None:
                case itemFlyType.EachFly:
                case itemFlyType.MultipleFly:
                    return $"{cfg.Count}";
                case itemFlyType.AddTimeFly:
                    return $"{(!isInfinitely ? "" : "∞")}{(cfg.Count <= 3600 ? $"{TimeUtil.GetSecToMin(cfg.Count)}m" : $"{TimeUtil.GetSecToHour(cfg.Count, true)}")}";
                default:
                    return $"{cfg.Count}";
            }
        }

        /// <summary>
        /// 格式化货币
        /// </summary>
        /// <param name="amount">货币数值</param>
        /// <returns></returns>
        public static string FormatCurrencyAuto(int amount)
        {
            if (amount >= 10000)
            {
                decimal value = amount / 10000m;
                if (value == Math.Floor(value))
                {
                    return $"{(int)value}w";
                }
                return string.Format("{0:0.00}w", value);
            }
            else//小数值直接显示
            {
                return amount.ToString();
            }
        }
        /// <summary>
        /// 获取图片地址
        /// </summary>
        /// <param name="imgName">传入图片名字</param>
        /// <param name="imgType">图片类型，后缀</param>
        /// <param name="AbType">图片AB面类型，0-A面，1-B面</param>
        /// <returns></returns>
        public static string GetImgUrl(string imgName, string imgType, int AbType)
        {
            //获取现在的展示逻辑，在线参数获取到指定的图片首地址
            string url;
            if (AbType == 1)//B面,测试期间默认使用B面
            {

#if UNITY_ANDROID
                url = $"{ConfigLoader.Constant.ImgBUrl}{imgName}.{imgType}";
#elif UNITY_IOS
                url = $"{ConfigLoader.Constant.IOSImgBUrl}{imgName}.{imgType}";
#else
                url = $"{ConfigLoader.Constant.ImgBUrl}{imgName}.{imgType}";
#endif
            }
            else
            {
#if UNITY_ANDROID

                url = $"{ConfigLoader.Constant.ImgAUrl}{imgName}.{imgType}";
#elif UNITY_IOS
                url = $"{ConfigLoader.Constant.IOSImgAUrl}{imgName}.{imgType}";
#else
                url = $"{ConfigLoader.Constant.ImgAUrl}{imgName}.{imgType}";
#endif
            }
            //Log.Info($"[PropsController] GetImgUrl: AB={AbType}, ImageID={imgName}, 最终URL={url}");
            return url;
        }

        public static string GetImgUrl(int PhotoId)
        {
            var _curPhotoInfo = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(PhotoId);
            return GetImgUrl(_curPhotoInfo.ImageID, _curPhotoInfo.ImageType, _curPhotoInfo.ABGroup);
        }
        /// <summary>
        /// 获取视频地址
        /// </summary>
        /// <param name="videoName">传入视频名称，带后缀</param>
        /// <param name="AbType">视频AB面类型，0-A面，1-B面</param>
        /// <returns></returns>
        public static string GetVideoUrl(string videoName, int AbType, bool isVideo)
        {
            //只有B面才有视频，A面直接返回一个默认视频地址
            //获取现在的展示逻辑，在线参数获取到指定的视频首地址
            string url=null;
            if (isVideo)//B面,测试期间默认使用B面
            {
#if UNITY_ANDROID
                string firstUrl = AbType == 1 ? $"{ConfigLoader.Constant.VideoBUrl}" : $"{ConfigLoader.Constant.VideoAUrl}";
#elif UNITY_IOS
                string firstUrl = AbType == 1 ? $"{ConfigLoader.Constant.IOSVideoBUrl}" : $"{ConfigLoader.Constant.IOSVideoAUrl}";
#else
                string firstUrl = AbType == 1 ? $"{ConfigLoader.Constant.VideoBUrl}" : $"{ConfigLoader.Constant.VideoAUrl}";
#endif
                url = $"{firstUrl}{Uri.EscapeDataString(videoName)}.mp4";
            }
            else//A面返回一个默认视频地址
            {
                //url = $"{ConfigLoader.Constant.VideoAUrl}Test1.mp4";
            }
            Log.Info($"[PropsController] GetVideoUrl: AB={AbType}, VideoName={videoName}, IsVideo={isVideo}, 最终URL={url}");
            return url;
        }

        public static string GetVideoUrl(int PhotoId)
        {
            var _curPhotoInfo = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(PhotoId);
            return GetVideoUrl(_curPhotoInfo.ImageID, _curPhotoInfo.ABGroup, _curPhotoInfo.IsVideo);
        }
        #endregion
    }



    /// <summary>
    /// 进入游戏时，携带的助推器道具信息
    /// </summary>
    public struct GameBooster
    {
        public PropType Type;
        public int Num;

        public GameBooster(PropType type, int num)
        {
            Type = type;
            Num = num;
        }
    }

    /// <summary>
    /// 临时奖励数据结构
    /// </summary>
    public struct OneRewardData
    {
        public ERewardType Type;
        public int Id;
        public long Count;

        public itemFlyType flyType;

        //无效奖励
        private static readonly OneRewardData _noneData = new OneRewardData(ERewardType.None, 0, 0, itemFlyType.None);

        public static OneRewardData NoneData
        {
            get => _noneData;
        }

        public OneRewardData(ERewardType type, int id, long count, itemFlyType fly)
        {
            Type = type;
            Id = id;

            Count = count;
            flyType = fly;
        }

        //设置金砖奖励
        public static OneRewardData GoldCube(int count)
        {
            return new OneRewardData(count);
        }

        private OneRewardData(int count)
        {
            Type = ERewardType.GoldCube;
            Id = 0;

            Count = count;
            flyType = itemFlyType.EachFly;
        }

    }
}