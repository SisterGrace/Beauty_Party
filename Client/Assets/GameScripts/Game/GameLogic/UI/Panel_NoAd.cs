using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using DG.Tweening;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI,PlayAnim = true)]
	public partial class Panel_NoAd : UIWindow
	{
		private CfgRecharge _rechargeItem;
		private bool _isShowAd;
		protected override void OnRefresh()
		{
			var rechargeId = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.Adfree).RechargeId;
			_rechargeItem = RechargeController.GetRechargeInfo(rechargeId);
			UpdateBuyButton(true);
			//RechargeController.IAPBtnShowLogEvent(rechargeId: _rechargeItem.Id);
		}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mPriceTxt, _rechargeItem, mAdicon, isLogEvent);
		}

		protected override void RegisterEvent()
		{
		}

		protected override void Close()
		{
			base.Close();
		}
	}
}
