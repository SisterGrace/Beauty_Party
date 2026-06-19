using System;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using UnityEngine;
using Y_Base;
using TK_Engine;

namespace GameLogic
{
    /// <summary>
    /// 游戏作弊管理器
    /// </summary>
    public class GmController : Singleton<GmController>
    {
        public void Init()
        {
            SetADFree();
            if (!SettingsUtils.FrameworkGlobalSettings.DebugModeEnabled) return;
            GameEvent.AddEventListener<int>(EventType.SetLevel, SetLevel);
            GameEvent.AddEventListener(EventType.SetGold, SetGold);
            GameEvent.AddEventListener<PropType>(EventType.SetProps, AddProp);
            GameEvent.AddEventListener(EventType.SetAdFree, SetADFree);
            GameEvent.AddEventListener(EventType.OpenAd, OpenAd);
            GameEvent.AddEventListener<bool>(EventType.SetGameUIShow, ShowUI);
            GameEvent.AddEventListener<bool>(EventType.SetAllActivity, OpenAllActivity);
            GameEvent.AddEventListener(EventType.QuicklyEndPassport, QuicklyEndPassport);
            GameEvent.AddEventListener<int>(EventType.setPayType, SetPayType);
            GameEvent.AddEventListener<string>(EventType.SetPayCountry, SetPayCountry);
            GameEvent.AddEventListener<int>(EventType.SetUserType, SetUserType);
            GameEvent.AddEventListener<bool>(EventType.SetUserASN, SetUserASN);
            GameEvent.AddEventListener<string>(EventType.TestPhotoRewardOverride, TestPhotoRewardOverride);
            GameEvent.AddEventListener<string>(EventType.TestIAPToIAAOverride, TestIAPToIAAOverride);
            GameEvent.AddEventListener(EventType.TestSkipCheckInDay, TestSkipCheckInDay);
        }

        private void SetLevel(int id)
        {
            UserController.Instance.Level = id;
            GameModule.UI.RemoveOrderPanel<Panel_UnlockOneProp>(false);
            GameModule.UI.RemoveOrderPanel<Panel_DreamPass>(false);
            GameModule.UI.RemoveOrderPanel<Panel_EndlessTreasure>(false);
            PhotoController.Instance.PhotoPreloading(true, true);
        }

        private void SetGold()
        {
            UserController.Instance.Diamond += 1000;
        }
    
        public void AddProp(PropType type)
        {
            UserController.Instance.SetOneProp(type, 3, isUpdateUI: true);
        }



        private void SetADFree()
        {
            UserController.Instance.SetNoAdsEndTime(-1);
            SDKManager.Instance.IsDebugFreeAds = true;
            //ActivityController.Instance.RemoveActivity(ActivityType.Adfree);
        }

        private void OpenAd()
        {
            UserController.Instance.SetNoAdsEndTime(1);
            SDKManager.Instance.IsDebugFreeAds = false;
        }

        //开启和隐藏游戏UI
        private void ShowUI(bool isHide)
        {
            GameModule.UI.HideAllWithOut<Panel_Game>(isHide);
        }

        private void OpenAllActivity(bool isOpen)
        {
            if (isOpen)
            {
                ActivityController.Instance.OpenAllActivity();
            }
            else
            {
                ActivityController.Instance.CloseAllActivity();
            }
        }
        //快速结束通行证
        private void QuicklyEndPassport()
        {
            if (UserController.Instance.GetOneActivityData(ActivityType.DreamPass) != null)
            {
                UserController.Instance.GetOneActivityData(ActivityType.DreamPass).CurrDreamPassLevel = 31;
                for (int i = 1; i < 31; i++)
                {
                    UserController.Instance.GetOneActivityData(ActivityType.DreamPass).BuyCount.Add(i);
                }
            }

        }


        #region 设置支付Debug

        private void SetPayType(int type)
        {
            SDKManager.Instance.MyPayType = type;
        }

        private void SetPayCountry(string country)
        {
            SDKManager.Instance.PayCountry = country;
        }
        #endregion
        //设置用户类型
        private void SetUserType(int type)
        {
            UserController.Instance.GetCurUserType = type;
           
        }
        //设置用户ASN
        private void SetUserASN(bool isASN)
        {
            SDKManager.Instance.SetUserASN(isASN);
        }

        private void TestPhotoRewardOverride(string testJson)
        {
            if (string.IsNullOrEmpty(testJson))
            {
                Log.Warning("[GM] 测试JSON为空，直接调用 ApplyPhotoRewardOverrides（从远程配置读取）");
                ActivityPhotoRemoteHelper.ApplyPhotoRewardOverrides();
                return;
            }

            ActivityPhotoRemoteHelper.ApplyPhotoRewardOverridesFromJson(testJson);
        }

        private void TestIAPToIAAOverride(string testJson)
        {
            if (string.IsNullOrEmpty(testJson))
            {
                Log.Warning("[GM] 测试JSON为空，请输入如 [1001, 1002] 的数组格式");
                return;
            }

            ActivityPhotoRemoteHelper.ApplyIAPToIAAOverridesFromJson(testJson);
        }

        private void TestSkipCheckInDay()
        {
            var data = UserController.Instance.GetOneActivityData(ActivityType.DayCheckIn);
            if (data == null)
            {
                Log.Warning("[GM] 七日签到活动数据不存在");
                return;
            }

            if (data.IsAllCheckInClaimed())
            {
                Log.Warning($"[GM] 已完成全部7天签到，自动重置签到周期");
                data.ResetCheckInCycle();
            }

            data.LastCheckInZeroTime = 0;
            Log.Info($"[GM] 签到日重置成功，当前进度=第{data.CurrentCheckInDay}天，可立即签到");
        }
    }
}
