using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_GachaTip
	{

		protected UIButton mmask;
		protected UIButton mClose;
		protected Image mPackIcon;
		protected TextMeshProUGUI mCurPackTips;
		protected TextMeshProUGUI mTitle;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mPackIcon = FindChildComponent<Image>("AnimNode/Bg/Img_PackIcon");
			mCurPackTips = FindChildComponent<TextMeshProUGUI>("AnimNode/Bg/frame/TMP_CurPackTips");
			mTitle = FindChildComponent<TextMeshProUGUI>("AnimNode/Bg/TitleBg/TMP_Title");
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
