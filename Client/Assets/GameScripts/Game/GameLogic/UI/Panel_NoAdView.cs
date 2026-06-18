using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_NoAd
	{

		protected UIButton mmask;
		protected UIButton mGetBuy;
		protected UIButton mClose;
		protected TextMeshProUGUI mPriceTxt;
		protected GameObject mAdicon;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mGetBuy = FindChildComponent<UIButton>("AnimNode/Btn_GetBuy");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mPriceTxt = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_GetBuy/TMP_PriceTxt");
			mAdicon = FindChild("AnimNode/Btn_GetBuy/Obj_Adicon").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mGetBuy.AddListener(ClickGetBuy);
		}

		private void ClickGetBuy(GameObject obj)
		{
			RechargeController.Instance.HandleBuyOrAd(_rechargeItem, Vector3.zero,
				() => { UpdateBuyButton(); },
				(productId) =>
				{
					PropsController.Instance.SendRewardData(_rechargeItem.RewardId);
					Close();
				});
		}
		private void ClickClose(GameObject obj)
		{
			Close();
		}
		#endregion
	}
}
