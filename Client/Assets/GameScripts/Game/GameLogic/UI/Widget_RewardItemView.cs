using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_RewardItem
	{

		protected Image mReIcon;
		protected TextMeshProUGUI mReTxt;
		protected TextMeshProUGUI mtimeTxt;
		protected GameObject mFrame;

		protected override void ScriptGenerator()
		{
			mReIcon = FindChildComponent<Image>("Img_ReIcon");
			mReTxt = FindChildComponent<TextMeshProUGUI>("TMP_ReTxt");
			mtimeTxt = FindChildComponent<TextMeshProUGUI>("Obj_Frame/TMP_timeTxt");
			mFrame = FindChild("Obj_Frame").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
