using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.config;
using GameBase;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_JianTouGuide : UIWidget
	{
		public void Open(Transform target,CfgGuide cfg)
		{
			transform.position = target.position;
			if (cfg.Id == 4)
			{
				mAllJinatous.Show();
				mJiantouAnim.gameObject.Hide();
			}
		}

		public void Hide()
		{
		}
	}
}
