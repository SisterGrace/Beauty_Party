using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using DG.Tweening;



namespace GameLogic
{
	public partial class Panel_ExitLevel
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mExit;
		protected UIButton mKeep;
		protected ScrollRect mTipsView;
		protected RectTransform mViewport;
		protected RectTransform mTipsContent;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mExit = FindChildComponent<UIButton>("AnimNode/Btn_Exit");
			mKeep = FindChildComponent<UIButton>("AnimNode/Btn_Keep");
			mTipsView = FindChildComponent<ScrollRect>("AnimNode/Scroll_TipsView");
			mViewport = FindChildComponent<RectTransform>("AnimNode/Scroll_TipsView/Rect_Viewport");
			mTipsContent = FindChildComponent<RectTransform>("AnimNode/Scroll_TipsView/Rect_Viewport/Rect_TipsContent");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mExit.AddListener(ClickExit);
			mKeep.AddListener(ClickKeep);
			mMask.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickExit(GameObject obj)
		{
			_exitTimes--;
			if (_exitTimes < 0)
			{
				var _selectPhotoInfo = LevelController.Instance.CurLevelSelectPhotoInfo;
				SDKManager.Instance.LogEvent("Level_End", new Dictionary<string, object>()
					{
						{ "Level_Time", LevelController.Instance.GetGameTime(TimeUtil.GetTimeStamp()) },
						{ "End_Type", 3 }
					},
					CommonEvent.ComLevelEvent);
				
				Close();
				GameModule.UI.CloseUI<Panel_Game>();
				GameModule.UI.ShowUI<Panel_Home>();
				//GameModule.UI.ShowUI<Panel_LevelInfo>();
				LevelController.Instance.ExitLevel(false);
				
				return;
			}
			mTipsView.DOHorizontalNormalizedPos(1, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
			{
				if (_exitTimes==0)
				{
					_failTipsList[1].FailTipsShow();
				}
			});
		}

		private void ClickKeep(GameObject obj)
		{
			Close();
		}
		#endregion
	}
}
