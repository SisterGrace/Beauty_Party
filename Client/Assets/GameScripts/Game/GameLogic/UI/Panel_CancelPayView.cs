using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_CancelPay
	{

		protected UIButton mmask;
		protected UIButton mContinue;
		protected UIButton mClose;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mContinue = FindChildComponent<UIButton>("AnimNode/Btn_Continue");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mContinue.AddListener(ClickContinue);
			mClose.AddListener(ClickClose);
			mmask.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickContinue(GameObject obj)
		{
			Close();
		}
		#endregion
	}
}
