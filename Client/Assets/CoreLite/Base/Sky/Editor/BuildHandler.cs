using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class BuildHandler
{
    private static string OUTPUT_DIR =>
       Path.GetFullPath(Path.Combine(Application.dataPath, "../../ToolBuild/output"));

    static BuildHandler()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(HandleBuild);
    }

    private static void HandleBuild(BuildPlayerOptions options)
    {
        // 只处理 Android 平台
        if (options.target != BuildTarget.Android)
        {
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            return;
        }

        // 动态获取 Android 包名
        string bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        if (string.IsNullOrEmpty(bundleId))
        {
            Debug.LogError("无法获取 Android 包名，请检查 PlayerSettings。");
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            return;
        }

        string versionName = PlayerSettings.bundleVersion;
        int versionCode = PlayerSettings.Android.bundleVersionCode;
        string timeStamp = System.DateTime.Now.ToString("MMddHHmm");

        // 根据是否勾选 "Build App Bundle" 决定文件后缀
        string extension = EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
        string fileName = $"{bundleId}_{versionName}_{versionCode}_{timeStamp}{extension}";

        // 确保输出目录存在
        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);

        string fullPath = Path.Combine(OUTPUT_DIR, fileName);
        options.locationPathName = fullPath;

        Debug.Log($"🚀 自动构建 Android 包 -> {fullPath}");
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }
}