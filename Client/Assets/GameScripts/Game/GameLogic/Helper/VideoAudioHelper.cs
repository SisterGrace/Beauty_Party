using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using TK_Engine;
using UnityEngine;

namespace GameLogic
{
    public static class VideoAudioHelper
    {
        // 卡顿检测开关（默认开启）
        public static bool EnableStallDetection = false;

        private static CancellationTokenSource _cts;
        private static bool _isChecking = false;

        private static string _currentVideoUrl = "";
        private static int _stallCount = 0;
        private static float _lastPlayTime = 0;
        private static bool _hasPlayedAudio = false;
        private static MediaPlayer _currentPlayer = null;
        private static string _currentPanelName = "";

        private const string BGM_AUDIO_NAME = AudioDef.BGM1;
        private const int MAX_STALL_COUNT = 5;

        public static void StartVideoAudioCheck(MediaPlayer mp, string videoUrl, string panelName)
        {
            if (mp == null) return;

            if (_currentPanelName != panelName && _isChecking)
            {
                Log.Info($"[VideoAudioHelper] 切换页面: {_currentPanelName} → {panelName}，停止旧检测并恢复BGM");
                StopCurrentCheck(restoreBGM: true);
            }

            _currentPanelName = panelName;
            _hasPlayedAudio = false;
            _lastPlayTime = 0;
            _isChecking = false;

            if (_currentVideoUrl != videoUrl)
            {
                Log.Info($"[VideoAudioHelper] 切换视频: {videoUrl}");
                if (_currentPlayer?.Control != null)
                    _currentPlayer.Control.MuteAudio(true);
                _currentVideoUrl = videoUrl;
                _stallCount = 0;
            }

            if (mp.Control != null)
            {
                mp.Control.MuteAudio(true);
                Log.Info($"[VideoAudioHelper] 视频静音，开始检测");
            }

            _currentPlayer = mp;
            _isChecking = true;
            CheckAudioAndStallAsync(mp).Forget();
        }

        public static void StopVideoAudioCheck(string panelName, bool restoreBGM = true)
        {
            if (_currentPanelName != panelName)
            {
                Log.Info($"[VideoAudioHelper] 停止请求来自 {panelName}，当前页面 {_currentPanelName}，忽略");
                return;
            }
            StopCurrentCheck(restoreBGM);
        }

        private static void StopCurrentCheck(bool restoreBGM)
        {
            Log.Info($"[VideoAudioHelper] 停止检测，恢复BGM: {restoreBGM}");
            _isChecking = false;
            CancelCheck();

            if (_currentPlayer?.Control != null)
                _currentPlayer.Control.MuteAudio(true);

            if (restoreBGM && !AudioManager.IsPlaying(TK_Engine.AudioType.Music))
            {
                AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
            }
        }

        public static void CancelCheck()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

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
                        _lastPlayTime = (float)(mp.Control.GetCurrentTime() * 1000);
                        Log.Info($"[VideoAudioHelper] 视频开始播放，起始位置: {_lastPlayTime}ms");
                        break;
                    }
                }
                if (!_isChecking) return;

                // 检查音轨
                if (!HasAudioTrack(mp))
                {
                    Log.Info($"[VideoAudioHelper] 视频无音轨，保持BGM");
                    AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                    _isChecking = false;
                    return;
                }

                // ===== 如果卡顿检测被禁用，立即切换至视频音频 =====
                if (!EnableStallDetection)
                {
                    Log.Info($"[VideoAudioHelper] 检测已禁用，立即切换至视频音频");
                    AudioManager.Stop(TK_Engine.AudioType.Music, true);
                    mp.Control?.MuteAudio(false);
                    _hasPlayedAudio = true;
                    _isChecking = false;
                    return;
                }

                // ===== 以下为正常的卡顿检测逻辑 =====
                double videoDuration = mp.Info?.GetDuration() ?? 0;
                int maxCheckSeconds = (int)videoDuration + 1;
                int requiredStableSeconds = Math.Min(1 + _stallCount, maxCheckSeconds > 0 ? maxCheckSeconds : int.MaxValue);
                Log.Info($"[VideoAudioHelper] 需要稳定播放 {requiredStableSeconds} 秒，历史卡顿 {_stallCount} 次");

                if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                    AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);

                int stablePlayedSeconds = 0;
                bool isFirstCheck = true;

                int totalTimeoutSeconds = Math.Max(30, (int)videoDuration + 10);
                int elapsedSeconds = 0;

                while (_isChecking)
                {
                    await UniTask.Delay(2000, cancellationToken: token);
                    if (!_isChecking) return;

                    elapsedSeconds += 2;
                    if (elapsedSeconds > totalTimeoutSeconds)
                    {
                        Log.Warning($"[VideoAudioHelper] 检测超时（{totalTimeoutSeconds}秒），放弃检测，恢复BGM");
                        mp.Control?.MuteAudio(true);
                        if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                            AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                        _isChecking = false;
                        return;
                    }

                    if (mp.Control != null && mp.Control.IsBuffering())
                    {
                        Log.Info($"[VideoAudioHelper] 缓冲中，重置稳定计数");
                        stablePlayedSeconds = 0;
                        isFirstCheck = true;
                        continue;
                    }

                    if (mp.Control == null || !mp.Control.IsPlaying())
                    {
                        Log.Warning($"[VideoAudioHelper] 视频停止，恢复BGM");
                        mp.Control?.MuteAudio(true);
                        if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                            AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                        _isChecking = false;
                        return;
                    }

                    float currentTime = (float)(mp.Control.GetCurrentTime() * 1000);

                    if (isFirstCheck)
                    {
                        _lastPlayTime = currentTime;
                        isFirstCheck = false;
                        continue;
                    }

                    float timeDiff = currentTime - _lastPlayTime;

                    if (timeDiff < 0)
                    {
                        if (currentTime < 500 && _stallCount > 0)
                        {
                            Log.Info($"[VideoAudioHelper] 视频循环，重置卡顿计数");
                            _stallCount = 0;
                            requiredStableSeconds = Math.Min(1, maxCheckSeconds > 0 ? maxCheckSeconds : 1);
                        }
                        else
                        {
                            Log.Info($"[VideoAudioHelper] 位置倒退 {timeDiff}ms，重置检测");
                        }
                        _lastPlayTime = currentTime;
                        stablePlayedSeconds = 0;
                        isFirstCheck = true;
                        continue;
                    }

                    // 卡顿判定
                    if (timeDiff < 1800)
                    {
                        // 因为开关已在上方判断，此处一定为true，保留原有逻辑
                        _stallCount++;
                        Log.Warning($"[VideoAudioHelper] 卡顿！位置变化 {timeDiff}ms，卡顿+1 (总 {_stallCount})");

                        if (_stallCount >= MAX_STALL_COUNT)
                        {
                            Log.Warning($"[VideoAudioHelper] 卡顿次数过多 ({_stallCount})，放弃检测，恢复BGM");
                            mp.Control?.MuteAudio(true);
                            if (!AudioManager.IsPlaying(TK_Engine.AudioType.Music))
                                AudioManager.Play(TK_Engine.AudioType.Music, BGM_AUDIO_NAME, false, false);
                            _isChecking = false;
                            return;
                        }

                        requiredStableSeconds = Math.Min(1 + _stallCount, maxCheckSeconds > 0 ? maxCheckSeconds : int.MaxValue);
                        stablePlayedSeconds = 0;
                        _lastPlayTime = currentTime;
                        continue;
                    }

                    // 正常播放
                    _lastPlayTime = currentTime;
                    stablePlayedSeconds += 2;
                    Log.Info($"[VideoAudioHelper] 稳定播放 {stablePlayedSeconds}/{requiredStableSeconds}");

                    if (stablePlayedSeconds >= requiredStableSeconds)
                    {
                        if (_hasPlayedAudio)
                        {
                            _isChecking = false;
                            return;
                        }

                        Log.Info($"[VideoAudioHelper] 切换至视频音频");
                        AudioManager.Stop(TK_Engine.AudioType.Music, true);
                        mp.Control?.MuteAudio(false);
                        _hasPlayedAudio = true;
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

        private static bool HasAudioTrack(MediaPlayer mp)
        {
            return mp?.Info != null && mp.Info.HasAudio();
        }
    }
}