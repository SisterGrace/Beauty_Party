using System;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace TK_Engine.Editor
{
    /// <summary>
    /// TK_Engine 项目的构建管线实现。
    /// 包含 HybridCLR DLL 编译和 YooAsset AssetBundle 打包。
    /// 其他项目可参照此类实现自己的 IBuildPipeline。
    /// </summary>
    public class TKBuildPipeline : FeishuBuildTool.Editor.IBuildPipeline
    {
        public string PipelineName => "TK_Engine Android";

        public void PreBuild(BuildTarget target, bool cleanBuild, Action<string, float> onProgress)
        {
            onProgress("编译 HybridCLR DLL...", 0.1f);
            BuildDLLCommand.BuildAndCopyDlls(target);
            AssetDatabase.Refresh();

            onProgress("打包 AssetBundle...", 0.3f);
            string packageVersion = ReleaseTools.GetBuildPackageVersion();
            BuildAssetBundle(target, packageVersion);
            AssetDatabase.Refresh();

            onProgress("预构建完成，准备构建 APK...", 0.55f);
        }

        private static void BuildAssetBundle(BuildTarget buildTarget, string packageVersion)
        {
            Debug.Log($"[TKBuildPipeline] Building AssetBundle: {buildTarget}");

            var pipeline = new ScriptableBuildPipeline();
            var buildParameters = new ScriptableBuildParameters
            {
                CompressOption = ECompressOption.LZ4
            };

            buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
            buildParameters.BuildTarget = buildTarget;
            buildParameters.BuildMode = EBuildMode.IncrementalBuild;
            buildParameters.PackageName = "DefaultPackage";
            buildParameters.PackageVersion = packageVersion;
            buildParameters.VerifyBuildingResult = true;
            buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
            buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
            buildParameters.BuildinFileCopyParams = string.Empty;
            buildParameters.EnableSharePackRule = true;

            var encryptionClassName = AssetBundleBuilderSetting
                .GetPackageEncyptionClassName("DefaultPackage", EBuildPipeline.ScriptableBuildPipeline);
            var encryptionClassTypes =
                YooAsset.Editor.EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            var classType = encryptionClassTypes.Find(x =>
                x.FullName != null && x.FullName.Equals(encryptionClassName));
            if (classType != null)
                buildParameters.EncryptionServices =
                    (IEncryptionServices)Activator.CreateInstance(classType);

            var buildResult = pipeline.Run(buildParameters, true);
            if (buildResult.Success)
                Debug.Log($"[TKBuildPipeline] AssetBundle build success: {buildResult.OutputPackageDirectory}");
            else
                Debug.LogWarning($"[TKBuildPipeline] AssetBundle build issue: {buildResult.ErrorInfo}");
        }
    }
}
