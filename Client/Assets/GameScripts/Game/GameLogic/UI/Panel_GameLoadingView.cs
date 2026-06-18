using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_GameLoading
	{

		protected Slider mloading;
		protected TextMeshProUGUI mLoadingTxt;

		protected override void ScriptGenerator()
		{
			mloading = FindChildComponent<Slider>("Slider_loading");
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
