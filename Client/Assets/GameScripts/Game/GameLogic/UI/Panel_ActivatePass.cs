using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.config;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI,PlayAnim = true)]
	public partial class Panel_ActivatePass : UIWindow
	{
		private CfgRecharge _cfgRecharge;
		private bool _isShowAd;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_cfgRecharge = (CfgRecharge)userDatas[0];
			}

			UpdateBuyButton(true);
			//RechargeController.IAPBtnShowLogEvent(rechargeId: _cfgRecharge.Id);
		}

		private void UpdateBuyButton(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mPriceTxt, _cfgRecharge, mAdicon, isLogEvent);
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
