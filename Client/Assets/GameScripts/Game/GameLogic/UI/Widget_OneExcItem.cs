using TK_Engine;
using UnityEngine;
using Config.config;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using GameBase;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneExcItem : UIWidget , IListDataItem<OneExcItemInfo>	
	{
		private OneExcItemInfo _data;
		private CfgRecharge _rechargeInfo;
		private Item_PhotoLoad _itemPhoto;
		private List<TBRewardData> _rewardList;
		private bool _isShowAd;
		
		public void SetItemData(OneExcItemInfo data)
		{
			_data = data;
			_rechargeInfo = RechargeController.GetRechargeInfoByShop(_data.Id);
			_rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(_rechargeInfo.RewardId).Reward;
			//更新显示状态
			UpdateView();

		}

		private void UpdateView()
		{
			UpdateBuyButton(true);
			mGetted.SetActive(_data.IsPurchased);
			mPackPrice.gameObject.SetActive(!_data.IsPurchased);
			mLock.SetActive(!_data.IsPurchased);
			mRedNot.SetActive(_data.IsUpdated && !_data.IsPurchased);
			int AbType = UserController.Instance.GetCurUserType;
			int curPhotoId = AbType==1?_data.PhotoIds[1]:_data.PhotoIds[0];
			_itemPhoto.LoadUIImg(curPhotoId);
		}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mPackPriceTxt, _rechargeInfo, mAdicon, isLogEvent);
		}
		
		private void OnClickBuy(GameObject obj)
		{
			if (_data.IsPurchased) return;

			RechargeController.Instance.HandleBuyOrAd(_rechargeInfo, transform.position,
				() => { UpdateBuyButton(); },
				OnBuySuccess);
		}
		
		/// <summary>
		/// 购买成功回调
		/// </summary>
		private void OnBuySuccess(string pId)
		{
			//存储购买记录和当前配置
			UserController.Instance.AddShopPhotoWithConfig(_data.GiftId, _data.PhotoIds);
			
			//发送奖励
			PropsController.Instance.ShowRewardCollectPanels(_rewardList,false);
			
			//更新显示状态
			_data.IsPurchased = true;
			UpdateView();
		}
	}
	public struct OneExcItemInfo
	{
		public int Id;
		public int GiftId;//Giftid
		public bool IsPurchased;//是否已购买
		public bool IsUpdated;//配置是否有更新（热更后照片变化）
		public List<int> PhotoIds;//当前配置的照片ID列表（ID和OtherID）
		public OneExcItemInfo(int id)
		{
			Id = id;
			GiftId = 0;
			IsPurchased = false;
			IsUpdated = false;
			PhotoIds = new List<int>();
		}
		public OneExcItemInfo(int id, int giftId, bool isPurchased, bool isUpdated, List<int> photoIds)
		{
			Id = id;
			GiftId = giftId;
			IsPurchased = isPurchased;
			IsUpdated = isUpdated;
			PhotoIds = photoIds;
		}
	}
}
