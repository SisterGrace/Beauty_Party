using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Y_Base;
using RenderHeads.Media.AVProVideo;
using UnityEngine.UI;


namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Home : UIWindow
	{
		private static readonly Vector2 PhotoAnchoredPosA = new(-50f, 225f);
		private static readonly Vector2 PhotoAnchoredPosB = new(-50f, 30f);

		#region 生命值相关
		private float timer = 1;

		#endregion

		#region UI动画
		//是否UI进入动画
		private bool _isEnterAnim = false;
		private CanvasGroup _topCanvasGroup;
		private CanvasGroup _bottomCanvasGroup;
		private Vector2 _topStarPos;
		private Vector2 _bottomStarPos;
		private Action _endAnimCallback;
		private bool _isAtOnceGuide;
		#endregion

		#region 活动相关
		private List<Widget_ActivityIcon> _activityList;
		private bool _isOpenShopCountDown;
		#endregion
		private bool _checkReward = false;
		//是否立即展示Banner广告
		private bool _isShowBanner = false;
		//主页视频动画正向播放
		private bool _Upsidedown = false;
		protected override void OnRefresh()
		{
			_activityList = new List<Widget_ActivityIcon>();
			_isShowBanner = true;
			_isAtOnceGuide = true;
			RefreshCurrency(-1);
			UpdateLevelInfo();
			SetActivity();
			SetRedNot();
			SetPhotoRedNot(-1);
			PlayHomeVideo(UserController.Instance.GetCurUserPhotoID);
			if (userDatas.Length > 0)
			{
				_isEnterAnim = (bool)userDatas[0];
				_endAnimCallback = (Action)userDatas[1];
				if (userDatas.Length > 2)
				{
					_isShowBanner = (bool)userDatas[2];
				}

				if (userDatas.Length > 3)
				{
					_isAtOnceGuide = (bool)userDatas[3];
				}
				if (_isEnterAnim)
				{
					PlayAllUIAnim(_isEnterAnim, () =>
					{
						CheckGuide(_isAtOnceGuide);
						_endAnimCallback?.Invoke();
					});
				}
			}
			else
			{
				CheckGuide(_isAtOnceGuide);
			}

			if (_isShowBanner)
			{
				SDKManager.Instance.Button_ShowBanner();
			}
			GameScene.Instance.LogEventHomeShow();
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int>(EventType.RefreshCurrency, RefreshCurrency);
			GameEvent.AddEventListener(EventType.RefreshLevel, UpdateLevelInfo);
			GameEvent.AddEventListener<bool>(EventType.SetAnimMask, SetAnimMask);
			GameEvent.AddEventListener<bool>(EventType.StartGuide1, StartGuide1);
			GameEvent.AddEventListener(EventType.RefreshActivity, SetActivity);
			GameEvent.AddEventListener(EventType.ShopRedPoint, SetRedNot);
			GameEvent.AddEventListener<int>(EventType.PhotoRedPoint, SetPhotoRedNot);
			GameEvent.AddEventListener(EventType.OpenLevelInfo, OpenLevelInfo);
			GameEvent.AddEventListener<bool>(TKEventType.MainHomeAnim, SetMainHomeAnim);
			GameEvent.AddEventListener(EventType.CloseAllActivityBtns, CloseAllActivityBtns);
			GameEvent.AddEventListener<int>(EventType.RefreshHomeVideo, PlayHomeVideo);
			GameEvent.AddEventListener(EventType.MuteHomeVideo, MuteHomeVideo);
			GameEvent.AddEventListener(EventType.UnmuteHomeVideo, UnmuteHomeVideo);
			//飞行奖励目标位置注册
			FlySystem.Instance.RegisterPos(FlyReType.prop, mLevel.transform);
			//FlySystem.Instance.RegisterPos(FlyReType.CashOut,mBtnMMO);

		}

		protected override void OnUpdate()
		{
			if (_isOpenShopCountDown)
			{
				timer += Time.deltaTime;
				if (timer > 1)
				{
					var t = UserController.Instance.GetFreeGetGoldTime - TimeUtil.GetTimeStamp();
					if (t <= 0)
					{
						_isOpenShopCountDown = false;
						mShopRedNot.SetActive(true);
						return;
					}
					if (mShopRedNot.activeSelf)
					{
						mShopRedNot.SetActive(false);
					}

				}
			}
		}
		private void OpenLevelInfo()
		{
			GameModule.UI.ShowUI<Panel_LevelPhoto>();
		}
		//刷新货币
		private void RefreshCurrency(int id)
		{
			switch (id)
			{
				case 0:
					mGameGoldCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
					break;
				case -1:
					mGameGoldCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
					break;
			}
		}

		private void UpdateLevelInfo()
		{
			var cfg = LevelController.GetCurrentLevelInfo();
			string btnIcon = "btn_zjm_easy";
			string levelTxtKey = "btn_home_norlevel";
			switch (cfg.Difficulty)
			{
				case 0:
					break;
				case 1:
					btnIcon = "btn_zjm_hard";
					levelTxtKey = "uitps_levelnfo_BOSS";
					break;
			}
			mLevel.GetComponent<Image>().SetSprite(btnIcon, true);
			mCurLevel.SetLanguage(levelTxtKey, $"{UserController.Instance.Level}");
			//根据AB不同类型设置不同布局位置
			Vector2 photoPos = UserController.Instance.GetCurUserType == 0 ? PhotoAnchoredPosA : PhotoAnchoredPosB;
			mPhotots.transform.GetComponent<RectTransform>().anchoredPosition = photoPos;
		}

		//用于事件驱动外部调用
		private void SetMainHomeAnim(bool isEnter)
		{
			PlayAllUIAnim(isEnter, null);
		}
		private void PlayAllUIAnim(bool isEnter, Action callback = null)
		{
			//UI进入动画
			if (isEnter)
			{
				_topStarPos = new Vector2(0, -117.4f);
				_bottomStarPos = new Vector2(0, 368f);
				mTopUI.anchoredPosition = new Vector2(_topStarPos.x, -_topStarPos.y);
				mBottomUI.anchoredPosition = new Vector2(_bottomStarPos.x, -_bottomStarPos.y);
				_topCanvasGroup.alpha = 0;
				_bottomCanvasGroup.alpha = 0;
				Sequence seq = DOTween.Sequence();
				seq.Insert(0, mTopUI.DOAnchorPosY(_topStarPos.y, 0.3f).SetEase(Ease.Linear));
				seq.Insert(0, mBottomUI.DOAnchorPosY(_bottomStarPos.y, 0.3f).SetEase(Ease.Linear));
				seq.Insert(0.1f, _topCanvasGroup.DOFade(1, 0.2f).SetEase(Ease.Linear));
				seq.Insert(0.1f, _bottomCanvasGroup.DOFade(1, 0.2f).SetEase(Ease.Linear));
				seq.OnComplete(() =>
				{
					callback?.Invoke();
				});
			}
			else
			{
				Sequence seq1 = DOTween.Sequence();
				seq1.Insert(0, mTopUI.DOAnchorPosY(-_topStarPos.y, 0.3f).SetEase(Ease.Linear));
				seq1.Insert(0, mBottomUI.DOAnchorPosY(-_bottomStarPos.y, 0.3f).SetEase(Ease.Linear));
				seq1.Insert(0.1f, _topCanvasGroup.DOFade(0, 0.2f).SetEase(Ease.Linear));
				seq1.Insert(0.1f, _bottomCanvasGroup.DOFade(0, 0.2f).SetEase(Ease.Linear));
				seq1.OnComplete(() =>
				{
					callback?.Invoke();
				});
			}
		}

		private void SetAnimMask(bool isShow)
		{
			mAnimmask.SetActive(isShow);
			if (!isShow)
			{
				CheckGuide(false);
				GameModule.UI.CheckOrderPanel();
			}
		}

		private void StartGuide1(bool isShow)
		{
			mTopUI.gameObject.SetActive(isShow);
			mLevel.gameObject.SetActive(isShow);
		}
		//设置活动按钮
		private void SetActivity()
		{
			var list = UserController.Instance.GetAllActivityData();
			list.RemoveAll(data => data.Id == ActivityType.Discard);
			var count = list.Count;
			if (list.Count < _activityList.Count && _activityList.Count > 0)
			{
				var tempList = new List<Widget_ActivityIcon>();
				int listCount = _activityList.Count;
				for (int a = 0; a < listCount; a++)
				{
					var t = _activityList[a];
					if (!list.Contains(t.Data))
					{
						tempList.Add(t);
						t.gameObject.Hide();
					}
				}

				foreach (var item in tempList)
				{
					_activityList.Remove(item);
				}
			}
			var itemCount = _activityList.Count;
			foreach (var item in _activityList)
			{
				item.gameObject.Hide();
			}
			for (int i = 0; i < count; i++)
			{
				if (i < itemCount)
				{
					_activityList[i].Open(list[i]);
					_activityList[i].gameObject.Show();
				}
				else
				{
					Transform parent = null;
					var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(list[i].Id);
					if (cfg.ActType != ActivityType.Gacha)
					{
						switch (cfg.LeftOrRight)
						{
							case 0:
								parent = mAllAct;
								break;
							case 1:
								parent = mtopLeft;
								break;
							case 2:
								parent = mtopRifgt;
								break;
						}
					}
					else//因为抽卡的需要单独生成其它位置
					{
						parent = mBottomUI;
					}
					Widget_ActivityIcon newAct = CreateWidgetByType<Widget_ActivityIcon>(parent);
					newAct.Open(list[i]);
					_activityList.Add(newAct);
				}
			}
		}
		private void SetRedNot()
		{
			_isOpenShopCountDown = UserController.Instance.CheckFreeGetGoldTime();
			bool hasPhotoUpdate = UserController.Instance.HasAnyPhotoProductUpdated();
			bool isShow = !_isOpenShopCountDown || hasPhotoUpdate;
			mShopRedNot.SetActive(isShow);
		}
		//设置相册红点
		private void SetPhotoRedNot(int typeID)
		{
			bool isShow = UserController.Instance.IsPhototLook();
			mPhotoRedNot.SetActive(isShow);
		}
		private void CheckGuide(bool isAtOnceCheck = true)
		{
			//是否立即检测引导
			if (isAtOnceCheck)//用于非奖励的引导
			{
				/*if (UserController.Instance.Level == 1 && GuideController.IsGuide1())
				{
					GuideController.Instance.AtOnceGuide(1);
				}*/
			}
			else//用于奖励发送完毕后执行引导
			{
				/*if (UserController.Instance.Level == 2 && GuideController.IsGuide1())
				{
					GuideController.Instance.AtOnceGuide(1);
				}
				int UnLockLevel = UserController.Instance.GetCurUserType == 1 ? 6 : 51;//抽卡引导
				if (UserController.Instance.Level == UnLockLevel && GuideController.IsGuide3())
				{
					GuideController.Instance.AtOnceGuide(3);
				}*/
			}


		}


		private void CloseAllActivityBtns()
		{
			if (_activityList != null && _activityList.Count > 0)
			{
				int count = _activityList.Count;
				for (int i = 0; i < count; i++)
				{
					_activityList[i].Destroy();
				}

			}
		}

		#region 主页视频播放或者展示动画

		private void PlayHomeVideo(int photoId)
		{
			var phtcfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(photoId);
			mAVProVideoUI.Hide();
			if (phtcfg == null)//没有找到视频配置，播放默认视频
			{

			}
			else
			{
				string iUrl = PropsController.GetImgUrl(phtcfg.ImageID, phtcfg.ImageType, phtcfg.ABGroup);
				LoadImageAsync(iUrl);
				if (phtcfg.IsVideo)
				{
					string vUrl = PropsController.GetVideoUrl(phtcfg.ImageID, phtcfg.ABGroup, phtcfg.IsVideo);
					mvideoPLayer.AudioVolume = 1f;
					mvideoPLayer.AudioMuted = false;
					mvideoPLayer.AutoOpen = true;
					mvideoPLayer.OpenMedia(MediaPathType.AbsolutePathOrURL, vUrl);
				}
			}
			_Upsidedown = false;

			/*mvideoPLayer.PlaybackRate = -1;
			mvideoPLayer.Play();*/
		}
		private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
		{
			//Log.Info("当前主界面Av状态：" + et, "错误：" + errorCode.ToString());
			switch (et)
			{
				case MediaPlayerEvent.EventType.Started:
					//开始播放，关闭加载动画
					mAVProVideoUI.Show();
					// 开始视频音频检测
					VideoAudioHelper.StartVideoAudioCheck(mp);
					break;
				case MediaPlayerEvent.EventType.FinishedPlaying:
					//播放结束，倒放
					/*_Upsidedown = !_Upsidedown;
					int rate = _Upsidedown? -1 : 1;
					mvideoPLayer.PlaybackRate = rate;
					mvideoPLayer.Play();*/
					break;
				case MediaPlayerEvent.EventType.Error:
					//播放错误，关闭加载动画
					mAVProVideoUI.Hide();
					// 停止检测并恢复BGM
					VideoAudioHelper.StopVideoAudioCheck();
					break;
		}
	}

	private void LoadImageAsync(string url)
	{
		_photoLoad.LoadUIImg(url);
	}

	/// <summary>
	/// 静音主页视频（其他Panel打开时调用）
	/// </summary>
		private void MuteHomeVideo()
		{
			if (mvideoPLayer != null)
			{
				mvideoPLayer.AudioMuted = true;
				mvideoPLayer.AudioVolume = 0f;
			}
		}

		/// <summary>
		/// 恢复主页视频声音（回到主页时调用）
		/// </summary>
		private void UnmuteHomeVideo()
		{
			if (mvideoPLayer != null)
			{
				mvideoPLayer.AudioMuted = false;
				mvideoPLayer.AudioVolume = 1f;
			}
			// 停止背景音乐
			AudioManager.Stop(TK_Engine.AudioType.Music, true);
			// 如果视频正在播放，开始检测音频
			if (mvideoPLayer != null && mvideoPLayer.Control != null && mvideoPLayer.Control.IsPlaying())
			{
				VideoAudioHelper.StartVideoAudioCheck(mvideoPLayer);
			}
		}

		protected override void Close()
		{
			// 关闭主页面时，取消音频检测
			VideoAudioHelper.CancelCheck();
			// 静音视频但不停止播放
			if (mvideoPLayer != null)
			{
				mvideoPLayer.AudioMuted = true;
				mvideoPLayer.AudioVolume = 0f;
			}
			// 恢复背景音乐（给其他页面使用）
			AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
			base.Close();
		}

		protected override void OnDestroy()
		{
			VideoAudioHelper.CancelCheck();
			mvideoPLayer?.CloseMedia();
			base.OnDestroy();
			GameEvent.RemoveEventListener<int>(EventType.RefreshCurrency, RefreshCurrency);
			GameEvent.RemoveEventListener(EventType.RefreshLevel, UpdateLevelInfo);
			GameEvent.RemoveEventListener<bool>(EventType.SetAnimMask, SetAnimMask);
			GameEvent.RemoveEventListener<bool>(EventType.StartGuide1, StartGuide1);
			GameEvent.RemoveEventListener(EventType.RefreshActivity, SetActivity);
			GameEvent.RemoveEventListener(EventType.ShopRedPoint, SetRedNot);
			GameEvent.RemoveEventListener<int>(EventType.PhotoRedPoint, SetPhotoRedNot);
			GameEvent.RemoveEventListener(EventType.OpenLevelInfo, OpenLevelInfo);
			GameEvent.RemoveEventListener<bool>(TKEventType.MainHomeAnim, SetMainHomeAnim);
			GameEvent.RemoveEventListener(EventType.CloseAllActivityBtns, CloseAllActivityBtns);
			GameEvent.RemoveEventListener<int>(EventType.RefreshHomeVideo, PlayHomeVideo);
			GameEvent.RemoveEventListener(EventType.MuteHomeVideo, MuteHomeVideo);
			GameEvent.RemoveEventListener(EventType.UnmuteHomeVideo, UnmuteHomeVideo);
			SDKManager.Instance.Button_HideBanner();
		}

		#endregion
	}
}
