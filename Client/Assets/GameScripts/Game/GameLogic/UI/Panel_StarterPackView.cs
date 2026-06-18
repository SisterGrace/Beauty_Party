using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
using Config.common;
namespace GameLogic
{
	public partial class Panel_StarterPack
	{

		protected UIButton mmask;
		protected UIButton mGetBuy;
		protected UIButton mClose;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected GameObject mAdicon;
		protected Transform mloadIcon;
		protected Transform mRewNodes;
		protected TextMeshProUGUI mTimeCountdown;
		protected TextMeshProUGUI mPriceTxt;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mGetBuy = FindChildComponent<UIButton>("AnimNode/Btn_GetBuy");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mPhotoNode = FindChild("AnimNode/Bg/Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("AnimNode/Bg/Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mAdicon = FindChild("AnimNode/Btn_GetBuy/Obj_Adicon").gameObject;
			mloadIcon = FindChild("AnimNode/Bg/Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mRewNodes = FindChild("AnimNode/Trans_RewNodes");
			mTimeCountdown = FindChildComponent<TextMeshProUGUI>("AnimNode/Time_bg/TMP_TimeCountdown");
			mPriceTxt = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_GetBuy/TMP_PriceTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_itemPhoto = mPhotoNode.GetComponent<Item_PhotoLoad>();
			mClose.AddListener(ClickClose);
			mGetBuy.AddListener(ClickBuy);
		}

		private void ClickBuy(GameObject obj)
		{
			RechargeController.Instance.HandleBuyOrAd(_rechargeItem, transform.position,
				() => { UpdateBuyButton(); },
				(productID) =>
				{
					Log.Info("购买新手礼包成功");
					PropsController.Instance.ShowRewardCollectPanels(_allRewardList,false);
					UserController.Instance.GetBugStarterPack = true;
					ActivityController.Instance.RemoveActivity(ActivityType.NewPlayerPack);
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
