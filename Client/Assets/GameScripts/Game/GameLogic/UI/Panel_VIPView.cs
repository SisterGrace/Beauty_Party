using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_VIP
	{

		protected UIButton mClose;
		protected Transform mAllItems;

		protected override void ScriptGenerator()
		{
			mClose = FindChildComponent<UIButton>("TopUI/Btn_Close");
			mAllItems = FindChild("Trans_AllItems");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(OnCloseBtnClick);
        }
		private void OnCloseBtnClick(GameObject go)
		{
			Close();
		}
		#endregion
	}
}
