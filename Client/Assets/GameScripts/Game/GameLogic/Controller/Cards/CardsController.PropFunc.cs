using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config.common;
using Y_Base;
using TK_Engine;
using UnityEngine;
using AudioType = TK_Engine.AudioType;
using UnityExtensions = Y_Base.UnityExtensions;

namespace GameLogic
{
    /// <summary>
    /// 棋子控制器——道具功能
    /// </summary>
    public partial class CardsController
    {
        #region 道具功能管理

        //撤回
        public bool Revoke()
        {
            CardCell tCard = _eliminateSlot.Revoke();
            if (tCard != null)
            {
                AudioManager.Play(AudioType.Sound, AudioDef.UseRevoke);
                tCard.SetParent(_cardMoving);
                tCard.MoveBack();
                AddTempCardCell(false, tCard);
                return true;
            }

            CurPlayCardType = playCardType.None;
            return false;
        }

        //打乱
        public bool MessUp()
        {
            List<CardCell> tRemakeCards = new List<CardCell>();
            foreach (CardCell item in _cards)
            {
                if (item.IsInSolt || item.IsStored) continue;
                tRemakeCards.Add(item);
            }

            if (tRemakeCards.Count > 1)
            {
                GameEvent.Send(EventType.DoPropAnim, PropType.MessUp, true, 1f);
                RemakeCards(tRemakeCards);
                AudioManager.Play(AudioType.Sound, AudioDef.UseClownHat);
            }
            else
            {
                GameEvent.Send(EventType.ShowTips, "uitps_setting_nohattip", true);
                CurPlayCardType = playCardType.None;
            }

            return tRemakeCards.Count > 1;
        }

        //消除一组
        public bool DestoryOneGruop()
        {
            var tempGroup = new List<CardCell>();
            int curType = 0;
            //tempGroup.AddRange(GameScene.Instance.TestCards);
            if (_eliminateSlot.SoltCardList.Count > 0)
            {
                //从收集框选择一个或者一对，优先选择一对
                //tempGroup.AddRange(_eliminateSlot.GetOneGroup());
                //curType = tempGroup != null ? tempGroup[0].CardType : 0;
                var allGroup = _eliminateSlot.GetAllGroup();
                if (allGroup.Count > 0)
                {
                    foreach (var oneGroup in allGroup)
                    {
                        tempGroup.Clear();
                        curType = oneGroup.Value[0].CardType;
                        tempGroup.AddRange(oneGroup.Value);
                        if (tempGroup != null && tempGroup.Count >= 3)
                        {
                            //直接消除
                        }
                        else if (tempGroup != null && tempGroup.Count == 2)
                        {
                            //再从卡牌堆中获取一张卡牌，与之组合成一组
                            var tempAdd = AddGroupCards(tempGroup, curType, 1);
                            tempGroup.AddRange(tempAdd);
                        }
                        else if (tempGroup != null && tempGroup.Count == 1)
                        {
                            //再从卡牌堆中获取两张卡牌，与之组合成一组
                            var tempAdd = AddGroupCards(tempGroup, curType, 2);
                            tempGroup.AddRange(tempAdd);
                        }

                        if (tempGroup.Count >= 3) break;
                    }
                }
            }
            else
            {
                var TopCards = GetVisibleCards();
                tempGroup.AddRange(GetVisibleGoup(TopCards));
                curType = (tempGroup != null && tempGroup.Count > 0) ? tempGroup[0].CardType : 0;

                if (tempGroup != null && tempGroup.Count >= 3)
                {
                    //直接消除
                }
                else if (tempGroup != null && tempGroup.Count == 2)
                {
                    //再从卡牌堆中获取一张卡牌，与之组合成一组
                    var tempAdd = AddGroupCards(tempGroup, curType, 1);
                    tempGroup.AddRange(tempAdd);
                }
                else if (tempGroup != null && tempGroup.Count == 1)
                {
                    //再从卡牌堆中获取两张卡牌，与之组合成一组
                    var tempAdd = AddGroupCards(tempGroup, curType, 2);
                    tempGroup.AddRange(tempAdd);
                }

            }
            if (tempGroup.Count < 3)
            {
                GameEvent.Send(EventType.ShowTips, "uitps_guide_tips31", true);
                CurPlayCardType = playCardType.None;
                return false;
            }

            GameEvent.Send(EventType.DoPropAnim, PropType.DestoryGruop, true, 1f);
            DestoryGroupCard(tempGroup, null);
            AudioManager.Play(AudioType.Sound, AudioDef.UseWand);
            return true;
        }

        //推进暂存区（最多一次3个）
        public bool MoveOut(bool isFail = false)
        {
            List<CardCell> tRemoveCards = _eliminateSlot.removeFirstThree();
            if (tRemoveCards != null && tRemoveCards.Count > 0)
            {
                for (int i = 0; i < tRemoveCards.Count; i++)
                {
                    tRemoveCards[i].SetParent(_cardMoving);
                    _storeSlot.StoreCard(tRemoveCards[i], i);
                    AddTempCardCell(false, tRemoveCards[i]);
                }

                AudioManager.Play(AudioType.Sound, AudioDef.UseSpring);
                if (isFail)
                {
                    _eliminateSlot.SetFull = false;
                }

                return true;
            }
            GameEvent.Send(EventType.ShowTips, "uitps_levelnfo_springboardtip", true);
            CurPlayCardType = playCardType.None;
            return false;
        }

        //提示卡牌
        public bool PromptCard()
        {
            CurPlayCardType = playCardType.useprop;
            var tempGroup = new List<CardCell>();
            var VisibleCards = GetVisibleCards(null);
            var TopCards = VisibleCards.FindAll(x =>
                x.MySpecialCardLogic.GetLockCardType != LockCardType.Rock &&
                !x.MySpecialCardLogic.IsLockCard &&
                !x.MySpecialCardLogic.IsFunctionCard(FunctionCardType.GoldCube) &&
                !x.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell));
            var corverCards = _cards.FindAll(x =>
                !x.IsInSolt && !x.IsStored && x.IsVisible && x.IsCovered && x.StackIndex < 0 &&
                x.MySpecialCardLogic.GetLockCardType != LockCardType.Rock &&
                !x.MySpecialCardLogic.IsFunctionCard(FunctionCardType.GoldCube) &&
                !x.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell));
            //游玩区卡牌
            var playCards = new List<CardCell>();
            playCards.AddRange(TopCards);
            //playCards.AddRange(corverCards);
            //暂存区顶上的卡牌
            var storeCards = _storeSlot.GetStoreCardList();
            //先从收集框开始查找
            if (_eliminateSlot.SoltCardList.Count > 0)
            {
                var findGroup1 = new List<CardCell>();
                for (int i = 0; i < _eliminateSlot.SoltCardList.Count; i++)
                {
                    findGroup1.Clear();
                    var curCard = _eliminateSlot.SoltCardList[i];
                    findGroup1.AddWithoutRepetition(curCard);
                    //查询自己所在的列表有相同的卡牌
                    var same = _eliminateSlot.SoltCardList.Find(x => x.CardType == curCard.CardType && x != curCard);
                    if (same != null)
                    {
                        findGroup1.AddWithoutRepetition(same);
                    }

                    //查询暂存区有相同的卡牌
                    if (storeCards.Count > 0)
                    {
                        for (int a = 0; a < storeCards.Count; a++)
                        {
                            if (storeCards[a].CardType == curCard.CardType)
                            {
                                findGroup1.AddWithoutRepetition(storeCards[a]);
                            }
                        }
                    }

                    if (findGroup1.Count >= 3) break;
                    for (int j = 0; j < playCards.Count; j++)
                    {
                        if (playCards[j].CardType == curCard.CardType)
                        {
                            findGroup1.AddWithoutRepetition(playCards[j]);
                            if (findGroup1.Count >= 3) break;
                        }
                    }

                    if (findGroup1.Count >= 3) break;
                }

                if (findGroup1.Count >= 3)
                {
                    tempGroup.AddRange(findGroup1);
                }
            }

            //再从暂存区开始查找
            if (tempGroup.Count < 3 && storeCards.Count > 0)
            {
                tempGroup.Clear();
                var findGroup2 = new List<CardCell>();
                for (int i = 0; i < storeCards.Count; i++)
                {
                    findGroup2.Clear();
                    var curCard = storeCards[i];
                    findGroup2.AddWithoutRepetition(curCard);
                    //查询自己所在的列表有相同的卡牌
                    var same = storeCards.Find(x => x.CardType == curCard.CardType && x != curCard);
                    if (same != null)
                    {
                        findGroup2.AddWithoutRepetition(same);
                    }

                    if (findGroup2.Count >= 3) break;
                    for (int j = 0; j < playCards.Count; j++)
                    {
                        if (playCards[j].CardType == curCard.CardType)
                        {
                            findGroup2.AddWithoutRepetition(playCards[j]);
                            if (findGroup2.Count >= 3) break;
                        }
                    }

                    if (findGroup2.Count >= 3) break;
                }

                if (findGroup2.Count >= 3)
                {
                    tempGroup.AddRange(findGroup2);
                }
            }

            //再从卡牌堆开始查找
            if (tempGroup.Count < 3 && playCards.Count >= 3)
            {
                var findGroup3 = new List<CardCell>();
                for (int i = 0; i < playCards.Count - 3; i++)
                {
                    findGroup3.Clear();
                    var currentType = playCards[i].CardType;
                    for (int j = i; j < playCards.Count; j++)
                    {
                        if (playCards[j].CardType == currentType)
                        {
                            findGroup3.AddWithoutRepetition(playCards[j]);
                            if (findGroup3.Count >= 3) break;
                        }
                    }

                    if (findGroup3.Count >= 3) break;
                }

                if (findGroup3.Count >= 3)
                {
                    tempGroup.AddRange(findGroup3);
                }
            }

            if (tempGroup.Count < 3)
            {
                CurPlayCardType = playCardType.None;
                GameEvent.Send(EventType.ShowTips, "No cards to reveal!", false);
                return false;
            }

            foreach (var item in tempGroup)
            {
                item.TipsCard();
            }

            AudioManager.Play(AudioType.Sound, AudioDef.UseTips);
            CurPlayCardType = playCardType.None;
            return true;
        }

        //抽卡牌
        public bool DrawCard()
        {
            //如果暂存区和槽位都没有卡牌，则从当前可见区域中随机抽取一张卡牌类型
            CurPlayCardType = playCardType.useprop;
            int tempType = 0;
            if (!_storeSlot.IsStoreHasCard(true) && !_eliminateSlot.IsEliminateSlotHas(true))
            {
                //需要排除钞票棋子
                var tempList = GetVisibleCards(Exclude: ConfigLoader.Constant.MoneyCard);
                if (tempList.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, tempList.Count);
                    tempType = tempList[randomIndex].CardType;
                }
                else
                {
                    tempType = 1;
                }
            }
            else
            {
                //从暂存区和槽位获取,但是都得检测是否有现金棋子
                if (_storeSlot.IsStoreHasCard(true))
                {
                    tempType = _storeSlot.GetStoreCardType(true);
                }
                else if (_eliminateSlot.IsEliminateSlotHas(true))
                {
                    tempType = _eliminateSlot.GetOneCartType(true);
                }
            }

            if (tempType == 0)
            {
                CurPlayCardType = playCardType.None;
                return false;
            }

            UIWindow parent = GameModule.UI.GetWindow(typeof(Panel_Game).FullName);
            PropYaoba yaoba = FlySystem.Instance.ShowEff("prefab_Yaoba", parent.transform, null, Vector3.one)
                .GetComponent<PropYaoba>();
            yaoba.transform.SetSiblingIndex(5);
            yaoba.InitYaoba(tempType, _cardMoving);
            return true;
        }
        //加收集槽位
        public bool AddSolt()
        {
            GameEvent.Send(EventType.SetGameMask, true);
            _eliminateSlot.AddSolt(() =>
            {
                GameEvent.Send(EventType.SetGameMask, false);
                CurPlayCardType = playCardType.None;
            });
            return true;
        }
        #endregion

        #region 道具功能之卡牌打乱

        private bool _isResetCardType = false;

        private void MessUpUpdate()
        {
            if (_isSendIn)
            {
                if (MessUpMoveCards(Time.deltaTime, true))
                {
                    if (!_isResetCardType)
                    {
                        _isResetCardType = true;
                        ResetCardType();
                    }
                }
            }
            else if (_isSendOut)
            {
                if (MessUpMoveCards(Time.deltaTime, false))
                {
                    MessUpOver();
                }
            }
        }

        private bool MessUpMoveCards(float pDeltaTime, bool pIsIn)
        {
            _messUpTimer -= pDeltaTime;
            for (int i = 0; i < _messUpCards.Count; i++)
            {
                _messUpCards[i].CardRect.anchoredPosition = _cardOriginalPoses[i] *
                                                            Mathf.Clamp(
                                                                (pIsIn ? _messUpTimer : SEND_TIME - _messUpTimer) /
                                                                SEND_TIME, 0, 1);
            }

            if (_messUpTimer <= 0)
            {
                return true;
            }

            return false;
        }

        private void RemakeCards(List<CardCell> tcards)
        {
            _messUpCards = tcards;
            _tempTypes = new List<int>();
            _cardOriginalPoses = new List<Vector2>(_messUpCards.Count);
            for (int i = 0; i < _messUpCards.Count; i++)
            {
                _tempTypes.AddRange(_messUpCards[i].AllCardTypes);
                _cardOriginalPoses.Add(_messUpCards[i].CardRect.anchoredPosition);
            }

            SendInOrOut(true);
        }

        private void ResetCardType()
        {
            List<int> OneCardType;
            for (int i = 0; i < _messUpCards.Count; i++)
            {
                OneCardType = new List<int>(_messUpCards[i].MyCoordData.Count);
                for (int a = 0; a < _messUpCards[i].MyCoordData.Count; a++)
                {
                    int tRandomIndex = UnityEngine.Random.Range(0, _tempTypes.Count);
                    OneCardType.Add(_tempTypes[tRandomIndex]);
                    _tempTypes.RemoveAt(tRandomIndex);
                }

                _messUpCards[i].SetType(OneCardType);
            }

            RemedyCardType(_messUpCards);
            int id = GameModule.Timer.AddTimer(timer => { SendInOrOut(false); }, 0.5f);
            timerIds.Add(id);
        }

        private void SendInOrOut(bool isIn)
        {
            _isSendIn = isIn;
            _isSendOut = !isIn;
            _messUpTimer = SEND_TIME;
        }

        private void MessUpOver()
        {
            _isSendIn = false;
            _isSendOut = false;
            _isResetCardType = false;
            for (int i = 0; i < _messUpCards.Count; i++)
            {
                _messUpCards[i].CardRect.position = _messUpCards[i].LocationWorldPos;
            }

            int id = GameModule.Timer.AddTimer(
                timer => { GameEvent.Send(EventType.DoPropAnim, PropType.MessUp, false, 1f); }, 0.2f);
            timerIds.Add(id);
        }

        //纠正卡牌类型
        public void RemedyCardType(List<CardCell> cards)
        {
            List<CardCell> _tempCards1 = new List<CardCell>();
            List<CardCell> _tempCards2 = new List<CardCell>();
            var RejectTypes = ConfigLoader.Constant.RejectTypes;
            int RemedyCount = 0;
            foreach (var item in cards)
            {
                if (item.MySpecialCardLogic.IsReject())
                {
                    for (int i = 0; i < item.AllCardTypes.Count; i++)
                    {
                        if (RejectTypes.Contains(item.AllCardTypes[i]))
                        {
                            _tempCards1.AddWithoutRepetition(item);
                            RemedyCount++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < item.AllCardTypes.Count; i++)
                    {
                        if (!RejectTypes.Contains(item.AllCardTypes[i]))
                        {
                            _tempCards2.AddWithoutRepetition(item);
                        }
                    }
                }
            }

            if (_tempCards1.Count == 0) return;
            if (_tempCards2.Count == 0) return;
            for (int j = 0; j < _tempCards1.Count; j++)
            {
                for (int a = 0; a < _tempCards1[j].AllCardTypes.Count; a++)
                {
                    if (RejectTypes.Contains(_tempCards1[j].AllCardTypes[a]))
                    {
                        var temp = _tempCards2.RandomFromAllList();
                        int tTyp = temp.AllCardTypes[0];
                        temp.AllCardTypes[0] = _tempCards1[j].AllCardTypes[a];
                        _tempCards1[j].AllCardTypes[a] = tTyp;
                        temp.SetType();
                    }
                }

                _tempCards1[j].SetType();
            }

            Log.Debug("纠正一次卡牌类型");
        }

        #endregion

        #region 道具功能之消除一组卡牌

        //直接消除一组卡牌（3个一组）
        private void DestoryGroupCard(List<CardCell> OneGroup, List<CardCell> VisibleCards)
        {
            int index = 0;
            if (OneGroup[0].CardType == ConfigLoader.Constant.NetbagCard)
            {
                for (int i = 0; i < OneGroup.Count; i++)
                {
                    if (OneGroup[i].MySpecialCardLogic.GetLockCardType == LockCardType.Butterfly)
                    {
                        OneGroup[i].MySpecialCardLogic.UnlockCurrentCard(null, true);
                    }
                }
            }
            foreach (var card in OneGroup)
            {
                card.SetVisible(true);
                bool doSpecial = false;
                //卡牌不在槽位上
                if (!card.IsInSolt)
                {
                    if (card.StackIndex < 0)
                    {
                        RemoveUnitCoverStack(card);
                        doSpecial = true;
                    }
                    else
                    {
                        if (card.IsStored)
                        {
                            _storeSlot.StackRemove(card);
                        }
                        else
                        {
                            RemoveFreeStack(card);
                             doSpecial = true;
                        }
                    }
                }
                else
                {
                    _eliminateSlot.SoltCardList.RemoveSafe(card);
                }

                card.SetParent(_cardMoving, false, true, _cardMoving.transform.GetChild(0));
                if (doSpecial)
                {
                    if (VisibleCards == null)
                    {
                        VisibleCards = GetVisibleCards();
                    }

                    //处理特殊卡牌
                    UnLockCard(card, VisibleCards, false, true, index == 2 ? true : false);
                    FunctionCardDone(card, VisibleCards, true);
                }
                card.ExposeCell(true);
                index++;
            }

            //有来自槽位的棋子，需要重新排列卡牌
            if (!OneGroup.TrueForAll(x => !x.IsInSolt))
            {
                _eliminateSlot.RearrangeSlot();
            }

            GameModule.Timer.AddTimer(timer =>
            {
                int tCartType = OneGroup[0].CardType;
                Vector3 tPos = new Vector3(OneGroup[1].transform.position.x, OneGroup[1].transform.position.y, OneGroup[1].transform.position.z);
                foreach (var card in OneGroup)
                {
                    card.Eliminate();
                    card.SetParent(_cardMoving);
                }

                GameEvent.Send(EventType.DoPropAnim, PropType.DestoryGruop, false, 1f);
                //检测一下是否消除了全部
                OnEliminateEnd(tCartType, tPos);
            }, 1.9f);
        }

        #endregion

        #region 道具功能通用方法

        //删除栈里面的一个元素，不一定是栈顶
        private void RemoveUnitCoverStack(CardCell card)
        {
            int coverId = 0;
            Stack<CardCell> tempStack;
            CardCell downCard = null;
            for (int i = 0; i < card.CoverIds.Count; i++)
            {
                coverId = card.CoverIds[i];
                tempStack = _unitCoverStack[coverId];
                // 创建一个临时列表来存储弹出的元素
                Stack<CardCell> tempHolder = new Stack<CardCell>();
                bool found = false;
                while (tempStack.Count > 0 && !found)
                {
                    CardCell current = tempStack.Pop();
                    if (current == card && !found)
                    {
                        found = true;
                    }
                    else
                    {
                        tempHolder.Push(current);
                    }
                }

                while (tempHolder.Count > 0)
                {
                    tempStack.Push(tempHolder.Pop());
                }

                if (tempStack.Count > 0)
                {
                    downCard = tempStack.Peek();
                    downCard.SetVisible(true);
                    if (CheckIsTop(downCard))
                    {
                        downCard.ExposeCell();
                    }
                }
                else
                {
                    _unitCoverStack.Remove(coverId);
                }
            }
        }

        //从自由堆栈移除一张卡牌
        private void RemoveFreeStack(CardCell card)
        {
            Stack<CardCell> tempStack = new Stack<CardCell>();
            Stack<CardCell> currentStack = _cardFreeStacks[card.StackIndex];
            bool isFind = false;
            while (currentStack.Count > 0 && !isFind)
            {
                CardCell tempCard = currentStack.Pop();
                if (tempCard == card && !isFind)
                {
                    isFind = true;
                }
                else
                {
                    tempStack.Push(tempCard);
                }
            }

            while (tempStack.Count > 0)
            {
                currentStack.Push(tempStack.Pop());
            }

            if (currentStack.Count > 0)
            {
                currentStack.Peek().ExposeCell();
            }
        }

        //获取可见区域的一组卡牌
        private List<CardCell> GetVisibleGoup(List<CardCell> VisibleCards)
        {
            Dictionary<int, List<CardCell>> tempGruop = new Dictionary<int, List<CardCell>>();
            if (VisibleCards.Count == 0) return new List<CardCell>();
            foreach (var item in VisibleCards)
            {
                //卡牌是贝壳时，不参与组合
                if (!item.MySpecialCardLogic.IsShell)
                {
                    if (!tempGruop.ContainsKey(item.CardType))
                    {
                        tempGruop[item.CardType] = new List<CardCell>();
                    }

                    tempGruop[item.CardType].Add(item);
                    if (tempGruop[item.CardType].Count >= 3)
                    {
                        return tempGruop[item.CardType];
                    }
                }
            }

            //如果不满足三个一组，则返回两个一组的，如果都不满足，则随机返回一个一组的
            foreach (var groups in tempGruop.Values)
            {
                if (groups.Count >= 2)
                {
                    return groups;
                }
            }

            return tempGruop.Values.Count > 0 ? tempGruop.Values.First() : null;
        }

        /// <summary>
        /// 获取一组卡牌的补充，当没有三张卡牌时，补充卡牌
        /// </summary>
        /// <param name="mycards">要补充的卡牌组</param>
        /// <param name="type">补充卡牌类型</param>
        /// <param name="num">补充数量</param>
        /// <param name="checkIsTop">是否补充顶部可见卡牌</param>
        /// <returns></returns>
        private List<CardCell> AddGroupCards(List<CardCell> mycards, int type, int num, bool checkIsTop = false)
        {
            List<CardCell> tempList = new List<CardCell>(num);
            bool isTop = false;
            List<CardCell> joinList = mycards == null ? tempList : mycards;
            List<CardCell> availableCards = new List<CardCell>();
            for (int i = 0; i < _cards.Count; i++)
            {
                var curCard = _cards[i];
                if (!curCard.IsInSolt)
                {
                    if (curCard.CardType == type && !curCard.MySpecialCardLogic.IsShell &&
                        !joinList.Contains(curCard) && (!checkIsTop ? true : CheckIsTop(curCard)))
                    {
                        availableCards.AddWithoutRepetition(curCard);
                    }
                }
                else
                {
                    if (curCard.CardType == type && !joinList.Contains(curCard))
                    {
                        availableCards.AddWithoutRepetition(curCard);
                    }
                }
            }

            // 先添加不在任何堆栈中的卡牌
            tempList.AddRange(availableCards.Where(card => card.StackIndex < 0).Take(num - tempList.Count));
            if (tempList.Count >= num) return tempList;
            // 添加在自由堆栈中但不在存储区的卡牌
            tempList.AddRange(availableCards.Where(card => card.StackIndex >= 0 && !card.IsStored && !card.IsCovered)
                .Take(num - tempList.Count));
            if (tempList.Count >= num) return tempList;
            // 添加在存储区中的卡牌
            tempList.AddRange(availableCards.Where(card => card.StackIndex >= 0 && card.IsStored && !card.IsCovered)
                .Take(num - tempList.Count));
            if (tempList.Count >= num) return tempList;
            // 添加已经在槽位中的卡牌
            tempList.AddRange(availableCards.Where(card => card.IsInSolt).Take(num - tempList.Count));
            return tempList;
        }

        #endregion
    }
}