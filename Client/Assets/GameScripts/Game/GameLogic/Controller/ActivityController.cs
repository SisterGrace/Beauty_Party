using System.Collections;
using System.Collections.Generic;
using Config.common;
using UnityEngine;
using Y_Base;
using TK_Engine;

namespace GameLogic
{
    public class ActivityController : Singleton<ActivityController>
    {
        private int level => UserController.Instance.Level;
        private Dictionary<ActivityType, ActityData> ActivityDic => UserController.Instance.CurActivityData;
        private List<ActivityType> RemoveActivityList => UserController.Instance.CurRemoveActivityList;
        private bool IsNoAdsActivity => UserController.Instance.AdFree;

        public void UpgradeCheckActivity()
        {
            var list = ConfigLoader.Instance.Tables.TbCfgActivity.DataList;
            foreach (var cfg in list)
            {
                // A类用户不解锁抽卡活动，需通关50关切换为B类用户后才能解锁
                if (cfg.ActType == ActivityType.Gacha && UserController.Instance.GetCurUserType == 0) continue;
                int unLockLevel = cfg.UnLockLevel;
                // B类用户从A类转换过来时(level>cfg.UnLockLevel)，若抽卡尚未解锁则强制匹配当前关卡
                if (cfg.ActType == ActivityType.Gacha && level > unLockLevel && !ActivityDic.ContainsKey(ActivityType.Gacha))
                {
                    unLockLevel = level;
                }
                if (unLockLevel == level && cfg.IsOpen)
                {
                    var data = new ActityData(cfg);
                    if (cfg.ActType == ActivityType.NewPlayerPack && UserController.Instance.GetBugStarterPack) continue;
                    UserController.Instance.AddActivityData(cfg.ActType, data);
                    //用于解锁后弹出
                    if (cfg.OpenType >= 2)
                    {
                        switch (cfg.ActType)
                        {
                            case ActivityType.DreamPass:
                                GameModule.UI.AddOrderPanel<Panel_DreamPass>(cfg.Order, false, data);
                                break;
                            case ActivityType.Adfree:
                                //GameModule.UI.AddOrderPanel<Panel_NoAd>(cfg.Order,false,cfg);
                                break;
                            case ActivityType.UntoldTreasure:
                                GameModule.UI.AddOrderPanel<Panel_EndlessTreasure>(cfg.Order, false, data);
                                break;
                            case ActivityType.VIP://VIP
                                GameModule.UI.AddOrderPanel<Panel_VIP>(cfg.Order, false);
                                break;
                            case ActivityType.Gacha:
                                GameModule.UI.AddOrderPanel<Panel_Gacha>(cfg.Order, false);
                                break;
                            case ActivityType.NewPlayerPack:
                                GameModule.UI.AddOrderPanel<Panel_StarterPack>(cfg.Order, false, data);
                                break;
                            case ActivityType.DayCheckIn:
                                GameModule.UI.AddOrderPanel<Panel_DayCheckIn>(cfg.Order, false);
                                break;
                            case ActivityType.Discard:
                                // 禁用 Discord 社区弹窗
                                // GameModule.UI.AddOrderPanel<Panel_Discard>(cfg.Order, false);
                                break;
                        }
                    }
                }
            }

            GameEvent.Send(EventType.RefreshActivity);
        }

        /// <summary>
        /// 是否开启活动
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isCheckOpen"></param>
        /// <returns></returns>
        public bool CheckActivityOpen(ActivityType id, bool isCheckOpen = true)
        {
            if (!ActivityDic.ContainsKey(id))
            {
                var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(id);
                if (cfg == null) return false;
                var isOpen = level >= cfg.UnLockLevel && cfg.IsOpen;
                switch (cfg.ActType)
                {
                    case ActivityType.DreamPass:
                        break;
                    case ActivityType.Adfree:
                        if (isOpen && !IsNoAdsActivity)
                        {
                            var data = new ActityData(cfg);
                            UserController.Instance.AddActivityData(cfg.ActType, data);
                            GameEvent.Send(EventType.RefreshActivity);
                            return true;
                        }

                        break;
                    case ActivityType.UntoldTreasure:
                        if (isOpen)
                        {
                            var data = new ActityData(cfg);
                            UserController.Instance.AddActivityData(cfg.ActType, data);
                            GameEvent.Send(EventType.RefreshActivity);
                            return true;
                        }

                        break;
                    case ActivityType.Gacha:
                        if (isOpen && UserController.Instance.GetCurUserType == 1)
                        {
                            var data = new ActityData(cfg);
                            UserController.Instance.AddActivityData(cfg.ActType, data);
                            GameEvent.Send(EventType.RefreshActivity);
                            return true;
                        }

                        break;
                    case ActivityType.NewPlayerPack:
                        //1. 玩家第二次登录之后，就进行新手礼包判定，未购买则弹出，已购买永久关闭弹窗以及隐藏入口icon
                        /*if (isOpen && !SDKManager.Instance.GetIsNewUser && !UserController.Instance.GetBugStarterPack)
                        {
                            var data = new ActityData(cfg);
                            UserController.Instance.AddActivityData(cfg.ActType, data);
                            GameEvent.Send(EventType.RefreshActivity);
                            return true;
                        } */
                        break;
                    case ActivityType.Shop:
                        if (isOpen)
                        {
                            var data = new ActityData(cfg);
                            UserController.Instance.AddActivityData(cfg.ActType, data);
                            GameEvent.Send(EventType.RefreshActivity);
                            return true;
                        }
                        break;
                    case ActivityType.H5Check:
                        //H5功能验证,此时需要验证一下在线参数是否开启
                        bool isH5CheckOpen = SDKManager.Instance.H5CheckInfo.H5CheckOpen;
                        if (isOpen&&isH5CheckOpen)
                        {
                            var data = new ActityData(cfg);
                            UserController.Instance.AddActivityData(cfg.ActType, data);
                            GameEvent.Send(EventType.RefreshActivity);
                            return true;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// 打开免广告活动
        /// </summary>
        public void OpenNoADActivity()
        {
            var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.Adfree);
            if (cfg == null || !cfg.IsOpen) return;

            CheckActivityOpen(ActivityType.Adfree);
            if (!IsNoAdsActivity)
            {
                GameModule.UI.ShowUI<Panel_NoAd>();
            }

        }

        public void RemoveActivity(ActivityType id)
        {
            UserController.Instance.RemoveOneActivityData(id);
            GameEvent.Send(EventType.RefreshActivity);
        }

        /// <summary>
        /// 打开所有活动
        /// </summary>
        public void OpenAllActivity()
        {
            var list = ConfigLoader.Instance.Tables.TbCfgActivity.DataList;
            foreach (var cfg in list)
            {
                if (cfg.IsOpen)
                {
                    var data = new ActityData(cfg);
                    UserController.Instance.AddActivityData(cfg.ActType, data);
                }
            }

            GameEvent.Send(EventType.RefreshActivity);
        }

        /// <summary>
        /// 关闭所有活动
        /// </summary>
        public void CloseAllActivity()
        {
            GameEvent.Send(EventType.CloseAllActivityBtns);
            var list = ConfigLoader.Instance.Tables.TbCfgActivity.DataList;
            foreach (var cfg in list)
            {
                if (cfg.IsOpen)
                {
                    RemoveActivity(cfg.ActType);
                }
            }

            GameEvent.Send(EventType.RefreshActivity);
        }
    }
}