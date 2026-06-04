using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_HandGuide
	{

		protected Image mHandAnim;
		protected GameObject mEffHand;

		protected override void ScriptGenerator()
		{
			mHandAnim = FindChildComponent<Image>("Img_HandAnim");
			mEffHand = FindChild("Obj_EffHand").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
