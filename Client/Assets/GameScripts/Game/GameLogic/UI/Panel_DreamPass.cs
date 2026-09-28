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
		// ✅ 标记当前页面为活跃页面
		ImgUtil.SetActivePage(this.GetType().Name);
		
		if (userDatas.Length > 0)
		{
			_activityData = (ActityData)userDatas[0];
		}
		_allPassList = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
		_isOpenCountDown = _activityData.EndTime > 0;
		CreatPassItemList();
		UpdataInfo();
		SDKManager.Instance.LogEvent("Pass_Show");
		// 静音主页视频，播放背景音乐
		VideoAudioHelper.SwitchToPage(this.WindowName);
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<CfgRecharge>(EventType.OpenDreamPass,ActivatePass);
			GameEvent.AddEventListener<List<Transform>,Item_OnePassItem>(EventType.GetDreamReward, GetOneReward);
			GameEvent.AddEventListener(EventType.RefreshDreamPassAdCount, RefreshAdCountText);
            // 独立领取事件
            GameEvent.AddEventListener<Item_OnePassItem>(EventType.GetDreamPassNormalOnly, GetNormalOnly);
            GameEvent.AddEventListener<Item_OnePassItem>(EventType.GetSpecialRewardPhoto, GetSpecialOnly);
            GameEvent.AddEventListener<Item_OnePassItem>(EventType.GetNormalRewardPhoto, GetNormalRewardPhoto);
        }
        // 普通图片视频成功后：只领普通
        private void GetNormalRewardPhoto(Item_OnePassItem item)
        {
            var info = item.Mdata;
            if (info.NorType != PassItemType.WaitGet) return;
            if (_activityData.NormalGotten != null && _activityData.NormalGotten.ContainsKey(info.Id)) return;

            List<TBRewardData> reward = new List<TBRewardData> { info.RewardList[0] };
            PropsController.Instance.AddRewardData(reward);
            UserController.Instance.AddDreamPassReward(info.Id, normalGotten: true, specialGotten: false);
            RefreshItemState(info.Id);
        }
        // 只领取高级（视频成功后调用）
        private void GetSpecialOnly(Item_OnePassItem item)
        {
            var info = item.Mdata;
            if (info.NorType != PassItemType.WaitGet) return;
            if (!_activityData.IsOpenDreamPass) return;

            bool already = _activityData.SpecialGotten.ContainsKey(info.Id) && _activityData.SpecialGotten[info.Id];
            if (already) return;

            List<TBRewardData> waitGetReward = new List<TBRewardData>();
            List<Vector3> starPos = new List<Vector3>();
            bool gotNormal = false;

            // 如果普通未领且普通不是图片类型，则一并领取；图片类型需要看视频，不能顺带领
            bool isNorPhoto = info.RewardList[0].Type == ERewardType.Photo;
            if (!isNorPhoto && _activityData.BuyCount.Contains(info.Id) && !_activityData.NormalGotten.ContainsKey(info.Id))
            {
                waitGetReward.Add(info.RewardList[0]);
                var btns2 = item.GetBtnTransforms();
                if (btns2 != null && btns2.Count > 0)
                    starPos.Add(btns2[0].position);
                gotNormal = true;
            }

            // 高级奖励始终领取
            waitGetReward.Add(info.RewardList[1]);
            var btns3 = item.GetBtnTransforms();
            if (btns3 != null && btns3.Count > 1)
                starPos.Add(btns3[1].position);

            if (waitGetReward.Count > 0)
            {
                PropsController.FlyReward(starPos, waitGetReward);
                UserController.Instance.AddDreamPassReward(info.Id, gotNormal, true);
                RefreshItemState(info.Id);
            }
        }
        // 只领取普通
        private void GetNormalOnly(Item_OnePassItem item)
        {
            var info = item.Mdata;
            if (info.NorType != PassItemType.WaitGet) return;
            if (_activityData.BuyCount.Contains(info.Id) && !_activityData.NormalGotten.ContainsKey(info.Id))
            {
                // 发放普通奖励
                List<TBRewardData> reward = new List<TBRewardData> { info.RewardList[0] };
                PropsController.Instance.AddRewardData(reward);
                // 标记普通已领，高级未领
                UserController.Instance.AddDreamPassReward(info.Id, normalGotten: true, specialGotten: false);
                // 刷新UI
                RefreshItemState(info.Id);
            }
        }

        private void RefreshItemState(int id)
        {
            // 重新生成整个列表数据并刷新（SetDatas 会自动更新所有可见 Item）
            var passListData = GetItemInfos(out int curIndex);
            _passList.SetDatas(passListData);
            _passList.selectIndex = curIndex;
        }

        protected override void OnUpdate()
		{

		}
        private void UpdataInfo()
        {
            _curLevel = _activityData.CurrDreamPassLevel;
            _maxPassLevel = _allPassList.Count;
            // 仅在未解锁时显示购买/进度按钮
            mBuyPass.gameObject.SetActive(!_activityData.IsOpenDreamPass);
            RefreshAdCountText(); // 未解锁时更新进度文本，解锁后自动跳过
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
					bool gotNormal = false, gotSpecial = false;
					if (_activityData.BuyCount.Contains(itemInfo.Id) &&
					    !_activityData.NormalGotten.ContainsKey(itemInfo.Id))
					{
						waitGetReward.Add(itemInfo.RewardList[0]);
						gotNormal = true;
					}
					if (_activityData.IsOpenDreamPass)
					{
						bool speGotten = _activityData.SpecialGotten.ContainsKey(itemInfo.Id) && _activityData.SpecialGotten[itemInfo.Id];
						if (!speGotten)
						{
							waitGetReward.Add(itemInfo.RewardList[1]);
							gotSpecial = true;
						}
					}
					UserController.Instance.AddDreamPassReward(itemInfo.Id, gotNormal, gotSpecial);
				}
			}
			PropsController.Instance.AddRewardData(waitGetReward);
			PropsController.Instance.FlyReward(transform.position);
			Close(); 
		}

        private void GetOneReward(List<Transform> CurStarPos, Item_OnePassItem Itemdata)
        {
            var info = Itemdata.Mdata;
            if (info.NorType != PassItemType.WaitGet) return;

            List<TBRewardData> waitGetReward = new List<TBRewardData>();
            List<Vector3> starPos = new List<Vector3>();
            bool gotNormal = false, gotSpecial = false;

            // 普通奖励（未领且可领）
            if (_activityData.BuyCount.Contains(info.Id) && !_activityData.NormalGotten.ContainsKey(info.Id))
            {
                waitGetReward.Add(info.RewardList[0]);
                starPos.Add(CurStarPos[0].position);
                gotNormal = true;
            }

            // 高级奖励（未领且可领）
            if (_activityData.IsOpenDreamPass)
            {
                bool isSpeGotten = _activityData.SpecialGotten.ContainsKey(info.Id) && _activityData.SpecialGotten[info.Id];
                if (!isSpeGotten)
                {
                    waitGetReward.Add(info.RewardList[1]);
                    starPos.Add(CurStarPos[1].position);
                    gotSpecial = true;
                }
            }

            if (waitGetReward.Count > 0)
            {
                PropsController.FlyReward(starPos, waitGetReward);
                UserController.Instance.AddDreamPassReward(info.Id, gotNormal, gotSpecial);
                RefreshItemState(info.Id);
            }
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
                }
                else if (cfg.Id == _activityData.CurrDreamPassLevel)
                {
                    type1 = PassItemType.Curr;
                    curIndex = i;
                }
                else
                {
                    bool normalGotten = _activityData.NormalGotten.ContainsKey(cfg.Id);
                    bool specialGotten = _activityData.SpecialGotten.ContainsKey(cfg.Id) && _activityData.SpecialGotten[cfg.Id];
                    // 关键修改：高级未开通时，只要普通已领就算全部已领
                    if (normalGotten && (!_activityData.IsOpenDreamPass || specialGotten))
                        type1 = PassItemType.Getted;
                    else
                        type1 = PassItemType.WaitGet;
                }
                var rewards = ConfigLoader.Instance.Tables.TbCfgReward.Get(cfg.RewardId).Reward;
                OnePassItemInfo itemInfo = new OnePassItemInfo(cfg.Id, cfg.NeedKeyCount, type1, rewards);
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
		// ✅ 清除活跃页面标记
		ImgUtil.ClearActivePage(this.GetType().Name);
		
            VideoAudioHelper.StopVideoAudioCheck(this.WindowName, true);
            base.Close();
	}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<CfgRecharge>(EventType.OpenDreamPass, ActivatePass);
			GameEvent.RemoveEventListener<List<Transform>, Item_OnePassItem>(EventType.GetDreamReward, GetOneReward);
			GameEvent.RemoveEventListener(EventType.RefreshDreamPassAdCount, RefreshAdCountText);
			GameEvent.RemoveEventListener<Item_OnePassItem>(EventType.GetDreamPassNormalOnly, GetNormalOnly);
			GameEvent.RemoveEventListener<Item_OnePassItem>(EventType.GetSpecialRewardPhoto, GetSpecialOnly);
			GameEvent.RemoveEventListener<Item_OnePassItem>(EventType.GetNormalRewardPhoto, GetNormalRewardPhoto);
			if (timeId > 0)
			{
				GameModule.Timer.RemoveTimer(timeId);
			}
			GameModule.UI.RemoveOrderPanel<Panel_DreamPass>(true);
		}
	}
}
