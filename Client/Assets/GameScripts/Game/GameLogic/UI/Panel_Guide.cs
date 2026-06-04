using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using GameBase;
using TMPro;
using UnityEngine.UI;

namespace GameLogic
{
	[Window(UILayer.Guide)]
	public partial class Panel_Guide : UIWindow
	{
		private CfgGuide _cfgGuide;
		private Widget_HandGuide _curHandGuide;
		private Widget_JianTouGuide _curJianTou;
		protected override void OnRefresh()
		{
			GuideController.Instance.WaitClickObjs.Clear();
			bool isNormalGuide = GuideController.Instance.IsGuideId;
			_cfgGuide = GuideController.Instance.GetGuideCfg(!isNormalGuide);
			SetGuide();
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener(EventType.NextGuide, NextGuide);
			GameEvent.AddEventListener(EventType.HideGuide, HideGuide);
		}

		private void SetGuide()
		{
			mBattleGuideNode.SetActive(_cfgGuide.GuideType == GuideType.CardCell||_cfgGuide.GuideType == GuideType.Prop);
			mUIGuideNode.SetActive(_cfgGuide.GuideType == GuideType.GameUI);
			mContinue.gameObject.SetActive(_cfgGuide.IsEveryClick);
			switch (_cfgGuide.GuideType)
			{
				case GuideType.GameUI:
					GameObject btn = GameModule.UI.UIRoot.Find(_cfgGuide.ButtonPath).gameObject;
					if (_cfgGuide.IsEveryClick && _cfgGuide.IsMask)
					{
						mContinue.transform.SetParent(mUIGuideNode.transform);
						mContinue.transform.SetSiblingIndex(1);
						mUImask.Show();
						mLookAt.gameObject.Show();
						mUImask.GetComponent<Mask>().showMaskGraphic = false;
						Sprite sp=btn.GetComponent<Image>().sprite;
						if (sp != null)
						{
							mLookAt.sprite = sp;
							mLookAt.SetNativeSize();
						}
						else
						{
							RectTransform btnRect = btn.GetComponent<RectTransform>();
							mLookAt.rectTransform.sizeDelta = new Vector2(btnRect.sizeDelta.x, btnRect.sizeDelta.y);
						}
						mLookAt.transform.position = btn.transform.position;
					}
					else if(!_cfgGuide.IsMask&&!_cfgGuide.IsEveryClick&&_cfgGuide.IsHand)
					{
						mUImask.Hide();
						GuideController.Instance.WaitClickObjs.Add(new GuideData(btn));
					}
					else if (_cfgGuide.IsHand && _cfgGuide.IsMask)
					{
						mUImask.Show();
						mLookAt.gameObject.Hide();
						mUImask.GetComponent<Mask>().showMaskGraphic = true;
						GuideController.Instance.WaitClickObjs.Add(new GuideData(btn));
						btn.transform.SetParent(mBtnNode);
					}
					SetHandOrJiantou(mUIGuideNode.transform, btn.transform, false);
					break;
				case GuideType.CardCell:
					if (_cfgGuide.IsEveryClick)
					{
						mContinue.transform.SetParent(mBattleGuideNode.transform);
						mContinue.transform.SetSiblingIndex(1);
					}
					var cards = CardsController.Instance.GetCardsByCardId(_cfgGuide.ClickCardIds);
					foreach (var card in cards)
					{
						if (!card.IsInSolt)
						{
							GuideController.Instance.WaitClickObjs.Add(new GuideData(card.gameObject));
							card.SetParent(mForNode);
						}
					}
					mFormask.SetActive(_cfgGuide.IsMask);
					SetHandOrJiantou(mBattleGuideNode.transform, GuideController.Instance.WaitClickObjs[0].Tr, true);
					break;
				case GuideType.Prop:
					mFormask.SetActive(_cfgGuide.IsMask);
					GameObject Propbtn = GameModule.UI.UIRoot.Find(_cfgGuide.ButtonPath).gameObject;
					GuideController.Instance.WaitClickObjs.Add(new GuideData(Propbtn));
					var oneProp = GuideController.Instance.WaitClickObjs[0];
					oneProp.Tr.SetParent(mForNode);
					SetHandOrJiantou(mBattleGuideNode.transform, GuideController.Instance.WaitClickObjs[0].Tr, true);
					break;
			}

			SetTipsInfo();
		}

		private void SetTipsInfo()
		{
			bool isCardOrPropTips = _cfgGuide.GuideType == GuideType.CardCell || _cfgGuide.GuideType == GuideType.Prop;
			RectTransform tipRect = isCardOrPropTips? mBattleTips:mUITips;
			TextMeshProUGUI tipTxt = isCardOrPropTips ? mBattleTipsTxt : mUITipsTxt;
			if (!string.IsNullOrEmpty(_cfgGuide.TipsKey))
			{
				tipRect.gameObject.Show();
				tipTxt.SetLanguage(_cfgGuide.TipsKey);
				if (_cfgGuide.TipsPos.Count > 0)
				{
					tipRect.anchoredPosition = new Vector2(_cfgGuide.TipsPos[0], _cfgGuide.TipsPos[1]);
				}
			}
			else
			{
				tipRect.gameObject.Hide();
			}
		}
		private void NextGuide()
		{
			foreach (var item in GuideController.Instance.WaitClickObjs)
			{
				item.Restore();
			}
			GuideController.Instance.WaitClickObjs.Clear();
			bool isNormalGuide = GuideController.Instance.IsGuideId;
			_cfgGuide = GuideController.Instance.GetGuideCfg(!isNormalGuide);
			SetGuide();
		}

		private void SetHandOrJiantou(Transform parent,Transform target,bool isEffGuide)
		{
			if (_cfgGuide.IsHand)
			{
				if (_curHandGuide == null)
				{
					_curHandGuide = CreateWidgetByType<Widget_HandGuide>(parent);
				}
				_curHandGuide.Open(target,isEffGuide);
			}

			if (_cfgGuide.IsJiantou)
			{
				if (_curJianTou == null)
				{
					_curJianTou = CreateWidgetByType<Widget_JianTouGuide>(parent);
				}
				_curJianTou.Open(target,_cfgGuide);
			}
		}
		private void HideGuide()
		{
			foreach (var item in GuideController.Instance.WaitClickObjs)
			{
				item.Restore();
			}
			GuideController.Instance.WaitClickObjs.Clear();
			mBattleGuideNode.Hide();
			mUIGuideNode.Hide();
			mContinue.gameObject.Hide();
			//如果这一步触发引导没有下一步，提前关闭引导
			if (_cfgGuide.NextId == 0)
			{
				GuideController.Instance.NextTriggerGuide();
			}
		}
		
		protected override void Close()
		{
			base.Close();
		}

		protected override void OnDestroy()
		{
			HideGuide();
			base.OnDestroy();
			GameEvent.RemoveEventListener(EventType.NextGuide, NextGuide);
			GameEvent.RemoveEventListener(EventType.HideGuide, HideGuide);
		}
	}
}
