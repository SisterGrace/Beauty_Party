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
			string rawPath = config.keystoreName.Replace('\\', '/'); // 统一为Unity风格斜杠
			if (rawPath.StartsWith("Assets/"))
			{
				// 去掉 "Assets/" 前缀，以项目根目录为基准拼接
				string relative = rawPath.Substring("Assets/".Length);
				PlayerSettings.Android.keystoreName = System.IO.Path.Combine(Application.dataPath, relative);
			}
			else
			{
				// 此时视作相对于项目根目录（即 Assets 的父级）的路径
				PlayerSettings.Android.keystoreName = rawPath;
			}
		}
	}
}
