using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameBase;
using TK_Engine;
using TMPro;
using TK_Engine.Localization;
namespace GameBase
{
    public static class LanguageSystem 
    {
        public static void SetLanguage(this TextMeshProUGUI text,string language,params string[] userDatas)
        {
            if(string.IsNullOrEmpty(language)) return;
            if (SettingsUtils.FrameworkGlobalSettings.IsLocalize)
            {
                Localize _translate =text.transform.GetOrAddComponent<Localize>();
                if (_translate != null)
                {
                    if (userDatas == null)
                    {
                        _translate.SetTerm(language);
                    }
                    else
                    {
                        _translate.MySetTerm(language, userDatas);
                    }
                    
                }
                else
                {
                    Log.Warning($"No Localize component found on the {text.gameObject.name} object");
                }
            }
            else
            {
                text.text = language;
            }
        }
    }
}
