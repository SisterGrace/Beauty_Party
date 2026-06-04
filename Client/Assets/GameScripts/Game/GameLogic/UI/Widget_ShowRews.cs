using TK_Engine;
using UnityEngine;
using Config.common;
using System.Collections;
using System.Collections.Generic;
using GameBase;
using UnityEngine.UI;
using TMPro;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_ShowRews : UIWidget
	{
		private List<OneRewardItem> _rewardItems = new List<OneRewardItem>();
		public void Open(List<TBRewardData> Propsdatas,TBRewardData oneData,bool isSetSizeDelta = false)
		{
			if (Propsdatas != null)
			{
				rectTransform.sizeDelta =isSetSizeDelta? new Vector2(265f, 300f) : new Vector2(225f, 300f);
				mGroup1.SetActive(Propsdatas.Count > 0);
				mGroup2.SetActive(Propsdatas.Count > 2);
				mLife.SetActive(false);
				for (int i = 0; i < Propsdatas.Count; i++)
				{
					var item = _rewardItems[i];
					item.Icon.gameObject.Show();
					item.Icon.SetSprite(PropsController.GetRewardIcon(Propsdatas[i]));
					item.Count.text = PropsController.GetRewardCount(Propsdatas[i]);
				}
			}
			else
			{
				mGroup1.SetActive(false);
				mGroup2.SetActive(false);
				rectTransform.sizeDelta =isSetSizeDelta? new Vector2(265f, 300f) : new Vector2(120f, 300f);
				if (oneData.Type == ERewardType.Currency && oneData.ID == 3)
				{
					mLife.SetActive(true);
					mNoAd.SetActive(false);
					mLifeCount.text = PropsController.GetRewardCount(oneData);
				}else if (oneData.Type == ERewardType.NoAd)
				{
					mLife.SetActive(false);
					mNoAd.SetActive(true);
				
				}
			}
		}

		public void Hide()
		{
		}
	}

	public struct OneRewardItem
	{
		public Image Icon;
		public TextMeshProUGUI Count;

		public OneRewardItem(Image icon, TextMeshProUGUI count)
		{
			Icon = icon;
			Count = count;
		}
	}
}
