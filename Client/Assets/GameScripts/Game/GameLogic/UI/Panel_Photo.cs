using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Photo  : UIWindow
	{
		private SwitchPageMgr _switchPage;
	protected override void OnRefresh()
	{
		var switchPageInfo = ConfigLoader.Constant.PhotoTab;
		_switchPage = new SwitchPageMgr(mTopsUI, mChildPageNode, this);
		foreach (var item in switchPageInfo)
		{
			int id = item.Id;
			string tabKey = item.PhotoItemKey;
			_switchPage.BindChildPage<Widget_AllPhotoItems>(id, tabKey);
			_switchPage.SetShareParam(id,tabKey);
		}

		_switchPage.CreatTab<Widget_SwitchPhoto>(0,false);
		//更新页签红点
		SetSwitchRedPoint();
		// 静音主页视频，播放背景音乐
		VideoAudioHelper.SwitchToPage("Panel_Photo");
		AudioManager.Play(TK_Engine.AudioType.Music, AudioDef.BGM1, false, true);
	}

		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<int>(EventType.SwitchRedPoint, SetSwitchRedPoint);
		}
		

		private void SetSwitchRedPoint(int id=-1)
		{
			if (id == -1)//更新全部页签红点
			{
				bool levelRed = UserController.Instance.IsPhototLook(0);
				_switchPage.SetTabRedNode(0, levelRed);
				
			}
			else
			{
				switch (id)
				{
					case 0:
						bool levelRed = UserController.Instance.IsPhototLook(0);
						_switchPage.SetTabRedNode(0, levelRed);
						break;
				}
			}
		}
	protected override void Close()
	{
            VideoAudioHelper.StopVideoAudioCheck("Panel_Photo", true);
            base.Close();
	}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<int>(EventType.SwitchRedPoint, SetSwitchRedPoint);
		}
		
	}
}
