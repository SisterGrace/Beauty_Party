using TK_Engine;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameBase;
using TMPro;
using UnityEngine.UI;
using Spine.Unity;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_EndlessItem : UIWidget, IListDataItem<EndlessItemData>
	{
		private List<OneEndlessItem> _items = new List<OneEndlessItem>();
		private bool _isLock;
		private CfgRecharge _rechargeItem;
		private CfgReward _rewards;
		public int _itemId;
		private bool _isShowAd;
		bool isHaveRandom = false;
		bool isHaveLastRandom = false;
		private int _curLoopTimes;

		private SkeletonGraphic _mLockAnim;
		public void InitData(EndlessItemData data)
		{
			transform.localScale = Vector3.one;
			mCanvasGroup.alpha = 1;
			mGet.transform.localScale = Vector3.one;
			_itemId = data.Id;
			_isLock = data.IsLock;
			_curLoopTimes = data.CurLoopTimes;
			_rechargeItem = data.RechargeItem;
			mTickNode.Hide();
			foreach (var item in _items)
			{
				item.Obj.SetActive(false);
			}
			_rewards = ConfigLoader.Instance.Tables.TbCfgReward.Get(data.RechargeItem.RewardId);
			isHaveRandom = PhotoController.Instance.IsHaveRandomPhotos(ActivityType.UntoldTreasure);
			isHaveLastRandom = PhotoController.Instance.IsHaveLastRandomPhotos(ActivityType.UntoldTreasure);
			mAllRewards.gameObject.SetActive(!_rewards.IsRandom || (!isHaveRandom && !isHaveLastRandom));
			mPhotoNode.SetActive(_rewards.IsRandom && isHaveRandom);
			if (!_rewards.IsRandom)
			{
				CreateNorRewItem(_rewards.Reward);

			}
			else
			{
				CreateRandomRewItem();
			}
			mlock.SetActive(data.IsLock);
			UpdateAdUnLockInfo(true);
		}
		public void SetItemData(EndlessItemData data)
		{
			InitData(data);
		}

		private void CreateNorRewItem(List<TBRewardData> tRewards)
		{
			//设置大小和字体大小和内间距
			Vector2 size = Vector2.zero;
			int fontSize = 51;
			float txtOffestY = 0;
			float spacing = 0;
			if (tRewards.Count == 1)
			{
				size = new Vector2(180, 180);
				fontSize = 51;
				txtOffestY = -75f;
			}
			else if (tRewards.Count == 2)
			{
				size = new Vector2(150, 150);
				fontSize = 40;
				txtOffestY = -60f;
				spacing = -15f;
			}
			else if (tRewards.Count == 3)
			{
				size = new Vector2(120, 120);
				fontSize = 30;
				txtOffestY = -40f;
				spacing = -30;
			}
			_mLayoutGroup.spacing = spacing;
			for (int i = 0; i < tRewards.Count; i++)
			{
				var item = _items[i];
				item.Obj.SetActive(true);
				item.Icon.sprite = GameModule.Resource.LoadAsset<Sprite>(PropsController.GetRewardIcon(tRewards[i]));
				item.Icon.SetNativeSize();
				item.Count.text = PropsController.GetRewardCount(tRewards[i]);
				item.Count.fontSizeMax = fontSize;
				item.Count.rectTransform.anchoredPosition =
					new Vector2(item.Count.rectTransform.anchoredPosition.x, txtOffestY);
				//item.Icon.rectTransform.sizeDelta = size;
				item.Icon.SetNativeSize();
			}
		}

		private void CreateRandomRewItem()
		{
			int curIndex = (isHaveRandom || isHaveLastRandom) ? 0 : 1;
			var rewCfg = _rewards.Reward[curIndex];
			if (!isHaveRandom && !isHaveLastRandom)
			{
				List<TBRewardData> tRewards = new List<TBRewardData>();
				tRewards.Add(rewCfg);
				CreateNorRewItem(tRewards);
			}
			else
			{
				var acLoopTimes = UserController.Instance.GetOneActivityData(ActivityType.UntoldTreasure).LoopCount;
				var randomPhotoId = (acLoopTimes == _curLoopTimes) ? PhotoController.Instance.GetCurActivityPhotoIds(ActivityType.UntoldTreasure)[Mathf.Abs(rewCfg.ID)] :
						PhotoController.Instance.GetLastActivityPhotoIds(ActivityType.UntoldTreasure)[Mathf.Abs(rewCfg.ID)];
				mPhotoNode.GetComponent<Item_PhotoLoad>().LoadPhoto(randomPhotoId, (int)rewCfg.Count);
			}
		}
		public void Unlock(Action callBack)
		{
			SpineController.PlayAnim(_mLockAnim,0,"animation",false,callBack: () =>
			{
				mlock.SetActive(false);
				_mLockAnim.Initialize(true);
				_isLock = false;
				callBack?.Invoke();
				//无尽宝藏解锁上报
				SDKManager.Instance.LogEvent("Endless_Level_Unlock","Endless_Level",_itemId+1);
			});
			/* mlock.SetActive(false);
			_isLock = false;
			callBack?.Invoke();
			//无尽宝藏解锁上报
			SDKManager.Instance.LogEvent("Endless_Level_Unlock", "Endless_Level", _itemId + 1);
			//发送红点事件
			GameEvent.Send(EventType.EndlessRedPoint, _rechargeItem.Price <= 0, ""); */
		}
		public void BuyOneItem()
		{
			//如果是看广告解锁的，领取之后需要重置次数
			if (_rechargeItem.AdUnlockTimes > 0)
			{
				UserController.Instance.SetAcItemUnLock(_rechargeItem.Id, 0);
			}
			GameEvent.Send(EventType.SetEndlessShowAnim, true);
			mTickNode.Show();
			mTick.localScale = Vector3.zero;
			ParticleSystem par = meff.GetComponent<ParticleSystem>();
			//按钮变小
			Sequence seq = DOTween.Sequence();
			seq.Append(mGet.transform.DOScale(0, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
			{
				par.Play();
			}));
			seq.Append(mTick.DOScale(1, 0.2f).SetEase(Ease.OutBack));
			seq.Append(transform.DOScale(0, 0.2f));
			seq.Insert(0.4f, mCanvasGroup.DOFade(0, 0.2f));
			seq.OnComplete(() =>
			{
				GameEvent.Send(EventType.RefreshEndlessItem, this);
			});
			UserController.Instance.AddRewGetList(ActivityType.UntoldTreasure, _itemId);

			//发送奖励
			var allItemsPos = new List<Vector3>();
			if (mAllRewards.gameObject.activeSelf)
			{
				foreach (var item in _items)
				{
					if (item.Obj.activeSelf)
					{
						allItemsPos.Add(item.Obj.transform.position);
					}
				}
			}
			else
			{
				allItemsPos.Add(mPhotoNode.transform.position);
			}
			PropsController.FlyReward(allItemsPos, _rewards, ActivityType.UntoldTreasure, _curLoopTimes);
			//隐藏奖励图标
			foreach (var item in _items)
			{
				item.Obj.SetActive(false);
			}
		}
		private void UpdateAdUnLockInfo(bool isLogEvent = false)
		{
			_isShowAd = RechargeController.UpdateBuyButtonUI(mGetPrice, _rechargeItem, mAdicon, isLogEvent);
		}

		public void Hide()
		{
		}
	}

	public struct EndlessItemData
	{
		//0-无 1-左 2-上 3-右
		public int Id;
		public bool IsLock;
		public CfgRecharge RechargeItem;
		public int CurLoopTimes;

		public EndlessItemData(int id, bool isLock, CfgRecharge rechargeItem, int curLoopTimes)
		{
			Id = id;
			IsLock = isLock;
			RechargeItem = rechargeItem;
			CurLoopTimes = curLoopTimes;
		}
	}

	public struct OneEndlessItem
	{
		public Image Icon;
		public TextMeshProUGUI Count;
		public GameObject Obj;
		public OneEndlessItem(GameObject obj, TextMeshProUGUI count)
		{
			Icon = obj.GetComponent<Image>();
			Count = count;
			Obj = obj;
		}
	}
}
