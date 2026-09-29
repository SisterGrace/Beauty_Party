using TK_Engine;
using UnityEngine;
using Config.config;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Y_Base;
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
		private CfgPhotos _photo;

		public CfgPhotos GetPhoto()
		{
			if( _photo == null )
			{
                _photo = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(_data.DynamicPhotoId);
            }
			return _photo;
		}
		
		public void SetItemData(OneExcItemInfo data)
		{
			_data = data;
			if (!_data.IsDynamic)
			{
				_rechargeInfo = RechargeController.GetRechargeInfoByShop(_data.Id);
				_rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(_rechargeInfo.RewardId).Reward;
			}
            //更新显示状态
            UpdateView();
            Debug.Log($"[ExcItem] SetItemData Id={_data.Id} frame={Time.frameCount}");

        }

		private void UpdateView()
		{
			if (_data.IsDynamic)
			{
				UpdateDynamicView();
				return;
			}
			UpdateBuyButton(true);
			mGetted.SetActive(_data.IsPurchased);
			mPackPrice.gameObject.SetActive(!_data.IsPurchased);
			mLock.SetActive(!_data.IsPurchased);
			mRedNot.SetActive(_data.IsUpdated && !_data.IsPurchased);
			int AbType = UserController.Instance.GetCurUserType;
			int curPhotoId = AbType==1?_data.PhotoIds[1]:_data.PhotoIds[0];
			_itemPhoto.RequestLoad(curPhotoId);
		}

		private void UpdateDynamicView()
		{
			mGetted.SetActive(_data.IsPurchased);
			mPackPrice.gameObject.SetActive(!_data.IsPurchased);
			mLock.SetActive(!_data.IsPurchased);
			mRedNot.SetActive(false);
			// 动态模式显示广告按钮文本
			if (!_data.IsPurchased)
			{
				int currentTime = UserController.Instance.GetPhotoUnLockTime(GetPhoto().ID);
				int needTime = GetPhoto().ContentRating;
				if(currentTime<needTime)
				{
                    mPackPriceTxt.text = currentTime + "/" + needTime;
                    mAdicon.SetActive(true);
                }
				else
				{
					mPackPriceTxt.SetLanguage("btn_get");
                   mAdicon.SetActive(false);
                }
				
				
				
			}
			_itemPhoto.RequestLoad(_data.DynamicPhotoId);
		}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mPackPriceTxt, _rechargeInfo, mAdicon, isLogEvent);
		}
		
		private void OnClickBuy(GameObject obj)
		{
			if (_data.IsPurchased) return;

			if (_data.IsDynamic)
			{
				OnClickBuyDynamic();
				return;
			}

			RechargeController.Instance.HandleBuyOrAd(_rechargeInfo, transform.position,
				() => { UpdateBuyButton(); },
				OnBuySuccess);
		}

		/// <summary>
		/// 动态模式下点击购买（看广告获得）
		/// </summary>
		private void OnClickBuyDynamic()
		{
			var photo = GetPhoto();
			int adNeedTime = photo.ContentRating;
			int currentTime = UserController.Instance.GetPhotoUnLockTime(photo.ID);
			if(currentTime<adNeedTime)
			{
                SDKManager.Instance.Button_ShowRV("RV_ShopDynamic", (success) =>
                {
                    if (success)
                    {
						currentTime++;
                        UserController.Instance.SetPhotoUnLockTime(photo.ID, currentTime);
						UpdateDynamicView();
                    }
                });
            }
			else
			{
                //?
                UserController.Instance.SetPhotoUnLockTime(photo.ID, 0);
				OnDynamicBuySuccess();
            }
           
		}
		

		/// <summary>
		/// 动态模式购买成功
		/// </summary>
		private void OnDynamicBuySuccess()
		{
            //存储购买记录和当前配置
            UserController.Instance.AddDynamicPhotoObtained(_data.DynamicPhotoId);
            GameModule.UI.AddOrderPanel<Panel_Collectphotos>(1, true, GetPhoto().ID,1);
            //更新显示状态
            _data.IsPurchased = true;
            UpdateDynamicView();
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
		public bool IsDynamic;//是否为动态生成的照片项
		public int DynamicPhotoId;//动态模式下的照片ID
		public OneExcItemInfo(int id)
		{
			Id = id;
			GiftId = 0;
			IsPurchased = false;
			IsUpdated = false;
			PhotoIds = new List<int>();
			IsDynamic = false;
			DynamicPhotoId = 0;
		}
		public OneExcItemInfo(int id, int giftId, bool isPurchased, bool isUpdated, List<int> photoIds)
		{
			Id = id;
			GiftId = giftId;
			IsPurchased = isPurchased;
			IsUpdated = isUpdated;
			PhotoIds = photoIds;
			IsDynamic = false;
			DynamicPhotoId = 0;
		}
		/// <summary>
		/// 动态模式构造函数
		/// </summary>
		public OneExcItemInfo(int dynamicPhotoId, bool isPurchased)
		{
			Id = 0;
			GiftId = 0;
			IsPurchased = isPurchased;
			IsUpdated = false;
			PhotoIds = new List<int> { dynamicPhotoId };
			IsDynamic = true;
			DynamicPhotoId = dynamicPhotoId;
		}
	}
}
