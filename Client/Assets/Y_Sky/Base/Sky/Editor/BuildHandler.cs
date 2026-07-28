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

        // 确保输出目录存在
        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);

        // ✅ 判断是否勾选了 Export Project
        bool isExportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;

        string fullPath;
        if (isExportProject)
        {
            // ===== 导出 Gradle 工程 =====
            // 路径应该是一个文件夹，而不是文件
            string folderName = $"{bundleId}_{versionName}_{versionCode}_{timeStamp}";
            fullPath = Path.Combine(OUTPUT_DIR, folderName);

            // 如果文件夹已存在，先删除（避免冲突）
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            Debug.Log($"📁 导出 Gradle 工程 -> {fullPath}");
        }
        else
        {
            // ===== 构建 APK / AAB =====
            string extension = EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
            string fileName = $"{bundleId}_{versionName}_{versionCode}_{timeStamp}{extension}";
            fullPath = Path.Combine(OUTPUT_DIR, fileName);

            Debug.Log($"🚀 构建 Android 包 -> {fullPath}");
        }

        options.locationPathName = fullPath;
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }
}