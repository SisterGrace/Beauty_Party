using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
using System.Collections.Generic;
using Config.common;
namespace GameLogic
{
	public partial class Panel_Fail
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mAdFree;
		protected UIButton mSkip;
		protected UIButton mCurGold;
		protected RectTransform mBg;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected Transform mloadIcon;
		protected TextMeshProUGUI mGoldCount;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("AnimNode/Rect_Bg/Btn_Close");
			mAdFree = FindChildComponent<UIButton>("AnimNode/Rect_Bg/Btn_AdFree");
			mSkip = FindChildComponent<UIButton>("AnimNode/Rect_Bg/Btn_Skip");
			mCurGold = FindChildComponent<UIButton>("AnimNode/Btn_CurGold");
			mBg = FindChildComponent<RectTransform>("AnimNode/Rect_Bg");
			mPhotoNode = FindChild("AnimNode/Rect_Bg/Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("AnimNode/Rect_Bg/Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mloadIcon = FindChild("AnimNode/Rect_Bg/Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mGoldCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_CurGold/TMP_GoldCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_photoLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
			mClose.AddListener(ClickClose);
			mAdFree.AddListener(ClickAdFree);
			mSkip.AddListener(ClickClose);
			mMask.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
            SDKManager.Instance.Button_ShowIV(HCIVPositionName.IV_Fail, IvType.IV1);
            _currentTipIndex++;
			//当关闭次数大于关闭提示次数，直接关闭面板并返回主界面
			if (_currentTipIndex>_closeTipTimes)
			{
				var _selectPhotoInfo = LevelController.Instance.CurLevelSelectPhotoInfo;
				SDKManager.Instance.LogEvent("Level_End",new Dictionary<string, object>()
				{
					{"Level_Time",LevelController.Instance.GetGameTime(TimeUtil.GetTimeStamp())},
					{"End_Type",2}
				},CommonEvent.ComLevelEvent);
				SDKManager.Instance.LogEvent("Level_Fail",CommonEvent.ComLevelEvent);
				GameModule.UI.CloseUI<Panel_Game>();
				GameModule.UI.ShowUI<Panel_Home>();
				if (!_isGetFailPack)
				{
					//GameModule.UI.ShowUI<Panel_LevelInfo>();
				}
				else
				{
					//GameModule.UI.AddOrderPanel<Panel_LevelInfo>(10,false);
					PropsController.Instance.FlyReward(transform.position);
				}
				Close();
				LevelController.Instance.ExitLevel(false);
				return;
			}

		}

		private void ClickAdFree(GameObject obj)
		{
			//每一局限制广告复活两次
			if (LevelController.Instance.UseAdReviveCount>=2)
			{
				GameEvent.Send(EventType.ShowTips,"uitips_morelifelAd_tips",true);
				return;
			}
			//SDKManager.Instance.Button_ShowNative(mnative);
			SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_Revive, (success) =>
			{
				if (success)
				{
					ContinueGame();
					LevelController.Instance.UseAdReviveCount++;
					LevelController.Instance.GameRvCount++;
				}
			});
			
		}
		private void ClickGoldContinued(GameObject obj)
		{
			if (_isFreeContinue)
			{
				ContinueGame();
				GuideController.Instance.NextGuide();
				//把失败次数重置为0，防止连续失败后，增加的金币不足以继续游戏
				CardsController.Instance._loseCount = 0;
				return;
			}
			int Curcost = 900 + (_failTimes - 1) * 300;
			if (UserController.Instance.CheckConsume(ERewardType.Currency, 0, Curcost))
			{
				UserController.Instance.UserConsume(ERewardType.Currency,0,Curcost);
				ContinueGame();
			}
			else
			{
				//GameModule.UI.ShowUI<Panel_Shop>(1);
				//提示金币不足
				GameEvent.Send(EventType.ShowTips, "uitps_floattip_notcoins",true);
			}
		}
		#endregion
	}
}
