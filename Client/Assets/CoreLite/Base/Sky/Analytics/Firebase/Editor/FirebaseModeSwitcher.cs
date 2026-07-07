using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class FirebaseModeSwitcher : EditorWindow
{
    // ========== 配置区域 ==========
    // Firebase 配置文件夹相对路径（相对于 Assets）
    private static readonly string FirebaseFolder = "CoreLite/Base/Sky/Analytics/Firebase";

    // 是否同时修改 iOS Bundle Identifier（默认 false，仅修改 Android）
    private static readonly bool UpdateIOSBundleId = false;
    // ============================

    private static string FullFirebaseFolder => Path.Combine(Application.dataPath, FirebaseFolder);
    private static string ProdJsonPath => Path.Combine(FullFirebaseFolder, "google-services_real.json");
    private static string TestJsonPath => Path.Combine(FullFirebaseFolder, "google-services_test.json");
    private static string ActiveJsonPath => Path.Combine(FullFirebaseFolder, "google-services.json");

    [MenuItem("Tools/Firebase/Switch to Test Mode", false, 1)]
    public static void SwitchToTestMode()
    {
        SwitchMode(isTestMode: true);
    }

    [MenuItem("Tools/Firebase/Switch to Prod Mode", false, 2)]
    public static void SwitchToProdMode()
    {
        SwitchMode(isTestMode: false);
    }

    private static void SwitchMode(bool isTestMode)
    {
        // 1. 更新宏定义
        SetScriptingDefineSymbol(isTestMode);

        // 2. 文件重命名（使得 google-services.json 指向正确的环境）
        if (!RenameFiles(isTestMode))
        {
            Debug.LogError("Firebase mode switching failed due to file operation errors.");
            return;
        }

        // 3. 从当前激活的 google-services.json 中读取 package_name，并设置 Android Bundle Identifier
        if (!UpdateAndroidBundleIdFromActiveJson())
        {
            Debug.LogError("Failed to update Android Bundle Identifier. Switching aborted.");
            return;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Firebase mode switched to {(isTestMode ? "TEST" : "PROD")} successfully.");
    }

    private static void SetScriptingDefineSymbol(bool isTestMode)
    {
        string defineSymbol = "FIREBASE_TEST_MODE";
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

        if (isTestMode)
        {
            if (!symbols.Contains(defineSymbol))
            {
                symbols = string.IsNullOrEmpty(symbols) ? defineSymbol : symbols + ";" + defineSymbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbols);
            }
        }
        else
        {
            if (symbols.Contains(defineSymbol))
            {
                symbols = symbols.Replace(defineSymbol, "").Replace(";;", ";").Trim(';');
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbols);
            }
        }
    }

    /// <summary>
    /// 重命名文件，使 google-services.json 指向正确的环境配置
    /// </summary>
    private static bool RenameFiles(bool toTestMode)
    {
        if (!Directory.Exists(FullFirebaseFolder))
        {
            Debug.LogError($"Firebase folder not found: {FullFirebaseFolder}");
            return false;
        }

        bool hasRealJson = File.Exists(ProdJsonPath);
        bool hasTestJson = File.Exists(TestJsonPath);
        bool hasActiveJson = File.Exists(ActiveJsonPath);

        if (toTestMode)
        {
            // 切换到测试模式：最终 Active 应该是测试文件
            if (!hasTestJson)
            {
                Debug.LogError("Cannot switch to test mode: google-services_test.json does not exist.");
                return false;
            }

            // 如果当前有正式配置文件，先将其备份为 google-services_real.json
            if (hasActiveJson && !IsFileSame(ActiveJsonPath, TestJsonPath))
            {
                if (hasRealJson)
                    File.Delete(ProdJsonPath);
                File.Move(ActiveJsonPath, ProdJsonPath);
            }

            // 将测试文件设为活动文件
            if (File.Exists(TestJsonPath))
            {
                if (File.Exists(ActiveJsonPath))
                    File.Delete(ActiveJsonPath);
                File.Move(TestJsonPath, ActiveJsonPath);
            }
        }
        else
        {
            // 切换到正式模式：最终 Active 应该是正式文件
            if (!hasRealJson)
            {
                Debug.LogError("Cannot switch to prod mode: google-services_real.json does not exist.");
                return false;
            }

            // 如果当前有活动文件且不是正式文件，先备份为 google-services_test.json
            if (hasActiveJson && !IsFileSame(ActiveJsonPath, ProdJsonPath))
            {
                if (hasTestJson)
                    File.Delete(TestJsonPath);
                File.Move(ActiveJsonPath, TestJsonPath);
            }

            // 将正式文件设为活动文件
            if (File.Exists(ProdJsonPath))
            {
                if (File.Exists(ActiveJsonPath))
                    File.Delete(ActiveJsonPath);
                File.Move(ProdJsonPath, ActiveJsonPath);
            }
        }

        return true;
    }

    private static bool IsFileSame(string path1, string path2)
    {
        if (!File.Exists(path1) || !File.Exists(path2)) return false;
        return File.ReadAllText(path1) == File.ReadAllText(path2);
    }

    /// <summary>
    /// 从当前激活的 google-services.json 中读取 package_name，并设置 Android Bundle Identifier
    /// </summary>
    private static bool UpdateAndroidBundleIdFromActiveJson()
    {
        if (!File.Exists(ActiveJsonPath))
        {
            Debug.LogError($"Active google-services.json not found at {ActiveJsonPath}");
            return false;
        }

        string jsonContent = File.ReadAllText(ActiveJsonPath);
        string packageName = ExtractPackageName(jsonContent);

        if (string.IsNullOrEmpty(packageName))
        {
            Debug.LogError("Failed to extract package_name from google-services.json");
            return false;
        }

        // 设置 Android Bundle Identifier
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);
        Debug.Log($"Android Bundle Identifier set to: {packageName}");

        // 可选：同时设置 iOS Bundle Identifier
        if (UpdateIOSBundleId)
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, packageName);
            Debug.Log($"iOS Bundle Identifier set to: {packageName}");
        }

        return true;
    }

    /// <summary>
    /// 从 google-services.json 中提取 package_name（位于 client[0].client_info.android_client_info 下）
    /// </summary>
    private static string ExtractPackageName(string json)
    {
        // 匹配 "android_client_info": { "package_name": "com.example.app" }
        Match match = Regex.Match(json, @"""android_client_info""\s*:\s*\{[^}]*""package_name""\s*:\s*""([^""]+)""");
        if (match.Success)
            return match.Groups[1].Value;

        // 降级匹配：直接找 "package_name": "..."
        match = Regex.Match(json, @"""package_name""\s*:\s*""([^""]+)""");
        if (match.Success)
            return match.Groups[1].Value;

        return null;
    }
}