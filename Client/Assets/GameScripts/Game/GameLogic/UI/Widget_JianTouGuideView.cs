using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_JianTouGuide
	{

		protected Image mJiantouAnim;
		protected GameObject mAllJinatous;

		protected override void ScriptGenerator()
		{
			mJiantouAnim = FindChildComponent<Image>("Img_JiantouAnim");
			mAllJinatous = FindChild("Obj_AllJinatous").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
