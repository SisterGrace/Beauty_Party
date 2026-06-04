using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneLanguage : UIWidget,IListDataItem<LanguageData>
	{
		private LanguageData _data;
		public void SetItemData(LanguageData data)
		{
			_data = data;
			if (_data.Curlanguage != Language.Unspecified)
			{
				mCurLanguage.text = $"{_data.LanguageShortName}";
			}
			else
			{
				mCurLanguage.text = "Stay tuned!";
			}
		}

		
		public void Hide()
		{
		}
	}

	public struct LanguageData
	{
		public Language Curlanguage;
		public string LanguageShortName;
		public LanguageData(Language curlanguage,string name)
		{
			Curlanguage = curlanguage;
			LanguageShortName = name;
		}
	}
}
