using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Photo
	{

		protected Transform mChildPageNode;
		protected Transform mTopsUI;
		protected UIButton mClose;

		protected override void ScriptGenerator()
		{
			mChildPageNode = FindChild("CenterNode/Trans_ChildPageNode");
			mTopsUI = FindChild("TopNode/Trans_TopsUI");
			mClose = FindChildComponent<UIButton>("Btn_Close");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
			SDKManager.Instance.Button_ShowIV(HCIVPositionName.IV_Back,IvType.IV2);
			Close();
		}

		#endregion
	}
}
