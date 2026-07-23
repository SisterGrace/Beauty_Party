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
        private const string HOME_PANEL_NAME = "Home";
        private const int REQUIRED_STABLE_CHECKS = 3;   // 连续稳定检测3次（每次2秒，共6秒）

        private static CancellationTokenSource _cts;
        private static bool _isChecking = false;

        private static string _currentVideoUrl = "";
        private static bool _hasPlayedAudio = false;
        private static MediaPlayer _currentPlayer = null;
        private static string _currentPanelName = "";

        private static Stack<string> _pageStack = new Stack<string>();
        private static MediaPlayer _homePlayer = null;
        private static string _homeVideoUrl = "";

        // ==================== 公共 API ====================

        public static void RegisterHome(MediaPlayer homePlayer, string homeVideoUrl)
        {
            ResetAllState();
            _homePlayer = homePlayer;
            _homeVideoUrl = homeVideoUrl;
            Log.Info($"[VideoAudioHelper] Home已注册");
        }

        public static void SwitchToPage(string newPanelName)
        {
            if (string.IsNullOrEmpty(newPanelName)) return;
            if (_currentPanelName == newPanelName) return;

            if (!string.IsNullOrEmpty(_currentPanelName) && _currentPanelName != HOME_PANEL_NAME)
            {
                _pageStack.Push(_currentPanelName);
                Log.Info($"[VideoAudioHelper] 压栈: {_currentPanelName}，栈深度: {_pageStack.Count}");
            }

            if (_isChecking)
                StopCurrentCheck(restoreBGM: false);

            _currentPanelName = newPanelName;
            Log.Info($"[VideoAudioHelper] 切换到页面: {newPanelName}");

            if (newPanelName == HOME_PANEL_NAME && _pageStack.Count == 0)
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

            _hasPlayedAudio = false;
            _isChecking = false;

            if (_currentVideoUrl != videoUrl)
            {
                Log.Info($"[VideoAudioHelper] 切换视频: {videoUrl}");
                _currentPlayer?.Control?.MuteAudio(true);
                _currentVideoUrl = videoUrl;
            }

            mp.Control?.MuteAudio(true);
            Log.Info($"[VideoAudioHelper] 视频静音，开始检测");

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

            if (panelName != HOME_PANEL_NAME)
            {
                // 弹出栈顶
                if (_pageStack.Count > 0 && _pageStack.Peek() == panelName)
                    _pageStack.Pop();
                else
                {
                    // 保险移除
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
                    _currentPanelName = HOME_PANEL_NAME;
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
                Log.Warning($"[VideoAudioHelper] Home未注册，无法启动检测");
                return;
            }
            Log.Info($"[VideoAudioHelper] 启动Home检测");
            StartVideoAudioCheck(_homePlayer, _homeVideoUrl, HOME_PANEL_NAME);
        }

        // ==================== 异步检测 + 切换后监控 ====================

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
                    Log.Info($"[VideoAudioHelper] 视频无音轨，保持BGM");
                    if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                        AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                    _isChecking = false;
                    return;
                }

                // 有音轨，开始稳定检测
                double videoDuration = mp.Info?.GetDuration() ?? 0;
                int totalTimeoutSeconds = Math.Max(30, (int)videoDuration + 10);
                int stableChecks = 0;
                int elapsedSeconds = 0;

                if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                    AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);

                // ---- 稳定检测循环 ----
                while (_isChecking)
                {
                    await UniTask.Delay(2000, cancellationToken: token);
                    if (!_isChecking) return;

                    elapsedSeconds += 2;
                    if (elapsedSeconds > totalTimeoutSeconds)
                    {
                        Log.Warning($"[VideoAudioHelper] 检测超时，放弃");
                        mp.Control?.MuteAudio(true);
                        if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                            AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                        _isChecking = false;
                        return;
                    }

                    if (mp.Control != null && mp.Control.IsBuffering())
                    {
                        Log.Info($"[VideoAudioHelper] 缓冲中，重置稳定计数");
                        stableChecks = 0;
                        continue;
                    }

                    if (mp.Control == null || !mp.Control.IsPlaying())
                    {
                        Log.Warning($"[VideoAudioHelper] 视频停止");
                        mp.Control?.MuteAudio(true);
                        if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                            AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                        _isChecking = false;
                        return;
                    }

                    stableChecks++;
                    Log.Info($"[VideoAudioHelper] 稳定检测 {stableChecks}/{REQUIRED_STABLE_CHECKS}");

                    if (stableChecks >= REQUIRED_STABLE_CHECKS)
                    {
                        // ---------- 切换至视频音效 ----------
                        Log.Info($"[VideoAudioHelper] 切换至视频音效");
                        AudioManager.Stop(TK_Engine.AudioType.Music, true);
                        mp.Control?.MuteAudio(false);
                        _hasPlayedAudio = true;

                        // ---------- 进入监控循环（容错） ----------
                        while (_isChecking)
                        {
                            await UniTask.Delay(1000, cancellationToken: token);
                            if (!_isChecking) return;

                            // 检测缓冲或停止
                            if (mp.Control == null || !mp.Control.IsPlaying() || mp.Control.IsBuffering())
                            {
                                Log.Warning($"[VideoAudioHelper] 视频播放异常（缓冲/停止），恢复BGM并静音视频");
                                mp.Control?.MuteAudio(true);
                                if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                                    AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                                _isChecking = false;
                                return;
                            }
                            // 若视频正常，继续监控
                        }
                        // 监控退出（_isChecking被外部置false）
                        Log.Info($"[VideoAudioHelper] 监控结束");
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