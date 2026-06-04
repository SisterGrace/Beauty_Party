using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_StoreSlot : UIWidget
	{
		private List<Stack<CardCell>> _cardStoreStacks;
		public List<Stack<CardCell>> CardStoreStacks=> _cardStoreStacks;
		public void Reset()
		{
			_cardStoreStacks = new List<Stack<CardCell>>();
		}

		public void StoreCard(CardCell cardCell, int stackIndex)
		{
			while (_cardStoreStacks.Count -1< stackIndex)
			{
				_cardStoreStacks.Add(new Stack<CardCell>());
			}
			cardCell.StackIndex = stackIndex;
			cardCell.IsStored =true;
			cardCell.MoveTo(mCasualSolts.GetChild(stackIndex).position+Vector3.up*0.1f*_cardStoreStacks[stackIndex].Count, OnCardMoveEnd);
		}
		//删除栈顶的卡牌
		public void StackOPop(CardCell card)
		{
			//card.IsStored = false;
			_cardStoreStacks[card.StackIndex].Pop();
			if (_cardStoreStacks[card.StackIndex].Count > 0)
			{
				_cardStoreStacks[card.StackIndex].Peek().ExposeCell();;
			}
		}

		public void StackRemove(CardCell card)
		{
			Stack<CardCell> tempStack = new Stack<CardCell>();
			Stack<CardCell> currentStack = _cardStoreStacks[card.StackIndex];
			bool isFind = false;
			while (currentStack.Count>0&&!isFind)
			{
				CardCell tempCard = currentStack.Pop();
				if (tempCard == card&&!isFind)
				{
					isFind = true;
				}
				else
				{
					tempStack.Push(tempCard);
				}
			}

			while (tempStack.Count>0)
			{
				currentStack.Push(tempStack.Pop());
			}
			if (currentStack.Count > 0)
			{
				currentStack.Peek().CoverCell();
			}
		}
		//将卡牌压入栈顶
		public void StackPush(CardCell card)
		{
			if (_cardStoreStacks[card.StackIndex].Count > 0)
			{
				_cardStoreStacks[card.StackIndex].Peek().CoverCell();
			}
			_cardStoreStacks[card.StackIndex].Push(card);
		}
		private void OnCardMoveEnd(CardCell card)
		{
			Stack<CardCell> tempStack = _cardStoreStacks[card.StackIndex];
			if (tempStack.Count > 0)
			{
				tempStack.Peek().CoverCell();
			}
			tempStack.Push(card);
			card.SetParent(mCasualSolts.GetChild(card.StackIndex));
			card.IsInSolt = false;
			card.RecordLocation();
			CardsController.Instance.CurPlayCardType = playCardType.None;
		}
		/// <summary>
		/// 判断暂存区是否有卡牌
		/// </summary>
		/// <param name="CheckCash">是否检测现金棋子</param>
		/// <returns></returns>
		public bool IsStoreHasCard(bool CheckCash =false)
		{
			if(_cardStoreStacks.Count==0)return false;
			if (!CheckCash)
			{
				foreach (Stack<CardCell> stack in _cardStoreStacks)
				{
					if (stack.Count > 0)
					{
						return true;
					}
				}
				return false;
			}
			else
			{
				int cashCount = 0;
				int normalCount = 0;
				foreach (Stack<CardCell> stack in _cardStoreStacks)
				{
					normalCount += stack.Count;
					foreach (CardCell card in stack)
					{
						if (card != null&&card.CardType == ConfigLoader.Constant.MoneyCard)
						{
							cashCount++;
						}
					}
				}

				return normalCount > cashCount;
			}
			
		}
		/// <summary>
		/// 获得一种暂存区的卡牌类型
		/// </summary>
		/// <param name="CheckCash">是否检测现金棋子</param>
		/// <returns></returns>
		public int GetStoreCardType(bool CheckCash = false)
		{
			if (!IsStoreHasCard(CheckCash)) return 1;
			if (!CheckCash)
			{
				foreach (Stack<CardCell> stack in _cardStoreStacks)
				{
					if (stack.Count > 0)
					{
						return stack.Peek().CardType;
					}
				}
				return 1;
			}
			else
			{
				foreach (Stack<CardCell> stack in _cardStoreStacks)
				{
					if (stack.Count > 0)
					{
						if (stack.Peek().CardType != ConfigLoader.Constant.MoneyCard)
						{
							return stack.Peek().CardType;
						}
					}
				}
				return 1;
			}
			
		}
		/// <summary>
		/// 获得暂存区堆顶卡牌
		/// </summary>
		/// <returns></returns>
		public List<CardCell> GetStoreCardList()
		{
			var cardList = new List<CardCell>();
			foreach (Stack<CardCell> stack in _cardStoreStacks)
			{
				if (stack.Count > 0)
				{
					cardList.Add(stack.Peek());
				}
			}
			return cardList;
		}
	}
}
