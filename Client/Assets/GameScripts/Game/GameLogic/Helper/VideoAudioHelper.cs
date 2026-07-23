using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using TK_Engine;
using UnityEngine;

namespace GameLogic
{
    public static class VideoAudioHelper
    {
        private const string BGM_AUDIO_NAME = AudioDef.BGM1;

        private static CancellationTokenSource _cts;
        private static bool _isChecking = false;

        private static string _currentVideoUrl = "";
        private static bool _hasPlayedAudio = false;
        private static MediaPlayer _currentPlayer = null;
        private static string _currentPanelName = "";

        private static Stack<string> _pageStack = new Stack<string>();
        private static MediaPlayer _homePlayer = null;
        private static string _homeVideoUrl = "";
        private static string _homePanelName = "Home";

        // ==================== 公共 API ====================

        public static void RegisterHome(MediaPlayer homePlayer, string homeVideoUrl, string panelName)
        {
            ResetAllState();
            _homePlayer = homePlayer;
            _homeVideoUrl = homeVideoUrl ?? "";

            if (!string.IsNullOrEmpty(panelName))
            {
                _homePanelName = panelName;
                _currentPanelName = panelName; // 关键：设置当前页面为 Home
            }

            Log.Info($"[VideoAudioHelper] Home已注册，名称: {_homePanelName}");
        }

        public static void SwitchToPage(string newPanelName)
        {
            if (string.IsNullOrEmpty(newPanelName)) return;
            if (_currentPanelName == newPanelName) return;

            // 如果当前页面不是 Home，且栈中已有该页面，则先移除（保证唯一）
            if (!string.IsNullOrEmpty(_currentPanelName) && _currentPanelName != _homePanelName)
            {
                // 如果栈中已包含当前页面，移除它（实际上应该只有一个）
                if (_pageStack.Contains(_currentPanelName))
                {
                    var temp = new Stack<string>();
                    while (_pageStack.Count > 0)
                    {
                        var top = _pageStack.Pop();
                        if (top != _currentPanelName)
                            temp.Push(top);
                    }
                    while (temp.Count > 0)
                        _pageStack.Push(temp.Pop());
                }
                _pageStack.Push(_currentPanelName);
                Log.Info($"[VideoAudioHelper] 压栈: {_currentPanelName}，栈深度: {_pageStack.Count}");
            }

            // 停止旧检测（但不恢复BGM）
            if (_isChecking)
                StopCurrentCheck(restoreBGM: false);

            // 更新当前页面名
            _currentPanelName = newPanelName;
            Log.Info($"[VideoAudioHelper] 切换到页面: {newPanelName}");

            // 如果切换到 Home 且栈为空，启动 Home 检测
            if (newPanelName == _homePanelName && _pageStack.Count == 0)
                StartHomeDetection();
        }

        public static void StartVideoAudioCheck(MediaPlayer mp, string videoUrl, string panelName)
        {
            if (mp == null) return;

            // 非顶层页面静音并忽略
            if (panelName != _currentPanelName)
            {
                Log.Info($"[VideoAudioHelper] 忽略启动请求：当前顶层 {_currentPanelName}，请求来自 {panelName}，静音视频");
                mp.Control?.MuteAudio(true);
                return;
            }

            // 确保 BGM 播放（检测期间保持）
            if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);

            // 重置状态
            _hasPlayedAudio = false;
            _isChecking = false;

            if (_currentVideoUrl != videoUrl)
            {
                Log.Info($"[VideoAudioHelper] 切换视频: {videoUrl}");
                _currentPlayer?.Control?.MuteAudio(true);
                _currentVideoUrl = videoUrl;
            }

            // ★ 先静音视频，直到确认有音轨再取消静音
            mp.Control?.MuteAudio(true);
            Log.Info($"[VideoAudioHelper] 视频已静音，开始检测");

            _currentPlayer = mp;
            _isChecking = true;
            CheckAudioAndStallAsync(mp).Forget();
        }

        public static void StopVideoAudioCheck(string panelName, bool restoreBGM = true)
        {
            if (panelName != _currentPanelName)
            {
                Log.Info($"[VideoAudioHelper] 停止请求来自 {panelName}，当前页面 {_currentPanelName}，忽略");
                return;
            }

            StopCurrentCheck(restoreBGM);

            if (panelName != _homePanelName)
            {
                if (_pageStack.Count > 0 && _pageStack.Peek() == panelName)
                    _pageStack.Pop();
                else
                {
                    var temp = new Stack<string>();
                    while (_pageStack.Count > 0)
                    {
                        var top = _pageStack.Pop();
                        if (top != panelName) temp.Push(top);
                    }
                    while (temp.Count > 0) _pageStack.Push(temp.Pop());
                }

                if (_pageStack.Count == 0)
                {
                    Log.Info($"[VideoAudioHelper] 所有非Home页面已关闭，自动恢复Home");
                    _currentPanelName = _homePanelName;
                    StartHomeDetection();
                }
                else
                {
                    _currentPanelName = _pageStack.Peek();
                    Log.Info($"[VideoAudioHelper] 回退到页面: {_currentPanelName}");
                    if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                        AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                }
            }
            else
            {
                Log.Warning($"[VideoAudioHelper] 尝试关闭Home页面，忽略");
            }
        }

        public static void CancelCheck()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // ==================== 内部方法 ====================

        private static void ResetAllState()
        {
            _isChecking = false;
            CancelCheck();
            _currentPlayer?.Control?.MuteAudio(true);
            _currentPlayer = null;
            _currentVideoUrl = "";
            _hasPlayedAudio = false;
            _currentPanelName = "";
            _pageStack.Clear();
            // 不重置 _homePlayer、_homeVideoUrl、_homePanelName
        }

        private static void StopCurrentCheck(bool restoreBGM)
        {
            Log.Info($"[VideoAudioHelper] 停止检测，恢复BGM: {restoreBGM}");
            _isChecking = false;
            CancelCheck();
            _currentPlayer?.Control?.MuteAudio(true);
            if (restoreBGM && !AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
        }

        private static void StartHomeDetection()
        {
            if (_homePlayer == null || string.IsNullOrEmpty(_homeVideoUrl))
            {
                Log.Warning($"[VideoAudioHelper] Home未注册或URL为空，无法启动检测");
                return;
            }
            Log.Info($"[VideoAudioHelper] 启动Home检测");
            StartVideoAudioCheck(_homePlayer, _homeVideoUrl, _homePanelName);
        }

        // ==================== 异步检测 ====================

        private static async UniTaskVoid CheckAudioAndStallAsync(MediaPlayer mp)
        {
            CancelCheck();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                Log.Info($"[VideoAudioHelper] 等待视频开始播放...");
                while (_isChecking)
                {
                    await UniTask.Delay(100, cancellationToken: token);
                    if (mp.Control != null && mp.Control.IsPlaying() && !mp.Control.IsBuffering())
                    {
                        Log.Info($"[VideoAudioHelper] 视频开始播放");
                        break;
                    }
                }
                if (!_isChecking) return;

                // 检查音轨
                if (!HasAudioTrack(mp))
                {
                    Log.Info($"[VideoAudioHelper] 视频无音轨，保持BGM，静音视频");
                    // 视频已静音，无需额外操作，保持静音即可
                    _isChecking = false;
                    return;
                }

                // ---- 有音轨，立即切换至视频音效 ----
                Log.Info($"[VideoAudioHelper] 立即切换至视频音效");
                AudioManager.Stop(TK_Engine.AudioType.Music, true);   // 停止BGM
                mp.Control?.MuteAudio(false);                          // 取消静音
                _hasPlayedAudio = true;

                // ---- 监控循环（检测卡顿/缓冲/停止） ----
                while (_isChecking)
                {
                    await UniTask.Delay(1000, cancellationToken: token);
                    if (!_isChecking) return;

                    if (mp.Control == null || !mp.Control.IsPlaying() || mp.Control.IsBuffering())
                    {
                        Log.Warning($"[VideoAudioHelper] 视频播放异常（缓冲/停止），恢复BGM并静音视频");
                        mp.Control?.MuteAudio(true);
                        if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                            AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                        _isChecking = false;
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info($"[VideoAudioHelper] 检测被取消");
            }
            catch (Exception ex)
            {
                Log.Error($"[VideoAudioHelper] 异常: {ex.Message}");
                mp.Control?.MuteAudio(true);
                if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                    AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                _isChecking = false;
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        private static bool HasAudioTrack(MediaPlayer mp) => mp?.Info != null && mp.Info.HasAudio();
    }
}