using System;
using TK_Engine;
using UnityEngine;
using Config.common;
using Config.config;
using Y_Base;
using UnityEngine.UI;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_ActivityIcon : UIWidget
	{
		private static readonly Vector2 EnterIconSize = new(225, 225);
		private static readonly Vector2 GachaAnchorHalf = new(0, 0.5f);
		private static readonly Vector2 GachaAnchoredPosA = new(120f, 225f);
		private static readonly Vector2 GachaAnchoredPosB = new(120f, 30f);

		private ActityData _data;
		private CfgActivity _cfg;
		private float _timer;
		private Action _unregisterRedPoint;
		private bool _isOpenShopCountDown;

		public ActityData Data => _data;

		public void Open(ActityData data)
		{
			_data = data;
			_cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(_data.Id);
			gameObject.name = $"Widget_{_data.Id}";

			if (_data.Id == ActivityType.DreamPass)
			{
				FlySystem.Instance.RegisterPos(FlyReType.DreamPassKey, transform);
			}

			mstar.SetActive(_cfg.OpenBtnEff);
			mEnter.transform.GetComponent<Item_ShineEffect>().enabled = _cfg.OpenBtnFlash;

			if (_data.Id == ActivityType.Gacha)
			{
				SetupGachaStyle();
			}
			else
			{
				SetupEnterStyle();
			}

			RegisterRedPointEvent();
		}

		private void SetupEnterStyle()
		{
			mEnter.gameObject.Show();
			mGacha.gameObject.Hide();
			rectTransform.sizeDelta = EnterIconSize;
			_timer = 1;
			mEnter.GetComponent<Image>().SetSprite(_cfg.Icon);
			mTimeNode.Hide();
			mname.SetLanguage(_cfg.ActivityName);
		}

		private void SetupGachaStyle()
		{
			mGacha.gameObject.Show();
			mEnter.gameObject.Hide();
			rectTransform.anchorMin = GachaAnchorHalf;
			rectTransform.anchorMax = GachaAnchorHalf;
			//根据AB不同类型设置不同布局位置
			Vector2 gachaPos = UserController.Instance.GetCurUserType == 0 ? GachaAnchoredPosA : GachaAnchoredPosB;
			rectTransform.anchoredPosition = gachaPos;
		}

		private void RegisterRedPointEvent()
		{
			if (_unregisterRedPoint != null || _cfg.RedType <= 0) return;

			switch (_data.Id)
			{
				case ActivityType.DreamPass:
					GameEvent.AddEventListener<bool, string>(EventType.DreamRedPoint, ShowRedPoint);
					_unregisterRedPoint = () => GameEvent.RemoveEventListener<bool, string>(EventType.DreamRedPoint, ShowRedPoint);
					_data.CheckDreamPassGetRew(false, out int count);
					ShowRedPoint(count > 0, "");
					break;
				case ActivityType.UntoldTreasure:
					GameEvent.AddEventListener<bool, string>(EventType.EndlessRedPoint, ShowRedPoint);
					_unregisterRedPoint = () => GameEvent.RemoveEventListener<bool, string>(EventType.EndlessRedPoint, ShowRedPoint);
					ShowRedPoint(UserController.Instance.CheckRewIsFree(ActivityType.UntoldTreasure), "");
					break;
				case ActivityType.NewPlayerPack:
					GameEvent.AddEventListener<bool, string>(EventType.NewPlayerPackRedPoint, ShowRedPoint);
					_unregisterRedPoint = () => GameEvent.RemoveEventListener<bool, string>(EventType.NewPlayerPackRedPoint, ShowRedPoint);
					ShowRedPoint(!_data.IsOpen, "");
					break;
				case ActivityType.DayCheckIn:
					GameEvent.AddEventListener<bool, string>(EventType.DayCheckInRedPoint, ShowRedPoint);
					_unregisterRedPoint = () => GameEvent.RemoveEventListener<bool, string>(EventType.DayCheckInRedPoint, ShowRedPoint);
					ShowRedPoint(UserController.Instance.CheckDayCheckInRedPoint(), "");
					break;
				case ActivityType.Shop:
					GameEvent.AddEventListener(EventType.ShopRedPoint, SetShopRedNot);
					_unregisterRedPoint = () => GameEvent.RemoveEventListener(EventType.ShopRedPoint, SetShopRedNot);
					SetShopRedNot();
					break;
				case ActivityType.Gacha:
					GameEvent.AddEventListener<bool, string>(EventType.SetGachaRedPoint, ShowRedPoint);
					_unregisterRedPoint = () => GameEvent.RemoveEventListener<bool, string>(EventType.SetGachaRedPoint, ShowRedPoint);
					ShowRedPoint(UserController.Instance.CheckGachaAdTime(), "");
					break;
			}
		}

		private void Click(GameObject obj)
		{
			switch (_cfg.ActType)
			{
				case ActivityType.DreamPass:
					GameModule.UI.ShowUI<Panel_DreamPass>(_data);
					break;
				case ActivityType.Adfree:
					GameModule.UI.ShowUI<Panel_NoAd>();
					break;
				case ActivityType.UntoldTreasure:
					GameModule.UI.ShowUI<Panel_EndlessTreasure>(_data);
					break;
				case ActivityType.Gacha:
					GuideController.Instance.NextGuide();
					GameModule.UI.ShowUI<Panel_Gacha>();
					break;
				case ActivityType.VIP:
					GameModule.UI.ShowUI<Panel_VIP>();
					break;
				case ActivityType.NewPlayerPack:
					GameModule.UI.ShowUI<Panel_StarterPack>(_data);
					break;
				case ActivityType.DayCheckIn:
					GameModule.UI.ShowUI<Panel_DayCheckIn>(_data);
					break;
				case ActivityType.Shop:
					GameModule.UI.ShowUI<Panel_Shop>();
					break;
				case ActivityType.Discard:
					GameModule.UI.ShowUI<Panel_Discard>();
					break;
				case ActivityType.H5Check:
					SDKManager.Instance.ForceShowH5Ad();
					break;
			}
		}

		private void ShowRedPoint(bool isShow, string info)
		{
			if (_cfg.ActType == ActivityType.Gacha)
			{
				mGachaRedNot.SetActive(isShow);
				return;
			}
			mRedNot.SetActive(isShow);
			mRedCount.text = string.IsNullOrEmpty(info) ? "!" : info;
		}

		private void SetShopRedNot()
		{
			_isOpenShopCountDown = UserController.Instance.CheckFreeGetGoldTime();
			bool hasPhotoUpdate = UserController.Instance.HasAnyPhotoProductUpdated();
			mRedNot.SetActive(!_isOpenShopCountDown || hasPhotoUpdate);
		}

		public void UpdateActivityName(ActityData data)
		{
			if (!(data.EndTime > 0) && _cfg != null)
			{
				mname.SetLanguage(_cfg.ActivityName);
			}
		}

		protected override void OnUpdate()
		{
			if (!_isOpenShopCountDown) return;

			_timer += Time.deltaTime;
			if (_timer < 1) return;
			_timer = 0;

			var t = UserController.Instance.GetFreeGetGoldTime - TimeUtil.GetTimeStamp();
			if (t <= 0)
			{
				_isOpenShopCountDown = false;
				mRedNot.SetActive(true);
				return;
			}
			if (mRedNot.activeSelf)
			{
				mRedNot.SetActive(false);
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_unregisterRedPoint?.Invoke();
		}
	}
}
