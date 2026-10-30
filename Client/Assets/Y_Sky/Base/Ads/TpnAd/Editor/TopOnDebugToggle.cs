using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class TopOnDebugToggle
{
    private const string MenuItemName = "Tools/TopOn Debug";
    private const string XmlFileName = "DebuggerUIDependencies.xml";
    private const string MacroSymbol = "TOPON_TEST";
    private const string ResolvePendingKey = "TopOnDebugToggle.ResolvePending";

    [MenuItem(MenuItemName)]
    private static void ToggleDebug()
    {
        bool enabled = !IsDebugEnabled();
        if (!SetDebugState(enabled))
        {
            return;
        }

        // Keep the request across the domain reload caused by changing scripting symbols.
        SessionState.SetBool(ResolvePendingKey, true);
        SetMacro(enabled);
        ResumePendingResolve();
        Debug.Log($"TopOn Android Debug 已{(enabled ? "开启" : "关闭")}，等待编译和资源导入完成后解析依赖。");
    }

    [InitializeOnLoadMethod]
    private static void ResumePendingResolve()
    {
        if (SessionState.GetBool(ResolvePendingKey, false))
        {
            EditorApplication.update -= ResolveWhenReady;
            EditorApplication.update += ResolveWhenReady;
        }
    }

    private static void ResolveWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        EditorApplication.update -= ResolveWhenReady;
        SessionState.SetBool(ResolvePendingKey, false);
        ForceResolve();
    }

    [MenuItem(MenuItemName, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuItemName, IsDebugEnabled());
        return !EditorApplication.isCompiling && !EditorApplication.isUpdating;
    }

    private static bool IsDebugEnabled()
    {
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
            .Split(';').Any(symbol => symbol.Trim() == MacroSymbol);
    }

    private static bool SetDebugState(bool enabled)
    {
        string[] paths = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(XmlFileName), new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileName(path) == XmlFileName).ToArray();
        if (paths.Length != 1)
        {
            Debug.LogError($"需要唯一的 {XmlFileName}，当前找到 {paths.Length} 个。请检查 Assets 下的 TopOn 调试依赖配置。");
            return false;
        }

        string path = paths[0];
        string fullPath = Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
        try
        {
            var document = new XmlDocument();
            document.Load(fullPath);
            XmlElement root = document.DocumentElement;
            if (root == null || root.Name != "dependencies")
            {
                throw new XmlException("缺少 dependencies 根节点。");
            }

            XmlNode packages = root.SelectSingleNode("androidPackages");
            if (enabled && packages == null)
            {
                XmlComment disabledPackages = root.ChildNodes.OfType<XmlComment>()
                    .FirstOrDefault(comment => comment.Value.TrimStart().StartsWith("<androidPackages"));
                if (disabledPackages == null)
                {
                    throw new XmlException("未找到已注释的 androidPackages 调试依赖。");
                }

                XmlDocumentFragment fragment = document.CreateDocumentFragment();
                fragment.InnerXml = disabledPackages.Value;
                root.ReplaceChild(fragment, disabledPackages);
            }
            else if (!enabled && packages != null)
            {
                // Leave a valid XML root so EDM can still parse the disabled file.
                root.ReplaceChild(document.CreateComment(packages.OuterXml), packages);
            }

            document.Save(fullPath);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is XmlException)
        {
            Debug.LogError($"修改 TopOn 调试依赖失败：{exception.Message}");
            return false;
        }

        AssetDatabase.ImportAsset(path);
        return true;
    }

    private static void ForceResolve()
    {
        const string menuPath = "Assets/External Dependency Manager/Android Resolver/Force Resolve";
        if (EditorApplication.ExecuteMenuItem(menuPath))
        {
            Debug.Log("已触发 TopOn Android 依赖 Force Resolve，请等待解析完成。");
        }
        else
        {
            Debug.LogWarning($"无法自动解析 TopOn 依赖，请手动执行 {menuPath}。");
        }
    }

    private static void SetMacro(bool enabled)
    {
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
            .Split(';').Select(symbol => symbol.Trim())
            .Where(symbol => !string.IsNullOrEmpty(symbol) && symbol != MacroSymbol).ToList();
        if (enabled)
        {
            symbols.Add(MacroSymbol);
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, string.Join(";", symbols));
    }
}
