using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Loading
	{

		protected Image mLoadProgressBar;
		protected TextMeshProUGUI mLoadingTxt;
		protected GameObject mNetTips;

		protected override void ScriptGenerator()
		{
			mLoadProgressBar = FindChildComponent<Image>("Load/BM/Img_LoadProgressBar");
			mLoadingTxt = FindChildComponent<TextMeshProUGUI>("Load/BM/TMP_LoadingTxt");
			mNetTips = FindChild("Obj_NetTips").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
