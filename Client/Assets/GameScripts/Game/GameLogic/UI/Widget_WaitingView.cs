using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_Waiting
	{

		protected Transform mloadIcon;
		protected TextMeshProUGUI mLoadingTxt;

		protected override void ScriptGenerator()
		{
			mloadIcon = FindChild("Trans_loadIcon");
			mLoadingTxt = FindChildComponent<TextMeshProUGUI>("TMP_LoadingTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
