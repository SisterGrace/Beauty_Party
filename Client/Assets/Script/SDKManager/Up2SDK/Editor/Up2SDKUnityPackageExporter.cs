using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Up2SDK.Editor
{
    public static class Up2SDKUnityPackageExporter
    {
        private const string MenuPath = "Tools/Up2SDK/Export UnityPackage";
        private const string PackageNamePrefix = "Up2SDK";

        private static readonly string[] ExportAssetPaths =
        {
            "Assets/Script/SDKManager/Up2SDK",
            "Assets/Plugins/Android",
            "Assets/ExternalDependencyManager"
        };

        [MenuItem(MenuPath)]
        public static void ExportUnityPackage()
        {
            var assetPaths = GetExistingAssetPaths();
            if (assetPaths.Length == 0)
            {
                EditorUtility.DisplayDialog("Up2SDK Export", "没有找到可导出的 Up2SDK 资源。", "OK");
                return;
            }

            var outputPath = EditorUtility.SaveFilePanel(
                "Export Up2SDK UnityPackage",
                Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
                GetPackageFileName(),
                "unitypackage");

            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }

            AssetDatabase.ExportPackage(
                assetPaths,
                outputPath,
                ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Up2SDK Export", "导出完成:\n" + outputPath, "OK");
            Debug.Log("[Up2SDK] UnityPackage exported: " + outputPath);
        }

        private static string[] GetExistingAssetPaths()
        {
            var existingPaths = new List<string>();
            foreach (var assetPath in ExportAssetPaths)
            {
                if (AssetDatabase.IsValidFolder(assetPath) || File.Exists(assetPath))
                {
                    existingPaths.Add(assetPath);
                }
                else
                {
                    Debug.LogWarning("[Up2SDK] Export asset path not found: " + assetPath);
                }
            }

            return existingPaths.ToArray();
        }

        private static string GetPackageFileName()
        {
            return PackageNamePrefix + "_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".unitypackage";
        }
    }
}
