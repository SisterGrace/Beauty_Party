using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_FailTips
	{

		protected Image mIcon;
		protected GameObject mloseLife;
		protected TextMeshProUGUI mTipsTxt;
		protected TextMeshProUGUI mGoldCount;

		protected override void ScriptGenerator()
		{
			mIcon = FindChildComponent<Image>("Img_Icon");
			mloseLife = FindChild("Obj_loseLife").gameObject;
			mTipsTxt = FindChildComponent<TextMeshProUGUI>("TMP_TipsTxt");
			mGoldCount = FindChildComponent<TextMeshProUGUI>("TMP_GoldCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			
		}
		#endregion
	}
}
