using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_FloatTips
	{

		protected Transform mNode;
		protected Transform mNode1;
		protected GameObject mMask;

		protected override void ScriptGenerator()
		{
			mNode = FindChild("Trans_Node");
			mNode1 = FindChild("Trans_Node1");
			mMask = FindChild("Obj_Mask").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
