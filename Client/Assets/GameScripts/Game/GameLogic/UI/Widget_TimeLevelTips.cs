using TK_Engine;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Config.config;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_TimeLevelTips : UIWidget
	{
		private Action _callback;
		public void Open(CfgLevelInfo info,Action callback)
		{
			gameObject.Show();
			_callback = null;
			_callback = callback;
			mCountTime.text = TimeUtil.SecondToTime2((long)info.CountdownTime);
		}

		public void Hide()
		{
			gameObject.Hide();
			_callback = null;
		}
	}
}
