using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_ActivityIcon
	{

		protected UIButton mEnter;
		protected UIButton mGacha;
		protected TextMeshProUGUI mname;
		protected TextMeshProUGUI mTimeTxt;
		protected TextMeshProUGUI mRedCount;
		protected GameObject mTimeNode;
		protected GameObject mRedNot;
		protected GameObject mstar;
		protected GameObject mGachaRedNot;

		protected override void ScriptGenerator()
		{
			mEnter = FindChildComponent<UIButton>("Btn_Enter");
			mGacha = FindChildComponent<UIButton>("Btn_Gacha");
			mname = FindChildComponent<TextMeshProUGUI>("Btn_Enter/TMP_name");
			mTimeTxt = FindChildComponent<TextMeshProUGUI>("Btn_Enter/Obj_TimeNode/TMP_TimeTxt");
			mRedCount = FindChildComponent<TextMeshProUGUI>("Btn_Enter/Obj_RedNot/TMP_RedCount");
			mTimeNode = FindChild("Btn_Enter/Obj_TimeNode").gameObject;
			mRedNot = FindChild("Btn_Enter/Obj_RedNot").gameObject;
			mstar = FindChild("Btn_Enter/Obj_star_01").gameObject;
			mGachaRedNot = FindChild("Btn_Gacha/Obj_GachaRedNot").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mEnter.AddListener(Click);
			mGacha.AddListener(Click);
		}
		#endregion
	}
}
