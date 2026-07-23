using System.Threading;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using TK_Engine;
using UnityEngine;

namespace GameLogic
{
	/// <summary>
	/// 视频音频统一管理工具类
	/// 负责检测视频是否有音轨，并自动控制BGM的播放/停止
	/// 支持卡顿检测和渐进式音频播放判定
	/// </summary>
	public static class VideoAudioHelper
	{
		private static bool _isChecking = false;
		private static CancellationTokenSource _cts;
		
		// 当前视频状态
		private static string _currentVideoUrl = "";      // 当前视频URL
		private static int _stallCount = 0;                // 当前视频的卡顿次数
		private static float _lastPlayTime = 0;            // 上次检测的播放位置（毫秒）
		private static bool _hasPlayedAudio = false;       // 是否已播放视频音效
		private static MediaPlayer _currentPlayer = null;  // 当前播放器

	/// <summary>
	/// 视频Started时调用，开始检测音频和卡顿
	/// </summary>
	/// <param name="mp">MediaPlayer实例</param>
	/// <param name="videoUrl">视频URL，用于识别是否换了视频</param>
	public static void StartVideoAudioCheck(MediaPlayer mp, string videoUrl)
	{
		if (mp == null) return;
		
		// 如果是新视频，重置所有状态
		if (_currentVideoUrl != videoUrl)
		{
			Log.Info($"[VideoAudioHelper] 切换视频: {videoUrl}，重置卡顿计数");
			_currentVideoUrl = videoUrl;
			_stallCount = 0;
			_hasPlayedAudio = false;
			_lastPlayTime = 0;
		}
		
		// 先静音视频，检测通过后再开启声音
		if (mp.Control != null)
		{
			mp.Control.MuteAudio(true);
			Log.Info($"[VideoAudioHelper] 视频静音，开始检测");
		}
		
		_currentPlayer = mp;
		_isChecking = true;
		CheckAudioAndStallAsync(mp).Forget();
	}

		/// <summary>
		/// 视频结束/错误/关闭时调用，停止检测并恢复BGM
		/// </summary>
		public static void StopVideoAudioCheck(bool restoreBGM = true)
		{
			_isChecking = false;
			CancelCheck();
			
			if (restoreBGM)
			{
				AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
			}
		}

		/// <summary>
		/// 取消当前检测
		/// </summary>
		public static void CancelCheck()
		{
			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}

		/// <summary>
		/// 异步检测音频和卡顿（渐进式判定）
		/// </summary>
		private static async UniTaskVoid CheckAudioAndStallAsync(MediaPlayer mp)
		{
			CancelCheck();
			_cts = new CancellationTokenSource();

			try
			{
				// 计算需要稳定播放的时间（秒）
				// 首次播放: 1秒, 卡顿1次: 2秒, 卡顿2次: 3秒...
				int requiredStableSeconds = 1 + _stallCount;
				Log.Info($"[VideoAudioHelper] 开始检测，需要稳定播放 {requiredStableSeconds} 秒，历史卡顿次数: {_stallCount}");
				
				// 等待视频开始播放（最多等3秒）
				for (int i = 0; i < 30; i++)
				{
					await UniTask.Delay(100, cancellationToken: _cts.Token);
					if (!_isChecking) return;
					
				if (mp.Control != null && mp.Control.IsPlaying() && !mp.Control.IsBuffering())
				{
					_lastPlayTime = (float)(mp.Control.GetCurrentTime() * 1000); // 转换为毫秒
					Log.Info($"[VideoAudioHelper] 视频开始播放，起始位置: {_lastPlayTime}ms");
					break;
				}
				}
				
				// 检测是否有音轨
				if (!HasAudioTrack(mp))
				{
					Log.Info($"[VideoAudioHelper] 视频无音轨，播放BGM");
					AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
					_isChecking = false;
					return;
				}
				
				// 开始稳定性检测（每秒检测一次）
				int stablePlayedSeconds = 0;
				
				for (int i = 0; i < requiredStableSeconds * 10; i++) // 最多检测10倍时间
				{
					await UniTask.Delay(100, cancellationToken: _cts.Token);
					if (!_isChecking) return;
					
			// 检查是否正在缓冲
			if (mp.Control != null && mp.Control.IsBuffering())
			{
				Log.Info($"[VideoAudioHelper] 视频缓冲中，等待恢复...");
				stablePlayedSeconds = 0; // 缓冲时重置稳定计数
				// 缓冲时确保BGM播放
				if (!_hasPlayedAudio)
				{
					AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
				}
				continue;
			}
					
					// 检查是否还在播放
					if (mp.Control == null || !mp.Control.IsPlaying())
					{
						Log.Warning($"[VideoAudioHelper] 视频停止播放");
						_isChecking = false;
						return;
					}
					
				// 每1秒检测一次播放位置
				if (i % 10 == 0)
				{
					float currentTime = (float)(mp.Control.GetCurrentTime() * 1000); // 转换为毫秒
					float timeDiff = currentTime - _lastPlayTime;
					
					// 检测卡顿（播放位置1秒内移动小于900ms）
					if (timeDiff < 900)
					{
						Log.Warning($"[VideoAudioHelper] 检测到卡顿！位置变化: {timeDiff}ms，卡顿次数+1");
						_stallCount++;
						stablePlayedSeconds = 0;
						
						// 卡顿时播放BGM（如果还没播放的话）
						if (!_hasPlayedAudio)
						{
							AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
						}
						
						// 继续检测
						continue;
					}
					
					_lastPlayTime = currentTime;
					stablePlayedSeconds++;
					
					Log.Info($"[VideoAudioHelper] 稳定播放 {stablePlayedSeconds}/{requiredStableSeconds} 秒");
					
					// 达到稳定播放要求，播放视频音效
					if (stablePlayedSeconds >= requiredStableSeconds && !_hasPlayedAudio)
					{
						Log.Info($"[VideoAudioHelper] 视频稳定播放，停止BGM，开启视频声音");
						// 停止BGM
						AudioManager.Stop(TK_Engine.AudioType.Music, true);
						// 打开视频声音
						if (mp.Control != null)
						{
							mp.Control.MuteAudio(false);
						}
						_hasPlayedAudio = true;
						_isChecking = false;
						return;
					}
				}
			}
			
			// 超时未达到稳定播放要求，播放BGM
			Log.Warning($"[VideoAudioHelper] 检测超时，未达到稳定播放要求，播放BGM");
			AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
			_isChecking = false;
			}
			catch (System.OperationCanceledException)
			{
				Log.Info($"[VideoAudioHelper] 检测被取消");
			}
		}

		/// <summary>
		/// 检查视频是否有音轨
		/// </summary>
		private static bool HasAudioTrack(MediaPlayer mp)
		{
			if (mp == null || mp.Info == null) return false;
			return mp.Info.HasAudio();
		}
	}
}
