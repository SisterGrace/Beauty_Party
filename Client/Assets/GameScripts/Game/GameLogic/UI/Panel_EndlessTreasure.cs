using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config.common;
using Config.config;
using DG.Tweening;
using Y_Base;
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.UI)]
    public partial class Panel_EndlessTreasure : UIWindow
    {
        private UIListWidget<Widget_EndlessItem, EndlessItemData> _endlessItemList;
        private List<CfgEndlessTreasure> _allInfos;
        private ActityData _activityData;
        private bool _isOpenCountDown = false;
        private float _timer = 1;
        private List<int> _getList => UserController.Instance.GetCurRewList(ActivityType.UntoldTreasure);

        //所有item的坐标
        private List<Vector3> _itemPos = new List<Vector3>();
        //是否更换数据
        private bool _isSwap = false;
        private bool _endlessItemShowAmim = false;
        private int _curLoopTimes => UserController.Instance.GetOneActivityData(ActivityType.UntoldTreasure).LoopCount;
        protected override void OnRefresh()
        {
            // ✅ 标记当前页面为活跃页面
            ImgUtil.SetActivePage(this.GetType().Name);
            
            if (userDatas.Length > 0)
            {
                _activityData = (ActityData)userDatas[0];
            }

            _endlessItemShowAmim = false;
            _allInfos = ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.DataList;
            _isOpenCountDown = _activityData.EndTime > 0;
            CheckLoop();
            CreatEndlessItem();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mAllItems.GetComponent<RectTransform>());
            mAllItems.GetComponent<GridLayoutGroup>().enabled = false;
            if(_itemPos.Count==6)return;
            foreach (Transform item in mAllItems)
            {
                _itemPos.Add(item.position);
            }
            _itemPos.Swap(2, 3);
            GameEvent.Send(TKEventType.MainHomeAnim,false);
            SDKManager.Instance.LogEvent("Endless_Show");
            // 静音主页视频，播放背景音乐
            VideoAudioHelper.SwitchToPage(this.WindowName);
            AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
        }

        protected override void RegisterEvent()
        {
            GameEvent.AddEventListener<Widget_EndlessItem>(EventType.RefreshEndlessItem, AllItemMove);
            GameEvent.AddEventListener<bool>(EventType.SetEndlessShowAnim, SetEndlessItemShow);
        }

        protected override void OnUpdate()
        {
            /* if (_isOpenCountDown)
            {
                _timer += Time.deltaTime;
                if (_timer >= 1)
                {
                    var t = _activityData.EndTime - TimeUtil.GetTimeStamp();
                    if (t <= 0)
                    {
                        UserController.Instance.CheckOneActivityTime(ActivityType.UntoldTreasure);
                        Close();
                        return;
                    }

                    mTimeTxt.text = TimeUtil.SecondToTime2(t);
                }
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_endlessItemShowAmim) return;
                GameEvent.Send(TKEventType.MainHomeAnim,true);
                Close();
            } */
        }

        private void CheckLoop()
        {
            if (_getList.Count >= _allInfos.Count)
            {
                UserController.Instance.AddRewGetList(ActivityType.UntoldTreasure,0, true);
                //重置循环次数
                UserController.Instance.AddActivityLoopCount(ActivityType.UntoldTreasure,true);
                //重置上一次循环列表
                PhotoController.Instance.ClearLastGetPhoto(ActivityType.UntoldTreasure);
            }
        }

        private void CreatEndlessItem()
        {
            if (_endlessItemList == null)
            {
                _endlessItemList = CreateWidget<UIListWidget<Widget_EndlessItem, EndlessItemData>>(mAllItems.gameObject);
            }
            //每次只生成六个数据，且第一个道具为解锁状态，其余道具为锁定状态
            List<EndlessItemData> itemDatas = new List<EndlessItemData>();
            var unclaimed = _allInfos.FindAll(x => !_getList.Contains(x.Id));
            var tempBatch = unclaimed.Take(6).ToList();
            int neededCount = 6 - tempBatch.Count;
            if (neededCount > 0)
            {
                for (int i = 0; i < neededCount; i++)
                {
                    tempBatch.Add(_allInfos[i]);
                }
            }

            for (int i = 0; i < tempBatch.Count; i++)
            {
                var recharge = RechargeController.GetRechargeInfo(tempBatch[i].Price.ID);
                bool isLast = false;
                if (neededCount > 0)
                {
                    isLast = i < (6 - neededCount);
                }
                EndlessItemData itemData = new EndlessItemData(tempBatch[i].Id, i != 0, recharge,
                    isLast?_curLoopTimes-1: _curLoopTimes);
                itemDatas.Add(itemData);
            }

            if (!_isSwap)
            {
                itemDatas.Swap(2, 3);
            }
            _endlessItemList.SetDatas(itemDatas);
            if (!_isSwap)
            {
                _isSwap = true;
                _endlessItemList.items.Swap(2, 3);
            }
           
        }

        private void AllItemMove(Widget_EndlessItem Tr)
        {
            Tr.transform.SetSiblingIndex(mAllItems.childCount - 1);
            Vector3 pos = _itemPos[_itemPos.Count - 1];
            Tr.transform.position = new Vector3(pos.x + 3, pos.y, pos.z);
            _endlessItemList.items.RemoveAt(0);
            Widget_EndlessItem TempEnd = _endlessItemList.items[_endlessItemList.items.Count - 1];
            _endlessItemList.items.Add(Tr);
            int newId = TempEnd._itemId + 1 < _allInfos.Count ? TempEnd._itemId + 1 : 0;
            if (newId == 0)
            {
                //开启新一轮循环
                UserController.Instance.AddActivityLoopCount(ActivityType.UntoldTreasure);
                //保存上一轮未领取的奖励id
                PhotoController.Instance.SaveLastGetPhoto(ActivityType.UntoldTreasure);
                //创建新一轮的随机奖励列表
                PhotoController.Instance.SetCurActivityPhotoIds(ActivityType.UntoldTreasure);
            }
            var info = ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.Get(newId);
            var recharge = RechargeController.GetRechargeInfo(info.Price.ID);
            var curLoop = UserController.Instance.GetOneActivityData(ActivityType.UntoldTreasure).LoopCount;
            EndlessItemData itemData = new EndlessItemData(newId, true, recharge,curLoop);
            Tr.InitData(itemData);
            Utility.Unity.StartCoroutine(moveItem());
        }

        private IEnumerator moveItem()
        {
            for (int i = 0; i < _endlessItemList.items.Count; i++)
            {
                int index = i;
                float delay = 0.2f;
                Transform item = _endlessItemList.items[index].transform;
                item.DOMove(_itemPos[index], 0.3f).SetEase(Ease.OutSine)
                    .OnComplete(index == 0 ? () => { _endlessItemList.items[0].Unlock(() =>
                    {
                        _endlessItemShowAmim = false;
                    }); } : null);
                if (index < _endlessItemList.items.Count - 2)
                {
                    //delay = 0.03f;
                    yield return new WaitForSeconds(delay);
                }
                
            }
        }

        private void SetEndlessItemShow(bool isShow)
        {
            _endlessItemShowAmim = isShow;
        }
        protected override void Close()
        {
            // ✅ 清除活跃页面标记
            ImgUtil.ClearActivePage(this.GetType().Name);
            
            VideoAudioHelper.StopVideoAudioCheck(this.WindowName, restoreBGM: true);
            base.Close();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvent.RemoveEventListener<Widget_EndlessItem>(EventType.RefreshEndlessItem, AllItemMove);
            GameModule.UI.RemoveOrderPanel<Panel_EndlessTreasure>(true);
            GameEvent.RemoveEventListener<bool>(EventType.SetEndlessShowAnim, SetEndlessItemShow);
        }
    }
}