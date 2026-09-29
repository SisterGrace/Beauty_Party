using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI, PlayAnim = true)]
	public partial class Panel_StarterPack : UIWindow
	{
		private CfgRecharge _rechargeItem;
		private ActityData _activityData;

		//照片奖励配置
		private List<TBRewardData> _rewardList1;
		//其它奖励配置
		private List<TBRewardData> _rewardList2;
		private List<TBRewardData> _allRewardList;
		private Item_PhotoLoad _itemPhoto;
		private bool _isShowAd;
		//是否开启倒计时
		private bool _isOpenCountDown = false;
		 private float _timer = 1;
	protected override void OnRefresh()
	{
		// ✅ 标记当前页面为活跃页面
		ImgUtil.SetActivePage(this.GetType().Name);
		
		if (userDatas.Length > 0)
		{
			_activityData = (ActityData)userDatas[0];
		}
		_isOpenCountDown = _activityData.EndTime > 0;
		if(!_isOpenCountDown)
		{
			mTimeCountdown.SetLanguage("uitips_startgift_time");
		}
		UserController.Instance.SetActivityIsEnter(ActivityType.NewPlayerPack);
		var rechargeId = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.NewPlayerPack).RechargeId;
		_rechargeItem = RechargeController.GetRechargeInfo(rechargeId);
		UpdateBuyButton(true);
		var newPack = ConfigLoader.Instance.Tables.TbCfgReward.Get(_rechargeItem.RewardId).Reward;
		_allRewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(newPack[0].ID).Reward;
		_rewardList1 = _allRewardList.
			FindAll(x => x.Type == ERewardType.Photo);
		_rewardList2 = _allRewardList.
			FindAll(x => x.Type != ERewardType.Photo);
		CreateRewardItem();
		//RechargeController.IAPBtnShowLogEvent(rechargeId: _rechargeItem.Id);
		LoadStarterPackPhoto();
	}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mPriceTxt, _rechargeItem, mAdicon, isLogEvent);
		}

		protected override void RegisterEvent()
		{
		}
		 protected override void OnUpdate()
        {
            if (_isOpenCountDown)
            {
                _timer += Time.deltaTime;
                if (_timer >= 1)
                {
                    var t = _activityData.EndTime - TimeUtil.GetTimeStamp();
                    if (t <= 0)
                    {
                        UserController.Instance.CheckOneActivityTime(ActivityType.NewPlayerPack);
                        Close();
						_isOpenCountDown = false;
						mTimeCountdown.SetLanguage("uitips_startgift_time");
                        return;
                    }

                    mTimeCountdown.text = TimeUtil.SecondToTime2(t);
                }
            }
        }
		private void CreateRewardItem()
		{
			//这里固定只显示两个奖励，第一个是照片类型，第二个是其它货币或者道具类型
			for (int i = 0; i < 2; i++)
			{
				Widget_OneStarterPackItem item = CreateWidgetByType<Widget_OneStarterPackItem>(mRewNodes.transform);
				TBRewardData cfg = i == 0 ? _rewardList1[0] : _rewardList2[0];
				int count = i == 0 ? _rewardList1.Count : (int)cfg.Count;
				item.Open(cfg, count);
			}

		}

		private void LoadStarterPackPhoto()
		{
			int AbType = UserController.Instance.GetCurUserType;
			if (_rewardList1[0].Type == ERewardType.Photo)
			{
				int picId = AbType == 1 ? _rewardList1[0].OtherID : _rewardList1[0].ID;
				_itemPhoto.LoadUIImg(picId);
			}
			else if (_rewardList1[0].Type == ERewardType.PhotoGroup)
			{
				var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(_rewardList1[0].ID);
				var allPhotoIds = AbType == 1 ? group.BPhotoRews : group.APhotoRews;
				_itemPhoto.LoadUIImg(allPhotoIds[0]);
			}
		}
	protected override void Close()
	{
		// ✅ 清除活跃页面标记
		ImgUtil.ClearActivePage(this.GetType().Name);
		
		base.Close();
	}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameModule.UI.RemoveOrderPanel<Panel_StarterPack>(true);
		}
	}
}
