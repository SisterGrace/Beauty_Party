using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using GameBase;
using UnityEngine.UI;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_DayCheckItem : UIWidget, IListDataItem<DayCheckItemInfo>
	{
		//TODO:因为签到item的样式不同
		private DayCheckItemInfo _data;
		private Item_PhotoLoad _itemPhotoLoad;
		public void SetItemData(DayCheckItemInfo data)
		{
			_data = data;
			SetDifferentStyles();
			CreateRewItem();
			RefreshRedPoint();
		}
		private void RefreshRedPoint()
		{
			mRedNot.SetActive(_data.State == DayCheckState.Available);
		}
		private void SetDifferentStyles()
		{
			mItemDay.SetLanguage("uitps_days", $"{_data.Id + 1}");
			//item选中的bg
			string availableBg = _data.IsNor ? "bg_qrqd_02" : "bg_qrqd_04";
			//item默认的bg
			string curBg = _data.IsNor ? "bg_qrqd_01" : "bg_qrqd_03";
			//大一点item默认bg
			if (!_data.IsNor)
			{
				rectTransform.sizeDelta = new Vector2(342f, 633f);
				mBg.rectTransform.sizeDelta = new Vector2(342f, 633f);
				mmask.GetComponent<RectTransform>().sizeDelta = new Vector2(342f, 633f);
				mPhotoNode.transform.localScale = Vector3.one * 0.5f;
			}
			mmask.SetActive(_data.State == DayCheckState.Claimed);
			mmask.GetComponent<Image>().sprite = GameModule.Resource.LoadAsset<Sprite>(curBg);
			switch (_data.State)
			{
				case DayCheckState.Claimed://已领取
					mBg.sprite = GameModule.Resource.LoadAsset<Sprite>(curBg);
					break;
				case DayCheckState.Available://可领取
					mBg.sprite = GameModule.Resource.LoadAsset<Sprite>(availableBg);
					break;
				case DayCheckState.Locked://未解锁
					mBg.sprite = GameModule.Resource.LoadAsset<Sprite>(curBg);
					break;
			}
		}
		//创建奖励item
		private void CreateRewItem()
		{
			var dayCfg = ConfigLoader.Instance.Tables.TbCfgDayCheck.Get(_data.Id);
			var rewCfg = ConfigLoader.Instance.Tables.TbCfgReward.Get(dayCfg.RewardId).Reward[0];
			mnorRew.SetActive(rewCfg.Type != ERewardType.Photo);
			mPhotoNode.SetActive(rewCfg.Type == ERewardType.Photo);
			if (rewCfg.Type == ERewardType.Photo)
			{
				//判断当前AB逻辑，显示对应的
				int AbType = UserController.Instance.GetCurUserType;
				int rewId = AbType == 1 ? rewCfg.OtherID : rewCfg.ID;
				_itemPhotoLoad.LoadPhoto(rewId, 0);
			}
			else
			{
				string iconName = PropsController.GetRewardIcon(rewCfg);
				mRewIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(iconName);
				mnorRewCount.text = PropsController.GetRewardCount(rewCfg);
				mRewIcon.SetNativeSize();
			}
		}

	}
	public struct DayCheckItemInfo
	{
		public int Id;
		public DayCheckState State;
		//是否是特殊奖励
		public bool IsNor;
		public DayCheckItemInfo(int id, DayCheckState state, bool isNor)
		{
			Id = id;
			State = state;
			IsNor = isNor;
		}
	}
}
