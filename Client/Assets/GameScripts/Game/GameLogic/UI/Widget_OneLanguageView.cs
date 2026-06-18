using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_OneLanguage
	{

		protected UIButton mset;
		protected TextMeshProUGUI mCurLanguage;

		protected override void ScriptGenerator()
		{
			mset = FindChildComponent<UIButton>("Btn_set");
			mCurLanguage = FindChildComponent<TextMeshProUGUI>("TMP_CurLanguage");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mset.AddListener(ClickSet);
		}

		private void ClickSet(GameObject obj)
		{
			if (_data.Curlanguage != Language.Unspecified)
			{
				GameModule.Localization.SetLanguage(_data.Curlanguage);
				GameModule.Setting.SetString(Constant.Setting.Language, _data.Curlanguage.ToString());
			}
			GameModule.UI.CloseUI<Panel_Language>();
		}
		#endregion
	}
}
