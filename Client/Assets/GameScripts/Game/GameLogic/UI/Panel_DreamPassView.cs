using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
using Config.common;
namespace GameLogic
{
	public partial class Panel_DreamPass
	{

		protected UIButton mMask;
		protected UIButton mBuyPass;
		protected UIButton mTips;
		protected UIButton mClose;
		protected GameObject mAllPassItem;
		protected GameObject mOnePassItem;
		protected TextMeshProUGUI mbuypassTxt;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mBuyPass = FindChildComponent<UIButton>("TopUI/Title/Btn_BuyPass");
			mTips = FindChildComponent<UIButton>("TopUI/Btn_Tips");
			mClose = FindChildComponent<UIButton>("TopUI/Btn_Close");
			mAllPassItem = FindChild("AllViews/Obj_AllPassItem").gameObject;
			mOnePassItem = FindChild("AllViews/Obj_AllPassItem/Viewport/Content/Obj_OnePassItem").gameObject;
			mbuypassTxt = FindChildComponent<TextMeshProUGUI>("TopUI/Title/Btn_BuyPass/TMP_buypassTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mTips.AddListener(ClickTips);
			//mBuyPass 按钮禁用功能：不再绑定点击事件，仅作为进度显示载体
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickTips(GameObject obj)
		{
			GameModule.UI.ShowUI<Panel_LevelDetail>(false);
		}
		#endregion
	}
}
