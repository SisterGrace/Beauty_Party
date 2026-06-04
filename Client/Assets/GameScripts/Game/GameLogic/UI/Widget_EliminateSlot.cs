using TK_Engine;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using GameBase;
using UnityEngine.UI;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_EliminateSlot : UIWidget
	{
		public Action<int, Vector3> DelEliminate;
		public Action<int> DelFull;
		private int _maxSlotCount;
		private List<CardCell> _cardList;
		private Stack<CardCell> _cardStack;
		private bool _isFull;
		private List<int> _timerids = new List<int>();
		//卡牌槽位最大数量
		public int MaxSlotCount
		{
			get=> _maxSlotCount;
			set
			{
				_maxSlotCount = value;
			}
		}
		//槽位是否已满
		public bool IsFull => _cardList.Count >= _maxSlotCount;
		public List<CardCell> SoltCardList => _cardList;

		public bool SetFull
		{
			get => _isFull;
			set
			{
				_isFull = value;
			}
		}
		public void Reset(int maxSlotCount)
		{
			_cardList = new List<CardCell>();
			_cardStack = new Stack<CardCell>();
			_isFull = false;
			_maxSlotCount = maxSlotCount;
			int SlotCount = ConfigLoader.Constant.MaxSlotCount;
			for (int i = 0; i < SlotCount; i++)
			{
				Widget_SlotPos pos = CreateWidgetByType<Widget_SlotPos>(mAllSlotPos);
				pos.gameObject.name = "SlotPos_" + i;
				bool isShowAdd = i > (maxSlotCount-1);
				pos.Open(isShowAdd);
			}
		
		}
		/// <summary>
		/// 添加卡牌到槽位(单个)
		/// </summary>
		/// <param name="card"></param>
		public void AddOneCard(CardCell card)
		{
			//获取插入索引
			int insertIndex = _cardList.Count;
			for (int i = _cardList.Count - 1; i >= 0; i--)
			{
				//如果卡牌类型相同，则插入到该类型卡牌后面
				if (_cardList[i].CardType == card.CardType)
				{
					insertIndex = i + 1;
					break;
				}
			}
			_cardList.Insert(insertIndex, card);
			_cardStack.Push(card);
			card.MoveTo2(GerPosByIndex(insertIndex),CardMoveEnd);
			RearrangeSlot();
		}
		/// <summary>
		/// 添加卡牌到槽位（多个）
		/// </summary>
		/// <param name="cards"></param>
		public void AddMultiCard(List<CardCell> cards)
		{
			foreach (var card in cards)
			{
				//获取插入索引
				int insertIndex = _cardList.Count;
				for (int i = _cardList.Count - 1; i >= 0; i--)
				{
					//如果卡牌类型相同，则插入到该类型卡牌后面
					if (_cardList[i].CardType == card.CardType)
					{
						insertIndex = i + 1;
						break;
					}
				}
				_cardList.Insert(insertIndex, card);
				_cardStack.Push(card);
			}

			for (int i = 0; i < cards.Count; i++)
			{
				int newIndex = _cardList.IndexOf(cards[i]);
				//注意：当最后一张卡牌移动结束时，需要调用带有检测CardMoveEnd
				cards[i].MoveTo2(GerPosByIndex(newIndex),i==cards.Count-1?CardMoveEnd:CardMoveEnd2);
				RearrangeSlot();
			}
		}
		private Vector3 GerPosByIndex(int index)
		{
			//如果获取到的索引大于等于最大槽位数量，则返回最后一个槽位索引位置加槽位间距
			if (index >= _maxSlotCount)
			{
				//获取每个槽位的间距
				float dis = Vector3.Distance(mAllSlotPos.GetChild(0).position, mAllSlotPos.GetChild(1).position);
				return mAllSlotPos.GetChild(mAllSlotPos.childCount - 1).position + new Vector3(dis, 0, 0);
			}
			return mAllSlotPos.GetChild(index).position;
		}
		//卡牌移动结束，有检测
		private void CardMoveEnd(CardCell card)
		{
			card.SetParent(mCards);
			card.CardRect.SetSiblingIndex(_cardList.IndexOf(card));
			/*if (GuideController.Instance.TriggerGuideId != 105&&GuideController.Instance.TriggerGuideId!= 112)
			{
				GuideController.Instance.NextTriggerGuide();
			}*/
			bool canEliminate = CheckStatus();
			

			CardsController.Instance.CurPlayCardType = playCardType.None;
		}
		//卡牌移动结束,没有检测
		private void CardMoveEnd2(CardCell card)
		{
			card.SetParent(mCards);
			card.CardRect.SetSiblingIndex(_cardList.IndexOf(card));
		}
		//检测此时卡牌是否可以消除，是否满位
		private bool CheckStatus()
		{
			int checkingType = 0;
			List<CardCell> tempList = new List<CardCell>();
			for (int i = 0; i < _cardList.Count; i++)
			{
				//排除暂存区域卡牌
				if(_cardList[i].CardRect.parent!=mCards) continue;
				if (checkingType != _cardList[i].CardType)
				{
					checkingType = _cardList[i].CardType;
					tempList.Clear();
				}
				tempList.Add(_cardList[i]);
				if (tempList.Count == 3) 
					break;
			}

			if (tempList.Count == 3)
			{
				float duration = 0.2f;
				for (int i = 0; i < tempList.Count; i++)
				{
					_cardList.Remove(tempList[i]);
					tempList[i].Eliminate(duration);
				}
				DelEliminate?.Invoke(tempList[1].CardType, tempList[1].transform.position);
				//手机震动反馈
				SDKManager.GameShake(1,1);
				//重新排列卡牌
				int timeId = GameModule.Timer.AddTimer(time =>
				{
					RearrangeSlot();
				}, duration+0.05f);
				_timerids.Add(timeId);
				return true;
			}
			else
			{
				int setDownCount = 0;
				for (int i = 0; i < _cardList.Count; i++)
				{
					if (_cardList[i].CardRect.parent == mCards)
					{
						setDownCount++;
					}
				}

				if (setDownCount >= _maxSlotCount)
				{
					if (!_isFull)
					{
						_isFull = true;
						DelFull?.Invoke(0);
					}
				}
				return false;
			}
		}
		//重新排列卡牌
		public void RearrangeSlot()
		{
			float duration = 0.2f;
			for (int i = 0; i < _cardList.Count; i++)
			{
				if(!_cardList[i].IsInSolt) continue;
				_cardList[i].MoveTo(GerPosByIndex(i),null,duration);
			}
			//动画结束后再次检测
			int timerId = GameModule.Timer.AddTimer(time =>
			{
				CheckStatus();
			}, duration+0.05f );
			_timerids.Add(timerId);
		}
		/// <summary>
		/// 从槽位中撤销一张卡牌
		/// </summary>
		/// <returns></returns>
		public CardCell Revoke()
		{
			if (_cardList.Count <= 0)
			{
				_cardStack.Clear();
				GameEvent.Send(EventType.ShowTips, "uitps_setting_noundotip",true);
				return null;
			}

			CardCell revokeCard = null;
			do
			{
				revokeCard = _cardStack.Pop();
			} while (!_cardList.Contains(revokeCard));

			_cardList.Remove(revokeCard);
			RearrangeSlot();
			_isFull = false;
			return revokeCard;
		}
		/// <summary>
		/// 从槽位中取出前三张卡牌
		/// </summary>
		/// <returns></returns>
		public List<CardCell> removeFirstThree()
		{
			if (_cardList.Count <= 0)
			{
				return null;
			}
			List<CardCell> tempList = new List<CardCell>();
			tempList.AddRange(_cardList.GetRange(0, Mathf.Min(3, _cardList.Count)));
			_cardList.RemoveRange(0,Mathf.Min(3, _cardList.Count));
			RearrangeSlot();
			_isFull = false;
			return tempList;
		}

		public void ClearTimerId()
		{
			foreach (var id in _timerids)
			{
				GameModule.Timer.RemoveTimer(id);
			}
		}
		#region 添加槽位相关
		public void AddSolt(Action callBack)
		{
			if (_maxSlotCount >= ConfigLoader.Constant.MaxSlotCount)
			{
				callBack?.Invoke();
				return;
			}
			_maxSlotCount++;
			//生成一个特效
			GameObject eff = GameModule.Resource.LoadGameObject("efc_stage_solt_loading_01", transform);
			eff.transform.position = transform.position;
			eff.GetComponent<ParticleSystem>().Play();
			AudioManager.Play(AudioType.Sound, AudioDef.UseLots);
			Sequence seq = DOTween.Sequence();
			seq.Append(mBg.DOScale(Vector3.one * 1.2f, 0.1f));
			seq.Append(mBg.DOScale(Vector3.one*0.8f, 0.1f));
			seq.Append(mBg.DOScale(Vector3.one, 0.1f).OnStart(() =>
			{
				/* Widget_SlotPos pos = CreateWidgetByType<Widget_SlotPos>(mAllSlotPos);
				pos.gameObject.name = "SlotPos_" + (_maxSlotCount-1);
				pos.Open(true);
				foreach (RectTransform solt in mAllSlotPos)
				{
					solt.SizeHeight(130f);
				} */
				callBack?.Invoke();
			}));
			
		}
		#endregion
		//得到收集框中的一个或者一对卡牌
		public List<CardCell> GetOneGroup()
		{
			Dictionary<int, List<CardCell> > tempGruop = new Dictionary<int, List<CardCell>>();
			for (int i = 0; i < _cardList.Count; i++)
			{
				var tCard = _cardList[i];
				if (!tempGruop.ContainsKey(tCard.CardType))
				{
					tempGruop[tCard.CardType] = new List<CardCell>();
				}

				tempGruop[tCard.CardType].AddWithoutRepetition(tCard);
				if(tempGruop[tCard.CardType].Count>=2)
				{
					return tempGruop[tCard.CardType];
				}
			}
			return tempGruop.Values.Count > 0 ? tempGruop.Values.First() : null;
		}

		public Dictionary<int, List<CardCell>> GetAllGroup()
		{
			Dictionary<int, List<CardCell> > tempGruop = new Dictionary<int, List<CardCell>>();
			for (int i = 0; i < _cardList.Count; i++)
			{
				var tCard = _cardList[i];
				if (!tempGruop.ContainsKey(tCard.CardType))
				{
					tempGruop[tCard.CardType] = new List<CardCell>();
				}
				tempGruop[tCard.CardType].AddWithoutRepetition(tCard);
			}
			return tempGruop;
		}
		//得到收集框的一个卡牌类型，需要排除钞票棋子
		public int GetOneCartType(bool checkCash =false)
		{
			if (!IsEliminateSlotHas(checkCash)) return 1;
			if (!checkCash)
			{
				return _cardList[0].CardType;
			}
			else
			{
				for (int i = 0; i < _cardList.Count; i++)
				{
					var card = _cardList[i];
					if (card.CardType != ConfigLoader.Constant.MoneyCard)
					{
						return card.CardType;
					}
				}

				return 1;
			}
		}
		//收集框是否有棋子，如果此时收集槽全是钞票棋子，也返回空槽
		public bool IsEliminateSlotHas(bool CheckCash =false)
		{
			if (!CheckCash)
			{
				return _cardList.Count > 0;
			}
			else
			{
				if (_cardList.Count == 0)
					return false;
				int cashCount = 0;
				for (int i = 0; i < _cardList.Count; i++)
				{
					int type = _cardList[i].CardType;
					if (type == ConfigLoader.Constant.MoneyCard)
					{
						cashCount++;
					}
				}

				return _cardList.Count > cashCount;
			}
		}
	}
}
