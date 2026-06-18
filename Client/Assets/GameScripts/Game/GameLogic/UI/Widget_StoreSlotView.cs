using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_StoreSlot
	{

		protected Transform mCasualSolts;

		protected override void ScriptGenerator()
		{
			mCasualSolts = FindChild("Trans_CasualSolts");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
