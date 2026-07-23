using Config.config;
using Y_Base;
using System;
using System.Collections.Generic;
using System.Linq;
using Config.common;
using TK_Engine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static ConfigLoader;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Gacha : UIWindow
	{
		#region UI数据字段
		UIListWidget<Widget_PhotoPackItem, PhotoPackItemInfo> _photoPackWidgetList;
		private int _curPackId = 0;

		/// <summary>
		/// 是否正在执行抽卡流程
		/// </summary>
		private bool _isGachaInProgress = false;
		#endregion
		#region 卡包轮播效果动画相关
		/// <summary>
		/// 当前选中的卡包索引
		/// </summary>
		private int _currentPackIndex = 0;
		/// <summary>
		/// 移动方向：1 向右（下一个），-1 向左（上一个）
		/// </summary>
		private int _moveDirection = 1;
		/// <summary>
		/// 是否正在播放动画
		/// </summary>
		private bool _isAnimating = false;
		/// <summary>
		/// 中心卡包的缩放比例
		/// </summary>
		private const float CENTER_SCALE = 1.2f;
		/// <summary>
		/// 侧边卡包的缩放比例
		/// </summary>
		private const float SIDE_SCALE = 0.8f;
		/// <summary>
		/// 动画持续时间
		/// </summary>
		private const float ANIM_DURATION = 0.3f;
		/// <summary>
		/// 卡包之间的间距（保留用于可能的扩展）
		/// </summary>
		private float _packSpacing = 0f;
		#endregion
		private bool _isHomePanel;
		private bool _isVipPack => UserController.Instance.GetVIPLevel > 0;
		private bool _isCommonPack;
		private Widget_GachaAnimation _gachaAnimation;

		private RectTransform _contentRect;
		private RectTransform[] _packItemRects;
		private CanvasGroup[] _packItemCanvasGroups;

		private List<PhotoPackItemInfo> PackList => GachaController.Instance.PhotoPackList;

	protected override void OnRefresh()
	{
		GameEvent.Send(EventType.SetGachaRedPoint, false, "");
		RefreshCurrency();
		GachaController.Instance.InitPhotoPacks();
		CreatePhotoPackList();
		InitCarouselEffect();
		SDKManager.Instance.LogEvent("Draw_Show");
		_isHomePanel = GameModule.UI.HasWindow<Panel_Home>();
		if (_isHomePanel)
		{
			SDKManager.Instance.Button_HideBanner();
		}
		// 静音主页视频，播放背景音乐
		VideoAudioHelper.SwitchToPage(this.WindowName);
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int>(EventType.RefreshCurrency, RefreshCurrency);
			GameEvent.AddEventListener(EventType.ReadFailOrder, Close);
			GameEvent.AddEventListener(EventType.UpdatePackDisplay, UpdatePackDisplay);
		}
		void RefreshCurrency(int id = 1)
		{
			mAllDiamondCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);
		}

		void CreatePhotoPackList()
		{
			if (_photoPackWidgetList == null)
			{
				_photoPackWidgetList = CreateWidget<UIListWidget<Widget_PhotoPackItem, PhotoPackItemInfo>>(mContent.gameObject);
			}
			_photoPackWidgetList.SetDatas(PackList);
			CachePackItemComponents();
		}

		private void CachePackItemComponents()
		{
			if (_contentRect == null)
			{
				_contentRect = mContent.GetComponent<RectTransform>();
			}
			var items = _photoPackWidgetList.items;
			int count = items.Count;
			_packItemRects = new RectTransform[count];
			_packItemCanvasGroups = new CanvasGroup[count];
			for (int i = 0; i < count; i++)
			{
				if (items[i] == null) continue;
				_packItemRects[i] = items[i].gameObject.GetComponent<RectTransform>();
				_packItemCanvasGroups[i] = items[i].transform.GetComponent<CanvasGroup>();
			}
		}
		/// <summary>
		/// 更新当前卡包显示状态
		/// </summary>
		private void UpdatePackDisplay()
		{
			if (_currentPackIndex >= 0 && _currentPackIndex < PackList.Count)
			{
				var packInfo = PackList[_currentPackIndex];
				_curPackId = packInfo.PackId;
				
				var gachaCfg = ConfigLoader.Instance.Tables.TbCfgGacha.GetOrDefault(_curPackId);
				if (GachaController.Instance.IsPackAllCollected(_curPackId))
				{
					mCurPackName.SetLanguage("uitips_gacha_nocard");
				}
				else
				{
					int pityCount = UserController.Instance.GetGachaPityCount(_curPackId);
					mCurPackName.SetLanguage(packInfo.Name, $"{gachaCfg.GachaPityCount - pityCount}");
				}
				if (gachaCfg != null)
				{
					if (gachaCfg.VIPLock && !_isVipPack)
					{
						mAdDraw.gameObject.Hide();
						mDiamondDraw.gameObject.Hide();
						mOpenVip.gameObject.Show();
					}
					else
					{
						mOpenVip.gameObject.Hide();
						if (gachaCfg.GachaAdTimes > 0)
						{
							mAdDraw.gameObject.Show();
							mDiamondDraw.gameObject.Show();
							int leftTimes = UserController.Instance.GetPhotoPackAdTimes(packInfo.PackId);
							mAdCost.text = $"{leftTimes}/{gachaCfg.GachaAdTimes}";
							string adPos = gachaCfg.Id == 0 ? HCRVPositionName.RV_CommonCard.ToString() : HCRVPositionName.RV_VIPCard.ToString();
							SDKManager.Instance.LogEvent("RV_Button_Show", "Position", adPos);
						}
						else
						{
							mAdDraw.gameObject.Hide();
							mDiamondDraw.gameObject.Show();
						}
						mCurCost.text = $"<sprite name=\"icon_zuanshi\">{gachaCfg.GachaCost}"; ;
					}
				}
			}
		}

		/// <summary>
		/// 获取当前选中的卡包ID
		/// </summary>
		public int GetCurrentPackId()
		{
			return _curPackId;
		}

		/// <summary>
		/// 判断指定的卡包是否是当前轮播选中的卡包
		/// </summary>
		public bool IsCurrentCenterPack(int packId)
		{
			return packId == _curPackId;
		}

		/// <summary>
		/// 关闭所有卡包的缩略图循环列表
		/// </summary>
		public void HideAllShrinkPicLists()
		{
			if (_photoPackWidgetList == null) return;

			var items = _photoPackWidgetList.items;
			if (items == null) return;

			for (int i = 0; i < items.Count; i++)
			{
				items[i]?.HideShrinkPicList();
			}
		}

		/// <summary>
		/// 显示当前中间卡包的缩略图循环列表
		/// </summary>
		private void ShowCurrentPackShrinkPicList()
		{
			if (_photoPackWidgetList == null) return;

			var currentItem = _photoPackWidgetList.GetItem(_currentPackIndex);
			currentItem?.ShowShrinkPicList();
		}

		#region 抽卡UI交互
		/// <summary>
		/// 钻石抽卡入口
		/// </summary>
		private void OnClickDiamondDraw(GameObject obj)
		{
			if (_isGachaInProgress) return;

			var gachaCfg = ConfigLoader.Instance.Tables.TbCfgGacha.GetOrDefault(_curPackId);
			if (gachaCfg == null) return;

			if (UserController.Instance.CheckConsume(ERewardType.Currency, 0, gachaCfg.GachaCost))
			{
				UserController.Instance.UserConsume(ERewardType.Currency, 0, gachaCfg.GachaCost);
				ExecuteGacha(_curPackId);
			}
			else
			{
				GameEvent.Send(EventType.ShowTips, "uitps_floattip_notcoins", true);
			}
			SDKManager.Instance.LogEvent("Draw_Diamond_Click", "Pack_Id", _curPackId);
		}

		/// <summary>
		/// 广告抽卡入口
		/// </summary>
		private void OnClickAdDraw(GameObject obj)
		{
			if (_isGachaInProgress) return;

			var gachaCfg = ConfigLoader.Instance.Tables.TbCfgGacha.GetOrDefault(_curPackId);
			if (gachaCfg == null) return;

			int leftTimes = UserController.Instance.GetPhotoPackAdTimes(_curPackId);
			if (leftTimes <= 0)
			{
				GameEvent.Send(EventType.ShowTips, "uitips_ad_tips", true);
				return;
			}
			HCRVPositionName adPos = _curPackId == 0 ? HCRVPositionName.RV_CommonCard : HCRVPositionName.RV_VIPCard;
			SDKManager.Instance.Button_ShowRV(adPos, (success) =>
			{
				if (success)
				{
					UserController.Instance.UsePhotoPackAdTime(_curPackId);
					ExecuteGacha(_curPackId);
					SDKManager.Instance.LogEvent("Draw_Ad_Click", "Pack_Id", _curPackId);
				}
			});
		}

		/// <summary>
		/// 执行抽卡流程（动画+结果）
		/// </summary>
		private void ExecuteGacha(int packId)
		{
			_isGachaInProgress = true;

			GachaResult result = GachaController.Instance.DoGacha(packId);

			Log.Info($"抽卡结果：{result.PhotoId},{result.PhotoRating},{result.IsDuplicate},{result.RefundDiamonds},{result.IsPityGuarantee}");

			PlayGachaAnimation(result, () =>
			{
				OnGachaComplete(result, packId);
			});
		}

		/// <summary>
		/// 播放抽卡动画
		/// </summary>
		private void PlayGachaAnimation(GachaResult result, Action onComplete)
		{
			if (_gachaAnimation == null)
			{
				_gachaAnimation = CreateWidgetByType<Widget_GachaAnimation>(manimNode);
			}
			onComplete?.Invoke();
			_gachaAnimation.Open(result);
		}

		/// <summary>
		/// 抽卡完成处理
		/// </summary>
		private void OnGachaComplete(GachaResult result, int packId)
		{
			_isGachaInProgress = false;

			GachaController.Instance.ApplyGachaResult(result, packId);

			RefreshCurrency();
			UpdatePackDisplay();
		}
		#endregion
		#region 卡包轮播效果动画
		/// <summary>
		/// 初始化轮播效果
		/// </summary>
		private void InitCarouselEffect()
		{
			if (PackList.Count == 0) return;
			_currentPackIndex = 0;
			SetSwitchButtonState(_currentPackIndex);
			mAllPacks.horizontal = false;
			mAllPacks.vertical = false;

			if (_packSpacing == 0f)
			{
				var layoutGroup = mContent.GetComponent<HorizontalLayoutGroup>();
				if (layoutGroup != null)
					_packSpacing = layoutGroup.spacing;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
			SetCarouselState(_currentPackIndex, false);
			UpdatePackDisplay();
		}
		private void SetSwitchButtonState(int index)
		{
			_isCommonPack = index == 0;
			mcommonMask.SetActive(!_isCommonPack);
			msupperMask.SetActive(_isCommonPack);
		}
		/// <summary>
		/// 点击切换按钮触发
		/// </summary>
		private void ShowPhotoPack()
		{
			if (_isAnimating || PackList.Count <= 1) return;

			HideAllShrinkPicLists();

			int nextIndex = _currentPackIndex + _moveDirection;

			if (nextIndex >= PackList.Count)
			{
				_moveDirection = -1;
				nextIndex = _currentPackIndex + _moveDirection;
			}
			else if (nextIndex < 0)
			{
				_moveDirection = 1;
				nextIndex = _currentPackIndex + _moveDirection;
			}

			SwitchToPack(nextIndex);
		}

		/// <summary>
		/// 切换到指定索引的卡包
		/// </summary>
		private void SwitchToPack(int targetIndex)
		{

			if (_isAnimating) return;
			if (targetIndex < 0 || targetIndex >= PackList.Count) return;
			if (targetIndex == _currentPackIndex) return;

			_isAnimating = true;
			_currentPackIndex = targetIndex;
			HideAllShrinkPicLists();
			SetCarouselState(targetIndex, true);
			UpdatePackDisplay();

			SetSwitchButtonState(targetIndex);
		}

		/// <summary>
		/// 获取指定卡包在Content中的实际X位置（考虑布局组件）
		/// </summary>
		private float GetPackCenterX(int index)
		{
			if (_packItemRects == null || index < 0 || index >= _packItemRects.Length)
				return 0f;

			var itemRect = _packItemRects[index];
			if (itemRect == null) return 0f;

			float itemWidth = itemRect.rect.width;
			float centerX = itemRect.anchoredPosition.x + itemWidth * (0.5f - itemRect.pivot.x);

			return centerX;
		}

		/// <summary>
		/// 设置轮播状态（位置和缩放）
		/// </summary>
		private void SetCarouselState(int centerIndex, bool animate)
		{
			if (_packItemRects == null || _packItemRects.Length == 0) return;

			var viewportRect = mAllPacks.viewport;
			float viewportWidth = viewportRect.rect.width;

			float packCenterX = GetPackCenterX(centerIndex);
			float targetX = -packCenterX + viewportWidth / 2f;

			Sequence sequence = DOTween.Sequence();

			if (animate)
			{
				sequence.Join(_contentRect.DOAnchorPosX(targetX, ANIM_DURATION).SetEase(Ease.OutQuad));
			}
			else
			{
				_contentRect.anchoredPosition = new Vector2(targetX, _contentRect.anchoredPosition.y);
			}

			for (int i = 0; i < _packItemRects.Length; i++)
			{
				var itemRect = _packItemRects[i];
				if (itemRect == null) continue;

				float targetScale = (i == centerIndex) ? CENTER_SCALE : SIDE_SCALE;
				float alpha = (i == centerIndex) ? 1f : 0f;

				if (animate)
				{
					sequence.Join(itemRect.DOScale(targetScale, ANIM_DURATION).SetEase(Ease.OutQuad));
					var cg = _packItemCanvasGroups[i];
					if (cg != null)
						sequence.Join(cg.DOFade(alpha, ANIM_DURATION).SetEase(Ease.OutQuad));
				}
				else
				{
					itemRect.localScale = Vector3.one * targetScale;
					var cg = _packItemCanvasGroups[i];
					if (cg != null)
						cg.alpha = alpha;
				}
			}

			if (animate)
			{
				sequence.OnComplete(() =>
				{
					_isAnimating = false;
					ShowCurrentPackShrinkPicList();
				});
			}
			else
			{
				_isAnimating = false;
				ShowCurrentPackShrinkPicList();
			}
		}

		#endregion
	protected override void Close()
	{
            VideoAudioHelper.StopVideoAudioCheck(this.WindowName, restoreBGM: true);
            base.Close();
	}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<int>(EventType.RefreshCurrency, RefreshCurrency);
			GameEvent.RemoveEventListener(EventType.UpdatePackDisplay, UpdatePackDisplay);
			GameEvent.RemoveEventListener(EventType.ReadFailOrder, Close);
			GameModule.UI.RemoveOrderPanel<Panel_Gacha>(true);
			if (_isHomePanel)
			{
				SDKManager.Instance.Button_ShowBanner();
			}
		}
	}
}
