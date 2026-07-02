using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class TopOnDebugToggle
{
    private const string MenuItemName = "Tools/TopOn Debug";
    private const string XmlFileName = "DebuggerUIDependencies.xml";

    [MenuItem(MenuItemName)]
    private static void ToggleDebug()
    {
        bool isEnabled = EditorPrefs.GetBool("TopOnDebugEnabled", false);
        isEnabled = !isEnabled;
        EditorPrefs.SetBool("TopOnDebugEnabled", isEnabled);
        SetDebugState(isEnabled);
        ForceResolve();
    }

    [MenuItem(MenuItemName, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuItemName, EditorPrefs.GetBool("TopOnDebugEnabled", false));
        return true;
    }

    private static void SetDebugState(bool enabled)
    {
        // 查找XML文件
        string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(XmlFileName));
        if (guids.Length == 0)
        {
            Debug.LogError($"未找到 {XmlFileName} 文件，请确保它存在于Assets目录下。");
            return;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"无法获取 {XmlFileName} 的路径。");
            return;
        }

        string fullPath = Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"文件不存在: {fullPath}");
            return;
        }

        string content = File.ReadAllText(fullPath);
        bool isCommented = content.TrimStart().StartsWith("<!--") && content.TrimEnd().EndsWith("-->");

        if (enabled && isCommented)
        {
            // 取消注释：去掉最外层的 <!-- 和 -->
            content = content.Trim();
            if (content.StartsWith("<!--") && content.EndsWith("-->"))
            {
                content = content.Substring(3, content.Length - 6);
                // 如果开头或结尾有换行符，保留原样
            }
            File.WriteAllText(fullPath, content);
            Debug.Log("TopOn Debug 已开启：取消注释 XML。");
        }
        else if (!enabled && !isCommented)
        {
            // 注释整个文件
            content = "<!--\n" + content + "\n-->";
            File.WriteAllText(fullPath, content);
            Debug.Log("TopOn Debug 已关闭：注释 XML。");
        }
        else
        {
            Debug.Log("TopOn Debug 状态未改变，无需操作。");
        }
        AssetDatabase.Refresh();
    }

    private static void ForceResolve()
    {
        // 尝试通过常见菜单路径执行 Force Resolve
        string[] menuPaths = new string[]
        {
            "Assets/External Dependency Manager/Android Resolver/Force Resolve",
            "Assets/External Dependency Manager/Android Resolver/Resolve",
            "Window/External Dependency Manager/Android Resolver/Force Resolve"
        };
        bool executed = false;
        foreach (var menuPath in menuPaths)
        {
            if (EditorApplication.ExecuteMenuItem(menuPath))
            {
                Debug.Log($"已通过菜单执行 Force Resolve: {menuPath}");
                executed = true;
                break;
            }
        }

        // 若菜单无效，尝试反射调用 Google.JarResolver.PlayServicesResolver.ForceResolve()
        if (!executed)
        {
            try
            {
                var resolverType = System.Type.GetType("Google.JarResolver.PlayServicesResolver, Google.JarResolver");
                if (resolverType != null)
                {
                    var method = resolverType.GetMethod("ForceResolve", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        method.Invoke(null, null);
                        Debug.Log("已通过反射执行 ForceResolve。");
                        executed = true;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"反射调用 ForceResolve 失败: {e.Message}");
            }
        }

        if (!executed)
        {
            Debug.LogWarning("无法自动执行 Force Resolve，请手动通过菜单或窗口执行。");
        }
    }
}