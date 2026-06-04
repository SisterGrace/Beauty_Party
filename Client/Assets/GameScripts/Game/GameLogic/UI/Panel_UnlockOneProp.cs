using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using DG.Tweening;
using GameBase;
using UnityEngine.UI;

namespace GameLogic
{
	[Window(UILayer.UI,PlayAnim = true)]
	public partial class Panel_UnlockOneProp : UIWindow
	{
		private PropType _propType;
		private CfgGamePropInfo _cfgGamePropInfo;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_propType = (PropType)userDatas[0];
			}

			if (_propType != PropType.None)
			{
				_cfgGamePropInfo = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetByType(_propType);
			}

			UpdateInfo();
		}

		private void UpdateInfo()
		{
			if (_cfgGamePropInfo != null)
			{
				mPropIcon.SetSprite("ui"+_cfgGamePropInfo.Icon,true);
				mPropTipsTxt.SetLanguage(_cfgGamePropInfo.PropTips);
			}
		}
		protected override void RegisterEvent()
		{
			
		}

		private void HidePanel()
		{
			mBg.DOScale(new Vector3(1.2f, 1.1f, 1), 0.15f).OnComplete(() =>
			{
				mBg.gameObject.Hide();
				mMask.GetComponent<Image>().color = new Color(1, 1, 1, 0);
			});
		}
		protected override void Close()
		{
			base.Close();
			GameModule.UI.RemoveOrderPanel<Panel_UnlockOneProp>();
		}
	}
}
