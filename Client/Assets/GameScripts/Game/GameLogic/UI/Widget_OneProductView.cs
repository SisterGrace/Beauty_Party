using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneProduct
	{

		protected GameObject mPackObj;
		protected GameObject mAdicon;
		protected GameObject mLoadingNode;
		protected Image mpackBg;
		protected Image mcurPropIcon;
		protected UIButton mPackPrice;
		protected UIButton mPhotoNode;
		protected TextMeshProUGUI mPackPriceTxt;
		protected TextMeshProUGUI mPackNameTxt;
		protected TextMeshProUGUI mpropCount;
		protected Transform mloadIcon;
		protected Transform mTips;

		protected override void ScriptGenerator()
		{
			mPackObj = FindChild("Obj_PackObj").gameObject;
			mAdicon = FindChild("Obj_PackObj/Btn_PackPrice/Hor/Obj_Adicon").gameObject;
			mLoadingNode = FindChild("Obj_PackObj/Frame/Btn_PhotoNode/Obj_LoadingNode").gameObject;
			mpackBg = FindChildComponent<Image>("Obj_PackObj/Img_packBg");
			mcurPropIcon = FindChildComponent<Image>("Obj_PackObj/Frame/PropNodeBg/Img_curPropIcon");
			mPackPrice = FindChildComponent<UIButton>("Obj_PackObj/Btn_PackPrice");
			mPhotoNode = FindChildComponent<UIButton>("Obj_PackObj/Frame/Btn_PhotoNode");
			mPackPriceTxt = FindChildComponent<TextMeshProUGUI>("Obj_PackObj/Btn_PackPrice/Hor/TMP_PackPriceTxt");
			mPackNameTxt = FindChildComponent<TextMeshProUGUI>("Obj_PackObj/TMP_PackNameTxt");
			mpropCount = FindChildComponent<TextMeshProUGUI>("Obj_PackObj/Frame/PropNodeBg/TMP_propCount");
			mloadIcon = FindChild("Obj_PackObj/Frame/Btn_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mTips = FindChild("Obj_PackObj/Trans_Tips");
			InitCodeBlock();
		}

		#region 可编辑代码块

		private void InitCodeBlock()
		{
			mPackPrice.AddListener(ClickPackPriceBtn);
			_itemPhoto = mPhotoNode.transform.GetComponent<Item_PhotoLoad>();
			mPhotoNode.AddListener(ClickPhotoBtn);
		}

		private void ClickPackPriceBtn(GameObject obj)
		{
			RechargeController.Instance.HandleBuyOrAd(_mCfgRecharge, transform.position,
				() => { UpdateBuyButton(); },
				(productID) =>
				{
					BuyOneProduct();
				});
		}
		private void ClickPhotoBtn(GameObject obj)
		{
			showPics();
		}

		#endregion
	}
}
