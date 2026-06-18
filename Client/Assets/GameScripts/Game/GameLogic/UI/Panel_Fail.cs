using System;
using TK_Engine;
using UnityEngine;
using Y_Base;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Fail : UIWindow
	{
		//关闭的提示次数
		private int _closeTipTimes = 0;
		//当前提示的索引
		private int _currentTipIndex = 0;
		//什么触发的失败，0-合成失败，1-炸弹卡牌触发失败，2倒计时触发失败
		private int _loseType;
		//游戏失败次数
		private int _failTimes = 0;

		private bool _isFreeContinue = false;
		private bool _isGetFailPack = false;
		private Item_PhotoLoad _photoLoad;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_failTimes = (int)userDatas[0];
				_loseType = (int)userDatas[1];
			}
			_closeTipTimes = 0;
			CheckGuide();
			UpdataInfo();
			AudioManager.Play(AudioType.Sound, AudioDef.Fail);
			SDKManager.Instance.LogEvent("RV_Button_Show", "Position", "RV_Revive");
			LevelController.Instance.FirstLose();
			SDKManager.Instance.Button_ShowIV(HCIVPositionName.IV_Victory,IvType.IV1);
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int>(EventType.RefreshCurrency, UpdateGoldCount);
			FlySystem.Instance.RegisterPos(FlyReType.FailGold, mCurGold.transform);
		}

		
		//刷新界面信息
		private void UpdataInfo()
		{	
			_photoLoad.LoadUIImg(LevelController.Instance.CurLevelSelectPhotoInfo.Id);
		}

		private void UpdateGoldCount(int id=1)
		{
			mGoldCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
		}
		private void CheckGuide()
		{
			/* GuideController.Instance.StartGuide(2);
			GuideController.Instance.CheckGuide();
			if (GuideController.Instance.GuideId == 2)
			{
				_isFreeContinue = true;
			} */
		}
		private void ContinueGame()
		{
			//确定是什么触发的失败，0-合成失败，1-炸弹卡牌触发失败，2倒计时触发失败
			switch (_loseType)
			{
				case 0:
					CardsController.Instance.MoveOut(true);
					break;
				case 1:
					//需要触发炸弹消失动画
					foreach (var Bomb in CardsController.Instance.CurFailBombCards)
					{
						Bomb.MySpecialCardLogic.CheckBomb(true,false, (remove, type) =>
						{
							Bomb.MySpecialCardLogic.DestroyBomb(type);
						});
					}
					//如果此时槽位也满了七个或者八个，触发MoveOut
					if (CardsController.Instance.EliminateSlot.IsFull)
					{
						CardsController.Instance.MoveOut(true);
					}
					break;
				case 2:
					//加30秒
					long addTime = ConfigLoader.Constant.SnowAddTime;
					GameEvent.Send(EventType.FreezeTime,(float)addTime,true);
					if (CardsController.Instance.EliminateSlot.IsFull)
					{
						CardsController.Instance.MoveOut(true);
					}
					break;
				case 3:
					//消除一组卡牌
					CardsController.Instance.DestoryOneGruop();
					//如果此时槽位也满了七个或者八个，触发MoveOut
					if (CardsController.Instance.EliminateSlot.IsFull)
					{
						CardsController.Instance.MoveOut(true);
					}
					break;
				case 4:
					//撤回当前卡牌
					CardsController.Instance.Revoke();
					break;
			}

			if (_isGetFailPack)
			{
				PropsController.FlyPropReward(transform);
				_isGetFailPack = false;
			}
			CardsController.Instance.IsFail = false;
			var levelInfo = LevelController.Instance.CurrentLevelInfo;
			if (_loseType != 2 && levelInfo.TimeLevel)
			{
				GameEvent.Send(EventType.PauseGameTime,false);
			}
			GameEvent.Send(EventType.SetGameMask,false);
			Close();
		}
		protected override void Close()
		{
			base.Close();
			
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			FlySystem.Instance. UnRegisterPos(FlyReType.FailGold);
			GameEvent.RemoveEventListener<int>(EventType.RefreshCurrency, UpdateGoldCount);
		}
	}
	

	public struct MyFailTipsInfo
	{
		public string IconName;
		public string TipsTxt;

		public MyFailTipsInfo(string icon, string tips)
		{
			IconName = icon;
			TipsTxt = tips;
		}
	}
}
