using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_ActivatePass
	{

		protected UIButton mmask;
		protected UIButton mActivate;
		protected UIButton mClose;
		protected TextMeshProUGUI mPriceTxt;
		protected GameObject mAdicon;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mActivate = FindChildComponent<UIButton>("AnimNode/Btn_Activate");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mPriceTxt = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_Activate/TMP_PriceTxt");
			mAdicon = FindChild("AnimNode/Btn_Activate/Obj_Adicon").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mActivate.AddListener(ClickActivate);
			mmask.AddListener(ClickClose);
		}

		private void ClickActivate(GameObject obj)
		{
			RechargeController.Instance.HandleBuyOrAd(_cfgRecharge, transform.position,
				() => { UpdateBuyButton(); },
				(productId) =>
				{
					GameEvent.Send(EventType.OpenDreamPass, _cfgRecharge);
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
