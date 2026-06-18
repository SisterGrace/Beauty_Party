using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_EndlessTreasure
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mtest;
		protected Transform mAllItems;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("Btn_Close");
			mtest = FindChildComponent<UIButton>("Btn_test");
			mAllItems = FindChild("Trans_AllItems");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mtest.AddListener(clickTest);
		}

		private void ClickClose(GameObject obj)
		{
			if(_endlessItemShowAmim)return;
			SDKManager.Instance.Button_ShowIV(HCIVPositionName.IV_Back,IvType.IV2);
			GameEvent.Send(TKEventType.MainHomeAnim,true);
			Close();
		}

		private void clickTest(GameObject obj)
		{
			OnRefresh();
		}
		#endregion
	}
}
