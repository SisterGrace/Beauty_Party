using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_DayCheckIn
	{

		protected Transform mDayListArea;
		protected GameObject mNormalDays;
		protected GameObject mMask;
		protected UIButton mClose;
		protected UIButton mCheck;
		protected TextMeshProUGUI mCheckState;

		protected override void ScriptGenerator()
		{
			mDayListArea = FindChild("Trans_DayListArea");
			mNormalDays = FindChild("Trans_DayListArea/Obj_NormalDays").gameObject;
			mMask = FindChild("Btn_Check/Obj_Mask").gameObject;
			mClose = FindChildComponent<UIButton>("Btn_Close");
			mCheck = FindChildComponent<UIButton>("Btn_Check");
			mCheckState = FindChildComponent<TextMeshProUGUI>("Btn_Check/TMP_CheckState");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mCheck.AddListener(ClickCheck);
		}
		private void ClickClose(GameObject obj)
		{
			Close();
		}
		private void ClickCheck(GameObject obj)
		{
			GetDayCheckRew();
		}
		#endregion
	}
}
