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
        private Action _finishCb;
        private bool _isSDKInited;
        private bool _isMMOInited;
        private bool _loginIn;
        private bool _loadUserDataIn;
        private bool _loadGameDataIn;
        public bool OpenLevelEditor = false;

        // 并行状态标记
        private bool _configDownloadCompleted;
        private bool _sdkInitCompleted;
        private bool _loginCompleted;
        private bool _userDataLoaded;

        private bool _isDownloading = false;
        private bool _downloadExecuted = false;
        private bool _configLoaded = false;

        // 独立计时器，避免全局 Stopwatch 打架
        private Stopwatch _loginSw;
        private Stopwatch _sceneSw;

        public void SetUpdateCb(Action<LoadType> cb)
        {
            _updateCb = null;
            _updateCb = cb;
        }

        public void SetFinishCb(Action cb)
        {
            _finishCb = null;
            _finishCb = cb;
        }

        public static void LoadCompleteCall()
        {
            Instance._finishCb?.Invoke();
        }

        public void StartLoading()
        {
            loadType = LoadType.DownloadConfig;
            AudioManager.Play(AudioType.Music, AudioDef.BGM1, false, true);
            Utility.Unity.AddUpdateListener(LoadingUpdate);
            GameModule.UI.ShowUI<Panel_Loading>();

            // 提前启动并行任务
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                DownloadConfigAsync().Forget();
                InitSDKAsync().Forget();
            }
            else
            {
                Log.Warning("无网络，跳过配置下载和SDK初始化");
                _downloadExecuted = false;
                _configDownloadCompleted = true;
                // 无网络时不初始化 SDK，后续 CheckNet 会进入 NotNet
            }
        }

        private void LoadingUpdate()
        {
            if (loadType == LoadType.None) return;
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
                    break;
                case LoadType.Login:
                    Login();
                    break;
                case LoadType.UserData:
                    LoadUserData();
                    break;
                case LoadType.GameData:
                    LoadGameData().Forget();
                    break;
                case LoadType.LoadScene:
                    LoadScene();
                    break;
                case LoadType.ResetGame:
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
            if (_configDownloadCompleted)
            {
                loadType = LoadType.Config;
            }
            // 否则等待 DownloadConfigAsync 完成
        }

        private async UniTaskVoid DownloadConfigAsync()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await ConfigDownloader.Instance.InitializeAsync();
                await ConfigDownloader.Instance.DownloadAllAsync();
                _downloadExecuted = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadingSystem] 下载配置异常: {e.Message}");
                _downloadExecuted = false;
            }
            finally
            {
                sw.Stop();
                Log.Info($"下载配置耗时：{sw.ElapsedMilliseconds}ms");
                _configDownloadCompleted = true;
                _updateCb?.Invoke(LoadType.DownloadConfig);
            }
        }

        #endregion

        private void LoadConfig()
        {
            if (_configLoaded) return;
            _configLoaded = true;

            Log.Warning("加载配置文件");
            ConfigLoader.Instance.UseOnlineTable = _downloadExecuted;
            ConfigLoader.Instance.Load();

            _updateCb?.Invoke(loadType);
            CheckNet();
        }

        private void InitSDK()
        {
            if (_sdkInitCompleted)
            {
                loadType = LoadType.Login;
            }
            // 否则等待 InitSDKAsync 完成
        }

        private async UniTaskVoid InitSDKAsync()
        {
            var sw = Stopwatch.StartNew();
            var tcs = new UniTaskCompletionSource();
            SDKManager.Instance.Init(() =>
            {
                sw.Stop();
                Log.Info($"初始SDK耗时：{sw.ElapsedMilliseconds}ms");
                _sdkInitCompleted = true;
                _updateCb?.Invoke(LoadType.InitSDK);
                tcs.TrySetResult();
            });
            await tcs.Task;
        }

        private void Login()
        {
            if (!_loginIn)
            {
                _loginIn = true;
                _loginSw = Stopwatch.StartNew();
                SDKManager.Instance.Login();
                // 同时加载用户数据
                LoadUserDataAsync().Forget();
            }

            if (SDKManager.Instance.IsLogin && !_loginCompleted)
            {
                _loginCompleted = true;
                _loginSw?.Stop();
                Log.Info($"初始登录耗时：{_loginSw?.ElapsedMilliseconds ?? 0}ms");
                _updateCb?.Invoke(loadType);
            }

            if (_loginCompleted && _userDataLoaded)
            {
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
            if (_userDataLoaded)
            {
                loadType = LoadType.GameData;
            }
        }

        private async UniTaskVoid LoadUserDataAsync()
        {
            var sw = Stopwatch.StartNew();
            UserController.Instance.InitUser();
            var isPromote = SDKManager.Instance.IsPromote;
            sw.Stop();
            Log.Info($"初始用户数据耗时：{sw.ElapsedMilliseconds}ms");
            _userDataLoaded = true;
            _updateCb?.Invoke(LoadType.UserData);
        }

        private async UniTask LoadGameData()
        {
            if (!_loadGameDataIn)
            {
                _loadGameDataIn = true;
                Log.Info("加载游戏数据");
                var sw = Stopwatch.StartNew();

#if UNITY_EDITOR
                OpenLevelEditor = UnityEditor.EditorPrefs.GetBool("OpenLevelEditor", false);
#else
                OpenLevelEditor = false;
#endif
                if (!OpenLevelEditor)
                {
                    // 保持串行：先 InitLevel，再 await InitPhoto
                    LevelController.Instance.InitLevel();
                    await PhotoController.Instance.InitPhoto();
                }
                sw.Stop();
                Log.Info($"初始游戏数据耗时：{sw.ElapsedMilliseconds}ms");
                _updateCb?.Invoke(loadType);
                loadType = LoadType.LoadScene;
            }
        }

        private void LoadScene()
        {
            if (_handle == null)
            {
                Log.Warning("加载场景");
                _sceneSw = Stopwatch.StartNew();
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
                _sceneSw?.Stop();
                Log.Info($"加载游戏场景耗时：{_sceneSw?.ElapsedMilliseconds ?? 0}ms");
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
            _configDownloadCompleted = false;
            _sdkInitCompleted = false;
            _loginCompleted = false;
            _userDataLoaded = false;
            _loginSw = null;
            _sceneSw = null;
        }

        public void CheckNet()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (loadType == LoadType.NotNet) return;
                loadType = LoadType.NotNet;
                GameEvent.Send(EventType.SetLoadNetTips, true);
            }
            else
            {
                loadType = LoadType.InitSDK;
                GameEvent.Send(EventType.SetLoadNetTips, false);
            }
        }
    }

    public enum LoadType
    {
        None = 0,
        DownloadConfig,
        Config,
        Login,
        UserData,
        GameData,
        LoadScene,
        ResetGame,
        InitSDK,
        NotNet,
    }
}