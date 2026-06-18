using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Y_Base;
using Sirenix.Utilities;
using System;
using Config.common;
using DG.Tweening;
using TK_Engine;
using UnityEngine.Experimental.Rendering;
using AudioType = TK_Engine.AudioType;


namespace GameLogic
{
    public partial class CardsController : Singleton<CardsController>
    {
        //所有的卡牌
        private List<CardCell> _cards = new List<CardCell>();

        public List<CardCell> Cards => _cards;

        //层级覆盖的卡牌
        private Dictionary<int, Stack<CardCell>> _unitCoverStack = new Dictionary<int, Stack<CardCell>>();

        //自由堆叠的卡牌
        private List<Stack<CardCell>> _cardFreeStacks = new List<Stack<CardCell>>();

        //分组的卡牌（藤蔓、拉链）
        private Dictionary<CombinedData, List<CardCell>> _gruopCards = new Dictionary<CombinedData, List<CardCell>>();

        private List<CardCell> _butterflyCards = new List<CardCell>();
        //当前触发游戏失败的卡牌
        public List<CardCell> CurFailBombCards = new List<CardCell>();

        //当前点击的卡牌
        private List<CardCell> _curClickCards = new List<CardCell>();

        //游戏失败次数
        public int _loseCount = 0;
        //游戏失败
        public bool IsFail;
        #region 卡牌槽位管理
        private Widget_EliminateSlot _eliminateSlot;
        private Widget_StoreSlot _storeSlot;
        public Widget_StoreSlot StoreSlot => _storeSlot;
        public Widget_EliminateSlot EliminateSlot => _eliminateSlot;
        private Transform _cardMoving;
        public Transform CardMoving => _cardMoving;
        #endregion

        #region 道具功能之卡牌打乱字段

        private List<CardCell> _messUpCards;
        private List<int> _tempTypes;
        private List<Vector2> _cardOriginalPoses;
        private bool _isSendIn;
        private bool _isSendOut;
        private float _messUpTimer;
        private const float SEND_TIME = 0.3f;
        #endregion
        int[] _rowOffest = { 0, 0, -2, 2 };
        int[] _colOffest = { -2, 2, 0, 0 };
        //延迟事件id
        private List<int> timerIds = new List<int>();
        //此时游玩状态，点击棋子状态、使用道具状态
        public playCardType CurPlayCardType;

        public void Init(Widget_EliminateSlot eliminateSlot, Widget_StoreSlot storeSlot, Transform cardMoving)
        {
            _cards.Clear();
            _unitCoverStack.Clear();
            _cardFreeStacks.Clear();
            _gruopCards.Clear();
            CurFailBombCards.Clear();
            _curClickCards.Clear();
            _eliminateSlot = eliminateSlot;
            _storeSlot = storeSlot;
            _cardMoving = cardMoving;

            _eliminateSlot.Reset(7);
            _eliminateSlot.DelEliminate = OnEliminateEnd;
            _eliminateSlot.DelFull = Lose;
            _storeSlot.Reset();
            _loseCount = 0;
            IsFail = false;
            CurPlayCardType = playCardType.None;
            Utility.Unity.AddUpdateListener(MessUpUpdate);
        }

        public void AddCard(CardCell card)
        {
            _cards.AddWithoutRepetition(card);
            //注册事件
            card.DelClick = OnClickCard;
            card.DelEliminate = OnEliminateCard;
            card.DelMoveBack = OnMoveBackCard;
        }

        public Stack<CardCell> GetCardFreeStack(int index)
        {
            if (index > _cardFreeStacks.Count - 1)
            {
                _cardFreeStacks.Add(new Stack<CardCell>());
            }

            return _cardFreeStacks[index];
        }

        #region 卡牌交互

        private void OnClickCard(CardCell card, bool useSpeRemoveStack = false)
        {
            if (_eliminateSlot.IsFull) return;
            CurPlayCardType = playCardType.clikcard;
            bool _multiCards = false;
            //是否执行卡牌特殊交互
            bool doSpecial = false;
            //是否等待动画结束
            bool waitAnim = false;
            //如果卡牌不在槽位上
            if (!card.IsInSolt)
            {
                var curVisibleCards = GetVisibleCards(card);
                if (card.StackIndex < 0)
                {
                    PopUnitCoverStack(card, out var downCards, useSpeRemoveStack);
                    if (card.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Zipper))
                    {
                        PopOtherCard(card, 1, out var downCards1);
                        if (downCards1 != null)
                        {
                            downCards.AddRange(downCards1);
                        }
                    }
                    doSpecial = true;
                    foreach (var item in downCards)
                    {
                        if (!item.IsCovered && item.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell))
                        {
                            curVisibleCards.AddWithoutRepetition(item);
                        }
                    }
                }
                else
                {
                    //处理临时区的卡牌
                    if (card.IsStored)
                    {
                        _storeSlot.StackOPop(card);
                        doSpecial = true;
                    }
                    //处理自由堆叠的卡牌
                    else
                    {
                        Stack<CardCell> tempStack = _cardFreeStacks[card.StackIndex];
                        tempStack.Pop();
                        if (tempStack.Count > 0)
                        {
                            tempStack.Peek().ExposeCell();
                        }
                        doSpecial = true;
                    }
                }
                card.IsInSolt = true;
                if (doSpecial)
                {
                    //var curVisibleCards = GetVisibleCards();
                    waitAnim = card.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Zipper);
                    //处理特殊交互
                    FunctionCardDone(card, curVisibleCards);
                    UnLockCard(card, curVisibleCards);

                }

                //如果卡牌不许等待动画结束，就直接移动到目标区域
                //如果像门卡牌、拉链、双面牌这类需要等待动画结束再移动的，他们的动画结束后会调用MoveToEliminateCard函数
                if (!waitAnim)
                {
                    MoveToEliminateCard(card, _multiCards);
                }

            }
        }
        //卡牌移向目标位置
        public void MoveToEliminateCard(CardCell card, bool isMultiCards)
        {
            if (GuideController.Instance.TriggerGuideId != 105 && GuideController.Instance.TriggerGuideId != 112)
            {
                GuideController.Instance.NextTriggerGuide();
            }
            //是否多卡牌添加
            if (!isMultiCards)
            {
                card.SetParent(_cardMoving);
                //移向卡槽
                _eliminateSlot.AddOneCard(card);
            }
            else
            {
                var multi = GetGroupCards(card.MySpecialCardLogic.GetGroupId, 1);
                foreach (var item in multi)
                {
                    item.SetParent(_cardMoving);
                }
                _eliminateSlot.AddMultiCard(multi);
                //从分组里面删除
                RemoveOneGroup(card.MySpecialCardLogic.GetGroupId, 1);
            }
            //检测当前游戏界面是否继续能操作，如果是冰块，触发失败
            CheckDoNotCards();
            //检测是否有锁住的卡牌能否操作
            CheckDoNotLockCard();
        }
        //棋子非点击移动
        public void NoClickCard(List<MyVector3Int> cardIds)
        {
            var allCards = GetCardsByCardId(cardIds);
            foreach (var item in allCards)
            {
                item.SetVisible(true, true);
                item.ExposeCell();
                OnClickCard(item, true);
            }
        }
        //一组卡牌的其它对象同时移出栈顶
        public void PopOtherCard(CardCell CurCard, int type, out List<CardCell> MydownCards)
        {
            var multi = GetGroupCards(CurCard.MySpecialCardLogic.GetGroupId, type);
            List<CardCell> alldownCards = null;
            foreach (var item in multi)
            {
                if (item != CurCard)
                {
                    item.IsInSolt = true;
                    PopUnitCoverStack(item, out var downCards);
                    alldownCards = downCards;
                }
            }

            MydownCards = alldownCards;
        }
        private void OnEliminateCard(CardCell card)
        {
            if (_cards.Contains(card))
            {
                _cards.Remove(card);
            }
            AddTempCardCell(false, card);
        }

        private void OnMoveBackCard(CardCell card)
        {
            CurPlayCardType = playCardType.None;
            if (card.StackIndex < 0)
            {
                PushUnitCoverStack(card);
            }
            else
            {
                if (card.IsStored) //撤回到暂存区
                {
                    _storeSlot.StackPush(card);
                }
                else //撤回到自由堆叠区
                {
                    Stack<CardCell> tempStack = _cardFreeStacks[card.StackIndex];
                    if (tempStack.Count > 0)
                    {
                        tempStack.Peek().CoverCell();
                    }

                    tempStack.Push(card);
                }
            }
            card.IsInSolt = false;
        }
        #endregion

        #region 卡牌槽位逻辑

        //消除卡牌结束回调
        private void OnEliminateEnd(int cardType, Vector3 pos)
        {
            //雪花卡牌冻结时间
            if (cardType == ConfigLoader.Constant.SnowCard)
            {
                //发送特效
                PropsController.FreezeTime(pos, (Tr) =>
                {
                    GameEvent.Send(EventType.FreezeTime, ConfigLoader.Constant.FreezeTime, false);
                });

            }
            //网兜卡牌抓蝴蝶
            else if (cardType == ConfigLoader.Constant.NetbagCard)
            {
                var curVisibleCards = _butterflyCards;
                CardCell butterfly = GetButterfly(curVisibleCards);
                if (butterfly == null)
                {
                    CheckResult();
                    return;
                }
                GameEvent.Send(EventType.SetTipsMask, true);
                butterfly.MySpecialCardLogic.DestoryButterfly();
                GameObject netObj = FlySystem.Instance.ShowEff("prefab_FlyNet", null, butterfly.transform, Vector3.one);
                ShowPropVfx netVfx = netObj.GetComponent<ShowPropVfx>();
                netVfx.PlayPropVfx(true, () =>
                {
                    GameEvent.Send(EventType.SetTipsMask, false);
                });
            }
            //网赚卡牌
            else if (cardType == ConfigLoader.Constant.MoneyCard)
            {
                /*_curGetMoneyIndex++;
                float tGetone1 = (float)Math.Round(_curGetOneMoney[_curGetMoneyIndex] * 0.01f, 3);
                //Log.Warning($"此时的金额={tGetone1}");
                FlySystem.Instance.ShowCashOutEff(tGetone1, (Tr) =>
                {
                    GameEvent.Send(EventType.SendLevelCash,tGetone1);
                    Sequence seq = DOTween.Sequence();
                    seq.Append(Tr?.DOScale(1.1f, 0.15f));
                    seq.Append(Tr?.DOScale(0.9f, 0.15f));
                    seq.Append(Tr?.DOScale(1f, 0.15f));
                });
                _curGetMoney += tGetone1;*/
                CheckResult();
            }
            else
            {
                CheckResult();
            }
        }

        //检测是否有胜利条件
        private void CheckResult()
        {
            if (_cards.Count <= 0)
            {
                Win();
            }
        }

        #endregion

        #region 卡牌层级堆叠管理

        //加入层级栈
        public void PushUnitCoverStack(CardCell card)
        {
            int coverId = 0;
            Stack<CardCell> stack;
            CardCell downCard = null;
            for (int i = 0; i < card.CoverIds.Count; i++)
            {
                coverId = card.CoverIds[i];
                if (!_unitCoverStack.ContainsKey(coverId))
                {
                    _unitCoverStack[coverId] = new Stack<CardCell>();
                }

                stack = _unitCoverStack[coverId];
                if (stack.Count > 0)
                {
                    downCard = stack.Peek();
                    downCard.CoverCell();
                }
                stack.Push(card);
                if (downCard != null && downCard.IsVisible && CheckIsVisible(downCard))
                {
                    downCard.SetVisible(false);
                }
            }
        }
        //贝壳创建棋子需要特殊入栈，这里单独处理(当贝壳生成的卡牌区域已经有可操作的卡牌，需要把生成的卡牌放在可操作卡牌的下一层)
        public void ShellPushUnitCoverStack(CardCell card)
        {
            int coverId = 0;
            Stack<CardCell> stack;
            CardCell downCard = null;
            bool isTop;
            for (int i = 0; i < card.CoverIds.Count; i++)
            {
                coverId = card.CoverIds[i];
                isTop = false;
                if (!_unitCoverStack.ContainsKey(coverId))
                {
                    _unitCoverStack[coverId] = new Stack<CardCell>();
                }

                stack = _unitCoverStack[coverId];
                if (stack.Count > 0)
                {
                    downCard = stack.Peek();
                    if (downCard.IsVisible && !downCard.IsCovered)
                    {
                        stack.Pop();
                        isTop = true;
                    }
                    else
                    {
                        downCard.CoverCell();
                    }
                }
                stack.Push(card);
                if (isTop)
                {
                    stack.Push(downCard);
                    card.CoverCell();
                }
                else
                {
                    if (downCard != null && downCard.IsVisible && CheckIsVisible(downCard))
                    {
                        downCard.SetVisible(false);
                    }
                }
            }
        }
        //加入分组列表
        public void AddGruopList(Dictionary<int, List<CardCell>> tempGruop)
        {
            foreach (var group in tempGruop)
            {
                //从左往右
                group.Value.Sort(SortRow);
                for (int i = 0; i < group.Value.Count; i++)
                {
                    int gruopid = group.Value[i].MySpecialCardLogic.GetGroupId;
                    //1-拉链，2-藤蔓
                    int groupType = group.Value[0].MySpecialCardLogic.GetFunctionCardTypes
                        .Contains(FunctionCardType.Zipper)
                        ? 1
                        : 2;
                    List<CardCell> templsit;
                    CombinedData curCombinedData = new CombinedData(gruopid, groupType);
                    if (!_gruopCards.ContainsKey(curCombinedData))
                    {
                        _gruopCards[curCombinedData] = new List<CardCell>();
                    }

                    templsit = _gruopCards[curCombinedData];
                    templsit.Add(group.Value[i]);
                    //加入分组根节点
                    if (i > 0)
                    {
                        group.Value[i].MySpecialCardLogic.SetGroupParent(true, group.Value[0].transform);
                    }
                }

            }
        }

        #region 排序

        private int SortRow(CardCell c1, CardCell c2)
        {
            if (c1.MyCoordData.X > c2.MyCoordData.X) return 1;
            else if (c1.MyCoordData.X < c2.MyCoordData.X) return -1;
            return 0;
        }

        #endregion

        //移出层级栈（栈顶）
        public void PopUnitCoverStack(CardCell card, out List<CardCell> downCards, bool useSpeRemoveStack = false)
        {
            int coverId = 0;
            Stack<CardCell> tempStack;
            CardCell downCard = null;
            downCards = new List<CardCell>();
            for (int i = 0; i < card.CoverIds.Count; i++)
            {
                coverId = card.CoverIds[i];
                tempStack = _unitCoverStack.GetValueOrDefaultDic(coverId);
                if (tempStack == default) continue;
                if (!useSpeRemoveStack)
                {
                    tempStack.Pop();
                }
                else
                {
                    //特殊移除方式
                    tempStack.RemoveItem(card);
                }

                if (tempStack.Count > 0)
                {
                    downCard = tempStack.Peek();
                    downCard.SetVisible(true, true);
                    if (CheckIsTop(downCard))
                    {
                        downCard.ExposeCell();
                    }
                    downCards.AddWithoutRepetition(downCard);
                }
                else
                {
                    _unitCoverStack.Remove(coverId);
                }
            }
        }

        //检测卡牌可见性
        private bool CheckIsVisible(CardCell card)
        {
            int coverId = 0;
            bool allCovered = true;
            Stack<CardCell> stack;
            if (card.MySpecialCardLogic.IsRoot)
            {
                int id = card.MySpecialCardLogic.GetLockCardType == LockCardType.Vines ? 2 : 1;
                var tmeplist = GetGroupCards(card.MySpecialCardLogic.GetGroupId, id);
                foreach (var item in tmeplist)
                {
                    for (int i = 0; i < item.CoverIds.Count; i++)
                    {
                        coverId = item.CoverIds[i];
                        stack = _unitCoverStack[coverId];
                        if (item == stack.Peek())
                        {
                            allCovered = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (card.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell)) return false;
                for (int i = 0; i < card.CoverIds.Count; i++)
                {
                    coverId = card.CoverIds[i];
                    stack = _unitCoverStack[coverId];
                    if (card == stack.Peek())
                    {
                        allCovered = false;
                        break;
                    }
                }
            }
            return allCovered;
        }

        public bool CheckIsTop(CardCell card)
        {
            int coverId = 0;
            Stack<CardCell> tempStack;
            if (card.MySpecialCardLogic.GetLockCardType == LockCardType.Vines)
            {
                var groups = GetGroupCards(card.MySpecialCardLogic.GetGroupId, 2);
                foreach (var item in groups)
                {
                    for (int i = 0; i < item.CoverIds.Count; i++)
                    {
                        coverId = item.CoverIds[i];
                        tempStack = _unitCoverStack.GetValueOrDefaultDic(coverId);
                        if (item != tempStack.Peek())
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < card.CoverIds.Count; i++)
                {
                    coverId = card.CoverIds[i];
                    tempStack = _unitCoverStack.GetValueOrDefaultDic(coverId);
                    if (tempStack == default) return true;
                    if (card != tempStack.Peek())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion


        private void Win()
        {
            GameEvent.Send(EventType.SetGameMask, true);

            OneSelectPhotoInfo oneSelect = LevelController.Instance.CurLevelSelectPhotoInfo;
            if (oneSelect.IsVideo)
            {
                Action callBack = () =>
                {
                    GameModule.UI.ShowUI<Panel_Succeed>();
                };
                GameEvent.Send(EventType.OpenGameBgAvPro, callBack);
            }
            else
            {
                //游戏面板关闭动画
                GameEvent.Send(EventType.ShowGameClosePanel);
                //弹出结算界面
                int timeid = GameModule.Timer.AddTimer(timer =>
                   {
                       GameModule.UI.ShowUI<Panel_Succeed>();
                   }, 0.5f);
                timerIds.Add(timeid);
            }

        }
        /// <summary>
        /// 游戏失败
        /// </summary>
        /// <param name="loseType">游戏失败类型：0-合成失败，1-炸弹卡牌触发失败，2倒计时触发失败,3冰块触发失败,4藤蔓触发失败</param>
        public void Lose(int loseType = 0)
        {
            if (IsFail) return;//避免多次触发
            IsFail = true;
            _loseCount++;
            GameEvent.Send(EventType.PauseGameTime, true);
            GameEvent.Send(EventType.SetGameMask, true);
            int timeid = GameModule.Timer.AddTimer(timer =>
            {
                GameModule.UI.ShowUI<Panel_Fail>(_loseCount, loseType);
            }, 0.5f);
            timerIds.Add(timeid);
        }

        //退出游戏
        public void ExitGame()
        {
            Utility.Unity.RemoveUpdateListener(MessUpUpdate);
            foreach (var id in timerIds)
            {
                GameModule.Timer.RemoveTimer(id);
            }
            _eliminateSlot.ClearTimerId();
            _curClickCards.Clear();
        }
        /// <summary>
        /// 用于记录操作的卡牌数量，当到达槽位限制数量，开启遮罩就不能继续点击
        /// </summary>
        /// <param name="isAdd">是否加入</param>
        /// <param name="cell"></param>
        public void AddTempCardCell(bool isAdd, CardCell cell)
        {
            if (isAdd)
            {
                if (cell.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Zipper))
                {
                    var multi = GetGroupCards(cell.MySpecialCardLogic.GetGroupId, 1);
                    foreach (var item in multi)
                    {
                        _curClickCards.AddWithoutRepetition(item);
                    }
                }
                else
                {
                    _curClickCards.AddWithoutRepetition(cell);
                }
                if (_curClickCards.Count >= _eliminateSlot.MaxSlotCount)
                {
                    GameEvent.Send(EventType.SetGameMask, true);
                }
            }
            else
            {
                if (cell.MySpecialCardLogic.IsFunctionCard(FunctionCardType.Zipper))
                {
                    var multi = GetGroupCards(cell.MySpecialCardLogic.GetGroupId, 1);
                    foreach (var item in multi)
                    {
                        _curClickCards.RemoveSafe(item);
                    }
                }
                else
                {
                    _curClickCards.RemoveSafe(cell);
                }

                if (_curClickCards.Count < _eliminateSlot.MaxSlotCount)
                {
                    GameEvent.Send(EventType.SetGameMask, false);
                }
            }
        }
    }

    public struct CombinedData
    {
        public int CombinedId;
        //1-拉链，2-藤蔓
        public int CombinedType;

        public CombinedData(int id, int type)
        {
            CombinedId = id;
            CombinedType = type;
        }
    }
}

public enum playCardType
{
    None,//无状态
    clikcard,//点击卡片棋子状态
    useprop,//使用道具状态
}