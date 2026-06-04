using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace GameLogic
{
	[Window(UILayer.Top)]
	public partial class Panel_GameLoading : UIWindow
	{
		private bool _isLoadSucceed;
		private float _value;
		protected override void OnRefresh()
		{
			_value = 20;
			_isLoadSucceed = false;
			mloading.maxValue = 100;
			mloading.DOValue(_value, 0.5f).OnUpdate(() =>
			{
				mLoadingTxt.text = $"{(int)mloading.value}%";
			}).OnComplete(() =>
			{
				LevelController.Instance.SetUpdateCb(UpdateProgress);
				GameModule.UI.ShowUI<Panel_Game>();
			});
		}

		protected override void RegisterEvent()
		{
		}

		private void UpdateProgress(GameLoadingState state)
		{
			_value += 20;
			mloading.DOKill(true);
			if (state == GameLoadingState.CreatStack)
			{
				mloading.DOValue(100, 0.5f).SetEase(Ease.Linear)
					.OnUpdate(() =>
					{
						mLoadingTxt.text = $"{(int)mloading.value}%";
					}).OnComplete(() =>
					{
						_isLoadSucceed = true;
					});
			}
			else
			{
				mloading.DOValue(_value, 0.5f).SetEase(Ease.Linear)
					.OnUpdate(() =>
					{
						mLoadingTxt.text = (int)mloading.value + "%";
					});
			}
		}

		protected override void OnUpdate()
		{
			if (_isLoadSucceed && LevelController.Instance.GameLoadingState == GameLoadingState.None)
			{
				Close();
				GameEvent.Send(EventType.GamePanelShow);
			}
		}
		protected override void Close()
		{
			base.Close();
		}
	}
}
