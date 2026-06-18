using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_EliminateSlot
	{

		protected Transform mBg;
		protected Transform mAllSlotPos;
		protected Transform mCards;

		protected override void ScriptGenerator()
		{
			mBg = FindChild("Trans_Bg");
			mAllSlotPos = FindChild("Trans_AllSlotPos");
			mCards = FindChild("Trans_Cards");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
