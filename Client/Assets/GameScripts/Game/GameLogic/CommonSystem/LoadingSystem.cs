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

        // 新增：下载相关状态
        private bool _isDownloading = false;
        private bool _downloadExecuted = false; // 是否执行过下载流程
        private bool _configLoaded = false;

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
            loadType = LoadType.DownloadConfig; // 从下载配置开始
            //播放背景音效
            AudioManager.Play(AudioType.Music,AudioDef.BGM1,false,true);
            Utility.Unity.AddUpdateListener(LoadingUpdate); //注册帧事件
            _timeSp = new Stopwatch();
            // 显示Loading界面（只调用一次，不放在帧循环里）
            GameModule.UI.ShowUI<Panel_Loading>();
        }

        private void LoadingUpdate()
        {
            if(loadType == LoadType.None) return;
            switch (loadType)
            {
                case LoadType.DownloadConfig:
                    DownloadConfig();
                    break;
                case LoadType.Config:
                    LoadConfig();
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

        #region 下载配置表

        private void DownloadConfig()
        {
            if (_isDownloading) return;

            // 无网络时跳过下载，直接使用包内配置
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Log.Warning("无网络，跳过配置下载，使用包内配置");
                _downloadExecuted = false;
                loadType = LoadType.Config;
                return;
            }

            _isDownloading = true;
            DownloadConfigAsync().Forget();
        }

        private async UniTaskVoid DownloadConfigAsync()
        {
            _timeSp.Start();
            try
            {
                // 初始化下载器（异步加载CDN URL）
                await ConfigDownloader.Instance.InitializeAsync();
                // 执行全量下载
                await ConfigDownloader.Instance.DownloadAllAsync();
                _downloadExecuted = true; // 只要跑过下载流程就标记，LoadByteBuf内部会自动fallback
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadingSystem] 下载配置异常: {e.Message}");
                _downloadExecuted = false;
            }
            finally
            {
                _timeSp.Stop();
                Log.Info($"下载配置耗时：{_timeSp.ElapsedMilliseconds}ms");
                _isDownloading = false;
                loadType = LoadType.Config;
            }
        }

        #endregion

        private void LoadConfig()
        {
            if (_configLoaded) return;
            _configLoaded = true;

            Log.Warning("加载配置文件");

            // 根据下载结果设置开关：只要执行过下载流程就启用线上表
            // LoadByteBuf内部有fallback机制，文件不存在时自动回退包内
            ConfigLoader.Instance.UseOnlineTable = _downloadExecuted;

            //加载配置
            ConfigLoader.Instance.Load();

            _updateCb?.Invoke(loadType);

            // 检测网络状态决定下一步
            CheckNet();
        }
        private void InitSDK()
        {
            if (!_isSDKInited)
            {
                _isSDKInited = true;
                _timeSp.Start();
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
            _isDownloading = false;
            _downloadExecuted = false;
            _configLoaded = false;
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
        DownloadConfig, // 新增：下载配置表
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
