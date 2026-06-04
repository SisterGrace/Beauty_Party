using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneDiaProduct
	{

		protected GameObject madGet;
		protected GameObject mAdMask;
		protected GameObject mRedNot;
		protected GameObject mPriceGet;
		protected GameObject mAdicon;
		protected Image mpackBg;
		protected Image mCurDiaIcon;
		protected UIButton mAdGet;
		protected UIButton mGetPrice;
		protected TextMeshProUGUI mAdBtnTxt;
		protected TextMeshProUGUI mRedCount;
		protected TextMeshProUGUI mAdGetCount;
		protected TextMeshProUGUI mPriceGetCount;
		protected TextMeshProUGUI mPackPriceTxt;

		protected override void ScriptGenerator()
		{
			madGet = FindChild("Obj_adGet").gameObject;
			mAdMask = FindChild("Obj_adGet/Btn_AdGet/Obj_AdMask").gameObject;
			mRedNot = FindChild("Obj_adGet/Obj_RedNot").gameObject;
			mPriceGet = FindChild("Obj_PriceGet").gameObject;
			mAdicon = FindChild("Obj_PriceGet/Btn_GetPrice/Hor/Obj_Adicon").gameObject;
			mpackBg = FindChildComponent<Image>("Obj_adGet/Img_packBg");
			mCurDiaIcon = FindChildComponent<Image>("Obj_PriceGet/Img_CurDiaIcon");
			mAdGet = FindChildComponent<UIButton>("Obj_adGet/Btn_AdGet");
			mGetPrice = FindChildComponent<UIButton>("Obj_PriceGet/Btn_GetPrice");
			mAdBtnTxt = FindChildComponent<TextMeshProUGUI>("Obj_adGet/Btn_AdGet/TMP_AdBtnTxt");
			mRedCount = FindChildComponent<TextMeshProUGUI>("Obj_adGet/Obj_RedNot/TMP_RedCount");
			mAdGetCount = FindChildComponent<TextMeshProUGUI>("Obj_adGet/TMP_AdGetCount");
			mPriceGetCount = FindChildComponent<TextMeshProUGUI>("Obj_PriceGet/TMP_PriceGetCount");
			mPackPriceTxt = FindChildComponent<TextMeshProUGUI>("Obj_PriceGet/Btn_GetPrice/Hor/TMP_PackPriceTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mAdGet.AddListener(ClickAdGetBtn);
			mGetPrice.AddListener(ClickGetPriceBtn);
		}
		private void ClickAdGetBtn(GameObject obj)
		{
			if (_isDisCountDown)
			{
				return;
			}
			SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_Coins, (success) =>
			{
				if (success)
				{
					SendRew();
				}
			});
		}
		private void ClickGetPriceBtn(GameObject obj)
		{
			RechargeController.Instance.HandleBuyOrAd(_mCfgRecharge, transform.position,
				() => { UpdateBuyButton(); },
				(pruductId) =>
				{
					SendRew();
				});
		}
		#endregion
	}
}
