using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_FloatTips
	{

		protected Image mBg;
		protected TextMeshProUGUI mContent;

		protected override void ScriptGenerator()
		{
			mBg = FindChildComponent<Image>("Img_Bg");
			mContent = FindChildComponent<TextMeshProUGUI>("TMP_Content");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
