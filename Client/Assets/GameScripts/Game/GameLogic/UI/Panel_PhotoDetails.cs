using Config.common;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using AudioType = TK_Engine.AudioType;
using Rect = UnityEngine.Rect;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_PhotoDetails : UIWindow
	{
	public PhotoItemInfo _PhotoItemInfo;
	private bool _isLoading;
	private float mRotateSpeed = -360f;
	//当前相片是否喜欢
	private bool _isLike;
	//当前选项卡所有数据列表
	private List<PhotoItemInfo> _photoList;
	private CancellationTokenSource cancellationTokenSource;
	//是否是视频
	private bool _isVideo;
	//是否能免费下载
	private bool _isFreeDownload;
	protected override void OnRefresh()
		{
            
            mVideoNode.Hide();
			mImgNode.Hide();
			mSetBtns.Hide();
			mVideoLoading.Show();
			_isLoading = true;
			if (userDatas.Length > 0)
			{
				_PhotoItemInfo = (PhotoItemInfo)userDatas[0];
				_photoList = (List<PhotoItemInfo>)userDatas[1];
				_isLike = _PhotoItemInfo.isLiked;
			}
			UpdateDownloadBtnStatus();
			UpdateGoldCount();
			SetABShow();
			mlikeIcon.SetActive(_isLike);
			SetswitchBtns(_PhotoItemInfo.Index);
            VideoAudioHelper.SwitchToPage(this.WindowName);
        }

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int>(EventType.RefreshCurrency, UpdateGoldCount);
			GameEvent.AddEventListener(EventType.DownloadPicture, AdDownloadPic);
		}

		private void UpdateGoldCount(int id = 1)
		{
			mGoldCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
		}

		private void UpdateDownloadBtnStatus()
		{
			bool isDownLoad = UserController.Instance.GetUserPhotoData(_PhotoItemInfo.Id).IsDownload;
			mAdToGet.gameObject.SetActive(!isDownLoad);
			if (isDownLoad) return;
			int curCount = UserController.Instance.VIPFreeDownloadCount();
			_isFreeDownload = curCount > 0 || curCount == -1;
			mAdicn.SetActive(!_isFreeDownload);
			mFreeDowCount.gameObject.SetActive(_isFreeDownload);
			if (!_isFreeDownload && mAdicn.activeSelf)
			{
				//RV按钮上报
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_DownloadPicture.ToString());
			}
			/* mCost.gameObject.SetActive(!_isFreeDownload);
			mFreeCount.gameObject.SetActive(_isFreeDownload); */
			if (_isFreeDownload)
			{
				string freeCount = curCount > 0 ? curCount.ToString() : "∞";
				mFreeDowCount.text = freeCount;
				//mFreeCount.SetLanguage("uitips_photo_free", freeCount);
			}
		}
	private void SetABShow()
	{
		_isVideo = _PhotoItemInfo.ABType == 1;
		cancellationTokenSource = new CancellationTokenSource();
		LoadImageAsync();
		if (_PhotoItemInfo.IsVideo)
		{
			mPhotoAVPro.AudioVolume = 1f;
			mPhotoAVPro.AudioMuted = false;
			mPhotoAVPro.AutoOpen = true;
			mPhotoAVPro.OpenMedia(MediaPathType.AbsolutePathOrURL, _PhotoItemInfo.VideoUrl);
		}
	}
	private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
	{
		//Log.Info($"此时的AVpro状态：{et}，错误码：{errorCode}");
		switch (et)
		{
	case MediaPlayerEvent.EventType.Started:
		//开始播放，关闭加载动画
		_isLoading = false;
		mVideoLoading.Hide();
		mVideoNode.Show();
		mSetBtns.Show();
		// 开始视频音频检测（传入视频URL和页面名称）
		VideoAudioHelper.StartVideoAudioCheck(mp, _PhotoItemInfo.VideoUrl, this.WindowName);
		break;
	case MediaPlayerEvent.EventType.FinishedPlaying:
		// 视频播放结束，停止检测并恢复背景音乐
		VideoAudioHelper.StopVideoAudioCheck(this.WindowName);
		break;
	case MediaPlayerEvent.EventType.Error:
                    Log.Warning($"视频播放出错：{errorCode}，自动触发完成回调");
                    // 播放错误，停止检测并恢复背景音乐
                    VideoAudioHelper.StopVideoAudioCheck(this.WindowName);
		break;
	}
}

/// <summary>
/// 检查视频是否有音轨
/// </summary>
	private bool HasAudioTrack(MediaPlayer mp)
	{
		if (mp == null || mp.Info == null) return false;
		return mp.Info.HasAudio();
	}
		private async UniTask LoadImageAsync()
		{
			try
			{
				Texture2D texture = await ImgUtil.GetTextureAsync(_PhotoItemInfo.Imgurl, cancellationTokenSource.Token);
				if (texture != null)
				{
					_isLoading = false;
					if (gameObject == null) return;
					mVideoLoading.Hide();
					mImgNode.Show();
					mSetBtns.Show();
					SetSprite(texture);
				}
			}
			catch (System.Exception e)
			{
				Log.Warning($"图片加载失败:{e.Message}");
				_isLoading = false;
				mVideoLoading.Hide();
			}
		}
		private void SetSprite(Texture2D tex)
		{
			mPhotoIcon.gameObject.Show();
			Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
			sprite.name = $"{_PhotoItemInfo.Imgurl}";
			//if(mPhotoIcon.sprite!= null) return;
			mPhotoIcon.sprite = sprite;
            if (aspectRatioFitter != null)
            {
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspectRatioFitter.aspectRatio = (float)tex.width / (float)tex.height;
                //Debug.LogError(aspectRatioFitter.aspectRatio + " width:" + tex.width + " height:" + tex.height);
            }
        }
		protected override void OnUpdate()
		{
			if (_isLoading)
			{
				mloadingIcon.Rotate(0, 0, mRotateSpeed * Time.unscaledDeltaTime);
			}
		}
		//设置照片是否喜欢
		private void SetPhotoLike()
		{
			_isLike = !_isLike;
			UserController.Instance.SetUserPhotoLike(_PhotoItemInfo.Id, _isLike);
			_PhotoItemInfo.isLiked = _isLike;
			mlikeIcon.SetActive(_isLike);
			AudioManager.Play(AudioType.Sound, _isLike ? AudioDef.UI_clickLove : AudioDef.UI_closeLove);
			//精准更新对应列表item的点赞状态（不刷新整个列表）
			GameEvent.Send(EventType.RefreshPhotoLikeStatus, _PhotoItemInfo.Id, _isLike);
			if (_isLike)
			{
				mLikeNode.PlayEffect();
			}
		}
		//设置切换按钮状态
		private void SetswitchBtns(int index)
		{
			if (_photoList == null || _photoList.Count == 0)
			{
				Log.Warning("PhotoList is null or empty");
				return;
			}
			mSetBack.gameObject.SetActive(_PhotoItemInfo.Id != UserController.Instance.GetCurUserPhotoID);

		}
		//设置视频到主界面
		//点击Set Background可把已下载视频设置为主界面展示视频，若未下载，点击后弹出弹窗提示下载视频
		//下载完成的视频会自动设置为主界面展示
		private void SetVideoToMain()
		{
			//检查当前是否下载了
			bool isAvCached = UserController.Instance.GetUserPhotoData(_PhotoItemInfo.Id).IsDownload;
			if (!isAvCached)
			{
				//弹出提示框，提示用户下载视频
				GameModule.UI.ShowUI<Panel_DownloadTip>(_PhotoItemInfo.Id);
			}
			else
			{
				//设置视频到主界面
				UserController.Instance.GetCurUserPhotoID = _PhotoItemInfo.Id;
				mSetBack.gameObject.SetActive(_PhotoItemInfo.Id != UserController.Instance.GetCurUserPhotoID);
				//提示设置成功
				GameEvent.Send(EventType.ShowTips, "uitips_photo_tips1", true);
			}
		}
		private void AdDownloadPic()
		{
			if (!_isFreeDownload)
			{
				SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_DownloadPicture, (success) =>
				{
					if (success)
					{
						SavePhotoToGallery();
					}
				});
			}
			else
			{
				SavePhotoToGallery(true);
			}

		}

		private async UniTask SaveVideoToGallery( bool isUseDowCount = false)
		{
			if (_isLoading)//视频正在加载中，不保存
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}

			bool videoSaved = false;
			bool photoSaved = false;
			GameEvent.Send(EventType.SetWaitingUI, true);
			try
			{
				using (var videoCts = new CancellationTokenSource())
				{
					//十秒超时取消
					videoCts.CancelAfter(TimeSpan.FromSeconds(8));
					//启动AVProVideo插件的缓存功能
					if (mPhotoAVPro.Cache.IsMediaCachingSupported())
					{
						mPhotoAVPro.Cache.AddMediaToCache(_PhotoItemInfo.VideoUrl);
					}

					//下载视频到相册
					VideoUtil.VideoSaveStatus status = await VideoUtil.SaveVideoToGallery(
						_PhotoItemInfo.VideoUrl,
						$"Video_{_PhotoItemInfo.Id}_{TimeUtil.GetTimeStamp()}",
						cancellationToken: videoCts.Token
					);
					videoSaved = status == VideoUtil.VideoSaveStatus.Success;
				}

				using (var picCts = new CancellationTokenSource())
				{
					//照片5秒超时取消
					picCts.CancelAfter(TimeSpan.FromSeconds(5));
					photoSaved = await ImgUtil.SaveUrlToGallery(_PhotoItemInfo.Imgurl,
						$"Photo_{_PhotoItemInfo.Id}_{TimeUtil.GetTimeStamp()}",
						ImgUtil.ImageFormat.PNG,
						picCts.Token);
				}
			}
			catch (OperationCanceledException)
			{
				Log.Info("下载超时");
				GameEvent.Send(EventType.ShowTips, "uitips_buy_tips2", true);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				GameEvent.Send(EventType.ShowTips, "uitips_buy_tips2", true);
				throw;
			}
			finally
			{
				GameEvent.Send(EventType.SetWaitingUI, false);
			}

			if (photoSaved && videoSaved)
			{
				Log.Info("视频和照片保存成功");
				if (isUseDowCount)
				{
					UserController.Instance.UseVIPFreeDownloadCount();
				}

				/* if (isUseCoins)
				{
					UserController.Instance.UserConsume(ERewardType.Currency, 0, 300);
				} */
				//图片下载成功上报
				SDKManager.Instance.LogEvent("Picture_Download", "Picture_ID", _PhotoItemInfo.Id,
					CommonEvent.ComLevelEvent);
				//视频保存成功
				UserController.Instance.SetUserPhotoCached(_PhotoItemInfo.Id, true);
				UpdateDownloadBtnStatus();
				GameEvent.Send(EventType.ShowTips, "uitips_photo_tips5", true);
			}



		}
		private async UniTask SavePhotoToGallery( bool isUseDowCount = false, bool showMask = true)
		{
			//如果此时的ID是有视频的，直接走视频下载方法
			if (_PhotoItemInfo.IsVideo)
			{
				SaveVideoToGallery( isUseDowCount);
				return;
			}
			if (_isLoading) //视频正在加载中，不保存
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}
			using (var cts = new CancellationTokenSource())
			{
				GameEvent.Send(EventType.SetWaitingUI, true);
				try
				{
					cts.CancelAfter(TimeSpan.FromSeconds(5));
					bool tIsDown = await ImgUtil.SaveUrlToGallery(_PhotoItemInfo.Imgurl,
						$"Photo_{_PhotoItemInfo.Id}_{TimeUtil.GetTimeStamp()}",
						ImgUtil.ImageFormat.PNG,
						cts.Token);
					if (tIsDown)
					{
						if (isUseDowCount)
						{
							UserController.Instance.UseVIPFreeDownloadCount();
						}

						/* if (isUseCoins)
						{
							UserController.Instance.UserConsume(ERewardType.Currency, 0, 300);
						} */

						var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(_PhotoItemInfo.Id);
						//图片下载成功上报
						SDKManager.Instance.LogEvent("Picture_Download", "Picture_ID", _PhotoItemInfo.Id,
							CommonEvent.ComLevelEvent);
						//视频保存成功
						UserController.Instance.SetUserPhotoCached(_PhotoItemInfo.Id, true);
						UpdateDownloadBtnStatus();
						GameEvent.Send(EventType.ShowTips, "uitips_photo_tips5", true);
					}
				}
				catch (OperationCanceledException)
				{
					Log.Info("下载超时");
					GameEvent.Send(EventType.ShowTips, "uitips_buy_tips2", true);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					GameEvent.Send(EventType.ShowTips, "uitips_buy_tips2", true);
					throw;
				}
				finally
				{
					GameEvent.Send(EventType.SetWaitingUI, false);
				}
			}
		}
protected override void Close()
{
	VideoAudioHelper.CancelCheck();
	
	// 如果是视频并且正在播放，不播放BGM（让视频继续播放）
	// 如果没有视频或视频未播放，播放BGM
	if (!_isVideo || mPhotoAVPro == null || mPhotoAVPro.Control == null || !mPhotoAVPro.Control.IsPlaying())
	{
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}
            VideoAudioHelper.StopVideoAudioCheck(this.WindowName, true);
            base.Close();

}

protected override void OnDestroy()
{
	VideoAudioHelper.CancelCheck();
	mPhotoAVPro?.CloseMedia();
	base.OnDestroy();
	cancellationTokenSource?.Cancel();
	cancellationTokenSource?.Dispose();
	GameEvent.RemoveEventListener<int>(EventType.RefreshCurrency, UpdateGoldCount);
	GameEvent.RemoveEventListener(EventType.DownloadPicture, AdDownloadPic);
}
	}
}
