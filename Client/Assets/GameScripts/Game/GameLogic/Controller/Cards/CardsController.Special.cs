using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExtensions = GameBase.UnityExtensions;
using Config.common;
using TK_Engine;
using GameBase;
using Random = UnityEngine.Random;

namespace GameLogic
{
    public partial class CardsController
    {
        #region 特殊卡牌处理
        
        private List<int> tempAllVinesGroupIds = new List<int>();

        /// <summary>
        /// 解锁卡牌
        /// </summary>
        /// <param name="card"></param>
        /// <param name="curVisibleCards">当前可见操作区域卡牌</param>
        /// <param name="anyUnlock">是否执行任意解锁</param>
        /// <param name="isDestroy">是否是消除当前卡牌</param>
        /// <param name="doOnce">是否是消除当前卡牌</param>
        private void UnLockCard(CardCell card, List<CardCell> curVisibleCards, bool anyUnlock = true,
            bool isDestroy = false,bool doOnce =false)
        {
            //卡牌解锁分为，解锁任意一个、解锁相邻
            //任意：冰块、蝴蝶、门
            //相邻:锁链、蜂蜜、藤蔓、石头、草
            var neighborCoverIDs = GetNeighborCoverIDs(card);
            //是否需要解锁当前卡牌，如果是门卡牌开着的情况，直接解锁，或者需要消除当前点击的卡牌
            bool IsFullyUnlock = card.MySpecialCardLogic.GetLockCardType == LockCardType.Door || isDestroy;
            if (IsFullyUnlock)
            {
                card.MySpecialCardLogic.UnlockCurrentCard(null,IsFullyUnlock);
            }
            UnLock(card, neighborCoverIDs, curVisibleCards, anyUnlock, isDestroy, doOnce);
        }

        private void UnLock(CardCell card,List<int> neighborCoverIDs,List<CardCell> curVisibleCards, bool anyUnlock = true,
            bool isDestroy = false, bool doOnce = false)
        {
            tempAllVinesGroupIds.Clear();
            _butterflyCards.Clear();
            bool isSetDoor = false;
            if (CheckOnlyDoorCard())
            {
                isSetDoor = true;
            }
            //获取蝴蝶飞行目标
            var butterflyTarget = GetButterflyTarget(curVisibleCards,isDestroy);
            _butterflyCards.AddRange(butterflyTarget);
            foreach (var item in curVisibleCards)
            {
                // 获取卡牌的锁定类型
                var lockCardType = item.MySpecialCardLogic.GetLockCardType;
                if (item == card||lockCardType == LockCardType.None) continue;
                //冰块、蝴蝶、门任意解锁
                if (lockCardType == LockCardType.IceCube ||
                    lockCardType == LockCardType.Butterfly||
                    lockCardType == LockCardType.Door)
                {
                    //当启用销毁道具的时候，如有冰块，需要清除冰块
                    if (anyUnlock||(doOnce&&lockCardType == LockCardType.IceCube))
                    {
                        bool isCheckButterfly = false;
                        if (lockCardType == LockCardType.Butterfly && butterflyTarget.Count > 0)
                        {
                            isCheckButterfly = true;
                        }
                        item.MySpecialCardLogic.UnlockCurrentCard(isCheckButterfly?butterflyTarget[0]:null,isDestroy,isSetDoor);
                        if (isCheckButterfly)
                        {
                            butterflyTarget?.RemoveAt(0);
                        }
                    }
                }
                //相邻解锁
                else if (neighborCoverIDs!=null&&CheckIsNeighbor(neighborCoverIDs, item, out CardCell temp))
                {
                    temp?.MySpecialCardLogic.UnlockCurrentCard();
                }
            }
            //如果是销毁一个被覆盖的，需要解锁相邻的卡牌
            if (isDestroy && card.IsCovered)
            {
                var neiIds = GetNeighborCoverIDs(card);
                List<CardCell> NeighborCards = new List<CardCell>();
                for (int i = 0; i < neiIds.Count; i++)
                {
                    int id = neiIds[i];
                    Stack<CardCell> curLayser = _unitCoverStack.GetValueOrDefaultDic(id,defaultValue:default);
                    if (curLayser != default && curLayser.Peek() != null&&curLayser.Peek().IsCovered)
                    {
                        NeighborCards.AddWithoutRepetition(curLayser.Peek());
                    }
                }

                for (int a = 0; a<NeighborCards.Count; a++)
                {
                    NeighborCards[a].MySpecialCardLogic.UnlockCurrentCard();
                }
            }

            if (anyUnlock)
            {
                foreach (var buffer in _butterflyCards)
                {
                    buffer.MySpecialCardLogic.SetButtterFlyInfo();
                }
            }
        }
        //功能卡牌
        private void FunctionCardDone(CardCell card, List<CardCell> curVisibleCards,bool isDestroy = false)
        {
            //功能卡牌，点击和非点击触发的功能不一样
            FunctionCardType functionCardType = FunctionCardType.None;
            if (card.MySpecialCardLogic.GetFunctionCardTypes != null &&
                card.MySpecialCardLogic.GetFunctionCardTypes.Count > 0)
            {
                functionCardType = card.MySpecialCardLogic.GetFunctionCardTypes[0];
            }
            card.MySpecialCardLogic.ExecuteFunction(true, Iszipper =>
            {
                if (Iszipper)
                {
                    //PopOtherCard(card,1);
                }
            },isDestroy:isDestroy);
            //获取当前点击卡牌上方的索引
            var neighborCoverIDs = GetNeighborCoverIDs(card,0,0);
            DoFuncTion(card, neighborCoverIDs,functionCardType, curVisibleCards, isDestroy);
        }

        private void DoFuncTion(CardCell card, List<int> neighborCoverIDs,FunctionCardType functionCardType, List<CardCell> curVisibleCards, bool isDestroy = false)
        {
            //执行其它功能卡牌的非点击功能
            foreach (var item in curVisibleCards)
            {
                if (item == card || item.MySpecialCardLogic.GetFunctionCardTypes == null ||
                    item.MySpecialCardLogic.GetFunctionCardTypes.Count == 0 ||
                    item.MySpecialCardLogic.IsLockCard) continue;

                if (item.MySpecialCardLogic.IsFunctionCard(FunctionCardType.GoldCube)||
                    item.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Reversible)||
                    item.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Bombshell))
                {
                    item.MySpecialCardLogic.ExecuteFunction(false,null,false,isDestroy);
                }
                //如果是贝壳，需要检测是否在点击卡牌的相邻区域
                if (card!=null&&item.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell))
                {
                    if (CheckIsNeighbor(neighborCoverIDs, card,item, out CardCell temp)&&functionCardType!= FunctionCardType.Reversible&&card.StackIndex < 0)
                    {
                        temp?.MySpecialCardLogic.ExecuteFunction(false,null,true,isDestroy);
                    }
                }
            }
        }
        /// <summary>
        /// 获取当前所有可见(能操作)卡牌
        /// </summary>
        /// <param name="curCard">需要排除的卡牌</param>
        /// <param name="isGetStored">是否获取暂存区顶部卡牌</param>
        /// <param name="Exclude">排除某类型卡牌</param>
        /// <returns></returns>
        public List<CardCell> GetVisibleCards(CardCell curCard = null,bool isGetStored = true,int Exclude =-1 )
        {
            List<CardCell> topCards = new List<CardCell>();
            CardCell downCard = null;
            foreach (var item in _unitCoverStack.Values)
            {
                downCard = item.Peek();
                if(downCard== null) continue;
                if (!downCard.IsCovered)
                {
                    topCards.AddWithoutRepetition(downCard);
                }
            }

            if (isGetStored)
            {
                foreach (var stack in  _cardFreeStacks)
                {
                    if(stack.Count==0)continue;
                    var topCard = stack.Peek();
                    if (topCard!= null && !topCard.IsCovered)
                    {
                        topCards.AddWithoutRepetition(topCard);
                    }
                }
            }
           
            if (curCard != null)
            {
                topCards.RemoveSafe(curCard);
            }

            if (Exclude > 0)
            {
                topCards.RemoveAll(x=>x.CardType == Exclude);
            }
            return topCards;
        }
        
        //检测是否为相邻卡牌
        private bool CheckIsNeighbor(List<int> NeighborCoverIDs,CardCell card1,out CardCell card2)
        {
            bool isNeighbor = true;
            foreach (var coverid in card1.CoverIds)
            {
                if (!NeighborCoverIDs.Contains(coverid))
                {
                    isNeighbor = false;
                    break;
                }
            }
            if (isNeighbor)
            {
                if (card1.MySpecialCardLogic.GetLockCardType == LockCardType.Vines)
                {
                    //为了保证到检测到同一组的好几张卡牌时，只执行一次解锁
                    bool isSameGroup = tempAllVinesGroupIds.Contains(card1.MySpecialCardLogic.GetGroupId);
                    tempAllVinesGroupIds.AddWithoutRepetition(card1.MySpecialCardLogic.GetGroupId);
                    //如果是同组，则解锁一次
                    card2 = isSameGroup?null:GetGoupRootCard(card1.MySpecialCardLogic.GetGroupId,2);
                }
                else
                {
                    card2 = card1;
                }
            }
            else
            {
                card2 = null;
            }
            return isNeighbor;
        }

        private bool CheckIsNeighbor(List<int> NeighborCoverIDs,CardCell card0, CardCell card1,out CardCell card2)
        {
            bool isUpNeighbor = true;
            bool isCoverNeighbor = false;
            //检测card1是否为card0的上方卡牌(既贝壳是否在点击棋子的正上方)
            foreach (var coverId in card1.CoverIds)
            {
                if (!NeighborCoverIDs.Contains(coverId))
                {
                    isUpNeighbor = false;
                    break;
                }
            }

            if (!CheckDirHaveCard(card1, 1))
            {
                int CoverCount = 0;
                //检测card1是否被card0覆盖,且card1正下方无卡牌
                foreach (var coverId in card1.CoverIds)
                {
                    if (card0.CoverIds.Contains(coverId))
                    {
                        CoverCount++;
                    }
                }

                if (CoverCount >=1) isCoverNeighbor = true;
            }

            if (isUpNeighbor || isCoverNeighbor)
            {
                card2 = card1;
            }
            else
            {
                card2 = null;
            }

            return isUpNeighbor || isCoverNeighbor;
        }
        //获取卡牌卡牌相邻区域的覆盖索引（上下左右16个索引）
        private List<int> GetNeighborCoverIDs(CardCell card1,int count =4,int dir=-1,int groupTy=1)
        {
            //先找到card1的上下左右四个相邻的方向的覆盖索引
            List<int> directions = new List<int>();
            List<CardCell> tempCards = new List<CardCell>();
            var groups = GetGroupCards(card1.MySpecialCardLogic.GetGroupId, groupTy);
            if (groups != null)
            {
                foreach (var item in groups)
                {
                    tempCards.AddWithoutRepetition(item);
                }
            }
            else
            {
                tempCards.AddWithoutRepetition(card1);
            }

            foreach (var item in tempCards)
            {
                //这是获取四个方向的覆盖索引
                if (count == 4 && dir == -1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int newX = item.MyCoordData.X + _rowOffest[i];
                        int newY = item.MyCoordData.Y + _colOffest[i];
                        Vector2Int temp = new Vector2Int(newX, newY);
                        directions.AddRange(GetCoverIDs(temp));
                    }
                }else if (count == 0 && dir >= 0)//这是获取指定方向索引，0-上，1-下，2-左，3-右
                {
                    int newX =item.MyCoordData.X + _rowOffest[dir];
                    int newY = item.MyCoordData.Y + _colOffest[dir];
                    Vector2Int temp = new Vector2Int(newX, newY);
                    directions.AddRange(GetCoverIDs(temp));
                }
            }

            if (groups != null)
            {
                foreach (var item in groups)
                {
                    foreach (var coverId in item.CoverIds)
                    {
                        directions.RemoveAll(x=>x==coverId);
                    }
                }
            }
            return directions;
        }
        /// <summary>
        /// 检测卡牌某方向是否有卡牌
        /// </summary>
        /// <param name="card"></param>
        /// <param name="dir">0-上，1-下，2-左，3-右</param>
        /// <returns></returns>
        public bool CheckDirHaveCard(CardCell card,int dir)
        {
            int newX = card.MyCoordData.X + _rowOffest[dir];
            int newY = card.MyCoordData.Y + _colOffest[dir];
            foreach (var item in _cards)
            {
                if (!item.IsInSolt&&!item.IsStored)
                {
                    if (item.MyCoordData.X == newX && item.MyCoordData.Y == newY)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 获取分组中的根节点卡牌
        /// </summary>
        /// <param name="gruopId">分组id</param>
        /// <param name="groupType">1-拉链，2-藤蔓</param>
        /// <returns></returns>
        public CardCell GetGoupRootCard(int gruopId,int groupType)
        {
            CombinedData temp = new CombinedData(gruopId, groupType);
            if (_gruopCards.ContainsKey(temp))
            {
                return _gruopCards[temp][0];
            }
            return null;
        }
        /// <summary>
        /// 根据分组id获取分组中的所有卡牌
        /// </summary>
        /// <param name="gruopId">分组id</param>
        /// <param name="groupType">1-拉链，2-藤蔓</param>
        /// <returns></returns>
        public List<CardCell> GetGroupCards(int gruopId,int groupType)
        {
            CombinedData temp = new CombinedData(gruopId, groupType);
            if (_gruopCards.ContainsKey(temp))
            {
                return _gruopCards[temp];
            }
            return null;
        }

        public void RemoveOneGroup(int gruopId, int groupType)
        {
            CombinedData temp = new CombinedData(gruopId, groupType);
            _gruopCards.Remove(temp);
        }
        //获取卡牌覆盖索引
        private List<int> GetCoverIDs(Vector2Int Coord)
        {
            List<int> coverIDs = new List<int>();
            coverIDs.Add(Coord.y*ConfigLoader.Constant.LayerRow+(Coord.x));
            coverIDs.Add(Coord.y*ConfigLoader.Constant.LayerRow+(Coord.x+1));
            coverIDs.Add((Coord.y+1)*ConfigLoader.Constant.LayerRow+(Coord.x));
            coverIDs.Add((Coord.y+1)*ConfigLoader.Constant.LayerRow+(Coord.x+1));
            return coverIDs;
        }
        //生成一个卡牌
        public CardCell CreatOneCard(CoordData data,Transform parent,int type,bool isPush = true,bool IsSetPos = true,bool isShell = false)
        {
            CardCell card = GameModule.Resource.LoadGameObject("CardCell", parent).GetComponent<CardCell>();
            card.InitCard(new List<int>(){type},data);
            card.SetVisible(true);
            card.SetParent(parent);
            AddCard(card);
            if (isPush)
            {
                card.SetAnchoredPos(LevelController.Instance.GetLayerLocation(data),IsSetPos);
                card.RecordLocation();
                var coverIds = GetCoverIDs(new Vector2Int(data.X, data.Y));
                card.SetCoverIds(coverIds);
                if (!isShell)
                {
                    PushUnitCoverStack(card);
                }
                else
                {
                    ShellPushUnitCoverStack(card);
                }
                
            }
            return card;
        }
        //蝴蝶卡牌获取飞行目标，如果目标为空，则蝴蝶消失
        public List<CardCell> GetButterflyTarget(List<CardCell> curVisibleCards,bool isDestroy=false)
        {
            //先检测当前可见卡牌中是否有蝴蝶卡牌。若没有，直接返回
            int ButterflyCount = 0;
            List<CardCell> targetList = new List<CardCell>();
            //获取场上蝴蝶数量
            foreach (var item in curVisibleCards)
            {
                if (item.MySpecialCardLogic.GetLockCardType == LockCardType.Butterfly)
                {
                    ButterflyCount++;
                    if (isDestroy)
                    {
                        targetList.Add(item);
                    }
                }
            }

            if (isDestroy) return targetList;
            if(ButterflyCount == 0)return targetList;
            List<CardCell> tempList = new List<CardCell>();
            foreach (var item in curVisibleCards)
            {
                //蝴蝶不能飞向锁住的卡牌和有功能的卡牌，且不是当前自己
                if (!item.MySpecialCardLogic.IsLock &&
                    (item.MySpecialCardLogic.GetFunctionCardTypes == null ||
                     item.MySpecialCardLogic.GetFunctionCardTypes.Count == 0))
                {
                    tempList.Add(item);
                }
            }

            if (tempList.Count >= ButterflyCount)
            {
                for (int i = 0; i < ButterflyCount; i++)
                {
                    targetList.Add(tempList.RandomFromList());
                }
            }
            return targetList;
        }
        //获取场上的蝴蝶卡牌
        public CardCell GetButterfly(List<CardCell> curVisibleCards)
        {
            CardCell target = null;
            List<CardCell> tempList = new List<CardCell>();
            foreach (var item in curVisibleCards)
            {
                tempList.Add(item);
            }

            if (tempList.Count > 0)
            {
                target = tempList.RandomFromList(false);
            }
            return target;
        }
        //检测贝壳是否需要生成卡牌
        public void CheckShellNeedCreateCard(CardCell shell)
        {
            int coverId = 0;
            Stack<CardCell> tempStack;
            CardCell downCard = null;
            List<CardCell> downCards = new List<CardCell>();
            for (int i = 0; i < shell.CoverIds.Count; i++)
            {
                coverId = shell.CoverIds[i];
                tempStack = _unitCoverStack.GetValueOrDefaultDic(coverId);
                if (tempStack == default) continue;
                if (tempStack.Count > 0)
                {
                    downCard = tempStack.Peek();
                    if (CheckIsTop(downCard)&&downCard.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell))
                    {
                        downCards.AddWithoutRepetition(downCard);
                    }
                }
               
            }

            if (downCards.Count > 0)
            {
                foreach (var curShell in downCards)
                {
                    //检测当前贝壳下方是否有卡牌
                    bool DownhaveCard = CheckDirHaveCard(curShell, 1);
                    if (!DownhaveCard)
                    {
                        curShell.MySpecialCardLogic.ExecuteFunction(false,null,true,false,true);
                    }
                }
            }
        }
        //根据卡牌id获取卡牌(新手教程使用)
        public List<CardCell> GetCardsByCardId(List<MyVector3Int> cardIds)
        {
            List<CardCell> tempList = new List<CardCell>();
            foreach (var id in cardIds)
            {
                CardCell card = _cards.Find(x => x.MyCoordData.X == id.X && x.MyCoordData.Y == id.Y&&x.MyCoordData.LayerIndex == id.Z);
                if (card != null)
                {
                    tempList.Add(card);
                }
            }
            //如果没有获取到卡牌，则获取到当前可操作的卡牌
            if (tempList.Count == 0)
            {
                var curVisibleCards = GetVisibleCards();
                tempList.AddRange(curVisibleCards);
            }
            return tempList;
        }
        //检测当前卡牌顶层是否有可操作的卡牌
        private void CheckDoNotCards()
        {
            var list = GetVisibleCards();
            //如果操作区域无卡牌或者暂存区也有卡牌，直接返回
            if (list.Count == 0||_storeSlot.IsStoreHasCard()) return;
            //不可操作
            bool NotOperate = true;
            bool isAllIce = false;
            for (int i = 0; i < list.Count; i++)
            {
                var card = list[i];
                if (!card.MySpecialCardLogic.IsLock)
                {
                    NotOperate = false;
                    break;
                }
            }
            //如果暂存区还有卡牌，则不可操作
            if (_storeSlot.IsStoreHasCard())
            {
                NotOperate = false;
            }
            if (NotOperate)
            {
                isAllIce = list.TrueForAll(x => x.MySpecialCardLogic.GetLockCardType == LockCardType.IceCube);
                if (isAllIce)
                {
                    Lose(3);
                }
            }
        }
        //检测当前界面只有门卡牌
        private bool CheckOnlyDoorCard()
        {
            //所有棋盘上的卡牌
            var allGameCards = _cards.FindAll(x => x.IsInSolt==false);
            bool allIsDoor = allGameCards.TrueForAll(x => x.MySpecialCardLogic.GetLockCardType == LockCardType.Door);
            if (allIsDoor)
            {
                bool allOpen = true;
                foreach (var card in allGameCards)
                {
                    if (card.MySpecialCardLogic.IsLock)
                    {
                        allOpen = false;
                        break;
                    }
                }
                return allOpen;
            }
            else
            {
                return false;
            }
        }
        //检测当前区域内需要相邻卡牌解锁的特殊卡牌能否操作
        private void CheckDoNotLockCard()
        {
            //找到当前可见卡牌，找出所有需要相邻卡牌解锁的特殊卡牌，且被覆盖的
            var tCards = new List<CardCell>();
            foreach (var item in _cards)
            {
                if (item.IsCovered && item.IsVisible)
                {
                    /*if (item.MySpecialCardLogic.GetLockCardType == LockCardType.OneShackle ||
                        item.MySpecialCardLogic.GetLockCardType == LockCardType.Honey ||
                        item.MySpecialCardLogic.GetLockCardType == LockCardType.Rock ||
                        item.MySpecialCardLogic.GetLockCardType == LockCardType.TwoShackles ||
                        item.MySpecialCardLogic.GetLockCardType == LockCardType.Grassy)
                    {
                        tCards.AddWithoutRepetition(item);
                    }*/

                    if (item.MySpecialCardLogic.GetLockCardType == LockCardType.Vines && item.MySpecialCardLogic.IsRoot)
                    {
                        tCards.AddWithoutRepetition(item);
                    }
                }
            }
            if(tCards.Count==0) return;
            for (int i = 0; i < tCards.Count; i++)
            {
                CardCell item = tCards[i];
                var coverIds = GetNeighborCoverIDs(item, groupTy: 2);
                int satisfyTimes = 0;
                //获取同一层卡牌
                var sameLayerCards = _cards.FindAll(x => x.MyCoordData.LayerIndex == item.MyCoordData.LayerIndex && x.IsVisible&&x!=item&&!x.MySpecialCardLogic.IsLockCard&&
                                                         !x.IsInSolt&&!x.IsStored);
                if (sameLayerCards.Count > 0)
                {
                    for (int j = 0; j < sameLayerCards.Count; j++)
                    {
                        var tempCard = sameLayerCards[j];
                        if (PlayGameExtensions.IsSubset(tempCard.CoverIds, coverIds))
                        {
                            satisfyTimes++;
                        }
                    }
                }

                if (satisfyTimes == 0)
                {
                    Lose(4);
                    return;
                }
            }
           
        }
        #endregion

        #region 现金卡牌
        /// <summary>
        /// 均分现金总额(红包算法)
        /// </summary>
        /// <param name="total"></param>
        /// <param name="groups"></param>
        /// <returns></returns>
        public List<float> DistributeCash(float totalDollars, int count)
        {
            List<float> rewards = new List<float>();
            float remainingDollars = totalDollars;
            int remainingCount = count;
            if (count == 1)
            {
                rewards.Add(totalDollars);
                return rewards;
            }
            System.Random random = new System.Random();
        
            for (int i = 0; i < count - 1; i++)
            {
                // 计算当前可分配的平均值
                float average = remainingDollars / remainingCount;
            
                // 设置波动范围（可调整）
                float min = Mathf.Max(0.01f, average * 0.7f); // 最小为平均值的70%，至少0.01美元
                float max = Mathf.Min(remainingDollars - (remainingCount - 1) * 0.01f, 
                    average * 1.5f); // 最大为平均值的130%
            
                // 确保有足够的余额给剩余的分配（至少每个0.01美元）
                max = Mathf.Min(max, remainingDollars - (remainingCount - 1) * 0.01f);
            
                // 随机生成当前奖励金额（保留2位小数）
                float reward = GenerateRandomFloat(random, min, max, 2);
            
                rewards.Add(reward);
                remainingDollars -= reward;
                remainingCount--;
            }
        
            // 最后一个直接分配剩余金额，并四舍五入到2位小数
            float lastReward = (float)System.Math.Round(remainingDollars, 2);
            rewards.Add(lastReward);
        
            return rewards;
        }
        // 生成指定小数位数的随机浮点数
        private static float GenerateRandomFloat(System.Random random, float min, float max, int decimalPlaces)
        {
            float randomValue = (float)(random.NextDouble() * (max - min) + min);
            return (float)System.Math.Round(randomValue, decimalPlaces);
        }
        #endregion
    }
}
