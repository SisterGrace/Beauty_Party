using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Y_Base;
using UnityEngine;
using YooAsset;
using UnityEngine.SceneManagement;
using AudioType = TK_Engine.AudioType;
using DG.Tweening;
using TK_Engine;
using Utility = TK_Engine.Utility;
using Debug = UnityEngine.Debug;

namespace GameLogic
{
    public class LoadingSystem : Singleton<LoadingSystem>
    {
        private SceneHandle _handle;
        public LoadType loadType;
        private Action<LoadType> _updateCb;
        private Action _finishCb;//加载完成回调
        private bool _isSDKInited;
        private bool _isMMOInited;
        private bool _loginIn;
        private bool _loadUserDataIn;
        private bool _loadGameDataIn;
        //是否开启关卡编辑器
        public bool OpenLevelEditor = false;
        
        //不同模块加载时间
        private Stopwatch _timeSp;
        public void SetUpdateCb(Action<LoadType> cb)
        {
            _updateCb = null;
            _updateCb = cb;
        }
        /// <summary>
        /// 注册加载完成回调
        /// </summary>
        /// <param name="cb"></param>
        public void SetFinishCb(Action cb)
        {
            _finishCb = null;
            _finishCb = cb;
        }
        /// <summary>
        /// 调用加载完成回调
        /// </summary>
        public static void LoadCompleteCall()
        {
            Instance._finishCb?.Invoke();
        }
        public void StartLoading()
        {
            loadType = LoadType.Config;
            //播放背景音效
            AudioManager.Play(AudioType.Music,AudioDef.BGM1,false,true);
            Utility.Unity.AddUpdateListener(LoadingUpdate); //注册帧事件
            _timeSp = new Stopwatch();
        }

        private void LoadingUpdate()
        {
            if(loadType == LoadType.None) return;
            switch (loadType)
            {
                case LoadType.Config:
                    LoadConfig();
                    Log.Warning("加载配置文件");
                    GameModule.UI.ShowUI<Panel_Loading>();
                    CheckNet();
                    break;
                case LoadType.InitSDK:
                    InitSDK();
                    Log.Warning("初始化SDK");
                    break;
                case LoadType.Login:
                    Login();
                    break;
                case LoadType.UserData:
                    LoadUserData();
                    break;
                case LoadType.GameData:
                    LoadGameData();
                    Log.Warning("加载游戏数据");
                    break;
                case LoadType.LoadScene:
                    LoadScene();
                    Log.Warning("加载场景");
                    break;
                case LoadType.ResetGame :
                    ResetGame();
                    break;
                case LoadType.NotNet:
                    CheckNet();
                    break;
            }
        }

        private void LoadConfig()
        {
            //加载配置
            ConfigLoader.Instance.Load();
        }
        private void InitSDK()
        {
            if (!_isSDKInited)
            {
                _isSDKInited = true;
                _timeSp.Start();
                //这里不去依赖SDK初始化，直接等三秒，模拟SDK初始化耗时
                //初始化SDK
                SDKManager.Instance.Init(() =>
                {
                    _timeSp.Stop();
                    Log.Info($"初始SDK耗时：{_timeSp.ElapsedMilliseconds}ms");
                    _updateCb?.Invoke(loadType);
                    loadType = LoadType.Login;
                });
            }
          
        }
        private void Login()
        {
            if (!_loginIn)
            {
                _loginIn = true;
                _timeSp.Restart();
                //此SDKManager是对后续阶段接入SDK的时候，所有SDK的逻辑均在这里面调用
                SDKManager.Instance.Login();
            }

            if (SDKManager.Instance.IsLogin)
            {
                _timeSp.Stop();
                Log.Info($"初始登录耗时：{_timeSp.ElapsedMilliseconds}ms");
                _updateCb?.Invoke(loadType);
                if (SDKManager.Instance.IsLoginSuccess)
                {
                    loadType = LoadType.UserData;
                }
                else
                {
                    loadType = LoadType.GameData;
                    Debug.LogError("登录失败或者为游客登录");
                }
            }
        }
        private void LoadUserData()
        {
            if (!_loadUserDataIn)
            {
                _timeSp.Restart();
                _loadUserDataIn = true;
                //这里是从云端加载用户数据，demo阶段先暂时不用管
                //CloudStorageSystem.Instance.StartGetUserData();
                UserController.Instance.InitUser();
                //判断是否买量用户
                var isPromote = SDKManager.Instance.IsPromote;
                _timeSp.Stop();
                Log.Info($"初始用户数据耗时：{_timeSp.ElapsedMilliseconds}ms");
                _updateCb?.Invoke(loadType);
                loadType = LoadType.GameData;
            }
          
        }
        private async UniTask LoadGameData()
        {
            if (!_loadGameDataIn)
            {
                
                _loadGameDataIn = true;
                _timeSp.Restart();
#if UNITY_EDITOR
                OpenLevelEditor = UnityEditor.EditorPrefs.GetBool("OpenLevelEditor", false);
#else
                OpenLevelEditor = false;
#endif
                if (!OpenLevelEditor)
                {
                    LevelController.Instance.InitLevel();
                    await PhotoController.Instance.InitPhoto();
                }
                _timeSp.Stop();
                Log.Info($"初始游戏数据耗时：{_timeSp.ElapsedMilliseconds}ms");
                _updateCb?.Invoke(loadType);
                loadType = LoadType.LoadScene;
            }
           
        }

        private void LoadScene()
        {
            if (_handle == null)
            {
                _timeSp.Restart();
                //_handle = GameModule.Resource.LoadSceneAsync("Game", LoadSceneMode.Single, false);
                if (!OpenLevelEditor)
                {
                    _handle = GameModule.Scene.LoadScene("Game", LoadSceneMode.Single, false);
                }
                else
                {
                    #if UNITY_EDITOR
                    OpenLevelEditor = true;
                    _handle = GameModule.Scene.LoadScene("LevelEditor", LoadSceneMode.Single, false);
                    UnityEditor.EditorPrefs.SetBool("OpenLevelEditor", false);
                    #endif
                   
                }
               
            }

            if (_handle.IsDone)
            {
                _timeSp.Stop();
                Log.Info($"加载游戏场景耗时：{_timeSp.ElapsedMilliseconds}ms");
                _updateCb?.Invoke(loadType);
                loadType = LoadType.None;
            }
        }

        private void ResetGame()
        {
            _loginIn = false;
            _loadUserDataIn = false;
            _loadGameDataIn = false;
            _isSDKInited = false;
        }
        /// <summary>
        /// 检测网络状态
        /// </summary>
        public void CheckNet()
        {
            //无网络状态
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if(loadType == LoadType.NotNet) return;
                loadType = LoadType.NotNet;
                GameEvent.Send(EventType.SetLoadNetTips,true);
            }
            else
            {
                loadType = LoadType.InitSDK;
                GameEvent.Send(EventType.SetLoadNetTips,false);
            }
        }
    }

    public enum LoadType
    {
        None=0,
        Config,
        Login,
        UserData,
        GameData,
        LoadScene,
        ResetGame,
        InitSDK,
        NotNet,//无网络
    }
}
