using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_Language
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected Transform mContent;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("AnimNode/Bg/Btn_Close");
			mContent = FindChild("AnimNode/Bg/Scroll View/Viewport/Trans_Content");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mMask.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}
		#endregion
	}
}
