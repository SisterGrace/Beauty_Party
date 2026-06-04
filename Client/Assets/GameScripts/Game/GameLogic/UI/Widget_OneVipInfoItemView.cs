using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneVipInfoItem
	{

		protected Image mCurbg;
		protected Image mIcon;
		protected TextMeshProUGUI mCurInfoTxt;

		protected override void ScriptGenerator()
		{
			mCurbg = FindChildComponent<Image>("Img_Curbg");
			mIcon = FindChildComponent<Image>("Img_Icon");
			mCurInfoTxt = FindChildComponent<TextMeshProUGUI>("TMP_CurInfoTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
