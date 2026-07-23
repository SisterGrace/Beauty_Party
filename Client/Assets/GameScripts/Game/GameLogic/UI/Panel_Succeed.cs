using System;
using TK_Engine;
using UnityEngine;
using Config.common;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using AudioType = TK_Engine.AudioType;
using Y_Base;
using RenderHeads.Media.AVProVideo;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Succeed : UIWindow
	{
		private List<Transform> _allAnimNodes = new List<Transform>();
		private List<TBRewardData> _rewardDatas;
	private bool _isAnim;
	//关卡所选照片信息
	private OneSelectPhotoInfo _selectPhotoInfo;
	//是否正在加载视频
	private bool _isLoading;
	private float mRotateSpeed = -360f;
	//是否能免费下载
	private bool _isFreeDownload;
	//关卡钻石奖励数量
	private int _diamondCount;
	//是否喜欢
	private bool _isLike;
		
		//动画相关参数（可通过GameScene配置）
		private static SucceedAnimConfig _animConfig = SucceedAnimConfig.Default;
		private Vector2 _titleBgOriginalPos;
		private Vector2 _photBtnsOriginalPos;
		private Vector3 _frameOriginalScale;
		private float _frameFullScreenScale; //全屏缩放比例
		
		/// <summary>
		/// 设置动画配置（从GameScene调用）
		/// </summary>
		public static void SetAnimConfig(SucceedAnimConfig config)
		{
			_animConfig = config;
		}
		//是否成功下载图片
		private bool _dowaloadComplete;
		protected override void OnRefresh()
		{
			_isAnim = false;
			_isLike = false;
			_dowaloadComplete = false;
			UpdateInfo();
			
			//关卡成功上报
			SDKManager.Instance.LogEvent("Level_Success",CommonEvent.ComLevelEvent);
			//关卡结束
			SDKManager.Instance.LogEvent("Level_End",new Dictionary<string, object>()
			{
				{"Level_Time",LevelController.Instance.GetGameTime(TimeUtil.GetTimeStamp())},
				{"End_Type",1}
			},CommonEvent.ComLevelEvent);
			int level = UserController.Instance.Level;
			if (level == 1 || level == 2)
			{
				//判断是否买量用户
				var isPromote = SDKManager.Instance.IsPromote;
			}
			LevelSendReward();
			AudioManager.Play(AudioType.Sound, AudioDef.Success);
			GameEvent.Send(EventType.PauseGameTime,true);
			//展示插屏
			SDKManager.Instance.Button_ShowIV(HCIVPositionName.IV_Victory,IvType.IV1);
			//RV按钮上报
			SDKManager.Instance.LogEvent("RV_Button_Show","Position",HCRVPositionName.RV_SuccMore.ToString());
			
		}

		protected override void RegisterEvent()
		{

		}

		protected override void OnUpdate()
		{
		}
		//更新界面信息
		private void UpdateInfo()
		{
			if (LevelController.Instance.CurrentLevelInfo == null)
			{
				Log.Error("关卡配置信息为空");
				return;
			}
			int reId = LevelController.Instance.CurrentLevelInfo.RewardId;

			_rewardDatas = ConfigLoader.Instance.Tables.TbCfgReward.Get(reId).Reward;
			_diamondCount = (int)_rewardDatas.Find(x => x.ID == 0).Count;
			mCurGetDiaCount.text = $"<sprite name=\"icon_zuanshi\">+{_diamondCount}";
			mAdMultiple.text = $"<sprite name=\"icon_zuanshi\">+{ConfigLoader.Constant.AdMultiple*_diamondCount}";
			mLikeIcon.Hide();
			//更新当前所得视频
			_selectPhotoInfo = LevelController.Instance.CurLevelSelectPhotoInfo;
			ABShow();

			//初始化动画并开始第一个动画
			InitAnimationState();
			PlayDiamondShowAnimation();
		}
		//关卡结算奖励
		private void LevelSendReward(bool isSendDefaultReward = true)
		{
			//发送关默认奖励
			if (isSendDefaultReward)
			{
				//为什么要先执行ExitLevel()	
				//因为要先执行关卡数+1，涉及到活动的开启，才去执行发奖励的活动
				LevelController.Instance.ExitLevel(true);
				PropsController.Instance.AddRewardData(_rewardDatas, OneRewardData.NoneData, false);
				//加载下一关的图
				PhotoController.Instance.PhotoPreloading(true, true);
			}
			else//发送Ad广告奖励
			{
				//暂时发送钻石多倍奖励，但是钻石已经默认添加了一次，倍数要减一
				int diamondCount = _diamondCount * (ConfigLoader.Constant.AdMultiple - 1);
				PropsController.Instance.SendRewardData(ERewardType.Currency, 0, diamondCount, itemFlyType.MultipleFly);
			}
		}
		private void UpdateDownloadBtnStatus(bool isLogEvent = false)
		{
			int curCount = UserController.Instance.VIPFreeDownloadCount();
			_isFreeDownload = curCount > 0 || curCount == -1;
			mAdicn.SetActive(!_isFreeDownload);
			mFreeDowCount.gameObject.SetActive(_isFreeDownload);
			//madDow.SetActive(!_isFreeDownload);
			if (isLogEvent)
			{
				if (!_isFreeDownload && mAdicn.activeSelf)
				{
					//RV按钮上报
					SDKManager.Instance.LogEvent("RV_Button_Show", "Position", "RV_DownloadPicture");
				}
			}
			if (_isFreeDownload)
			{
				string freeCount = curCount > 0 ? curCount.ToString() : "∞";
				mFreeDowCount.text = freeCount;
			}
		}

		private void ABShow()
		{
			mAVProVideoUI.Hide();
			_isLoading = true;
			
			LoadImageAsync();
			if (_selectPhotoInfo.IsVideo)
			{
				mPhotoAVPro.gameObject.SetActive(true);
                mPhotoAVPro.OpenMedia(MediaPathType.AbsolutePathOrURL, _selectPhotoInfo.VideoUrl);
			}
			else
			{
                mPhotoAVPro.gameObject.SetActive(false);
            }
		}
		private void LoadImageAsync()
		{
			_photoLoad.LoadUIImg(_selectPhotoInfo.Imgurl);
		}
	private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
	{
		switch (et)
		{
	case MediaPlayerEvent.EventType.Started:
		//开始播放，关闭加载动画
		_isLoading = false;
		mAVProVideoUI.Show();
		// 开始视频音频检测（传入视频URL）
		VideoAudioHelper.StartVideoAudioCheck(mp, _selectPhotoInfo.VideoUrl);
		break;
		case MediaPlayerEvent.EventType.FinishedPlaying:
			// 视频播放结束，停止检测并恢复背景音乐
			VideoAudioHelper.StopVideoAudioCheck();
			break;
		case MediaPlayerEvent.EventType.Error:
		// 播放错误，停止检测并恢复背景音乐
		VideoAudioHelper.StopVideoAudioCheck();
		break;
	}
}

private void SetLikeIcon()
{
	_isLike = !_isLike;
	mLikeIcon.SetActive(_isLike);
	UserController.Instance.SetUserPhotoLike(_selectPhotoInfo.Id, _isLike,true);
	AudioManager.Play(AudioType.Sound, _isLike?AudioDef.UI_clickLove:AudioDef.UI_closeLove);
	if (_isLike)
	{
		mLike.PlayEffect();
	}
}
		#region 视频和照片保存
		private async UniTask SaveVideoAndPhotoToGallery(bool isUseDowCount = false)
		{
			if (_isLoading) //视频正在加载中，不保存
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}
			bool videoSaved = false;
			bool photoSaved = false;
			GameEvent.Send(EventType.SetWaitingUI, true);
			try
			{
				//保存视频
				using (var videoCts = new CancellationTokenSource())
				{
					videoCts.CancelAfter(TimeSpan.FromSeconds(8));
					VideoUtil.VideoSaveStatus status = await VideoUtil.SaveVideoToGallery(
						_selectPhotoInfo.VideoUrl,
						$"Video_{_selectPhotoInfo.Id}_{TimeUtil.GetTimeStamp()}",
						cancellationToken: videoCts.Token
					);
					videoSaved = status == VideoUtil.VideoSaveStatus.Success;
				}

				//保存照片
				using (var picCts = new CancellationTokenSource())
				{
					picCts.CancelAfter(TimeSpan.FromSeconds(5));
					photoSaved = await ImgUtil.SaveUrlToGallery(_selectPhotoInfo.Imgurl,
						$"Photo_{_selectPhotoInfo.Id}_{TimeUtil.GetTimeStamp()}",
						ImgUtil.ImageFormat.PNG,
						picCts.Token);

				}
			}
			catch (OperationCanceledException)
			{
				//下载超时
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
			// 成功处理
			if (videoSaved && photoSaved)
			{
				Log.Info("视频和照片保存成功");
				if (isUseDowCount)
				{
					UserController.Instance.UseVIPFreeDownloadCount();
					//更新按钮显示
					UpdateDownloadBtnStatus(true);
				}
				//图片和视频下载成功上报
				SDKManager.Instance.LogEvent("Picture_Download", "Picture_ID", _selectPhotoInfo.Id,
					new CommonEvent(UserController.Instance.Level-1));
				//视频保存成功
				_dowaloadComplete = true;
				UserController.Instance.SetUserPhotoCached(_selectPhotoInfo.Id, true);
				GameEvent.Send(EventType.ShowTips, "uitips_photo_tips5", true);
			}
		}
		private async UniTask SavePhotoToGallery(bool isUseDowCount = false)
		{
			//如果此时的ID是有视频的，直接走视频下载方法
			if (_selectPhotoInfo.IsVideo)
			{
				SaveVideoAndPhotoToGallery(isUseDowCount);
				return;
			}
			if (_isLoading) //视频正在加载中，不保存
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}

			using (var picCts = new CancellationTokenSource())
			{
				GameEvent.Send(EventType.SetWaitingUI, true);
				try
				{
					//5秒超时使用
					picCts.CancelAfter(TimeSpan.FromSeconds(5));
					bool tIsDown = await ImgUtil.SaveUrlToGallery(_selectPhotoInfo.Imgurl,
						$"Photo_{_selectPhotoInfo.Id}_{TimeUtil.GetTimeStamp()}", 
						ImgUtil.ImageFormat.PNG,
						picCts.Token);
					if (tIsDown)
					{
						if (isUseDowCount)
						{
							UserController.Instance.UseVIPFreeDownloadCount();
							//更新按钮显示
							UpdateDownloadBtnStatus(true);
						}
						//图片下载成功上报
						SDKManager.Instance.LogEvent("Picture_Download", "Picture_ID", _selectPhotoInfo.Id,
							new CommonEvent(UserController.Instance.Level-1));
						//照片保存成功
						_dowaloadComplete = true;
						UserController.Instance.SetUserPhotoCached(_selectPhotoInfo.Id, true);
						GameEvent.Send(EventType.ShowTips, "uitips_photo_tips5", true);
					}
				}
				catch (OperationCanceledException)
				{
					Log.Info("图片下载超时");
					GameEvent.Send(EventType.ShowTips,"uitips_buy_tips2",true);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					GameEvent.Send(EventType.ShowTips,"uitips_buy_tips2",true);
					throw;
				}
				finally
				{
					GameEvent.Send(EventType.SetWaitingUI, false);
				}
			}
			
		}
		#endregion
		#region 结算界面的动画

		/// <summary>
		/// 初始化动画状态
		/// </summary>
		private void InitAnimationState()
		{
			//保存原始位置和缩放
			_titleBgOriginalPos = mTitlebg.anchoredPosition;
			_photBtnsOriginalPos = mPhotBtns.anchoredPosition;
			_frameOriginalScale = mFrame.localScale;

			//计算全屏缩放比例
			_frameFullScreenScale = CalculateFullScreenScale();

			//初始状态：显示钻石界面，隐藏照片界面
			mDiamondShow.Show();
			mPhotoShow.Hide();
			mLike.gameObject.Hide();

			//钻石界面子节点初始状态（缩放为0，准备弹出动画）
			mDiaTitle.localScale = Vector3.zero;
			mDiaIcon.localScale = Vector3.zero;
			mDiaBtns.localScale = Vector3.zero;

			//照片界面初始状态
			mPhotoBgCanvasGroup.alpha = 0f;
			mTitlebg.anchoredPosition = _titleBgOriginalPos + new Vector2(0, _animConfig.TitleBgOffsetY);
			mPhotBtns.anchoredPosition = _photBtnsOriginalPos + new Vector2(0, _animConfig.PhotoBtnsOffsetY);
			//mFrame从全屏缩小到当前大小
			mFrame.localScale = _frameOriginalScale * _frameFullScreenScale;
			Log.Info($"全屏缩放比例: {_frameFullScreenScale}");
		}
		
		/// <summary>
		/// 计算能覆盖全屏的缩放比例
		/// </summary>
		private float CalculateFullScreenScale()
		{
			//获取Canvas的RectTransform尺寸（UI坐标系下的全屏尺寸）
			Vector2 canvasSize = rectTransform.rect.size;
			//获取mFrame的当前尺寸
			Vector2 frameSize = mFrame.rect.size;
			
			//计算需要的缩放比例（取宽高中较大的比例，确保能覆盖全屏）
			float scaleX = canvasSize.x / frameSize.x;
			float scaleY = canvasSize.y / frameSize.y;
			
			//取较大值并加一点余量，确保完全覆盖
			float scale = Mathf.Max(scaleX, scaleY) * 1.1f;
			
			Log.Info($"[Panel_Succeed] Canvas尺寸: {canvasSize}, Frame尺寸: {frameSize}, 计算缩放: {scale}");
			return scale;
		}
		
		/// <summary>
		/// 播放钻石展示动画（依次弹出mDiaTitle、mDisIcon、mDiaBtns）
		/// </summary>
		private void PlayDiamondShowAnimation()
		{
			_isAnim = true;
			
			Sequence sequence = DOTween.Sequence();
			
			//依次弹出三个节点
			sequence.Append(mDiaTitle.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.AppendInterval(_animConfig.DiamondAnimDelay);
			sequence.Append(mDiaIcon.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.AppendInterval(_animConfig.DiamondAnimDelay);
			sequence.Append(mDiaBtns.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			
			sequence.OnComplete(() =>
			{
				_isAnim = false;
			});
			
			sequence.SetUpdate(true); //使用unscaledTime，不受Time.timeScale影响
		}
		
		/// <summary>
		/// 从钻石界面切换到照片界面的动画
		/// </summary>
		private void SwitchToPhotoShowAnimation()
		{
			_isAnim = true;
			
			//先隐藏钻石界面
			mDiamondShow.SetActive(false);
			
			//显示照片界面
			mPhotoShow.SetActive(true);
			UpdateDownloadBtnStatus(true);
			//播放照片界面动画
			Sequence sequence = DOTween.Sequence();
			AudioManager.Play(AudioType.Sound, AudioDef.UI_winPhoto);
			//1. mFrame从大scale缩小到1（先执行）
			sequence.Append(mFrame.DOScale(_frameOriginalScale, _animConfig.PhotoAnimDuration).SetEase(_animConfig.PhotoEaseType));
			
			//2. 延迟后执行其他动画（使用负延迟实现提前开始，或正延迟实现等待）
			float otherAnimStartTime = _animConfig.PhotoAnimDuration - _animConfig.OtherAnimDelay;
			if (otherAnimStartTime < 0) otherAnimStartTime = 0;
			
			//3. mPhotoBg的CanvasGroup从透明度0变成1
			sequence.Insert(otherAnimStartTime, mPhotoBgCanvasGroup.DOFade(1f, _animConfig.OtherAnimDuration).SetEase(_animConfig.PhotoEaseType));
			
			//4. mTitlebg从上往下移动到原始位置
			sequence.Insert(otherAnimStartTime, mTitlebg.DOAnchorPos(_titleBgOriginalPos, _animConfig.OtherAnimDuration).SetEase(_animConfig.PhotoEaseType));
			
			//5. mPhotBtns从下往上移动到原始位置
			sequence.Insert(otherAnimStartTime, mPhotBtns.DOAnchorPos(_photBtnsOriginalPos, _animConfig.OtherAnimDuration).SetEase(_animConfig.PhotoEaseType));
			
			sequence.OnComplete(() =>
			{
				_isAnim = false;
				 mLike.gameObject.Show();
			});
			
			sequence.SetUpdate(true); //使用unscaledTime，不受Time.timeScale影响
		}
		
		#endregion
		
		#region 动画测试方法（供GameScene调用）
		
		/// <summary>
		/// 测试钻石界面动画
		/// </summary>
		public void TestDiamondAnimation()
		{
			DOTween.Kill(mDiaTitle);
			DOTween.Kill(mDiaIcon);
			DOTween.Kill(mDiaBtns);
			
			//确保钻石界面显示
			mDiamondShow.Show();
			mPhotoShow.Hide();
			
			//重置状态并播放
			mDiaTitle.localScale = Vector3.zero;
			mDiaIcon.localScale = Vector3.zero;
			mDiaBtns.localScale = Vector3.zero;
			
			PlayDiamondShowAnimation();
			Log.Info("[Panel_Succeed] 钻石界面动画开始播放");
		}

		/// <summary>
		/// 测试照片界面动画
		/// </summary>
		public void TestPhotoAnimation()
		{
			DOTween.Kill(mPhotoBgCanvasGroup);
			DOTween.Kill(mTitlebg);
			DOTween.Kill(mPhotBtns);
			DOTween.Kill(mFrame);

			//保存原始位置（如果还没保存）
			if (_titleBgOriginalPos == Vector2.zero)
			{
				_titleBgOriginalPos = mTitlebg.anchoredPosition;
				_photBtnsOriginalPos = mPhotBtns.anchoredPosition;
				_frameOriginalScale = mFrame.localScale;
			}

			//计算全屏缩放比例
			_frameFullScreenScale = CalculateFullScreenScale();

			//重置照片界面状态
			mPhotoBgCanvasGroup.alpha = 0f;
			mTitlebg.anchoredPosition = _titleBgOriginalPos + new Vector2(0, _animConfig.TitleBgOffsetY);
			mPhotBtns.anchoredPosition = _photBtnsOriginalPos + new Vector2(0, _animConfig.PhotoBtnsOffsetY);
			mFrame.localScale = _frameOriginalScale * _frameFullScreenScale;

			SwitchToPhotoShowAnimation();
			Log.Info("[Panel_Succeed] 照片界面动画开始播放");
			Log.Info($"全屏缩放比例: {_frameFullScreenScale}");
		}
		
		/// <summary>
		/// 测试完整动画流程
		/// </summary>
		public void TestFullAnimation()
		{
			DOTween.KillAll();
			
			//保存原始位置
			_titleBgOriginalPos = mTitlebg.anchoredPosition;
			_photBtnsOriginalPos = mPhotBtns.anchoredPosition;
			_frameOriginalScale = mFrame.localScale;
			
			//计算全屏缩放比例
			_frameFullScreenScale = CalculateFullScreenScale();
			
			//确保钻石界面显示
			mDiamondShow.Show();
			mPhotoShow.Hide();
			
			//重置钻石界面状态
			mDiaTitle.localScale = Vector3.zero;
			mDiaIcon.localScale = Vector3.zero;
			mDiaBtns.localScale = Vector3.zero;
			
			_isAnim = true;
			Sequence sequence = DOTween.Sequence();
			
			//钻石界面动画
			sequence.Append(mDiaTitle.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.AppendInterval(_animConfig.DiamondAnimDelay);
			sequence.Append(mDiaIcon.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			sequence.AppendInterval(_animConfig.DiamondAnimDelay);
			sequence.Append(mDiaBtns.DOScale(Vector3.one, _animConfig.DiamondAnimDuration).SetEase(_animConfig.DiamondEaseType));
			
			//等待后切换到照片界面
			sequence.AppendInterval(_animConfig.SwitchDelay);
			sequence.AppendCallback(() =>
			{
				//重置照片界面状态
				mPhotoBgCanvasGroup.alpha = 0f;
				mTitlebg.anchoredPosition = _titleBgOriginalPos + new Vector2(0, _animConfig.TitleBgOffsetY);
				mPhotBtns.anchoredPosition = _photBtnsOriginalPos + new Vector2(0, _animConfig.PhotoBtnsOffsetY);
				mFrame.localScale = _frameOriginalScale * _frameFullScreenScale;
				
				mDiamondShow.SetActive(false);
				mPhotoShow.SetActive(true);
			});
			
			//照片界面动画 - Frame先执行，其他动画延迟
			float frameAnimEndTime = sequence.Duration() + _animConfig.PhotoAnimDuration;
			float otherAnimStartTime = frameAnimEndTime - _animConfig.OtherAnimDelay;
			if (otherAnimStartTime < sequence.Duration()) otherAnimStartTime = sequence.Duration();
			
			sequence.Append(mFrame.DOScale(_frameOriginalScale, _animConfig.PhotoAnimDuration).SetEase(_animConfig.PhotoEaseType));
			sequence.Insert(otherAnimStartTime, mPhotoBgCanvasGroup.DOFade(1f, _animConfig.OtherAnimDuration).SetEase(_animConfig.PhotoEaseType));
			sequence.Insert(otherAnimStartTime, mTitlebg.DOAnchorPos(_titleBgOriginalPos, _animConfig.OtherAnimDuration).SetEase(_animConfig.PhotoEaseType));
			sequence.Insert(otherAnimStartTime, mPhotBtns.DOAnchorPos(_photBtnsOriginalPos, _animConfig.OtherAnimDuration).SetEase(_animConfig.PhotoEaseType));
			
			sequence.OnComplete(() =>
			{
				_isAnim = false;
				Log.Info("[Panel_Succeed] 完整动画流程播放完毕");
			});
			
			sequence.SetUpdate(true);
			Log.Info("[Panel_Succeed] 完整动画流程开始播放");
		}
		
		/// <summary>
		/// 重置动画状态
		/// </summary>
		public void TestResetAnimation()
		{
			DOTween.KillAll();
			_isAnim = false;
			
			mDiamondShow.Show();
			mPhotoShow.Hide();
			
			mDiaTitle.localScale = Vector3.one;
			mDiaIcon.localScale = Vector3.one;
			mDiaBtns.localScale = Vector3.one;
			
			mPhotoBgCanvasGroup.alpha = 1f;
			if (_titleBgOriginalPos != Vector2.zero)
			{
				mTitlebg.anchoredPosition = _titleBgOriginalPos;
				mPhotBtns.anchoredPosition = _photBtnsOriginalPos;
				mFrame.localScale = _frameOriginalScale;
			}
			
			Log.Info("[Panel_Succeed] 动画状态已重置");
		}

		#endregion

protected override void Close()
{
	VideoAudioHelper.CancelCheck();
	
	// 如果视频正在播放，不播放BGM
	// 如果没有视频或视频未播放，播放BGM
	if (mPhotoAVPro == null || mPhotoAVPro.Control == null || !mPhotoAVPro.Control.IsPlaying())
	{
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}
	
	base.Close();

}

protected override void OnDestroy()
{
	VideoAudioHelper.CancelCheck();
	base.OnDestroy();
	mPhotoAVPro?.CloseMedia();
}
	}
	
	/// <summary>
	/// Panel_Succeed 动画配置
	/// </summary>
	[System.Serializable]
	public struct SucceedAnimConfig
	{
		[Header("=== 钻石界面动画 ===")]
		[FieldName("弹出动画时长")] public float DiamondAnimDuration;
		[FieldName("节点间延迟")] public float DiamondAnimDelay;
		[FieldName("缓动类型")] public Ease DiamondEaseType;
		
		[Header("=== 照片界面动画 ===")]
		[FieldName("Frame缩放动画时长")] public float PhotoAnimDuration;
		[FieldName("其他动画延迟(相对Frame动画结束)")] public float OtherAnimDelay;
		[FieldName("其他动画时长")] public float OtherAnimDuration;
		[FieldName("缓动类型")] public Ease PhotoEaseType;
		
		[Header("=== 位移参数 ===")]
		[FieldName("标题栏Y偏移")] public float TitleBgOffsetY;
		[FieldName("按钮栏Y偏移")] public float PhotoBtnsOffsetY;
		
		
		[Header("=== 其他 ===")]
		[FieldName("切换延迟(测试用)")] public float SwitchDelay;
		
		/// <summary>
		/// 默认配置
		/// </summary>
		public static SucceedAnimConfig Default => new SucceedAnimConfig
		{
			DiamondAnimDuration = 0.2f,
			DiamondAnimDelay = 0.03f,
			DiamondEaseType = Ease.OutQuint,
			PhotoAnimDuration = 0.9f,
			OtherAnimDelay = 0.2f,  //Frame动画结束前0.5秒开始其他动画
			OtherAnimDuration = 0.2f, //其他动画持续0.4秒
			PhotoEaseType = Ease.OutCirc,
			TitleBgOffsetY = 50f,
			PhotoBtnsOffsetY = -50f,
			SwitchDelay = 1f
		};
	}
}
