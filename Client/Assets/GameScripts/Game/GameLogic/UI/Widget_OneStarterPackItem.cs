using TK_Engine;
using UnityEngine;
using Config.common;
using Config.config;
using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneStarterPackItem : UIWidget
	{
		private Item_PhotoLoad _itemPhoto;
		private int abType;
		public void Open(TBRewardData cfg, int amount)
		{
			mrewardIcon.gameObject.SetActive(cfg.Type != ERewardType.Photo && cfg.Type != ERewardType.PhotoGroup);
			mPhotoNode.SetActive(cfg.Type == ERewardType.Photo || cfg.Type == ERewardType.PhotoGroup);
			abType = UserController.Instance.GetCurUserType;
			if (cfg.Type == ERewardType.Photo)
			{
				int rewId = abType == 1 ? cfg.OtherID : cfg.ID;
				_itemPhoto.LoadUIImg(rewId);
			}
			else if (cfg.Type == ERewardType.PhotoGroup)
			{
				var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(cfg.ID);
				var allPhotoIds = abType == 1 ? group.BPhotoRews : group.APhotoRews;
				_itemPhoto.LoadUIImg(allPhotoIds[0]);
			}
			else
			{
				string iconName = PropsController.GetRewardIcon(cfg);
				mrewardIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(iconName);
			}

			mrewardCount.text = $"X{amount}";
		}

		public void Hide()
		{
		}
	}
}
