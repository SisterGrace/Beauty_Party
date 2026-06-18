using System.Collections.Generic;
using System.Reflection;
using Y_Base;
using GameLogic;
using TK_Engine;
using UnityEngine;

/// <summary>
/// 游戏App。
/// </summary>
public partial class GameApp:Singleton<GameApp>
{
    private static List<Assembly> _hotfixAssembly;
    public int IS_FIRSR_OPEN_GAME = 0;
    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        /*//正式包关闭Log输出
        Debug.unityLogger.logEnabled =false;*/
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Log.Warning("======= Entrance GameApp =======");
        Instance.Active();
        Instance.Start();
        Utility.Unity.AddUpdateListener(Instance.Update);
        Utility.Unity.AddFixedUpdateListener(Instance.FixedUpdate);
        Utility.Unity.AddLateUpdateListener(Instance.LateUpdate);
        Utility.Unity.AddDestroyListener(Instance.OnDestroy);
        Utility.Unity.AddOnDrawGizmosListener(Instance.OnDrawGizmos);
        Utility.Unity.AddOnApplicationPauseListener(Instance.OnApplicationPause);
        GameModule.Procedure.RestartProcedure(new GameLogic.OnEnterGameAppProcedure());
        Instance.StartGameLogic();
    }

    /// <summary>
    /// 开始游戏业务层逻辑。
    /// <remarks>显示UI、加载场景等。</remarks>
    /// </summary>
    private void StartGameLogic()
    {
        //开启游戏数据加载
        LoadingSystem.Instance.StartLoading();
        //获取服务器时间
        TimeUtil.GetServerTime();
        if (!PlayerPrefs.HasKey("IS_FIRSR_OPEN_GAME"))
        {
            PlayerPrefs.SetInt("IS_FIRSR_OPEN_GAME",0);
            PlayerPrefs.Save();
        }
        IS_FIRSR_OPEN_GAME = PlayerPrefs.GetInt("IS_FIRSR_OPEN_GAME");
    }
    
    /// <summary>
    /// 关闭游戏。
    /// </summary>
    /// <param name="shutdownType">关闭游戏框架类型。</param>
    public static void Shutdown(ShutdownType shutdownType)
    {
        Log.Info("GameApp Shutdown");
        if (PlayerPrefs.HasKey("IS_FIRSR_OPEN_GAME")&&Instance.IS_FIRSR_OPEN_GAME==0)
        {
            PlayerPrefs.SetInt("IS_FIRSR_OPEN_GAME",1);
            PlayerPrefs.Save();
        }
        if (shutdownType == ShutdownType.None)
        {
            return;
        }

        if (shutdownType == ShutdownType.Restart)
        {
            Utility.Unity.RemoveUpdateListener(Instance.Update);
            Utility.Unity.RemoveFixedUpdateListener(Instance.FixedUpdate);
            Utility.Unity.RemoveLateUpdateListener(Instance.LateUpdate);
            Utility.Unity.RemoveDestroyListener(Instance.OnDestroy);
            Utility.Unity.RemoveOnDrawGizmosListener(Instance.OnDrawGizmos);
            Utility.Unity.RemoveOnApplicationPauseListener(Instance.OnApplicationPause);
        }
        
        SingletonSystem.Release();
    }

    private void Start()
    {
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            logic.OnStart();
        }
    }

    private void Update()
    {
        TProfiler.BeginFirstSample("Update");
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            TProfiler.BeginSample(logic.GetType().FullName);
            logic.OnUpdate();
            TProfiler.EndSample();
        }
        TProfiler.EndFirstSample();
    }

    private void FixedUpdate()
    {
        TProfiler.BeginFirstSample("FixedUpdate");
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            TProfiler.BeginSample(logic.GetType().FullName);
            logic.OnFixedUpdate();
            TProfiler.EndSample();
        }
        TProfiler.EndFirstSample();
    }

    private void LateUpdate()
    {
        TProfiler.BeginFirstSample("LateUpdate");
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            TProfiler.BeginSample(logic.GetType().FullName);
            logic.OnLateUpdate();
            TProfiler.EndSample();
        }
        TProfiler.EndFirstSample();
    }

    private void OnDestroy()
    {
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            logic.OnDestroy();
        }
        Shutdown(ShutdownType.Restart);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            logic.OnDrawGizmos();
        }
#endif
    }

    private void OnApplicationPause(bool isPause)
    {
        var listLogic = _listLogicMgr;
        var logicCnt = listLogic.Count;
        for (int i = 0; i < logicCnt; i++)
        {
            var logic = listLogic[i];
            logic.OnApplicationPause(isPause);
        }
    }
}