using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneExcItem
	{

		protected Image mpackBg;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected GameObject mLock;
		protected GameObject mAdicon;
		protected GameObject mGetted;
		protected GameObject mRedNot;
		protected Transform mloadIcon;
		protected UIButton mPackPrice;
		protected TextMeshProUGUI mPackPriceTxt;
		protected TextMeshProUGUI mRedCount;

		protected override void ScriptGenerator()
		{
			mpackBg = FindChildComponent<Image>("Img_packBg");
			mPhotoNode = FindChild("Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mLock = FindChild("Obj_Lock").gameObject;
			mAdicon = FindChild("Btn_PackPrice/Hor/Obj_Adicon").gameObject;
			mGetted = FindChild("Obj_Getted").gameObject;
			mRedNot = FindChild("Obj_RedNot").gameObject;
			mloadIcon = FindChild("Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mPackPrice = FindChildComponent<UIButton>("Btn_PackPrice");
			mPackPriceTxt = FindChildComponent<TextMeshProUGUI>("Btn_PackPrice/Hor/TMP_PackPriceTxt");
			mRedCount = FindChildComponent<TextMeshProUGUI>("Obj_RedNot/TMP_RedCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_itemPhoto = mPhotoNode.GetComponent<Item_PhotoLoad>();
			//绑定购买按钮事件
			mPackPrice.AddListener(OnClickBuy);
		}
		#endregion
	}
}
