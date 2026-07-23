using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_LevelPhoto : UIWindow
	{
		private UIListWidget<Widget_OneSelectPhoto, OneSelectPhotoInfo> _levelPhotoList;
		//是否Boos免费
		public bool _isVipBoos => UserController.Instance.GetVIPLevel > 1;
		public int _IsAdRefresh = 0;
		public bool _isLogEventRefresh = false;
		//是否是Boss关卡
		private bool _isBossLevel = false;
		//当前选择的照片索引
		private int _currentSelectIndex = 0;

	protected override void OnRefresh()
	{
		_IsAdRefresh = UserController.Instance.CurAdLevelRefreshState;
		_currentSelectIndex = 0;
		//首先判断是boss关卡还是普通关卡
		UpdateLevelPhoto();
		CheckGuide();
		if (!SDKManager.Instance.GetLevelChooseReported)
		{
			SDKManager.Instance.GetLevelChooseReported = true;
			SDKManager.Instance.LogEvent("Level_Choose", CommonEvent.ComLevelEvent);
		}
		// 静音主页视频，播放背景音乐
		VideoAudioHelper.SwitchToPage("Panel_LevelPhoto");
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int>(EventType.LevelPhotoRefresh, RefreshSinglePhoto);
			GameEvent.AddEventListener(EventType.EnterGame, EnterGame);
		}

		private void UpdateLevelPhoto()
		{
			if (_levelPhotoList == null)
			{
				_levelPhotoList = CreateWidget<UIListWidget<Widget_OneSelectPhoto, OneSelectPhotoInfo>>(mContent.gameObject);
			}
			var levelInfo = LevelController.GetCurrentLevelInfo();
			_isBossLevel = levelInfo.Difficulty > 0;
			OnSuperscriptShow();
			string titleIcon = !_isBossLevel ? "bg_gqzp_bt" : "bg_gqzp_bt2";
			string titleKey = !_isBossLevel ? "uitps_levelnfo_choosephoto" : "uitps_levelnfo_boss";
			mTitleBM.sprite = GameModule.Resource.LoadAsset<Sprite>(titleIcon);
			mTitleTxt.SetLanguage(titleKey);
			if (_isBossLevel)//如果是boss关卡
			{
				List<OneSelectPhotoInfo> tempList = new List<OneSelectPhotoInfo>();
				var curId = PhotoController.Instance.GetCurLevelPhotoIds[0];

				var phCfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(curId);
				//需要判断此时的AB面
				string phUrl = PropsController.GetImgUrl(phCfg.ImageID, phCfg.ImageType, phCfg.ABGroup);
				string viUrl = PropsController.GetVideoUrl(phCfg.ImageID, phCfg.ABGroup, phCfg.IsVideo);
				OneSelectPhotoInfo info = new OneSelectPhotoInfo(0, phCfg.ID, phCfg.ABGroup, phUrl, viUrl, true, 3, phCfg.IsVideo, true, _IsAdRefresh);
				tempList.Add(info);
				_levelPhotoList.SetDatas(tempList);
			}
			else//普通关卡
			{
				OnRefreshlevelPhoto();
			}

		}

		private void OnRefreshlevelPhoto(bool isOpen = true)
		{
			List<OneSelectPhotoInfo> tempList = new List<OneSelectPhotoInfo>();
			var phIdlist = PhotoController.Instance.GetCurLevelPhotoIds;
			for (int i = 0; i < 3; i++)
			{
				CfgPhotos cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(phIdlist[i]);
				if (cfg != null)
				{
					string phUrl = PropsController.GetImgUrl(cfg.ImageID, cfg.ImageType, cfg.ABGroup);
					string viUrl = PropsController.GetVideoUrl(cfg.ImageID, cfg.ABGroup, cfg.IsVideo);
					OneSelectPhotoInfo info = new OneSelectPhotoInfo(i, cfg.ID, cfg.ABGroup, phUrl, viUrl, false, cfg.ContentRating, cfg.IsVideo, false, _IsAdRefresh);
					tempList.Add(info);
				}
			}
			_levelPhotoList.SetDatas(tempList);
			_levelPhotoList.funcOnItemClick = null;
			_levelPhotoList.funcOnItemClick += index =>
			{
				OnSuperscriptShow(index);
			};
			// 强制刷新布局，确保布局组件计算完成后再设置选中
			LayoutRebuilder.ForceRebuildLayoutImmediate(mContent as RectTransform);
			// 先初始化默认选中项的位置（不播放动画）
			if (_levelPhotoList.items.Count > _currentSelectIndex)
			{
				_levelPhotoList.items[_currentSelectIndex].InitSelectPosition();
			}
			_levelPhotoList.selectIndex = _currentSelectIndex;

		}
		//刷新单个图片
		private void RefreshSinglePhoto(int index)
		{
			//Boss关卡不支持刷新
			//每一关只能刷新两次，第一次免费，第二次AD刷新
			if (_isBossLevel) return;
			//检查照片是否还在加载中，加载中不允许刷新
			if (!IsLoadEnd(index))
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}
			//判断是免费刷新还是还是激励刷新
			if (_IsAdRefresh == 0)
			{
				_IsAdRefresh = 1;
				UserController.Instance.CurAdLevelRefreshState = 1;
				DoRefreshPhoto(index);
				//这里需要把刷新按钮设置成AD状态
			}
			else
			{
				SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_Refresh, (success) =>
				{
					if (success)
					{
						_IsAdRefresh = 2;
						UserController.Instance.CurAdLevelRefreshState = 2;
						DoRefreshPhoto(index);
						//这里需要把刷新按钮设置成无刷新状态,即隐藏
					}
				});
			}
		}
		private void DoRefreshPhoto(int index)
		{
			// 1. 调用数据层刷新指定索引的照片ID
			PhotoController.Instance.SetLevelUsePhotoId(true, index);
			//获取新的照片配置
			var phIdlist = PhotoController.Instance.GetCurLevelPhotoIds;
			var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(phIdlist[index]);
			if (cfg != null)
			{
				string phUrl = PropsController.GetImgUrl(cfg.ImageID, cfg.ImageType, cfg.ABGroup);
				string viUrl = PropsController.GetVideoUrl(cfg.ImageID, cfg.ABGroup, cfg.IsVideo);
				OneSelectPhotoInfo info = new OneSelectPhotoInfo(index,
				 cfg.ID,
				  cfg.ABGroup,
				   phUrl,
				   viUrl,
				   false,
				   cfg.ContentRating,
				   cfg.IsVideo, false, _IsAdRefresh);
				if (_levelPhotoList.datas != null && _levelPhotoList.datas.Count > index)
				{
					_levelPhotoList.datas[index] = info;
				}

				if (_levelPhotoList.items != null && _levelPhotoList.items.Count > index)
				{
					_levelPhotoList.items[index].SetItemData(info);
				}
				//设置刷新按钮状态（跳过已在SetItemData中更新的index）
				for (int i = 0; i < _levelPhotoList.items.Count; i++)
				{
					if (i == index) continue;
					_levelPhotoList.items[i].SetRefreshButtonState(_IsAdRefresh);
				}
			}
		}
		private void EnterGame()
		{
			selectPhoto(_levelPhotoList.GetData(_currentSelectIndex));
		}
		private void selectPhoto(OneSelectPhotoInfo info)
		{
			if (!IsLoadEnd(info.Index))
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}
			if (_isBossLevel)
			{
				if (_isVipBoos)
				{
					StartGame(info);
				}
				else
				{
					SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_CrushLevel, (success) =>
					{
						if (success)
						{
							StartGame(info);
						}
					});
				}
				return;
			}
			switch (info.Index)
			{
				case 0:
					StartGame(info);
					break;
				case 1:
					SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_LevelSelect, (success) =>
					{
						if (success)
						{
							StartGame(info);
						}
					});
					break;
				case 2:
					SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_LevelSelect_2, (success) =>
					{
						if (success)
						{
							StartGame(info);
						}
					});
					break;
			}
		}

		private void StartGame(OneSelectPhotoInfo info)
		{
			GameModule.UI.CloseUI<Panel_Home>();
			if (GameModule.UI.HasWindow<Panel_NoAd>())
			{
				GameModule.UI.CloseUI<Panel_NoAd>();
			}
			SDKManager.Instance.Button_HideBanner();
			Close();
			//GameModule.UI.ShowUI<Panel_GameLoading>();
			GameModule.UI.ShowUI<Panel_Game>(info);
			LevelController.Instance.CurLevelSelectPhotoInfo = info;

		}
		/// <summary>
		/// 判断是否加载完毕
		/// </summary>
		/// <returns></returns>
		private bool IsLoadEnd(int index = 0)
		{
			bool isEnd = true;
			var item = _levelPhotoList.items[index];
			if (item._isLoad)
			{
				return false;
			}
			return isEnd;
		}

		private void CheckGuide()
		{
			/* if (UserController.Instance.Level == 6)
			{
				//重置数量，避免频繁打开面板
				UserController.Instance.ResetOneProp(PropType.Prompt);
				UserController.Instance.ResetOneProp(PropType.AddSolt);
				//设置免费道具数量
				UserController.Instance.SetOneProp(PropType.Prompt, 3, isFree: true);
				UserController.Instance.SetOneProp(PropType.AddSolt, 1, isFree: true);
			} */
		}

		private void OnSuperscriptShow(int index = 0)
		{
			if (_isBossLevel)
			{
				mCommon.SetActive(_isVipBoos);
				mAD.SetActive(!_isVipBoos);
				mVIP.Hide();
				if (!_isVipBoos)
				{
					//设置AD按钮事件上报事件
					//RV按钮上报:看视频解锁Boss关卡
					SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_CrushLevel.ToString());
				}
				return;
			}
			_currentSelectIndex = index;
			mCommon.SetActive(index == 0);
			mAD.SetActive(index == 1 || index == 2);
			mVIP.Hide();
			if (index == 1)
			{
				//设置AD按钮事件上报事件
				//RV按钮上报：看视频挑战精选关卡
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_LevelSelect.ToString());
			}
			else if (index == 2)
			{
				//RV按钮上报：看视频挑战第三个精选关卡
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_LevelSelect_2.ToString());
			}
		}
	protected override void Close()
	{
            VideoAudioHelper.StopVideoAudioCheck("Panel_LevelPhoto", restoreBGM: true);
            base.Close();
		GameModule.UI.RemoveOrderPanel<Panel_LevelPhoto>();
	}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<int>(EventType.LevelPhotoRefresh, RefreshSinglePhoto);
			GameEvent.RemoveEventListener(EventType.EnterGame, EnterGame);
		}
	}
}
