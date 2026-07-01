using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SkyKeystore
{
    static SkyKeystore()
   {
        PlayerSettings.Android.keystorePass = "Blue!Coffee$42River@Sky";
        PlayerSettings.Android.keyaliasName = "s";
        PlayerSettings.Android.keyaliasPass = "x9F#kL2$qR7@vM8!pN3w";
        PlayerSettings.Android.keystoreName = Application.dataPath + @"/CoreLite/Base/Sky/Android/KeyStore2.keystore";
    }
}