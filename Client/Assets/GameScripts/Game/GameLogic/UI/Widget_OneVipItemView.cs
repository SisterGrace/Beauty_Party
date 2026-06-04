using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneVipItem
	{

		protected Image mBg;
		protected Image mVipIcon;
		protected Image mTimeNode;
		protected TextMeshProUGUI mItemName;
		protected TextMeshProUGUI mCurVipTime;
		protected TextMeshProUGUI mvipPrice;
		protected Transform mAllVipInfos;
		protected UIButton mBuyVip;
		protected GameObject mbtnMask;
		protected GameObject mAdicon;
		protected GameObject mGetTips;

		protected override void ScriptGenerator()
		{
			mBg = FindChildComponent<Image>("Img_Bg");
			mVipIcon = FindChildComponent<Image>("Img_VipIcon");
			mTimeNode = FindChildComponent<Image>("Img_TimeNode");
			mItemName = FindChildComponent<TextMeshProUGUI>("Img_Bg/TMP_ItemName");
			mCurVipTime = FindChildComponent<TextMeshProUGUI>("Img_TimeNode/TMP_CurVipTime");
			mvipPrice = FindChildComponent<TextMeshProUGUI>("Btn_BuyVip/TMP_vipPrice");
			mAllVipInfos = FindChild("Trans_AllVipInfos");
			mBuyVip = FindChildComponent<UIButton>("Btn_BuyVip");
			mbtnMask = FindChild("Btn_BuyVip/Obj_btnMask").gameObject;
			mAdicon = FindChild("Btn_BuyVip/Obj_Adicon").gameObject;
			mGetTips = FindChild("Obj_GetTips").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mBuyVip.AddListener(ClickBuyVipBtn);
			_vipInfoListLayout = mAllVipInfos.GetComponent<VerticalLayoutGroup>();
		}
		private void ClickBuyVipBtn(GameObject obj)
		{
			RechargeController.Instance.HandleBuyOrAd(_rechargeInfo, transform.position,
				() => { UpdateBuyButton(); },
				(productID) =>
				{
					SendVipReward();
				});
		}
		#endregion
	}
}
