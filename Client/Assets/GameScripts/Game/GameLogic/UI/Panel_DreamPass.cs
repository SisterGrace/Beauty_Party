using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using DG.Tweening;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_DreamPass : UIWindow
	{
		private UILoopListWidget<OnePassItem,OnePassItemInfo> _passList;
		private ActityData _activityData;
		private List<CfgDreamPass> _allPassList;
		private bool _isOpenCountDown = false;
		private int timeId =0;
		private int _curLevel;
		private int _maxPassLevel;
		private CfgRecharge _rechargeItem;
		private bool _isShowAd = false;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_activityData = (ActityData)userDatas[0];
			}
			_allPassList = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
			_isOpenCountDown = _activityData.EndTime > 0;
			CreatPassItemList();
			UpdataInfo();
			SDKManager.Instance.LogEvent("Pass_Show"); 
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<CfgRecharge>(EventType.OpenDreamPass,ActivatePass);
			GameEvent.AddEventListener<List<Transform>,Item_OnePassItem>(EventType.GetDreamReward, GetOneReward);
			GameEvent.AddEventListener(EventType.RefreshDreamPassAdCount, RefreshAdCountText);
		}

		protected override void OnUpdate()
		{

		}
		private void UpdataInfo()
		{
			_curLevel = _activityData.CurrDreamPassLevel;
			_maxPassLevel = _allPassList.Count;
			mBuyPass.gameObject.SetActive(!_activityData.IsOpenDreamPass);
			RefreshAdCountText();
		}

		private void RefreshAdCountText()
		{
			if (_activityData.IsOpenDreamPass) return;
			var actCfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.DreamPass);
			if (actCfg == null) return;
			int target = ConfigLoader.Constant.DreamPassAdTimes;
			int cur = UserController.Instance.GetAcItemUnLock(actCfg.RechargeId);
			if (cur > target) cur = target;
			mbuypassTxt.text = $"{cur}/{target}";
		}
		private void CreatPassItemList()
		{
			if (_passList == null)
			{
				_passList = CreateWidget<UILoopListWidget<OnePassItem, OnePassItemInfo>>(mAllPassItem);
				_passList.itemBase = mOnePassItem;
			}
			var passListData = GetItemInfos(out int curIndex);
			_passList.SetDatas(passListData);
			_passList.selectIndex = curIndex;
			_passList.funcOnItemClick = null;
			_passList.funcOnMiddleChange += (preIndex, curIndex) =>
			{
				RefreshItemData(preIndex, curIndex);
			};
		}

		private void RefreshItemData(int preIndex, int curIndex)
		{
			for (int i = preIndex; i <= curIndex; i++)
			{
				var itemInfo = _passList.GetData(i);
				var item = _passList.GetItem(i);
				if (i < curIndex)
				{
					_passList.datas[i] = new OnePassItemInfo(itemInfo.Id,itemInfo.UnlockLevel, PassItemType.WaitGet, itemInfo.RewardList);
					item.SetItemData(_passList.datas[i]);
				}

				if (i == curIndex)
				{
					_passList.datas[i] = new OnePassItemInfo(itemInfo.Id,itemInfo.UnlockLevel, PassItemType.Curr, itemInfo.RewardList);
				}
			}
		}
		private void GetAllReward(List<OnePassItemInfo> allItemInfo)
		{
			//首先检测当前所有等待领取的奖励
			List<TBRewardData> waitGetReward = new List<TBRewardData>();
			foreach (var itemInfo in allItemInfo)
			{
				if (itemInfo.NorType == PassItemType.WaitGet)
				{
					if (_activityData.BuyCount.Contains(itemInfo.Id) &&
					    !_activityData.GetPassReward.ContainsKey(itemInfo.Id))
					{
						waitGetReward.Add(itemInfo.RewardList[0]);
					}
					if (_activityData.IsOpenDreamPass)
					{
						waitGetReward.Add(itemInfo.RewardList[1]);
					}

					UserController.Instance.AddDreamPassReward(itemInfo.Id);
				}
			}
			PropsController.Instance.AddRewardData(waitGetReward);
			PropsController.Instance.FlyReward(transform.position);
			Close(); 
		}

		private void GetOneReward(List<Transform> CurStarPos,Item_OnePassItem Itemdata)
		{
			List<TBRewardData> waitGetReward = new List<TBRewardData>();
			List<Vector3> starPos = new List<Vector3>();
			var itemInfo = Itemdata.Mdata;
			if (itemInfo.NorType == PassItemType.WaitGet)
			{
				if (_activityData.BuyCount.Contains(itemInfo.Id) &&
				    !_activityData.GetPassReward.ContainsKey(itemInfo.Id))
				{
					waitGetReward.Add(itemInfo.RewardList[0]);
					starPos.Add(CurStarPos[0].position);
				}
				if (_activityData.IsOpenDreamPass)
				{
					waitGetReward.Add(itemInfo.RewardList[1]);
					starPos.Add(CurStarPos[1].position);
				}
			}
			UserController.Instance.AddDreamPassReward(itemInfo.Id);
			PropsController.FlyReward(starPos,waitGetReward);
			var tData = _passList.datas.Find(x => x.Id == itemInfo.Id);
			var curIndex = _passList.datas.IndexOf(tData);
			_passList.datas[curIndex] = new OnePassItemInfo(itemInfo.Id,itemInfo.UnlockLevel, PassItemType.Getted, itemInfo.RewardList);
			Itemdata.InitPassItem(_passList.datas[curIndex]);
		}
		private List<OnePassItemInfo> GetItemInfos(out int Index)
		{
			var passListData = new List<OnePassItemInfo>();
			int curIndex = 0;
			for (int i = 0; i < _allPassList.Count; i++)
			{
				var cfg = _allPassList[i];
				PassItemType type1 = PassItemType.None;
				if (!_activityData.BuyCount.Contains(cfg.Id) && _activityData.CurrDreamPassLevel != cfg.Id)
				{
					type1 = PassItemType.Lock;
				}else if (cfg.Id == _activityData.CurrDreamPassLevel)
				{
					type1 = PassItemType.Curr;
					curIndex = i;
				}else if (_activityData.BuyCount.Contains(cfg.Id) && !_activityData.GetPassReward.ContainsKey(cfg.Id))
				{
					type1 = PassItemType.WaitGet;
				}
				else if (_activityData.GetPassReward.ContainsKey(cfg.Id) && _activityData.GetPassReward[cfg.Id] == false &&
				         _activityData.IsOpenDreamPass)
				{
					type1 = PassItemType.WaitGet;
				}
				else if (_activityData.GetPassReward.ContainsKey(cfg.Id))
				{
					type1 = PassItemType.Getted;
				}
				
				var rewards = ConfigLoader.Instance.Tables.TbCfgReward.Get(cfg.RewardId).Reward;
				OnePassItemInfo itemInfo = new OnePassItemInfo(cfg.Id,cfg.NeedKeyCount, type1, rewards);
				passListData.Add(itemInfo);
			}
			Index = curIndex;
			return passListData;
		}
		//激活通行证
		private void ActivatePass(CfgRecharge recharge)
		{
			mBuyPass.gameObject.Hide();
			foreach (var item in _passList.GetItemList())
			{
				item.UnlockAnim();
			}
			//防御：hook 路径达标时已在外部发奖励，避免重复
			if (!_activityData.IsOpenDreamPass)
			{
				PropsController.Instance.SendRewardData(recharge.RewardId);
			}
			timeId = GameModule.Timer.AddTimer(timer =>
			{
				var passListData = GetItemInfos(out int curIndex);
				_passList.SetDatas(passListData);
			}, 1.3f);
		}
		protected override void Close()
		{
			base.Close();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<CfgRecharge>(EventType.OpenDreamPass, ActivatePass);
			GameEvent.RemoveEventListener<List<Transform>, Item_OnePassItem>(EventType.GetDreamReward, GetOneReward);
			GameEvent.RemoveEventListener(EventType.RefreshDreamPassAdCount, RefreshAdCountText);
			if (timeId > 0)
			{
				GameModule.Timer.RemoveTimer(timeId);
			}
			GameModule.UI.RemoveOrderPanel<Panel_DreamPass>(true);
		}
	}
}
