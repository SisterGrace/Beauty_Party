using System.IO;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

public class TK_EngineSettingsProvider : SettingsProvider
{
    const string k_SettingsPath = "Assets/TK_Engine/ResRaw/Resources/TK_EngineGlobalSettings.asset";
    private const string headerName = "TK_Engine/TK_EngineSettings";
    private SerializedObject m_CustomSettings;

    internal static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(SettingsUtils.GlobalSettings);
    }

    public static bool IsSettingsAvailable()
    {
        return File.Exists(k_SettingsPath);
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        base.OnActivate(searchContext, rootElement);
        m_CustomSettings = GetSerializedSettings();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        SaveAssetData(k_SettingsPath);
    }
    
    void SaveAssetData(string path)
    {
        TK_EngineSettings old = AssetDatabase.LoadAssetAtPath<TK_EngineSettings>(k_SettingsPath);
        TK_EngineSettings data = ScriptableObject.CreateInstance<TK_EngineSettings>();
        data.Set(old.FrameworkGlobalSettings, old.BybridCLRCustomGlobalSettings);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(data, path);              
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    public override void OnGUI(string searchContext)
    {
        base.OnGUI(searchContext);
        using var changeCheckScope = new EditorGUI.ChangeCheckScope();
        EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_FrameworkGlobalSettings"));

        if (GUILayout.Button("Refresh HotUpdateAssemblies"))
        {
            SyncAssemblyContent.RefreshAssembly();
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
            m_CustomSettings = null;
            m_CustomSettings = GetSerializedSettings();
        }

        EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_HybridCLRCustomGlobalSettings"));
        EditorGUILayout.Space(20);
        if (!changeCheckScope.changed)
        {
            return;
        }
        m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    public TK_EngineSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
    {
    }

    [SettingsProvider]
    private static SettingsProvider CreateSettingProvider()
    {
        if (IsSettingsAvailable())
        {
            var provider = new TK_EngineSettingsProvider(headerName, SettingsScope.Project);
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<TK_EngineSettings>();
            return provider;
        }
        else
        {
            Debug.LogError($"Open TK_Engine Settings error,Please Create TK_Engine TK_EngineGlobalSettings.assets File in Path TK_Engine/ResRaw/Resources/");
        }

        return null;
    }
}