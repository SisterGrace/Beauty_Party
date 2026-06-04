using UnityEngine;

namespace TK_Engine.Localization
{

    public class AutoChangeCultureInfo : MonoBehaviour
    {
        public void Start()
        {
            LocalizationManager.EnableChangingCultureInfo(true);
        }
    }
}