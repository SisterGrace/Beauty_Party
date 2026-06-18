using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using DG.Tweening;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_RewardItem : UIWidget
	{
		public void Open(OneRewardData rewardData)
		{
			string iconName = PropsController.GetRewardIcon(rewardData);
			mReIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(iconName);
			mReIcon.SetNativeSize();
			bool isCurrency = rewardData.Type == ERewardType.Currency;
			bool isAddTime = rewardData.flyType == itemFlyType.AddTimeFly;
			if (isAddTime)
			{
				mFrame.SetActive(!isCurrency);
				mReTxt.gameObject.SetActive(isCurrency);
				//string timeTxt = $"∞{TimeUtil.GetSecToMin(rewardData.Count)}m";
				string timeTxt = PropsController.GetRewardCount(rewardData,true);
				mReTxt.text = isCurrency?timeTxt:mtimeTxt.text = timeTxt;
			}
			else
			{
				mFrame.Hide();
				mReTxt.gameObject.Show();
				mReTxt.text = PropsController.GetRewardCount(rewardData);
				if (isCurrency && rewardData.Id == 1)
				{
					mReIcon.rectTransform.localScale = Vector3.one * 0.9f;
				}
				
			}
		}

		public void ShowAnim(Vector3 startPos, Vector2 endPos, float duration)
		{
			transform.position = startPos;
			transform.localScale = Vector3.zero;
			Sequence seq = DOTween.Sequence();
			seq.Insert(0, transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBounce));
			seq.Insert(0, rectTransform.DOAnchorPos(endPos, duration).SetEase(Ease.OutBack));
		}
		public void Hide()
		{
		}
	}
}
