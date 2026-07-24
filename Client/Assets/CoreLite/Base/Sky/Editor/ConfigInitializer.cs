using UnityEngine;
using UnityEditor;
using System.IO;

namespace Sky.Editor
{
    /// <summary>
    /// 自动初始化项目的自定义配置文件（从 Package 复制默认模板）
    /// </summary>
    [InitializeOnLoad]
    public static class ConfigInitializer
    {
        // ------------------- 请在这里修改为你自己的包名和路径 -------------------
        private const string PackageName = "com.y.sky";

        // 定义需要复制的文件列表：源路径（相对于包根目录） -> 目标路径（相对于项目根目录）
        private static readonly (string Source, string Target)[] ConfigFiles = new (string, string)[]
        {
            (
                "CoreLite/Editor/CoreGameConfigData.asset",
                "Assets/Resources/CoreGameConfigData.asset"
            ),
            (
                "CoreLite/Editor/SkyConfigData.asset",
                "Assets/Resources/SkyConfigData.asset"
            )
        };
        // --------------------------------------------------------------------

        static ConfigInitializer()
        {
            // 编辑器启动、脚本重编译时自动执行
            InitializeAllConfigs();
        }

        [MenuItem("Tools/初始化所有自定义配置")]
        public static void ForceInitialize()
        {
            InitializeAllConfigs();
        }

        private static void InitializeAllConfigs()
        {
            foreach (var (source, target) in ConfigFiles)
            {
                CopyConfigFile(source, target);
            }
        }

        private static void CopyConfigFile(string sourceRelativePath, string targetAssetPath)
        {
            // 1. 检查目标是否已存在
            if (AssetDatabase.LoadAssetAtPath<Object>(targetAssetPath) != null)
            {
                // 已存在，跳过（保留项目自己的修改）
                return;
            }

            // 2. 构建源文件的完整路径
            string fullSourcePath = $"Packages/{PackageName}/{sourceRelativePath}";

            // 3. 检查源文件是否存在
            if (!File.Exists(fullSourcePath))
            {
                Debug.LogError($"❌ 找不到默认配置文件: {fullSourcePath}。请确认 PackageName 和源路径是否正确。");
                return; // 如果用了 foreach，这里用 continue；如果用 for 用 return，我这里是 foreach，所以用 continue 跳过一个文件的错误继续复制下一个
            }

            // 4. 确保目标目录存在（逐级创建文件夹）
            string targetDir = Path.GetDirectoryName(targetAssetPath).Replace('\\', '/');
            if (!string.IsNullOrEmpty(targetDir) && !AssetDatabase.IsValidFolder(targetDir))
            {
                string[] folders = targetDir.Split('/');
                string currentPath = "Assets";
                for (int i = 1; i < folders.Length; i++)
                {
                    string newFolder = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(newFolder))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newFolder;
                }
            }

            // 5. 复制资源
            if (AssetDatabase.CopyAsset(fullSourcePath, targetAssetPath))
            {
                Debug.Log($"✅ 已从 Package 复制默认配置到: {targetAssetPath}");
            }
            else
            {
                Debug.LogError($"❌ 复制配置文件失败: {targetAssetPath}，请检查路径或权限。");
            }
        }
    }
}