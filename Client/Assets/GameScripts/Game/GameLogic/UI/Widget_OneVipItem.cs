using TK_Engine;
using UnityEngine;
using GameBase;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using UnityEngine.UI;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneVipItem : UIWidget, IListDataItem<VIPItemData>
	{
		private VIPItemData _data;
		private UIListWidget<Widget_OneVipInfoItem, VIPInfoData> _vipInfoList;
		private CfgRecharge _rechargeInfo;
		private int _rewId;
		private VerticalLayoutGroup _vipInfoListLayout;
		private int _infoItemSpacing = 0;
		private bool _isCountDown = false;
		private float _timer = 1;

		private bool _isShowAd;
		private int _curGetVIPLevel => UserController.Instance.GetVIPLevel;

		public void SetItemData(VIPItemData data)
		{
			_data = data;
			_rechargeInfo = RechargeController.GetRechargeInfo(_data.RechargeId);
			_rewId = _rechargeInfo.RewardId;
			SetItemStyle();
			CreatInfoItemList();
			_infoItemSpacing = data.VipInfoTxts.Count > 3 ? data.VipInfoTxts.Count > 4 ? 0 : -40 : -120;
			_vipInfoListLayout.spacing = _infoItemSpacing;
		
			//如果等于当前VIP开启倒计时
			_isCountDown = data.Id + 1 == _curGetVIPLevel;
			bool isShowBtn = _curGetVIPLevel < 3;
			mBuyVip.gameObject.SetActive(isShowBtn);
			mGetTips.SetActive(!isShowBtn);
			if (isShowBtn)
			{
				UpdateBuyButton(true);
			}
			mTimeNode.gameObject.SetActive(_isCountDown);
		}
		//设置item样式
		private void SetItemStyle()
		{
			mBg.sprite = GameModule.Resource.LoadAsset<Sprite>(_data.Bg);
			mVipIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(_data.Icon);
			mTimeNode.sprite = GameModule.Resource.LoadAsset<Sprite>(_data.TimeNode);
			mItemName.SetLanguage(_data.Name);

			mCurVipTime.text = _data.Days > 0 ? TimeUtil.SecondToTime2(_data.Days * 86400, formatWithLeadingZero: false) : "∞";
		}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mvipPrice, _rechargeInfo, mAdicon, isLogEvent);
		}
		protected override void OnUpdate()
		{
			if (_isCountDown)
			{
				_timer += Time.deltaTime;
				if (_timer >= 1)
				{
					var t = UserController.Instance.GetVIPEndTime - TimeUtil.GetTimeStamp();
					if (t < 0)
					{
						_isCountDown = false;
						mTimeNode.gameObject.SetActive(_isCountDown);
						return;
					}
					mCurVipTime.text = TimeUtil.SecondToTime2(t);
				}
			}
		}
		private void CreatInfoItemList()
		{
			if (_vipInfoList == null)
			{
				_vipInfoList = CreateWidget<UIListWidget<Widget_OneVipInfoItem, VIPInfoData>>(mAllVipInfos.gameObject);
			}
			List<VIPInfoData> vipInfoList = new List<VIPInfoData>();
			string infoBg = "bg_vip_04";
			if (_data.Id == 1)
			{
				infoBg = "bg_vip_05";
			}
			else if (_data.Id == 2)
			{
				infoBg = "bg_vip_06";
			}
			for (int i = 0; i < _data.VipInfoTxts.Count; i++)
			{
				var vipInfoData = new VIPInfoData(infoBg, _data.VipInfoTxts[i].InfoIcon, _data.VipInfoTxts[i].InfoTxt);
				vipInfoList.Add(vipInfoData);
			}
			if (vipInfoList.Count == 0) return;
			_vipInfoList.SetDatas(vipInfoList);
		}
		private void SendVipReward()
		{
			if (_rewId > 0)
			{
				//发送奖励
				var rewardCfg = ConfigLoader.Instance.Tables.TbCfgReward.Get(_rewId).Reward;
				PropsController.Instance.ShowRewardCollectPanels(rewardCfg,false);
			}
			//关闭VIP界面
			GameModule.UI.CloseUI<Panel_VIP>();
		}
		public void Hide()
		{
		}
	}
	public struct VIPItemData
	{
		public int Id;
		public string Name;
		public string Bg;
		public string Icon;
		public string TimeNode;
		public int RechargeId;
		public int Days;
		public List<VipInfoTxt> VipInfoTxts;
		public VIPItemData(int id, string name, string bg, string icon, string timeNode, int rechargeId, int days, List<VipInfoTxt> vipInfoTxts)
		{
			Id = id;
			Name = name;
			Bg = bg;
			Icon = icon;
			TimeNode = timeNode;
			RechargeId = rechargeId;
			Days = days;
			VipInfoTxts = vipInfoTxts;
		}
	}
}
