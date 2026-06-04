using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_GetReward_1
	{

		protected GameObject mStoryRew;
		protected Transform meff;
		protected Transform mlihe;

		protected override void ScriptGenerator()
		{
			mStoryRew = FindChild("Obj_StoryRew").gameObject;
			meff = FindChild("Obj_StoryRew/efc_main_gift_01/Trans_eff");
			mlihe = FindChild("Obj_StoryRew/efc_main_gift_01/Trans_lihe");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
