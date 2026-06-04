using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 场景快速启动/停止
/// </summary>
public static class SceneQuickLauncher
{
    // 配置常量
    private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
    private const float KEY_COOLDOWN = 0.5f;

    // 状态变量
    private static double _lastExecuteTime;

    [MenuItem("Tools/TogglePlayMode &s")]
    static void TogglePlayMode()
    {
        // 防抖处理
        if (EditorApplication.timeSinceStartup - _lastExecuteTime < KEY_COOLDOWN)
            return;
        _lastExecuteTime = EditorApplication.timeSinceStartup;

        // 如果已经在播放模式，停止游戏
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        // 如果即将进入播放模式，忽略（防止重复触发）
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // 编辑器模式下启动流程
        LaunchMainScene();
    }

    static void LaunchMainScene()
    {
        // 场景路径验证
        if (!ValidateScenePath(MAIN_SCENE_PATH))
        {
            Debug.LogError($"场景路径无效: {MAIN_SCENE_PATH}");
            EditorUtility.DisplayDialog("错误", $"主场景路径无效\n{MAIN_SCENE_PATH}", "确定");
            return;
        }

        // 保存当前场景
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        try
        {
            // 直接加载场景（移除异步可能导致的延迟问题）
            EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
            // 标记启动来源并开始播放
            EditorApplication.isPlaying = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"场景加载失败: {e}");
            EditorUtility.DisplayDialog("错误", $"场景加载失败\n{e.Message}", "确定");
        }
    }

    #region 辅助方法
    // 验证场景文件是否存在
    static bool ValidateScenePath(string scenePath)
    {
        string fullPath = System.IO.Path.Combine(Application.dataPath, "../", scenePath);
        bool exists = System.IO.File.Exists(fullPath);
        return exists;
    }
    #endregion

    #region 运行时热键检测（独立模块）
    [InitializeOnLoad]
    public static class RuntimeHotkeyManager
    {
        private static bool _altPressed = false;
        private static bool _sPressed = false;
        private static double _lastRuntimePressTime = 0;

        static RuntimeHotkeyManager()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            // 只在播放模式下检测
            if (!EditorApplication.isPlaying) return;

            // 获取当前事件
            Event currentEvent = Event.current;
            if (currentEvent == null) return;

            // 检测Alt键状态
            _altPressed = (currentEvent.modifiers & EventModifiers.Alt) != 0;

            // 检测S键按下（KeyDown事件）
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.S)
            {
                _sPressed = true;
            }
            else if (currentEvent.type == EventType.KeyUp && currentEvent.keyCode == KeyCode.S)
            {
                _sPressed = false;
            }

            // 组合键触发（带防抖）
            if (_altPressed && _sPressed)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - _lastRuntimePressTime > KEY_COOLDOWN)
                {
                    _lastRuntimePressTime = currentTime;
                    EditorApplication.isPlaying = false;

                    // 重置按键状态防止连续触发
                    _altPressed = _sPressed = false;
                }
            }
        }
    }
    #endregion
}