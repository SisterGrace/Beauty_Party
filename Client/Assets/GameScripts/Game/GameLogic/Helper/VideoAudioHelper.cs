using System.Threading;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using TK_Engine;

namespace GameLogic
{
	/// <summary>
	/// 视频音频统一管理工具类
	/// 负责检测视频是否有音轨，并自动控制BGM的播放/停止
	/// </summary>
	public static class VideoAudioHelper
	{
		private static bool _isChecking = false;
		private static CancellationTokenSource _cts;

		/// <summary>
		/// 视频Started时调用，开始检测音频
		/// 1秒内检测3次，如果检测到有声音则停止BGM
		/// </summary>
		public static void StartVideoAudioCheck(MediaPlayer mp)
		{
			if (mp == null) return;
			
			_isChecking = true;
			CheckAudioAsync(mp).Forget();
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
		/// 异步检测音频（1秒内检测3次）
		/// </summary>
		private static async UniTaskVoid CheckAudioAsync(MediaPlayer mp)
		{
			CancelCheck();
			_cts = new CancellationTokenSource();

			try
			{
				// 1秒内检测3次，每次间隔约333ms
				for (int i = 0; i < 3; i++)
				{
					await UniTask.Delay(333, cancellationToken: _cts.Token);

					if (!_isChecking) return;

					// 检测音频是否可用
					if (HasAudioTrack(mp))
					{
						// 检测到有声音，停止背景音乐
						AudioManager.Stop(TK_Engine.AudioType.Music, true);
						_isChecking = false;
						return;
					}
				}

				// 3次检测都没有声音，不做任何操作（保持当前BGM状态）
				_isChecking = false;
			}
			catch (System.OperationCanceledException)
			{
				// 取消了，不做任何操作
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
