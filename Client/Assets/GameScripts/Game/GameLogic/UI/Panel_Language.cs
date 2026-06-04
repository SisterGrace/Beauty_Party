using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Language : UIWindow
	{
		private UIListWidget<Widget_OneLanguage, LanguageData> _listWidget;
		protected override void OnRefresh()
		{
			CreatLanguageItems();
		}

		protected override void RegisterEvent()
		{
		}

		private void CreatLanguageItems()
		{
			_listWidget = CreateWidget<UIListWidget<Widget_OneLanguage, LanguageData>>(mContent.gameObject);
			List<LanguageData> datas = new List<LanguageData>();
			var languages = ConfigLoader.Instance.Tables.TblanguageCfg.DataList;
			foreach (var item in languages)
			{
				LanguageData data1 = new LanguageData(CfgLanguageToEnum(item.Type), item.Name);
				datas.Add(data1);
			}
			_listWidget.SetDatas(datas);
		}

		private Language CfgLanguageToEnum(QLanguageType cfgLanguage)
		{
			switch (cfgLanguage)
			{
				case QLanguageType.Chinese_Simplified:
					return Language.ChineseSimplified;
				case QLanguageType.Chinese_Traditional:
					return Language.ChineseTraditional;
				case QLanguageType.English:
					return Language.English;
				case QLanguageType.German:
					return Language.German;
				case QLanguageType.Japanese:
					return Language.Japanese;
				case QLanguageType.Korean:
					return Language.Korean;
				case QLanguageType.French:
					return Language.French;
				case QLanguageType.Italian:
					return Language.Italian;
				case QLanguageType.Polish:
					return Language.Polish;
				case QLanguageType.Swedish:
					return Language.Swedish;	
				case QLanguageType.Spanish:
					return Language.Spanish;
				case QLanguageType.Spanish_Mexico:
					return Language.Spanish;
				case QLanguageType.German_Switzerland:
					return Language.German;
				case QLanguageType.Romanian:
					return Language.Romanian;
				case QLanguageType.Czech:
					return Language.Czech;

				default:
					return Language.English;
			}
		}
		protected override void Close()
		{
			base.Close();
		}
	}
}
