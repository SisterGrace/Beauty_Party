using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_EditorHome
	{

		protected GameObject mAllLevlelInfo;
		protected GameObject mOneLevelInfo;
		protected TMP_InputField mLevelname;
		protected UIButton mCreat;

		protected override void ScriptGenerator()
		{
			mAllLevlelInfo = FindChild("AllLevelNode/Obj_AllLevlelInfo").gameObject;
			mOneLevelInfo = FindChild("AllLevelNode/Obj_AllLevlelInfo/Viewport/Content/Obj_OneLevelInfo").gameObject;
			mLevelname = FindChildComponent<TMP_InputField>("Input_Levelname");
			mCreat = FindChildComponent<UIButton>("Btn_Creat");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mCreat.AddListener(ClickCreat);
		}

		private void ClickCreat(GameObject obj)
		{
			if (isCheckInPut())
			{
				StartCreatLevelBase();
			}
		}
		#endregion
	}
}
