using UnityEditor;

public static class SettingsMenu
{
    [MenuItem("TK_Engine/Settings/TK_EngineSettings", priority = -1)]
    public static void OpenSettings() => SettingsService.OpenProjectSettings("TK_Engine/TK_EngineSettings");
}