using System;
using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI,PlayAnim = true)]
	public partial class Panel_ExitLevel : UIWindow
	{
		//是否收集到金块
		private bool _isHasNugget = false;
		//是否收集到网赚现金
		private bool _isHasCash = false;
		private List<Widget_FailTips> _failTipsList;
		private int _exitTimes = 0;
		protected override void OnRefresh()
		{
			//_isHasNugget = CardsController.Instance.GetNuggetCardCount > 0;
			_exitTimes = (_isHasNugget ? 1 : 0)+(_isHasCash ? 1 : 0);
			_failTipsList = new List<Widget_FailTips>();
			var failInfo = ConfigLoader.Constant.AllFailTipsInfos1;
			for (int i = 1; i < failInfo.Count; i++)
			{
				bool shouldCreate = i==failInfo.Count-1;
				if (shouldCreate)
				{
					Widget_FailTips tip = CreateWidgetByType<Widget_FailTips>(mTipsContent);
					_failTipsList.Add(tip);
					tip.Open(failInfo[i].IconName,failInfo[i].TipTxt);
					/* if (!UserController.Instance.GetLifeData.IsInfinite&&!_isHasNugget&&!_isHasCash)
					{
						tip.FailTipsShow();
					} */
				}
				
			}
			
		}

		protected override void RegisterEvent()
		{
		}
		#region 滑动布局的相关计算
		private float CalculateNormalizedPosition(RectTransform target)
		{
			float contentWidth = mTipsContent.rect.width;
			float viewWidth = mViewport.rect.width;
			float scroolableWidth = contentWidth - viewWidth;

			float elementleft = target.anchoredPosition.x;
			//计算偏移量
			float scrollAmount = elementleft-(viewWidth/2f);
			float normalizedPosition = (scrollAmount / scroolableWidth);
			return Mathf.Clamp01(normalizedPosition);
		}
		#endregion
		protected override void Close()
		{
			base.Close();
		}
	}
}
