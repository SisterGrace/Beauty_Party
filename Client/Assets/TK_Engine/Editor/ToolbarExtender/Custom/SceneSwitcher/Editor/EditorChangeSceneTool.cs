using System.ComponentModel.Design.Serialization;
using UnityEditor.SceneManagement;
using UnityEditor;

public class EditorChangeSceneTool
{
    public const string INIT_SCENE_PATH = "Assets/Scenes/Main.unity";
    public const string GAME_SCENE_PATH = "Assets/AssetRaw/Scenes/Game.unity";
    public const string LEVELEDITOR_SCENE_PATH = "Assets/AssetRaw/LevelEditor/LevelEditor.unity";

    [MenuItem("跳转场景/游戏启动场景(Main)", false, 1)] static void SelectInitScene() => EditorSceneManager.OpenScene(INIT_SCENE_PATH);
    [MenuItem("跳转场景/游戏运行场景(GameScene)", false, 2)] static void SelectGameScene() => EditorSceneManager.OpenScene(GAME_SCENE_PATH);
    [MenuItem("跳转场景/关卡编辑器(LevelEditor)",false,3)] static void SelecltLevelEditorScene() => EditorSceneManager.OpenScene(LEVELEDITOR_SCENE_PATH);
}
