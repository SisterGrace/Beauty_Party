using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace TK_Engine.Editor
{
    /// <summary>
    /// 打包工具类。
    /// <remarks>通过CommandLineReader可以不前台开启Unity实现静默打包以及CLI工作流，详见CommandLineReader.cs example1</remarks>
    /// </summary>
    public static class ReleaseTools
    {
        public static void BuildDll()
        {
            string platform = CommandLineReader.GetCustomArgument("platform");
            if (string.IsNullOrEmpty(platform))
            {
                Debug.LogError($"Build Asset Bundle Error！platform is null");
                return;
            }

            BuildTarget target = GetBuildTarget(platform);

            BuildDLLCommand.BuildAndCopyDlls(target);
        }

        public static void BuildAssetBundle()
        {
            string outputRoot = CommandLineReader.GetCustomArgument("outputRoot");
            if (string.IsNullOrEmpty(outputRoot))
            {
                Debug.LogError($"Build Asset Bundle Error！outputRoot is null");
                return;
            }

            string packageVersion = CommandLineReader.GetCustomArgument("packageVersion");
            if (string.IsNullOrEmpty(packageVersion))
            {
                Debug.LogError($"Build Asset Bundle Error！packageVersion is null");
                return;
            }

            string platform = CommandLineReader.GetCustomArgument("platform");
            if (string.IsNullOrEmpty(platform))
            {
                Debug.LogError($"Build Asset Bundle Error！platform is null");
                return;
            }

            BuildTarget target = GetBuildTarget(platform);
            BuildInternal(target, outputRoot);
            Debug.LogWarning($"Start BuildPackage BuildTarget:{target} outputPath:{outputRoot}");
        }

        [MenuItem("TK_Engine/Quick Build/一键打包AssetBundle")]
        public static void BuildCurrentPlatformAB()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildInternal(target, Application.dataPath + "/../Builds/", packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
        }

        private static BuildTarget GetBuildTarget(string platform)
        {
            BuildTarget target = BuildTarget.NoTarget;
            switch (platform)
            {
                case "Android":
                    target = BuildTarget.Android;
                    break;
                case "IOS":
                    target = BuildTarget.iOS;
                    break;
                case "Windows":
                    target = BuildTarget.StandaloneWindows64;
                    break;
                case "MacOS":
                    target = BuildTarget.StandaloneOSX;
                    break;
                case "Linux":
                    target = BuildTarget.StandaloneLinux64;
                    break;
                case "WebGL":
                    target = BuildTarget.WebGL;
                    break;
                case "Switch":
                    target = BuildTarget.Switch;
                    break;
                case "PS4":
                    target = BuildTarget.PS4;
                    break;
                case "PS5":
                    target = BuildTarget.PS5;
                    break;
            }

            return target;
        }

        private static void BuildInternal(BuildTarget buildTarget, string outputRoot, string packageVersion = "1.0",
            EBuildPipeline buildPipeline = EBuildPipeline.ScriptableBuildPipeline)
        {
            Debug.Log($"开始构建 : {buildTarget}");

            IBuildPipeline pipeline = null;
            BuildParameters buildParameters = null;

            if (buildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                // 构建参数
                BuiltinBuildParameters builtinBuildParameters = new BuiltinBuildParameters();

                // 执行构建
                pipeline = new BuiltinBuildPipeline();
                buildParameters = builtinBuildParameters;

                builtinBuildParameters.CompressOption = ECompressOption.LZ4;
            }
            else
            {
                ScriptableBuildParameters scriptableBuildParameters = new ScriptableBuildParameters();

                // 执行构建
                pipeline = new ScriptableBuildPipeline();
                buildParameters = scriptableBuildParameters;

                scriptableBuildParameters.CompressOption = ECompressOption.LZ4;
            }

            buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = buildPipeline.ToString();
            buildParameters.BuildTarget = buildTarget;
            buildParameters.BuildMode = EBuildMode.IncrementalBuild;
            buildParameters.PackageName = "DefaultPackage";
            buildParameters.PackageVersion = packageVersion;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
            buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
            buildParameters.BuildinFileCopyParams = string.Empty;
            buildParameters.EncryptionServices = CreateEncryptionInstance("DefaultPackage", buildPipeline);
            // 启用共享资源打包
            buildParameters.EnableSharePackRule = true;

            var buildResult = pipeline.Run(buildParameters, true);
            if (buildResult.Success)
            {
                Debug.Log($"构建成功 : {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"构建失败 : {buildResult.ErrorInfo}");
            }
        }

        /// <summary>
        /// 创建加密类实例
        /// </summary>
        private static IEncryptionServices CreateEncryptionInstance(string packageName, EBuildPipeline buildPipeline)
        {
            var encryptionClassName =
                AssetBundleBuilderSetting.GetPackageEncyptionClassName(packageName, buildPipeline);
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            var classType =
                encryptionClassTypes.Find(x => x.FullName != null && x.FullName.Equals(encryptionClassName));
            if (classType != null)
            {
                Debug.Log($"Use Encryption {classType}");
                return (IEncryptionServices)Activator.CreateInstance(classType);
            }
            else
            {
                return null;
            }
        }

        [MenuItem("TK_Engine/Quick Build/一键打包Window", false, 90)]
        public static void AutomationBuild()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildDLLCommand.BuildAndCopyDlls(target);
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Builds/Windows",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            BuildImp(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64,
                $"{Application.dataPath}/../Builds/Windows/Release_Windows.exe");
        }

        internal static string GetBuildPackageVersion()
        {
            int totalMinutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        /// <summary>
        /// 生成 APK 文件名: 包名_月日时分.apk
        /// 例如: com.dream.tile.sweet.girl.puzzle.love.werc_03111530.apk
        /// </summary>
        internal static string GetApkFileName()
        {
            string bundleId = PlayerSettings.applicationIdentifier;
            string timestamp = DateTime.Now.ToString("MMddHHmm");
            return $"{bundleId}_{timestamp}.apk";
        }

        /// <summary>
        /// 获取 APK 输出目录（项目根目录/ToolBuild/output/）
        /// </summary>
        internal static string GetApkOutputDir()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ToolBuild/output"));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        [MenuItem("TK_Engine/Quick Build/一键打包Android", false, 90)]
        public static void AutomationBuildAndroid()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildDLLCommand.BuildAndCopyDlls(target);
            AssetDatabase.Refresh();
            BuildInternal(target, outputRoot: Application.dataPath + "/../Bundles",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            Debug.Log("11111111111111");
            BuildImp(BuildTargetGroup.Android, BuildTarget.Android,
                Path.Combine(GetApkOutputDir(), GetApkFileName()));
            Debug.Log("22222222222222222");
        }

        [MenuItem("TK_Engine/Quick Build/一键打包IOS", false, 90)]
        public static void AutomationBuildIOS()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildDLLCommand.BuildAndCopyDlls(target);
            AssetDatabase.Refresh();
            BuildInternal(target, outputRoot: Application.dataPath + "/../Bundles",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            BuildImp(BuildTargetGroup.iOS, BuildTarget.iOS, $"{Application.dataPath}/../Build/IOS/XCode_Project");
        }

        [MenuItem("TK_Engine/Quick Build/一键打包WebGL", false, 91)]
        public static void AutomationBuildWebGL()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildDLLCommand.BuildAndCopyDlls(target);
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Builds/WebGL", packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            BuildImp(BuildTargetGroup.WebGL, BuildTarget.WebGL, $"{Application.dataPath}/../Builds/WebGL");
        }

        public static void BuildImp(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string locationPathName)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, BuildTarget.StandaloneWindows64);
            AssetDatabase.Refresh();

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = locationPathName,
                targetGroup = buildTargetGroup,
                target = buildTarget,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build success: {summary.totalSize / 1024 / 1024} MB");
            }
            else
            {
                Debug.Log($"Build Failed" + summary.result);
            }
        }

        #region Editor Menu Build

        [MenuItem("TK_Engine/Quick Build/一键打包Android (Build)", false, 100)]
        public static void MenuBuildAndroid()
        {
            if (!EditorUtility.DisplayDialog("Android Build",
                    "确定要开始 Android 普通构建？\n(增量构建，速度较快)", "开始打包", "取消"))
                return;

            EditorBuildAndroid(false);
        }

        [MenuItem("TK_Engine/Quick Build/一键打包Android (Clean Build)", false, 101)]
        public static void MenuCleanBuildAndroid()
        {
            if (!EditorUtility.DisplayDialog("Android Clean Build",
                    "确定要开始 Android Clean Build？\n(清除缓存后完整重建，耗时较长)", "开始打包", "取消"))
                return;

            EditorBuildAndroid(true);
        }

        private static void EditorBuildAndroid(bool cleanBuild)
        {
            string mode = cleanBuild ? "Clean Build" : "Build";
            Debug.Log($"[Editor] Starting Android {mode}...");

            try
            {
                BuildTarget target = BuildTarget.Android;

                EditorUtility.DisplayProgressBar("Android Build", "编译 HybridCLR DLL...", 0.1f);
                BuildDLLCommand.BuildAndCopyDlls(target);
                AssetDatabase.Refresh();

                EditorUtility.DisplayProgressBar("Android Build", "打包 AssetBundle...", 0.3f);
                string packageVersion = GetBuildPackageVersion();
                BuildInternal(target, outputRoot: Application.dataPath + "/../Bundles",
                    packageVersion: packageVersion);
                AssetDatabase.Refresh();

                EditorUtility.DisplayProgressBar("Android Build", "构建 APK...", 0.6f);
                string apkPath = Path.Combine(GetApkOutputDir(), GetApkFileName());

                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                AssetDatabase.Refresh();

                var options = cleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None;
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                    locationPathName = apkPath,
                    targetGroup = BuildTargetGroup.Android,
                    target = BuildTarget.Android,
                    options = options
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                BuildSummary summary = report.summary;

                EditorUtility.ClearProgressBar();

                if (summary.result == BuildResult.Succeeded)
                {
                    long sizeMB = (long)(summary.totalSize / 1024 / 1024);
                    WriteResultFile(true, apkPath);
                    Debug.Log($"[Editor] Build success: {sizeMB} MB, output: {apkPath}");
                    EditorUtility.DisplayDialog("构建成功",
                        $"Android {mode} 完成！\n\n大小: {sizeMB} MB\n路径: {apkPath}", "确定");
                }
                else
                {
                    string errorMsg = $"Build result: {summary.result}, totalErrors: {summary.totalErrors}";
                    WriteResultFile(false, errorMsg);
                    Debug.LogError($"[Editor] Build failed: {errorMsg}");
                    EditorUtility.DisplayDialog("构建失败",
                        $"Android {mode} 失败\n\n{errorMsg}", "确定");
                }
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                WriteResultFile(false, e.Message);
                Debug.LogError($"[Editor] Build exception: {e}");
                EditorUtility.DisplayDialog("构建异常", $"发生异常:\n{e.Message}", "确定");
            }
        }

        #endregion

        #region CLI Build Methods

        /// <summary>
        /// CLI 专用 Android 构建入口，会通过 EditorApplication.Exit 返回退出码。
        /// 用法: Unity -quit -batchmode -executeMethod TK_Engine.Editor.ReleaseTools.BuildAndroidCLI
        /// 通过 -CustomArgs:cleanBuild=true 启用 Clean Build
        /// </summary>
        public static void BuildAndroidCLI()
        {
            try
            {
                bool cleanBuild = CommandLineReader.GetCustomArgument("cleanBuild") == "true";
                BuildTarget target = BuildTarget.Android;
                Debug.Log($"[CLI] Starting Android build... (cleanBuild={cleanBuild})");

                SetupKeystoreFromArgs();

                BuildDLLCommand.BuildAndCopyDlls(target);
                AssetDatabase.Refresh();

                string packageVersion = GetBuildPackageVersion();
                BuildInternal(target, outputRoot: Application.dataPath + "/../Bundles",
                    packageVersion: packageVersion);
                AssetDatabase.Refresh();

                string apkPath = Path.Combine(GetApkOutputDir(), GetApkFileName());
                BuildImpCLI(BuildTargetGroup.Android, BuildTarget.Android, apkPath, cleanBuild);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CLI] Build exception: {e}");
                WriteResultFile(false, e.Message);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 从命令行参数读取 Keystore 密码并设置到 PlayerSettings。
        /// batchmode 下 Unity 不会持久化密码，必须在构建前通过代码设置。
        /// </summary>
        private static void SetupKeystoreFromArgs()
        {
            string keystorePass = CommandLineReader.GetCustomArgument("keystorePass");
            string keyAliasPass = CommandLineReader.GetCustomArgument("keyAliasPass");

            if (!string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
                Debug.Log("[CLI] Keystore password set from command line args");
            }

            if (!string.IsNullOrEmpty(keyAliasPass))
            {
                PlayerSettings.Android.keyaliasPass = keyAliasPass;
                Debug.Log("[CLI] Key alias password set from command line args");
            }

            if (string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(keyAliasPass))
            {
                Debug.LogWarning("[CLI] Keystore passwords not provided via args. " +
                                 "If signing fails, run: ./ToolBuild/build_android.sh --setup-keystore");
            }
        }

        private static void BuildImpCLI(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget,
            string locationPathName, bool cleanBuild = false)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
            AssetDatabase.Refresh();

            var options = cleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None;
            Debug.Log($"[CLI] BuildOptions: {options}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = locationPathName,
                targetGroup = buildTargetGroup,
                target = buildTarget,
                options = options
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeMB = (long)(summary.totalSize / 1024 / 1024);
                Debug.Log($"[CLI] Build success: {sizeMB} MB, output: {locationPathName}");
                WriteResultFile(true, locationPathName);
                EditorApplication.Exit(0);
            }
            else
            {
                string errorMsg = $"Build result: {summary.result}, totalErrors: {summary.totalErrors}";
                Debug.LogError($"[CLI] Build failed: {errorMsg}");
                WriteResultFile(false, errorMsg);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 将构建结果写入 JSON 文件，供外部脚本读取。
        /// 输出到 ToolBuild/build_result.json
        /// </summary>
        private static void WriteResultFile(bool success, string message)
        {
            string resultDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ToolBuild"));
            if (!Directory.Exists(resultDir))
                Directory.CreateDirectory(resultDir);
            string resultPath = Path.Combine(resultDir, "build_result.json");
            string json =
                $"{{\"success\":{(success ? "true" : "false")},\"message\":\"{EscapeJson(message)}\",\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
            File.WriteAllText(resultPath, json);
            Debug.Log($"[CLI] Build result written to: {resultPath}");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        #endregion
    }
}