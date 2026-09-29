using UnityEngine;
using UnityEditor;
using System.IO;
using Sky;

namespace Sky.Editor
{
    [InitializeOnLoad]
    public static class ConfigInitializer
    {
        private static bool _initialized = false;

        // 定义所有需要代码生成的配置文件
        // 元组：目标路径, 类型, 初始化默认值的委托（可为 null）
        private static readonly (string Target, System.Type Type, System.Action<ScriptableObject> Initializer)[] ConfigsToGenerate =
        new (string, System.Type, System.Action<ScriptableObject>)[]
        {
            // 1. SkyGameConfigData
            (
                "Assets/Resources/SkyGameConfigData.asset",
                typeof(SkyGameConfigData),
                (obj) => {
                    var config = obj as SkyGameConfigData;
                    if (config != null)
                    {
                        // ⭐ 在这里填写你想要的默认值（占位符）
                        // 注意：这里只设置初始默认值，项目改过后不会被覆盖
                        
                        // 如果有嵌套对象，确保不为 null
                        if (config.toponLocalConfig == null) config.toponLocalConfig = new SkyToponLocalConfig();
                    }
                }
            ),
            // 3. KeystoreConfig
            (
                "Assets/Settings/KeystoreConfig.asset",
                typeof(KeystoreConfig),
                (obj) => {
                    var config = obj as KeystoreConfig;
                    if (config != null)
                    {
                        config.keystorePass = "请修改密码";
                        config.keyaliasName = "请修改别名";
                    }
                }
            )
        };

        static ConfigInitializer()
        {
            EditorApplication.delayCall += InitializeAllConfigsDelayed;
        }

        private static void InitializeAllConfigsDelayed()
        {
            if (_initialized) return;
            _initialized = true;

            foreach (var (target, type, initializer) in ConfigsToGenerate)
            {
                GenerateConfigFile(target, type, initializer);
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/初始化所有自定义配置")]
        public static void ForceInitialize()
        {
            _initialized = false;
            InitializeAllConfigsDelayed();
        }

        private static void GenerateConfigFile(
            string targetAssetPath,
            System.Type type,
            System.Action<ScriptableObject> initializer = null)
        {
            // 如果已存在且能正常加载，跳过（保护用户修改的数据）
            if (AssetDatabase.LoadAssetAtPath<Object>(targetAssetPath) != null)
                return;

            // 如果文件损坏，删除重建
            if (File.Exists(targetAssetPath))
            {
                string metaPath = targetAssetPath + ".meta";
                if (File.Exists(metaPath)) File.Delete(metaPath);
                File.Delete(targetAssetPath);
                AssetDatabase.Refresh();
            }

            // 确保目录存在
            string targetDir = Path.GetDirectoryName(targetAssetPath).Replace('\\', '/');
            EnsureDirectoryExists(targetDir);

            // ⭐ 核心：创建 ScriptableObject 实例
            ScriptableObject obj = ScriptableObject.CreateInstance(type);
            if (obj == null)
            {
                Debug.LogError($"❌ 无法创建类型 {type.Name}");
                return;
            }

            // ⭐ 执行初始化委托（填入默认值）
            initializer?.Invoke(obj);

            // 保存为 .asset 文件
            AssetDatabase.CreateAsset(obj, targetAssetPath);
            Debug.Log($"✅ 已通过代码生成配置: {targetAssetPath}");
        }

        private static void EnsureDirectoryExists(string targetDir)
        {
            if (string.IsNullOrEmpty(targetDir) || AssetDatabase.IsValidFolder(targetDir))
                return;

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
    }
}