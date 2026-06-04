using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_TimeLevelTips
	{

		protected RectTransform mBattleTips1;
		protected TextMeshProUGUI mBattleTipsTxt1;
		protected TextMeshProUGUI mCountTime;
		protected UIButton mContinue;

		protected override void ScriptGenerator()
		{
			mBattleTips1 = FindChildComponent<RectTransform>("Rect_BattleTips1");
			mBattleTipsTxt1 = FindChildComponent<TextMeshProUGUI>("Rect_BattleTips1/TMP_BattleTipsTxt1");
			mCountTime = FindChildComponent<TextMeshProUGUI>("Rect_BattleTips1/TMP_CountTime");
			mContinue = FindChildComponent<UIButton>("Btn_Continue");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mContinue.AddListener(ClickContinue);
		}

		private void ClickContinue(GameObject obj)
		{
			_callback?.Invoke();
			Hide();
		}
		#endregion
	}
}
