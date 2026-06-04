using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Shop
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected GameObject mAllProducts;
		protected GameObject mpacksNode;
		protected GameObject mExcNode;
		protected GameObject mDiaNode;
		protected Transform mPack;
		protected Transform mAllExc;
		protected Transform mAllDia;
		protected Transform motherDia;
		protected Transform mAllDiamond;
		protected TextMeshProUGUI mAllDiaCount;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("TopUI/Btn_Close");
			mAllProducts = FindChild("Bg/Obj_AllProducts").gameObject;
			mpacksNode = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_packsNode").gameObject;
			mExcNode = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_ExcNode").gameObject;
			mDiaNode = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_DiaNode").gameObject;
			mPack = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_packsNode/Trans_Pack");
			mAllExc = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_ExcNode/Trans_AllExc");
			mAllDia = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_DiaNode/Trans_AllDia");
			motherDia = FindChild("Bg/Obj_AllProducts/Viewport/Content/Obj_DiaNode/Trans_AllDia/Trans_otherDia");
			mAllDiamond = FindChild("TopUI/Trans_AllDiamond");
			mAllDiaCount = FindChildComponent<TextMeshProUGUI>("TopUI/Trans_AllDiamond/TMP_AllDiaCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}
		#endregion
	}
}
