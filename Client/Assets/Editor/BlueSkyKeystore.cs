using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BlueSkyKeystore
{
    static BlueSkyKeystore()
   {
        PlayerSettings.Android.keystorePass = "123456";
        PlayerSettings.Android.keyaliasName = "s";
        PlayerSettings.Android.keyaliasPass = "123456";
        PlayerSettings.Android.keystoreName = Application.dataPath + @"/Plugins/Android/blueSky.keystore";
    }
}