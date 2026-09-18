/*#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 热力引擎（SolarEngine）文件自动切换工具。
/// 根据 SOLAR_ENGINE 宏定义，自动重命名 .java 和 .xml 文件。
/// - 宏定义存在 → 启用文件（正常编译）
/// - 宏定义不存在 → 禁用文件（重命名为 .java_not / .xml_not）
/// 
/// 触发时机：
/// 1. 每次脚本编译完成后（[InitializeOnLoad]）
/// 2. Android 构建前（IPreprocessBuildWithReport）
/// </summary>
[InitializeOnLoad]
public class SolarEngineToggle : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    // ========== 文件路径（相对于 Assets 目录） ==========
    private const string JavaFilePath = "SolarEngine/Plugins/Android/SolarEngineProxy.java";
    private const string JavaFileDisabled = "SolarEngine/Plugins/Android/SolarEngineProxy.java_not";

    private const string XmlFilePath = "SolarEngine/Editor/SolarEngineDependencies.xml";
    private const string XmlFileDisabled = "SolarEngine/Editor/SolarEngineDependencies.xml_not";

    // ========== 宏定义名称 ==========
    private const string MacroName = "SOLAR_ENGINE";

    // ========== 静态构造函数：编译完成后自动执行 ==========
    static SolarEngineToggle()
    {
        // 延迟一帧执行，避免编辑器初始化时文件锁冲突
        EditorApplication.delayCall += () =>
        {
            SyncFilesWithMacro();
        };
    }

    // ========== 构建前自动触发 ==========
    public void OnPreprocessBuild(BuildReport report)
    {
        SyncFilesWithMacro();
    }

    // ========== 手动菜单项 ==========
    [MenuItem("Tools/SolarEngine/Toggle Enable/Disable")]
    public static void ToggleMenu()
    {
        bool current = IsSolarEngineEnabled();
        SetSolarEngineEnabled(!current);
        Debug.Log($"SolarEngine 已手动切换为：{(current ? "禁用" : "启用")}");
    }

    [MenuItem("Tools/SolarEngine/Status")]
    public static void ShowStatus()
    {
        bool filesEnabled = IsSolarEngineEnabled();
        bool macroDefined = IsMacroDefined();
        Debug.Log($"SolarEngine 文件状态：{(filesEnabled ? "启用" : "禁用")}，宏定义：{(macroDefined ? "已定义" : "未定义")}");
    }

    // ========== 核心同步逻辑 ==========
    private static void SyncFilesWithMacro()
    {
        bool macroDefined = IsMacroDefined();
        bool filesEnabled = IsSolarEngineEnabled();

        if (macroDefined && !filesEnabled)
        {
            SetSolarEngineEnabled(true);
            Debug.Log("[SolarEngine] 宏已启用，自动激活文件");
        }
        else if (!macroDefined && filesEnabled)
        {
            SetSolarEngineEnabled(false);
            Debug.Log("[SolarEngine] 宏已禁用，自动屏蔽文件");
        }
        // 其他情况：状态一致，不做任何操作
    }

    // ========== 检测函数 ==========

    /// <summary>
    /// 检查当前是否启用（以 .java 文件是否存在为准）
    /// </summary>
    private static bool IsSolarEngineEnabled()
    {
        string fullPath = Path.Combine(Application.dataPath, JavaFilePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// 检查是否定义了 SOLAR_ENGINE 宏
    /// </summary>
    private static bool IsMacroDefined()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup);
        return defines.Contains(MacroName);
    }

    // ========== 核心切换逻辑 ==========

    /// <summary>
    /// 启用或禁用热力引擎（重命名文件）
    /// </summary>
    /// <param name="enable">true 启用，false 禁用</param>
    private static void SetSolarEngineEnabled(bool enable)
    {
        string dataPath = Application.dataPath;

        // Java 文件
        string javaActive = Path.Combine(dataPath, JavaFilePath);
        string javaDisabled = Path.Combine(dataPath, JavaFileDisabled);

        // XML 文件
        string xmlActive = Path.Combine(dataPath, XmlFilePath);
        string xmlDisabled = Path.Combine(dataPath, XmlFileDisabled);

        try
        {
            if (enable)
            {
                // 启用：将 *_not 改回原名
                if (File.Exists(javaDisabled))
                {
                    // 如果原名已存在，先删除（避免冲突）
                    if (File.Exists(javaActive)) File.Delete(javaActive);
                    File.Move(javaDisabled, javaActive);
                }
                if (File.Exists(xmlDisabled))
                {
                    if (File.Exists(xmlActive)) File.Delete(xmlActive);
                    File.Move(xmlDisabled, xmlActive);
                }
            }
            else
            {
                // 禁用：将原名改为 *_not
                if (File.Exists(javaActive))
                {
                    if (File.Exists(javaDisabled)) File.Delete(javaDisabled);
                    File.Move(javaActive, javaDisabled);
                }
                if (File.Exists(xmlActive))
                {
                    if (File.Exists(xmlDisabled)) File.Delete(xmlDisabled);
                    File.Move(xmlActive, xmlDisabled);
                }
            }

            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SolarEngine] 文件切换失败: {e.Message}");
        }
    }
}
#endif*/
