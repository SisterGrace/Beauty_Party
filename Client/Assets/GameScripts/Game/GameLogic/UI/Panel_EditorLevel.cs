using TK_Engine;
using UnityEngine;
using Config.common;
using System.Collections;
using System.Collections.Generic;
using Y_Base;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_EditorLevel : UIWindow
	{
		private EventTriggerListener _eventTriggerListener;
		private MapConfig _mapConfigData;
		private bool _isCustom = false;
		#region 设置卡牌
		private bool _isSet = false;
		//1-代表设置锁，2-代表设置功能
		private int _curSet=0;
		#endregion
		
		
		protected override void OnRefresh()
		{
			LevelEdirtorController.Instance.MyStackParent = mEditBoard.transform;
			if (userDatas.Length > 0)
			{
				_mapConfigData = userDatas[0] as MapConfig;
				LevelEdirtorController.Instance.CreatGrid(true, _mapConfigData, mAllUnits);
			}
			_eventTriggerListener = EventTriggerListener.Get(mEditBoard);
			_eventTriggerListener.MyOnClick += OnUnitClick;
			mGetLockType.text = "0";
			mGetFunctionType.text = "";
			
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int,int>(EventType.UpdateLayerUI, UpdateLayerDataUI);
			GameEvent.AddEventListener<int, int>(EventType.UpdateCardUI, UpdateCardUI);
			GameEvent.AddEventListener<int>(EventType.UpdateCardTypeCount, UpdateCardTypeCount);
		}
		protected override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				mIsAdd.isOn =!mIsAdd.isOn;
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Close();
				#if UNITY_EDITOR
				AssetDatabase.Refresh();
				#endif
				GameModule.UI.ShowUI<Panel_EditorHome>();
			}
		}
		#region 编辑器交互事件

		private void OnUnitClick(GameObject obj, PointerEventData eventData)
		{
			LevelEdirtorController.Instance.OnClickedEditUnit(eventData.position,mIsAdd.isOn);
		}
		

		#endregion

		#region 关卡编辑器UI

		private void UpdateLayerDataUI(int allLayerCount, int currentLayerIndex)
		{
			mAllLayerCount.text = $"总层数：{allLayerCount}";
			mCurrLayer.text = $"第{currentLayerIndex + 1}层";
		}

		private void UpdateCardUI(int cardCount, int currentLayerCount)
		{
			mAllCardCount.text = $"总卡牌：{cardCount}";
			mCurrLayerCount.text = $"当前层卡牌：{currentLayerCount}";
		}

		private void UpdateCardTypeCount(int typeCount)
		{
			mTypeTxt.text = $"{typeCount}";
		}
		private void OnGetLockType(string value)
		{
			int temp = 0;
			int.TryParse(value, out temp);
			temp = Mathf.Clamp(temp, 0, 9);
			mGetLockType.text = temp.ToString();
			LevelEdirtorController.Instance.CurrentLockCardType = (LockCardType)temp;
		}
		private void OnGetFunctionType(string value)
		{
			LevelEdirtorController.Instance.CurrentFunctionCardTypes = null;
			LevelEdirtorController.Instance.ShellOrReversibleCount = 0;
			LevelEdirtorController.Instance.BombCount = 0;
			if (string.IsNullOrEmpty(value)) return;
			List<FunctionCardType> tempList = new List<FunctionCardType>();
			string[] temp = value.Split(',');
			foreach (string s in temp)
			{
				int temp2 = 0;
				int.TryParse(s, out temp2);
				temp2 = Mathf.Clamp(temp2, 0, 5);
				tempList.Add((FunctionCardType)temp2);
			}
			//贝壳需要设置数量
			if (tempList.Contains(FunctionCardType.Shell))
			{
				mGetShellCount.gameObject.Show();
				mGetShellCount.text = "0";
				LevelEdirtorController.Instance.ShellOrReversibleCount = 0;
			} else if (tempList.Contains(FunctionCardType.Reversible))
			{
				mGetShellCount.gameObject.Hide();
				LevelEdirtorController.Instance.ShellOrReversibleCount = 2;
			}
			else if (tempList.Contains(FunctionCardType.Bombshell))
			{
				mGetBombCount.gameObject.Show();
				mGetBombCount.text = "0";
				LevelEdirtorController.Instance.ShellOrReversibleCount = 0;
				LevelEdirtorController.Instance.BombCount = 0;
			}
			else
			{
				mGetShellCount.gameObject.Hide();
			}

			LevelEdirtorController.Instance.CurrentFunctionCardTypes=tempList;
		}

		private void OnGetShellCount(string value)
		{
			if (int.TryParse(value, out int num))
			{
				mGetShellCount.text = num.ToString();
				LevelEdirtorController.Instance.ShellOrReversibleCount = num;
			}
		}

		private void OnGetBombCount(string value)
		{
			if (int.TryParse(value, out int num))
			{
				int count = Mathf.Abs(num);
				mGetBombCount.text = count.ToString();
				LevelEdirtorController.Instance.BombCount = count;
			}
		}

		private void SetCustomType()
		{
			_isCustom = !_isCustom;
			mCustomNode.SetActive(_isCustom);
			mCustomType.text = "";
			LevelEdirtorController.Instance.CustomTypes = null;
		}

		private void OnGetCustomTyps(string value)
		{
			LevelEdirtorController.Instance.CustomTypes = null;
			if(string.IsNullOrEmpty(value)) return;
			List<int> templist = new();
			string[] temp = value.Split(',');
			foreach (var t in temp)
			{
				int temp2 = 0;
				int.TryParse(t, out temp2);
				templist.Add(temp2);
			}

			LevelEdirtorController.Instance.CustomTypes = templist;
		}
		#endregion

		
		protected override void Close()
		{
			base.Close();
			GameEvent.RemoveEventListener<int,int>(EventType.UpdateLayerUI, UpdateLayerDataUI);
			GameEvent.RemoveEventListener<int, int>(EventType.UpdateCardUI, UpdateCardUI);
			GameEvent.RemoveEventListener<int>(EventType.UpdateCardTypeCount, UpdateCardTypeCount);
			LevelEdirtorController.Instance.CreatGrid(false);
		}
	}
}
