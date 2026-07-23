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
	private static string _currentPanelName = "";      // 当前页面名称

	/// <summary>
	/// 视频Started时调用，开始检测音频和卡顿
	/// </summary>
	/// <param name="mp">MediaPlayer实例</param>
	/// <param name="videoUrl">视频URL，用于识别是否换了视频</param>
	/// <param name="panelName">页面名称，用于页面级检测隔离</param>
	public static void StartVideoAudioCheck(MediaPlayer mp, string videoUrl, string panelName)
	{
		if (mp == null) return;
		
		// 如果是新页面，停止上一个页面的检测
		if (_currentPanelName != panelName && _isChecking)
		{
			Log.Info($"[VideoAudioHelper] 切换页面: {_currentPanelName} → {panelName}，停止上一个页面的检测");
			_isChecking = false;
			CancelCheck();
		}
		
		// 更新当前页面
		_currentPanelName = panelName;
		
		// 每次开始检测都重置状态（无论是否是新视频）
		Log.Info($"[VideoAudioHelper] 开始新的检测周期，重置状态");
		_hasPlayedAudio = false;
		_lastPlayTime = 0;
		
		// 如果是新视频，关闭上一个视频的声音并重置卡顿计数
		if (_currentVideoUrl != videoUrl)
		{
			Log.Info($"[VideoAudioHelper] 切换视频: {videoUrl}，关闭上一个视频声音，重置卡顿计数");
			
			// 关闭上一个视频的声音
			if (_currentPlayer != null && _currentPlayer.Control != null)
		{
			_currentPlayer.Control.MuteAudio(true);
		}
		
		_currentVideoUrl = videoUrl;
		_stallCount = 0;
	}
	
	// 先静音当前视频，检测通过后再开启声音
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
	/// <param name="panelName">页面名称，只停止当前页面的检测</param>
	/// <param name="restoreBGM">是否恢复BGM</param>
	public static void StopVideoAudioCheck(string panelName, bool restoreBGM = true)
	{
		// 只有当前页面的检测才停止
		if (_currentPanelName != panelName)
		{
			Log.Info($"[VideoAudioHelper] 停止检测请求来自 {panelName}，但当前页面是 {_currentPanelName}，忽略");
			return;
		}
		
		Log.Info($"[VideoAudioHelper] 停止页面 {panelName} 的检测");
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
			// 获取视频总时长
			double videoDuration = 0;
			if (mp.Info != null)
			{
				videoDuration = mp.Info.GetDuration();
			}
			int maxCheckSeconds = (int)videoDuration + 1; // 检测上限 = 视频时长 + 1
			
			// 计算需要稳定播放的时间（秒）
			// 首次播放: 1秒, 卡顿1次: 2秒, 卡顿2次: 3秒...
			// 但不能超过视频时长+1
			int requiredStableSeconds = 1 + _stallCount;
			if (maxCheckSeconds > 0)
			{
				requiredStableSeconds = Mathf.Min(requiredStableSeconds, maxCheckSeconds);
			}
			Log.Info($"[VideoAudioHelper] 开始检测，需要稳定播放 {requiredStableSeconds} 秒（上限:{maxCheckSeconds}秒），历史卡顿次数: {_stallCount}");
			
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
			
		// 开始检测前先播放BGM（默认状态）
		Log.Info($"[VideoAudioHelper] 开始检测，先播放BGM");
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
		
		// 开始稳定性检测（每2秒检测一次，直到通过或视频结束）
		int stablePlayedSeconds = 0;
		bool isFirstCheck = true; // 标志位：是否第一次检测
		
		while (_isChecking)
		{
			// 等待2秒
			await UniTask.Delay(2000, cancellationToken: _cts.Token);
			if (!_isChecking) return;
			
			// 检查是否正在缓冲
			if (mp.Control != null && mp.Control.IsBuffering())
			{
				Log.Info($"[VideoAudioHelper] 视频缓冲中，等待恢复...");
				stablePlayedSeconds = 0; // 缓冲时重置稳定计数
				isFirstCheck = true; // 缓冲后重置首次检测标志
				continue;
			}
			
			// 检查是否还在播放
			if (mp.Control == null || !mp.Control.IsPlaying())
			{
				Log.Warning($"[VideoAudioHelper] 视频停止播放");
				_isChecking = false;
				return;
			}
			
			// 获取当前播放位置
			float currentTime = (float)(mp.Control.GetCurrentTime() * 1000); // 转换为毫秒
			
			// 第一次检测时，只记录位置，不判断卡顿
			if (isFirstCheck)
			{
				Log.Info($"[VideoAudioHelper] 首次检测，记录起始位置: {currentTime}ms");
				_lastPlayTime = currentTime;
				isFirstCheck = false;
				continue;
			}
			
			// 计算播放位置变化
			float timeDiff = currentTime - _lastPlayTime;
			
			// 检测到视频位置倒退（负数），说明视频跳转或重新播放了
			if (timeDiff < 0)
			{
				Log.Info($"[VideoAudioHelper] 视频位置倒退（{timeDiff}ms），重置检测状态");
				_lastPlayTime = currentTime;
				stablePlayedSeconds = 0;
				isFirstCheck = true; // 重置首次检测标志
				continue;
			}
			
			// 检测卡顿（播放位置2秒内移动小于1800ms）
			if (timeDiff < 1800)
			{
				Log.Warning($"[VideoAudioHelper] 检测到卡顿！位置变化: {timeDiff}ms，卡顿次数+1");
				_stallCount++;
				stablePlayedSeconds = 0;
				_lastPlayTime = currentTime;
				continue;
			}
				
			_lastPlayTime = currentTime;
			stablePlayedSeconds += 2; // 每次检测通过算2秒
			
			Log.Info($"[VideoAudioHelper] 稳定播放 {stablePlayedSeconds}/{requiredStableSeconds} 秒");
			
			// 达到稳定播放要求，播放视频音效
			if (stablePlayedSeconds >= requiredStableSeconds)
			{
				// 如果已经播放过视频音效，说明检测已完成，直接退出
				if (_hasPlayedAudio)
				{
					Log.Info($"[VideoAudioHelper] 视频音效已播放，检测完成，退出检测");
					_isChecking = false;
					return;
				}
				
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
