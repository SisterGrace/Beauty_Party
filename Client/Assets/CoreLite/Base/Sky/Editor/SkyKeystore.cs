using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SkyKeystore
{
    private const string ConfigPath = "Assets/Settings/KeystoreConfig.asset";

    static SkyKeystore()
    {
        var config = AssetDatabase.LoadAssetAtPath<KeystoreConfig>(ConfigPath);
        if (config == null)
        {
            Debug.LogWarning("未找到 KeystoreConfig，请创建：右键 Assets -> Create -> Sky -> Keystore Config，并填写密钥信息。");
            return;
        }

        PlayerSettings.Android.keystorePass = config.keystorePass;
        PlayerSettings.Android.keyaliasName = config.keyaliasName;
        PlayerSettings.Android.keyaliasPass = config.keyaliasPass;
        // 如果路径是相对路径，可以转换为绝对路径
        if (!string.IsNullOrEmpty(config.keystoreName))
        {
            if (config.keystoreName.StartsWith("Assets/"))
                PlayerSettings.Android.keystoreName = Application.dataPath + "/" + config.keystoreName.Substring("Assets/".Length);
            else
                PlayerSettings.Android.keystoreName = config.keystoreName;
        }
    }
}