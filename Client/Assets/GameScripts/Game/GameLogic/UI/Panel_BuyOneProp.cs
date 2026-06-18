using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using Y_Base;
using EventType = GameLogic.EventType;
namespace GameLogic
{
	[Window(UILayer.UI,PlayAnim = true)]
	public partial class Panel_BuyOneProp : UIWindow
	{
		private PropType _propType;
		private CfgGamePropInfo _cfgGamePropInfo;
		//道具生成的窗口
		private UIWindow _parentWindow;
		private HCRVPositionName curAdPos;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_propType = (PropType)userDatas[0];
				_parentWindow = (UIWindow)userDatas[1];
			}

			if (_propType != PropType.None)
			{
				_cfgGamePropInfo = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetByType(_propType);
			}
			UpdateInfo();
			ShowAdEnter();
			SDKManager.Instance.LogEvent("RV_Button_Show","Position",curAdPos.ToString());
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener(EventType.SetAdEnter, ShowAdEnter);
			GameEvent.AddEventListener<int>(EventType.RefreshCurrency, UpdateCurrency);
		}
		//更新界面信息
		private void UpdateInfo()
		{
			mPropName.SetLanguage(_cfgGamePropInfo.PropName);
			mPropIcon.SetSprite("ui"+_cfgGamePropInfo.Icon,true);
			mPropCount.text = $"x{_cfgGamePropInfo.GetCount}";
			mPropTipsTxt.SetLanguage(_cfgGamePropInfo.PropTips);
			mCost.text = $"<sprite name=\"icon_jinbi\">{_cfgGamePropInfo.Cost}";
			UpdateCurrency(1);
			switch (_cfgGamePropInfo.Type)
			{
				case PropType.Revoke:
					curAdPos = HCRVPositionName.RV_PropA;
					break;
				case PropType.MessUp:
					curAdPos = HCRVPositionName.RV_PropB;
					break;
				case PropType.DestoryGruop:
					curAdPos = HCRVPositionName.RV_PropC;
					break;
				case PropType.MoveOut:
					curAdPos = HCRVPositionName.RV_PropD;
					break;
				case PropType.Prompt:
					curAdPos = HCRVPositionName.RV_Tip;
					break;
				case PropType.DrawCard:
					curAdPos = HCRVPositionName.RV_Puzzle;
					break;
				case PropType.AddSolt:
					curAdPos = HCRVPositionName.RV_Slot;
					break;
			}
		}

		private void UpdateCurrency(int typeId)
		{
			if (typeId == 1)
			{
				mGoldCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
			}
		}
		private void ShowAdEnter()
		{
			if (!UserController.Instance.AdFree&&UserController.Instance.GetOneActivityData(ActivityType.Adfree)!=null)
			{
				mGiftNode.gameObject.Show();
			}
			else
			{
				mGiftNode.gameObject.Hide();
			}
		}
		protected override void Close()
		{
			base.Close();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener(EventType.SetAdEnter, ShowAdEnter);
			GameEvent.RemoveEventListener<int>(EventType.RefreshCurrency, UpdateCurrency);
		}
	}
}
