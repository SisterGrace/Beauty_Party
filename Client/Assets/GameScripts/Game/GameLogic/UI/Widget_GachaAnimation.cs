using TK_Engine;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Coffee.UIExtensions;
using Y_Base;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_GachaAnimation : UIWidget
	{
		private GachaResult _result;

		//普通照片特效
		private UIParticle _normalPhotoEffect;
		//保底照片特效
		private UIParticle _pityPhotoEffect;
		private int _timeId = 0;
		private readonly Stopwatch _animStopwatch = new Stopwatch();
		public void Open(GachaResult result)
		{
			_timeId = 0;
			_result = result;
			_animStopwatch.Restart();
			AudioManager.Stop(TK_Engine.AudioType.Music, false);

			// 检查视频是否可用
			if (mAvMediaPlayer != null && mAvMediaPlayer.MediaPath != null && !string.IsNullOrEmpty(mAvMediaPlayer.MediaPath.Path))
			{
				// 有视频，播放动画
				mSkip.gameObject.Show();
				gameObject.SetActive(true);
				mAvMediaPlayer.Rewind(false);
				mAvMediaPlayer.Play();
			}
			else
			{
				// 没有视频，直接展示结果
				gameObject.SetActive(false);
				_animStopwatch.Stop();
				AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
				OpenRewShow();
			}
		}
		private void OpenRewShow()
		{
			if (!_result.IsDuplicate)
			{
				//新照片，触发照片动画
				GameModule.UI.ShowUI<Panel_Collectphotos>(_result);
			}
			else
			{
				//重复照片，弹出钻石收集界面
				GameModule.UI.ShowUI<Panel_CollectDiamond>(_result.RefundDiamonds);
			}

		}
		private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
		{
			Log.Info($"此时抽卡动画的视频播放器状态：{et}");
			switch (et)
			{
				case MediaPlayerEvent.EventType.FinishedPlaying://播放结束，关闭当前界面
					Hide();
					break;
			}
		}
		public void Hide()
		{
			_animStopwatch.Stop();
			SDKManager.Instance.LogEvent("Draw_Animation", "Time", _animStopwatch.Elapsed.TotalSeconds);
			mSkip.gameObject.Hide();
			mAvMediaPlayer.Stop();
			//重置播放起点
			mAvMediaPlayer.Rewind(false);
			gameObject.SetActive(false);
			AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
			OpenRewShow();
		}

	}
}
