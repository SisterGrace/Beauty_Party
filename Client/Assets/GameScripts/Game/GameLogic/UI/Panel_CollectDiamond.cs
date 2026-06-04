using TK_Engine;
using UnityEngine;
using DG.Tweening;
using Config.common;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_CollectDiamond : UIWindow
	{
		//动画参数
		private SucceedAnimConfig _animConfig = SucceedAnimConfig.Default;
		private bool _isAnim;
		private int _diamondCount;
		private int _adMultiple;

		private bool _isMultiple;

		protected override void OnRefresh()
		{
			//这个界面为打开之前，就已经把奖励数据写入了，只是此时写入的奖励是默认的
			_isAnim = false;
			UpdateInfo();
			InitAnimationState();
			
		}

		private void UpdateInfo()
		{
			_isMultiple = true;
			if (userDatas.Length > 0)
			{
				_diamondCount = (int)userDatas[0];
				if (userDatas.Length > 1)
				{
					_isMultiple = (bool)userDatas[1];
				}

			}
			mCurDiaCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
			mnorDiaCount.text = $"<sprite name=\"icon_zuanshi\">+{_diamondCount}";
			mAdGet.gameObject.SetActive(_isMultiple);
			if (_isMultiple)
			{
				_adMultiple = ConfigLoader.Constant.AdMultiple;
				mAdMultiple.text = $"<sprite name=\"icon_zuanshi\">+{_adMultiple * _diamondCount}";
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_Daily.ToString());
			}

		}

		private void InitAnimationState()
		{
			mDiaTitle.localScale = Vector3.zero;
			mDisIcon.localScale = Vector3.zero;
			mDiaBtns.localScale = Vector3.zero;
			_isAnim = true;
			Sequence sequence = DOTween.Sequence();
			//依次弹出三个节点
			sequence.Append(mDiaTitle.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.AppendInterval(_animConfig.DiamondAnimDelay);
			sequence.Append(mDisIcon.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.AppendInterval(_animConfig.DiamondAnimDelay);
			sequence.Append(mDiaBtns.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.OnComplete(() =>
			{
				_isAnim = false;
			});
		}

		private void SendAdReward()
		{
			// 使用 PropsController 的方法发送广告倍数奖励
			// 发送（当前倍数-1）的数量，因为这个界面显示之前就已经发送过一次基础奖励了
			PropsController.Instance.SendAdMultipleDiamondReward(_diamondCount, _adMultiple);
		}

		protected override void RegisterEvent()
		{
		}

		protected override void Close()
		{
			base.Close();
			// 面板关闭时刷新钻石UI（数据在面板打开前已写入）
			GameEvent.Send(EventType.RefreshCurrency, 0);
			// 从 OrderPanel 队列中移除，并触发下一个面板显示
			GameModule.UI.RemoveOrderPanel<Panel_CollectDiamond>();
		}
	}
}
