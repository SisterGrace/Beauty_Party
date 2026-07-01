using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using Y_Base;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_DayCheckIn : UIWindow
	{
		//前6天签到
		private UIListWidget<Widget_DayCheckItem, DayCheckItemInfo> _daysListWidget;
		//最后一天签到
		private Widget_DayCheckItem _lastDayCheckItem;
		private ActityData _activityData;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_activityData = (ActityData)userDatas[0];
			}
			else
			{
				_activityData = UserController.Instance.GetOneActivityData(ActivityType.DayCheckIn);
			}
			if (_activityData == null) return;
			CreateDaysList();
			SetCheckBtn();
			// 确保按钮锁被释放，防止禁用按钮被点击后锁定所有按钮
			if (GameModule.UI != null)
			{
				GameModule.UI.IsButtonLock = false;
			}
			SDKManager.Instance.LogEvent("Sign_Show");
		}
		private void CreateDaysList()
		{
			//这里创建列表，只创建6天的，第七天是额外的
			var daysInfo = ConfigLoader.Instance.Tables.TbCfgDayCheck;
			int needCount = daysInfo.DataList.Count - 1;
			if (needCount > 0)
			{
				if (_daysListWidget == null)
				{
					_daysListWidget = CreateWidget<UIListWidget<Widget_DayCheckItem, DayCheckItemInfo>>(mNormalDays);
				}
				List<DayCheckItemInfo> infoList = new List<DayCheckItemInfo>();
				for (int i = 0; i < needCount; i++)
				{
					var curState = _activityData.GetDayState(i + 1);
					DayCheckItemInfo info = new DayCheckItemInfo(daysInfo.Get(i).Id, curState, true);
					infoList.Add(info);
				}
				_daysListWidget.SetDatas(infoList);
			}
			//最后一天签到
			var lastState = _activityData.GetDayState(daysInfo.DataList.Count);
			DayCheckItemInfo lastinfo = new DayCheckItemInfo(daysInfo.Get(daysInfo.DataList.Count - 1).Id, lastState, false);
			if (_lastDayCheckItem == null)
			{
				_lastDayCheckItem = CreateWidgetByType<Widget_DayCheckItem>(mDayListArea);
			}
			_lastDayCheckItem.SetItemData(lastinfo);
		}
		private void SetCheckBtn()
		{
			bool isCanCheck = _activityData.CanCheckInToday();
			//mMask.SetActive(!isCanCheck);
			// 设置按钮启用/禁用状态（控制变灰和点击）
			mCheck.ButtonEnabled = isCanCheck;
			if (isCanCheck)
			{
				mCheckState.SetLanguage("uitps_daycheck_check");
			}
			else
			{
				mCheckState.SetLanguage("uitps_daycheck_checked");
			}
		}
		private void GetDayCheckRew()
		{
			if (_activityData.CanCheckInToday())
			{
				//弹出钻石获得界面或者照片获得界面
				//先获取到此时点击下去的奖励配置
				var dayCfg = ConfigLoader.Instance.Tables.TbCfgDayCheck.Get(_activityData.CurrentCheckInDay - 1);
				int rewId = dayCfg.RewardId;
				PropsController.Instance.SendRewardData(rewId);
				var rewCfg = ConfigLoader.Instance.Tables.TbCfgReward.Get(dayCfg.RewardId).Reward[0];
				if (rewCfg.Type == ERewardType.Photo)
				{
					//显示照片获得界面
					//这里需要判断当前AB逻辑，显示对应的
					int AbType = UserController.Instance.GetCurUserType;
					int picId = AbType == 1 ? rewCfg.OtherID : rewCfg.ID;
					GameModule.UI.ShowUI<Panel_Collectphotos>(picId, (int)rewCfg.Count);
				}
				else
				{
					//显示钻石获得界面
					GameModule.UI.ShowUI<Panel_CollectDiamond>((int)rewCfg.Count);
				}
				int checkInDay = _activityData.CurrentCheckInDay;
				int daystate = UserController.Instance.DoCheckIn();
				if (daystate > 0)
				{
					SDKManager.Instance.LogEvent("Sign_Click", "Time", checkInDay);
					SetCheckBtn();
					CreateDaysList();
					// 释放按钮锁，防止禁用按钮导致其他按钮无法点击
					if (GameModule.UI != null)
					{
						GameModule.UI.IsButtonLock = false;
					}
					bool hasRedPoint = UserController.Instance.CheckDayCheckInRedPoint();
					GameEvent.Send(EventType.DayCheckInRedPoint, hasRedPoint, "");
				}
				else
				{
					//签到失败
				}
			}
		}
		protected override void RegisterEvent()
		{
		}

		protected override void Close()
		{
			base.Close();
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameModule.UI.RemoveOrderPanel<Panel_DayCheckIn>(true);
		}
	}
}
