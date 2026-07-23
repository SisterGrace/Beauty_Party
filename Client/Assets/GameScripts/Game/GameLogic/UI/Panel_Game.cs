using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using DG.Tweening;
using Y_Base;
using TMPro;
using AudioType = TK_Engine.AudioType;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using RenderHeads.Media.AVProVideo;
using System.Threading;


namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Game : UIWindow
	{
		private OneSelectPhotoInfo _selectPhotoInfo;
		private Widget_CardBoard _cardBoard;
		private Widget_EliminateSlot _eliminateSlot;
		private Widget_StoreSlot _storeSlot;
		private Dictionary<PropType, PropBtnInfo> _propBtns = new Dictionary<PropType, PropBtnInfo>();

	private bool _isPropAnim;
	private GameObject _propAnimObj;
	private GameObject _freezeEff;
	private bool _countdownWarning;
	private bool _isBanner;
	#region 倒计时相关
		private bool _isStartTime2CountDown;//关卡总倒计时
		private bool _isStartTime1CountDown;//关卡暂停倒计时
		private float _allTimer2;
		private float _allTimer1;
		private float _countDownTime2;
		private float _countDownTime1;
		private float _timer;
		#endregion

		#region 加槽道具和提示道具相关
		//加槽使用次数
		private int _soltUseCount;
		//提示使用次数
		private int _tipUseCount;
		#endregion

		private Action _finishAvShow;
		private CanvasGroup _gameAvCanvasGroup;
		#region 视频超时检测
		private CancellationTokenSource _videoTimeoutCts;
		private const float VIDEO_TIMEOUT_SECONDS = 10f; // 视频超时时间（秒）
		#endregion
	protected override void OnRefresh()
	{
		if (userDatas.Length > 0)
		{
			_selectPhotoInfo = (OneSelectPhotoInfo)userDatas[0];
		}
		ABShow();
		_cardBoard = CreateWidgetByType<Widget_CardBoard>(mCardBoardNode);
		_eliminateSlot = CreateWidgetByType<Widget_EliminateSlot>(mSlotNode);
		_storeSlot = CreateWidgetByType<Widget_StoreSlot>(mStoreSlotNode);
		_cardBoard.Open(_eliminateSlot, _storeSlot, mCardMoving);
		_soltUseCount = 0;
		_tipUseCount = 0;
		_finishAvShow = null;
		UpdatePropBtns();
		mLevelName.SetLanguage("uitips_level", $"{UserController.Instance.Level}");
		//mLevelName.gameObject.Hide();
		_isPropAnim = false;
		GamePanelShow();
		SDKManager.Instance.LogEvent("Level_Enter",CommonEvent.ComLevelEvent);
		// 静音主页视频，播放背景音乐
		VideoAudioHelper.SwitchToPage("Panel_Game");
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener(EventType.GamePanelShow, GamePanelShow);
			GameEvent.AddEventListener<PropType>(EventType.UpdatePropBtnState, UpdatePropBtns);
			GameEvent.AddEventListener<PropType, bool, float>(EventType.DoPropAnim, PlayPropAnim);
			GameEvent.AddEventListener<float, bool>(EventType.FreezeTime, StartCountDown);
			GameEvent.AddEventListener<bool>(EventType.PauseGameTime, PauseGameCountDown);
			GameEvent.AddEventListener<bool>(EventType.SetGameMask, SetGameMask);
			GameEvent.AddEventListener(EventType.ShowGameClosePanel, ShowClosePanel);
			GameEvent.AddEventListener<Action>(EventType.OpenGameBgAvPro, ShowAVBg);
			//注册飞行特效目标位置
			FlySystem.Instance.RegisterPos(FlyReType.Revoke, mRevoke.transform);
			FlySystem.Instance.RegisterPos(FlyReType.MessUp, mMessUp.transform);
			FlySystem.Instance.RegisterPos(FlyReType.DestoryGruop, mDestoryGruop.transform);
			FlySystem.Instance.RegisterPos(FlyReType.MoveOut, mMoveOut.transform);
			FlySystem.Instance.RegisterPos(FlyReType.FreezeTime, mtimeNode.transform);
		}

		//游戏界面打开
		private void GamePanelShow()
		{
			//TODO：处理动画效果或者事件上报
			var levelInfo = LevelController.Instance.CurrentLevelInfo;
			_isBanner = levelInfo.IsBanner;
			//战斗界面动画效果
			ShowPanel(() =>
			{
				//检测是否有触发的引导
				GuideController.Instance.ClearTriggerGuide();
				if (levelInfo.TimeLevel)
				{
					mtimeNode.Show();
					mTime2CountTimeTxt.text = TimeUtil.SecondToTime2((long)levelInfo.CountdownTime, false);
					//计时提示
					Action call = () =>
					{
						StartCountDown(levelInfo.CountdownTime, true);
						GuideController.Instance.ClearTriggerGuide();
						GuideController.Instance.CheckTriggerGuide(levelInfo.GuideId);
					};
					GameEvent.Send(EventType.StartTimeTips, levelInfo, call);
					//将关卡字设置在倒计时下
					mLevelName.rectTransform.SetParent(mtimeBg1);
					//mLevelName.gameObject.Show();
					mLevelName.rectTransform.anchoredPosition = new Vector2(0, -75f);
					mLevelName.enableAutoSizing = false;
					mLevelName.fontSize = 53f;
					mLevelName.color = "#FF3969".GetColor();
				}
				else
				{
					mtimeNode.Hide();
					//mLevelName.gameObject.Show();

					//第六关的助推器引导得动画加载完才出现
					if (levelInfo.GuideId == 107) return;
					GuideController.Instance.CheckTriggerGuide(levelInfo.GuideId);
				}
			});
		}
		/// <summary>
		/// 开始倒计时
		/// </summary>
		/// <param name="time"></param>
		private void StartCountDown(float time, bool isAllCountDown)
		{
			if (isAllCountDown)
			{
				_isStartTime2CountDown = true;
				_isStartTime1CountDown = false;
				_countDownTime2 = time;
				_allTimer2 = time;
				_timer = 0;
				mTime2Progress.value = 1;

				_countdownWarning = false;
				SpineController.PlayAnim(_clockAnim, 0, "idle", true);
			}
			else
			{
				_isStartTime2CountDown = false;
				_isStartTime1CountDown = true;
				_countDownTime1 = time;
				_allTimer1 = time;
				_timer = 0;
				mLevelName.gameObject.Hide();
				mtime1Bar.Show();
				mtimeBg1.gameObject.Show();
				_freezeEff = FlySystem.Instance.ShowEff("efc_icon_match_snow_04", transform,
					transform, new Vector3(40, 55, 40));
				var cs = _freezeEff.GetComponent<ShowPropVfx>();
				cs.PlayPropVfx(false);
			}
		}

		private void PauseGameCountDown(bool isPause)
		{
			//如果此时冻结时间，则不进行关卡倒计时暂停
			if (_countDownTime1 > 0)
			{
				_isStartTime1CountDown = !isPause;
				return;
			}
			if (_countDownTime2 > 0)
			{
				_isStartTime2CountDown = !isPause;
			}

		}
		protected override void OnUpdate()
		{
			if (_isStartTime2CountDown)
			{
				_timer += Time.deltaTime;
				if (_timer >= 1)
				{
					_timer = 0;
					_countDownTime2--;
					mTime2CountTimeTxt.text = TimeUtil.SecondToTime2((long)_countDownTime2, false);
					mTime2Progress.value = _countDownTime2 / _allTimer2;
					timeWarning();
					if (_countDownTime2 <= 0)
					{
						_isStartTime2CountDown = false;
						//游戏结束
						CardsController.Instance.Lose(2);
						return;
					}
				}
			}
			if (_isStartTime1CountDown)
			{
				_timer += Time.deltaTime;
				if (_timer >= 1)
				{
					_timer = 0;
					_countDownTime1--;
					mTime1CountTimeTxt.text = $"{_countDownTime1}";
					mTime1Progress.value = _countDownTime1 / _allTimer1;
					if (_countDownTime1 <= 0)
					{
						_isStartTime1CountDown = false;
						_isStartTime2CountDown = true;
						//mLevelName.gameObject.Show();
						mtime1Bar.Hide();
						mtimeBg1.gameObject.Hide();
						if (_freezeEff != null && _freezeEff.activeSelf)
						{
							FlySystem.Instance.HideObj(_freezeEff);
						}
					}
				}
			}
		}

		private void ShowPanel(Action callback, bool isShow = true)
		{
			if (isShow)
			{
				if (_isBanner)
				{
					SDKManager.Instance.Button_ShowBanner();
				}
				mTopsUI.anchoredPosition = new Vector2(0, 300f);
				mCardBoardNode.anchoredPosition = new Vector2(1000f, 300f);
				mBottomsUI.anchoredPosition = new Vector2(0, -550f);
				mTopsUI.DOAnchorPosY(0, 0.4f).SetEase(Ease.OutBack);
				mCardBoardNode.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutBack);
				mBottomsUI.DOAnchorPosY(_isBanner ? 80f : 0f, 0.4f).SetEase(Ease.OutBack).OnComplete(() =>
				{
					callback?.Invoke();
				});
			}
			else
			{
				mTopsUI.DOAnchorPosY(300f, 0.4f).SetEase(Ease.Flash);
				mCardBoardNode.DOAnchorPosX(1000f, 0.4f).SetEase(Ease.Flash);
				mBottomsUI.DOAnchorPosY(-750f, 0.4f).SetEase(Ease.Flash);
				if (_isBanner)
				{
					SDKManager.Instance.Button_HideBanner();
				}
			}

		}
		//显示道具按钮,仅限于处理底下四个道具
		private void UpdatePropBtns(PropType propType = PropType.None)
		{
			if (propType == PropType.None)
			{
				var props = PropsController.Instance.GetTempPropsInfos(false);
				foreach (var prop in props)
				{
					PropBtnInfo btnInfo = _propBtns[prop.mType];
					btnInfo.mlock.SetActive(!prop.IsUnlock);
					btnInfo.count.transform.parent.gameObject.SetActive(prop.IsUnlock);
					if (!prop.IsUnlock) continue;
					btnInfo.count.gameObject.SetActive(prop.Num > 0);
					btnInfo.count.text = $"{prop.Num}";
					btnInfo.add.SetActive(!(prop.Num > 0));
					if (btnInfo.add.activeSelf)
					{
						PropsController.PropShowRVEvent(prop.mType,out HCRVPositionName adPos);
					}
				}
			}
			else
			{
				var oneprop = PropsController.Instance.GetCurPropsInfo(propType);
				PropBtnInfo btnInfo = _propBtns[propType];
				btnInfo.count.gameObject.SetActive(oneprop.Num > 0);
				btnInfo.count.text = $"{oneprop.Num}";
				btnInfo.add.SetActive(!(oneprop.Num > 0));
				if (btnInfo.add.activeSelf)
				{
					PropsController.PropShowRVEvent(propType,out HCRVPositionName adPos);
				}
			}
		}

		#region 道具按钮功能
		//撤回一步
		private void RevokeOneStep()
		{
			if (CardsController.Instance.CurPlayCardType == playCardType.clikcard) return;
			//先判断槽位是否为空
			PropsController.Instance.PropBtnClick(PropType.Revoke, this, () =>
			{
			});
		}
		//打乱卡牌
		private void MessUpCards()
		{
			if (CardsController.Instance.CurPlayCardType == playCardType.clikcard) return;
			PropsController.Instance.PropBtnClick(PropType.MessUp, this, () =>
			{
			});
		}
		//消除一组卡牌
		private void DestroyCards()
		{
			if (CardsController.Instance.CurPlayCardType == playCardType.clikcard) return;
			PropsController.Instance.PropBtnClick(PropType.DestoryGruop, this, () =>
			{
			});
		}
		//移出卡牌
		private void MoveOutCards()
		{
			if (CardsController.Instance.CurPlayCardType == playCardType.clikcard) return;
			PropsController.Instance.PropBtnClick(PropType.MoveOut, this, () =>
			{
			});
		}

		private void PlayPropAnim(PropType propType, bool isShow, float alpha)
		{
			if (!isShow)
			{
				CardsController.Instance.CurPlayCardType = playCardType.None;
			}
			_isPropAnim = isShow;
			mMask.SetActive(_isPropAnim);
			mMask.GetComponent<CanvasGroup>().alpha = alpha;
			if (!isShow && _propAnimObj != null)
			{
				FlySystem.Instance.HideObj(_propAnimObj);
				return;
			}
			var propInfo = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetByType(propType);
			switch (propType)
			{
				case PropType.MessUp:
					if (isShow)
					{
						_propAnimObj = FlySystem.Instance.ShowEff(propInfo.AnimName, transform,
							mCardMoving.transform, Vector3.one * 45f);
						var cs = _propAnimObj.GetComponent<ShowPropVfx>();
						cs.PlayPropVfx(false);
					}
					break;
				case PropType.DestoryGruop:
					if (isShow)
					{
						_propAnimObj = FlySystem.Instance.ShowEff(propInfo.AnimName, transform,
							mCardMoving.transform, Vector3.one * 45f);
						var cs = _propAnimObj.GetComponent<ShowPropVfx>();
						cs.PlayPropVfx(false);
					}
					break;
				case PropType.DrawCard:
					break;
			}
		}
		#endregion

		private void timeWarning()
		{
			if (_countDownTime2 <= 10)
			{
				AudioManager.Play(AudioType.Sound, AudioDef.clockRed);
				if (!_countdownWarning)
				{
					_countdownWarning = true;
					//播放报警动画
					SpineController.PlayAnim(_clockAnim, 0, "alarm", true);
				}
			}
			else
			{
				AudioManager.Play(AudioType.Sound, AudioDef.clockSlow);
			}

		}
	

		private void SetGameMask(bool isShow)
		{
			mMask2.SetActive(isShow);
		}

		#region 背景展示相关

		private void ABShow()
		{
			mAVProVideoUI.Hide();
			LoadImageAsync();
			//如果是B类用户，需要提前加载视频
			if (_selectPhotoInfo.ABType == 1 || _selectPhotoInfo.IsVideo)
			{
				Debug.Log($"[Panel_Game] 视频加载地址: {_selectPhotoInfo.VideoUrl}");
				mMediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, _selectPhotoInfo.VideoUrl, false);
			}
		}
	private void ShowAVBg(Action callback)
	{
		_finishAvShow = callback;
		mAVProVideoUI.Show();
		//UI界面隐藏
		ShowPanel(null, false);
		
		// 启用视频声音
		mMediaPlayer.Control.SetVolume(1f);
		mMediaPlayer.Control.MuteAudio(false);
		
		mMediaPlayer.Play();

		// 开启视频超时检测
		StartVideoTimeoutCheck().Forget();
	}
		/// <summary>
		/// 视频播放超时检测
		/// </summary>
		private async UniTaskVoid StartVideoTimeoutCheck()
		{
			// 取消之前的超时检测
			CancelVideoTimeout();

			_videoTimeoutCts = new CancellationTokenSource();
			var token = _videoTimeoutCts.Token;

			try
			{
				// 等待超时时间
				await UniTask.Delay(TimeSpan.FromSeconds(VIDEO_TIMEOUT_SECONDS), cancellationToken: token);

				// 如果没有被取消，说明超时了
				Log.Warning($"视频播放超时（{VIDEO_TIMEOUT_SECONDS}秒），自动触发完成回调");
				OnVideoTimeout();
			}
			catch (OperationCanceledException)
			{
				// 正常取消，视频已经播放完成，不需要处理
				Log.Info("视频超时检测已取消，视频正常播放完成");
			}
		}

		/// <summary>
		/// 取消视频超时检测
		/// </summary>
		private void CancelVideoTimeout()
		{
			if (_videoTimeoutCts != null)
			{
				_videoTimeoutCts.Cancel();
				_videoTimeoutCts.Dispose();
				_videoTimeoutCts = null;
			}
		}

	/// <summary>
	/// 视频超时处理
	/// </summary>
	private void OnVideoTimeout()
	{
		_videoTimeoutCts = null;
		// 恢复背景音乐
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
		_finishAvShow?.Invoke();
		_finishAvShow = null;
	}

		private void ShowClosePanel()
		{
			//UI界面隐藏
			ShowPanel(null, false);
		}
		private void LoadImageAsync()
		{
			_photoLoad.LoadUIImg(_selectPhotoInfo.Imgurl);
		}

	private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
	{
		Log.Info($"此时关卡界面的视频播放器状态：{et}");
		switch (et)
		{
	case MediaPlayerEvent.EventType.Started:
		// 开始视频音频检测（传入视频URL和页面名称）
		VideoAudioHelper.StartVideoAudioCheck(mp, _selectPhotoInfo.VideoUrl, "Panel_Game");
		break;
	case MediaPlayerEvent.EventType.FinishedPlaying://播放结束，弹出下一个视频
		CancelVideoTimeout();
		// 停止检测并恢复BGM
		VideoAudioHelper.StopVideoAudioCheck("Panel_Game");
		_finishAvShow?.Invoke();
		_finishAvShow = null;
		break;
	case MediaPlayerEvent.EventType.Error://播放出错，直接触发完成回调
		Log.Warning($"视频播放出错：{errorCode}，自动触发完成回调");
		CancelVideoTimeout();
		// 停止检测并恢复BGM
		VideoAudioHelper.StopVideoAudioCheck("Panel_Game");
		_finishAvShow?.Invoke();
		_finishAvShow = null;
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
		#endregion
		private void ExitLevel()
		{
			var _selectPhotoInfo = LevelController.Instance.CurLevelSelectPhotoInfo;
			SDKManager.Instance.LogEvent("Level_End", new Dictionary<string, object>()
					{
						{ "Level_Time", LevelController.Instance.GetGameTime(TimeUtil.GetTimeStamp()) },
						{ "End_Type", 3 }
					},
				CommonEvent.ComLevelEvent);

			Close();
			GameModule.UI.CloseUI<Panel_Game>();
			GameModule.UI.ShowUI<Panel_Home>();
			LevelController.Instance.ExitLevel(false);
		}
protected override void Close()
{
	// 恢复主页视频声音
	VideoAudioHelper.CancelCheck();
            VideoAudioHelper.StopVideoAudioCheck("Panel_Game", restoreBGM: true);
            base.Close();
}

protected override void OnDestroy()
{
	VideoAudioHelper.CancelCheck();
	if (_isBanner)
			{
				SDKManager.Instance.Button_HideBanner();
			}
			mMediaPlayer?.CloseMedia();
			base.OnDestroy();
			GameEvent.RemoveEventListener(EventType.GamePanelShow, GamePanelShow);
			GameEvent.RemoveEventListener<PropType>(EventType.UpdatePropBtnState, UpdatePropBtns);
			GameEvent.RemoveEventListener<PropType, bool, float>(EventType.DoPropAnim, PlayPropAnim);
			GameEvent.RemoveEventListener<float, bool>(EventType.FreezeTime, StartCountDown);
			GameEvent.RemoveEventListener<bool>(EventType.PauseGameTime, PauseGameCountDown);
			GameEvent.RemoveEventListener<bool>(EventType.SetGameMask, SetGameMask);
			GameEvent.RemoveEventListener(EventType.ShowGameClosePanel, ShowClosePanel);
			GameEvent.RemoveEventListener<Action>(EventType.OpenGameBgAvPro, ShowAVBg);
			FlySystem.Instance.UnRegisterPos(FlyReType.LevelCash);
		}
	}
	//道具按钮信息
	public struct PropBtnInfo
	{
		public TextMeshProUGUI count;
		public Image BtnIcon;
		public GameObject icon;
		public GameObject add;
		public GameObject mlock;
		public GameObject FreeObj;
		public UIButton btn;

		public PropBtnInfo(TextMeshProUGUI count, GameObject icon, GameObject add, GameObject mlock, UIButton btn, GameObject freeObj)
		{
			this.count = count;
			this.icon = icon;
			this.add = add;
			this.mlock = mlock;
			this.btn = btn;
			BtnIcon = btn.GetComponent<Image>();
			FreeObj = freeObj;
		}
	}
}
