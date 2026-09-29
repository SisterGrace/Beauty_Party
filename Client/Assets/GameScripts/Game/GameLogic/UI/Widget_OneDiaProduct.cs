using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using Y_Base;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneDiaProduct : UIWidget, IListDataItem<OneDiaProductInfo>
	{
		private CfgRecharge _mCfgRecharge;
		private List<TBRewardData> _mRewList;

		private bool _isDisCountDown;
		private float timer = 1;
		private bool _isShowAd;
		public void SetItemData(OneDiaProductInfo data)
		{
			_mCfgRecharge = RechargeController.GetRechargeInfoByShop(data.Id);
			_mRewList = data.RewardData;
			InitData(data);
            Debug.Log($"[OneDia] SetItemData Id={data.Id} frame={Time.frameCount}");
        }
		private void InitData(OneDiaProductInfo data)
		{
			//mDiaCount.text = data.Count.ToString();
			bool isAd = _mCfgRecharge.Price == 0;
			madGet.SetActive(isAd);
			mPriceGet.SetActive(!isAd);
			Vector2 sizeDelta = isAd ? new Vector2(923f, 205f) : new Vector2(447f, 280f);
			rectTransform.sizeDelta = sizeDelta;
			if (!isAd)
			{
				UpdateBuyButton(true);
				mPriceGetCount.text = PropsController.GetRewardCount(_mRewList[0]);
				mCurDiaIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(diaIconName(data.Id));
				mCurDiaIcon.SetNativeSize();
			}
			else
			{
				_isDisCountDown = UserController.Instance.CheckFreeGetGoldTime();
				mAdGetCount.text = PropsController.GetRewardCount(_mRewList[0]);
				mAdMask.SetActive(_isDisCountDown);
				mRedNot.SetActive(!_isDisCountDown);
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_Coins.ToString());
			}
		}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mPackPriceTxt, _mCfgRecharge, mAdicon, isLogEvent);
		}
		protected override void OnUpdate()
		{
			if (_isDisCountDown)
			{
				timer += Time.deltaTime;
				if (timer > 1)
				{
					var t = UserController.Instance.GetFreeGetGoldTime - TimeUtil.GetTimeStamp();
					if (t <= 0)
					{
						_isDisCountDown = false;
						mAdBtnTxt.text = "GET";
						UserController.Instance.GetFreeGetGoldTime = 0;
						mAdMask.Hide();
						mRedNot.SetActive(!_isDisCountDown);
						return;
					}
					mAdBtnTxt.text = TimeUtil.SecondToTime((long)t);
				}
			}
		}
		private void SendRew()
		{
			//先写入奖励
			PropsController.Instance.ShowRewardCollectPanels(_mRewList,false);
			//如果是免费的开启广告倒计时，
			if (_mCfgRecharge.Price == 0 && !_isDisCountDown)
			{
				UserController.Instance.GetFreeGetGoldTime = TimeUtil.GetTimeStamp() + ConfigLoader.Constant.DIaCountdownTime;
				_isDisCountDown = true;
				mRedNot.SetActive(!_isDisCountDown);
				mAdMask.Show();
			}
		}
		private string diaIconName(int id)
		{

			switch (id)
			{
				case 8:
					return "icon_sc_zs01";
				case 9:
					//return "icon_gqjs_zs";
					return "icon_sc_zs01";
				case 10:
					return "icon_sc_zs02";
				case 11:
					return "icon_sc_zs03";
				case 12:
					return "icon_sc_zs04";
				default:
					return "icon_sc_zs01";
			}
		}
	}
	public struct OneDiaProductInfo
	{
		public int Id;
		public string Price;
		public List<TBRewardData> RewardData;
		public OneDiaProductInfo(int id, string price, List<TBRewardData> rewardData)
		{
			Id = id;
			Price = price;
			RewardData = rewardData;
		}
	}
}
