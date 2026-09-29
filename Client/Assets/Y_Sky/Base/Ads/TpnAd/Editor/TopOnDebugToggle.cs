using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class TopOnDebugToggle
{
    private const string MenuItemName = "Tools/TopOn Debug";
    private const string XmlFileName = "DebuggerUIDependencies.xml";
    private const string MacroSymbol = "TOPON_TEST";

    private static float _waitTimer = 0f;
    private static bool _isWaiting = false;
    private static bool _pendingMacroState = false;

    [MenuItem(MenuItemName)]
    private static void ToggleDebug()
    {
        if (_isWaiting)
        {
            EditorApplication.update -= WaitAndSetMacro;
            _isWaiting = false;
        }

        bool isEnabled = EditorPrefs.GetBool("TopOnDebugEnabled", false);
        isEnabled = !isEnabled;
        EditorPrefs.SetBool("TopOnDebugEnabled", isEnabled);

        SetDebugState(isEnabled);
        bool forceCalled = ForceResolve();

        _pendingMacroState = isEnabled;
        _waitTimer = 10f;
        _isWaiting = true;
        EditorApplication.update += WaitAndSetMacro;
        Debug.Log($"已触发 Force Resolve，等待 10 秒后修改宏...");
    }

    private static void WaitAndSetMacro()
    {
        _waitTimer -= Time.deltaTime;
        if (_waitTimer <= 0f)
        {
            EditorApplication.update -= WaitAndSetMacro;
            _isWaiting = false;
            SetMacro(_pendingMacroState);
            Debug.Log($"延迟 10 秒结束，宏已{(_pendingMacroState ? "添加" : "移除")} {MacroSymbol}");
        }
    }

    [MenuItem(MenuItemName, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuItemName, EditorPrefs.GetBool("TopOnDebugEnabled", false));
        return true;
    }

    // ---------- 修改 XML（最彻底：提取核心内容） ----------
    private static void SetDebugState(bool enabled)
    {
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

        // 用正则提取 <dependencies> ... </dependencies> 核心内容（忽略注释和前缀）
        string pattern = @"<dependencies\b[^>]*>(.*?)</dependencies>";
        Match match = Regex.Match(content, pattern, RegexOptions.Singleline);
        string SkyContent = match.Success ? match.Value : null;

        if (string.IsNullOrEmpty(SkyContent))
        {
            Debug.LogError("无法提取 <dependencies> 核心内容，文件可能已损坏。");
            return;
        }

        // 根据状态决定是否注释
        if (enabled)
        {
            // 开启：直接写核心内容（不加注释）
            File.WriteAllText(fullPath, SkyContent);
            Debug.Log("TopOn Debug 已开启：取消注释 XML。");
        }
        else
        {
            // 关闭：用注释包装核心内容
            File.WriteAllText(fullPath, "<!--" + SkyContent + "-->");
            Debug.Log("TopOn Debug 已关闭：注释 XML。");
        }

        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();
    }

    // ---------- Force Resolve ----------
    private static bool ForceResolve()
    {
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
        return executed;
    }

    // ---------- 宏定义管理 ----------
    private static void SetMacro(bool enabled)
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        var symbolsList = currentSymbols.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        bool changed = false;
        if (enabled)
        {
            if (!symbolsList.Contains(MacroSymbol))
            {
                symbolsList.Add(MacroSymbol);
                changed = true;
            }
        }
        else
        {
            if (symbolsList.Contains(MacroSymbol))
            {
                symbolsList.Remove(MacroSymbol);
                changed = true;
            }
        }

        if (changed)
        {
            string newSymbols = string.Join(";", symbolsList);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
            Debug.Log($"宏定义已更新：{(enabled ? "添加" : "移除")} {MacroSymbol}");
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log($"宏定义 {MacroSymbol} 状态未变，无需操作。");
        }
    }
}