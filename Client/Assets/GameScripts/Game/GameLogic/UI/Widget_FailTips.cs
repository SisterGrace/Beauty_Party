using TK_Engine;
using UnityEngine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_FailTips : UIWidget
	{
		private SkeletonGraphic _heartAniml;
		private bool _infiniteLife;
		private int _timerId;
		public void Open(string iconName,string message,bool InfiniteLife =true,int GoldCount=0)
		{
			_heartAniml = mloseLife.GetComponent<SkeletonGraphic>();
			_infiniteLife = InfiniteLife;
			
			mGoldCount.gameObject.SetActive(GoldCount > 0);
			mGoldCount.text = $"x{GoldCount}";
			//当前无限生命
			if (InfiniteLife)
			{
				mIcon.SetSprite(iconName,true);
				mTipsTxt.SetLanguage(message);
				
				mloseLife.Hide();
			}
			//当前有限生命
			else
			{
				mIcon.gameObject.Hide();
				mloseLife.Show();
				mTipsTxt.SetLanguage("uitps_exitlevel_life");
			}
			
		}

		public void FailTipsShow()
		{
			if (mloseLife.activeSelf)
			{
				ShowHeartAnim();
			}
		}
		private void ShowHeartAnim()
		{
			if (!_infiniteLife)
			{
				_timerId = GameModule.Timer.AddTimer(tiemr =>
				{
					SpineController.PlayAnim(_heartAniml, 0,"Split",true);
				},0.25f);
			}
		}
		public void Hide()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (_timerId > 0)
			{
				GameModule.Timer.RemoveTimer(_timerId);
			}
		}
	}
}
