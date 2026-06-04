using TK_Engine;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_InlevelSetting : UIWindow
	{
		protected override void OnRefresh()
		{
			SetDifferent(true);
			GameEvent.Send(EventType.PauseGameTime, true);
		}

		protected override void RegisterEvent()
		{
		}

		private void SetDifferent(bool IsEnter, Action callback = null)
		{
			if (IsEnter)
			{
				mSoundEffS2.anchoredPosition = new Vector2(300f, mSoundEffS2.anchoredPosition.y);
				mAudioS2.anchoredPosition = new Vector2(300f, mAudioS2.anchoredPosition.y);
				mShakeS2.anchoredPosition = new Vector2(300f, mShakeS2.anchoredPosition.y);
				mExitGame.RectTr.anchoredPosition = new Vector2(300f, mExitGame.RectTr.anchoredPosition.y);
			}
			float endPosx = IsEnter ? 0f : 300f;
			Sequence seq = DOTween.Sequence();
			seq.Insert(IsEnter ? 0f : 0.15f, mSoundEffS2.DOAnchorPosX(endPosx, 0.25f).SetEase(Ease.OutBack));
			seq.Insert(IsEnter ? 0.05f : 0.1f, mAudioS2.DOAnchorPosX(endPosx, 0.25f).SetEase(Ease.OutBack));
			seq.Insert(IsEnter ? 0.1f : 0.05f, mShakeS2.DOAnchorPosX(endPosx, 0.25f).SetEase(Ease.OutBack));
			seq.Insert(IsEnter ? 0.15f : 0f, mExitGame.RectTr.DOAnchorPosX(endPosx, 0.25f).SetEase(Ease.OutBack));
			seq.OnComplete(() => { callback?.Invoke(); });
		}
		private void ExitGame()
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
			LevelController.Instance.ExitLevel(false);
		}
		protected override void Close()
		{
			SetDifferent(false, () =>
			{
				base.Close();
				GameEvent.Send(EventType.PauseGameTime, false);
			});
		}
	}
}
