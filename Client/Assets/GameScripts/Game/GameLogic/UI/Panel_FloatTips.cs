using System;
using TK_Engine;
using UnityEngine;
using Y_Base;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;

namespace GameLogic
{
	[Window(UILayer.Tips)]
	public partial class Panel_FloatTips : UIWindow
	{
		private int _maxTipsCount = 10;
		private List<Widget_FloatTips> _list;
		private List<Widget_TipsNode> _bubbleTipsList;
		private Widget_TimeLevelTips _timeLevelTips;
		private Widget_PhotoThum _photoThum;
		private bool _isShowbubbleTips = false;
		private Widget_Waiting _waiting;
		protected override void OnRefresh()
		{
			_list = new ();
			_bubbleTipsList = new();
			SecureScreen.EnableSecure();
		}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<string, bool>(EventType.ShowTips, ShowTips);
			GameEvent.AddEventListener<string, bool, string[]>(EventType.ShowTips2, ShowTips);
			GameEvent.AddEventListener<string, Transform, int, string[]>(EventType.ShowTips3, ShowPropTips);
			GameEvent.AddEventListener<string, Transform, int>(EventType.ShowTips4, ShowPropTips);
			GameEvent.AddEventListener(EventType.HideBubbleTips, HideBubbleTips);
			GameEvent.AddEventListener<bool>(EventType.SetTipsMask, SetTipsMask);
			GameEvent.AddEventListener<CfgLevelInfo, Action>(EventType.StartTimeTips, CreatTimeLevelTips);
			GameEvent.AddEventListener<bool>(EventType.SetWaitingUI, SetWaiting);
			GameEvent.AddEventListener<bool, string>(EventType.SetWaitingUIAndTips, SetWaiting);
			GameEvent.AddEventListener<List<int>, Transform>(EventType.ShowPhotoThum, ShowPhotoThum);
		}

		protected override void OnUpdate()
		{
			if (_isShowbubbleTips && Input.GetMouseButton(0))
			{
				HideBubbleTips();
			}
		}
		protected override void Close()
		{
			base.Close();
		}

		#region 全局消息Tips

		private float timer = -0.5f;
		private void ShowTips(string tips, bool isKey = false)
		{
			if (GameTime.time - timer < 0.5f)
			{
				return;
			}
			timer = GameTime.time;
			var item = GetTipsItem();
			item.Open(tips,isKey);
		}

		private void ShowTips(string tips, bool isKey = false, params string[] userDatas)
		{
			if (GameTime.time - timer < 0.5f)
			{
				return;
			}
			timer = GameTime.time;
			var item = GetTipsItem();
			item.Open(tips,isKey,userDatas);
		}
		private Widget_FloatTips GetTipsItem()
		{
			foreach (var tips in _list)
			{
				if (tips.isCanUse)
				{
					return tips;
				}
			}
			if (_list.Count >= _maxTipsCount) return null;
			var floatTips = CreateWidgetByType<Widget_FloatTips>(mNode);
			_list.Add(floatTips);
			return floatTips;
		}
		#endregion

		#region 道具提示
		private void ShowPropTips(string tips, Transform tr,int openNode)
		{
			if (Time.time - timer < 0.5f)
			{
				return;
			}
			timer = Time.time;
			var item = GetBubbleTipsItem();
			item.OnOpen(tips, tr,openNode);
			_isShowbubbleTips = true;
		}
		private void ShowPropTips(string tips, Transform tr,int openNode,params string[] userDatas)
		{
			if (Time.time - timer < 0.5f)
			{
				return;
			}
			timer = Time.time;
			var item = GetBubbleTipsItem();
			item.OnOpen(tips, tr,openNode,userDatas);
			_isShowbubbleTips = true;
		}

		private Widget_TipsNode GetBubbleTipsItem()
		{
			foreach (var item in _bubbleTipsList)
			{
				if (item.isCanUse)
				{
					return item;
				}
			}
			if(_bubbleTipsList.Count >= _maxTipsCount) return null;
			var tipsNode = CreateWidgetByType<Widget_TipsNode>(mNode1);
			_bubbleTipsList.Add(tipsNode);
			return tipsNode;
		}

		private void HideBubbleTips()
		{
			foreach (var item in _bubbleTipsList)
			{
				item.Hide();
			}
			_isShowbubbleTips = false;
		}
		#endregion

		public void CreatTimeLevelTips(CfgLevelInfo info,Action callback)
		{
			if (_timeLevelTips == null)
			{
				_timeLevelTips = CreateWidgetByType<Widget_TimeLevelTips>(transform);
			}
			_timeLevelTips.Open(info, callback);
		}

		private void SetTipsMask(bool isShow)
		{
			mMask.SetActive(isShow);
		}
		#region 等待提示
		private void SetWaiting(bool isShow)
		{
			if (isShow)
			{
				if (_waiting == null)
				{
					_waiting = CreateWidgetByType<Widget_Waiting>(transform);
				}
				_waiting.Open();
			}
			else
			{
				if (_waiting != null)
				{
					_waiting.Hide();
				}
			}
		}

		private void SetWaiting(bool isShow, string tips)
		{
			if (isShow)
			{
				if (_waiting == null)
				{
					_waiting = CreateWidgetByType<Widget_Waiting>(transform);
				}
				_waiting.Open(tips);
			}
			else
			{
				if (_waiting != null)
				{
					_waiting.Hide();
				}
			}
		}
		#endregion
		#region 照片缩略图提示
		private void ShowPhotoThum(List<int> photoIds,Transform tr)
		{
			if (_photoThum == null)
			{
				_photoThum = CreateWidgetByType<Widget_PhotoThum>(transform);
			}
			_photoThum.Open(photoIds, tr);
		}
		#endregion
		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<string, bool>(EventType.ShowTips, ShowTips);
			GameEvent.RemoveEventListener<string, bool, string[]>(EventType.ShowTips2, ShowTips);
			GameEvent.RemoveEventListener<string, Transform, int, string[]>(EventType.ShowTips3, ShowPropTips);
			GameEvent.RemoveEventListener<string, Transform, int>(EventType.ShowTips4, ShowPropTips);
			GameEvent.RemoveEventListener(EventType.HideBubbleTips, HideBubbleTips);
			GameEvent.RemoveEventListener<bool>(EventType.SetTipsMask, SetTipsMask);
			GameEvent.RemoveEventListener<CfgLevelInfo, Action>(EventType.StartTimeTips, CreatTimeLevelTips);
			GameEvent.RemoveEventListener<bool>(EventType.SetWaitingUI, SetWaiting);
			GameEvent.RemoveEventListener<bool, string>(EventType.SetWaitingUIAndTips, SetWaiting);
			GameEvent.RemoveEventListener<List<int>, Transform>(EventType.ShowPhotoThum, ShowPhotoThum);
			SecureScreen.DisableSecure();

		}
	}
}
