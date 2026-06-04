using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.config;
using GameBase;
namespace GameLogic
{
	[Window(UILayer.UI,playAnim:true)]
	public partial class Panel_GachaTip : UIWindow
	{
		private int _packId;
		private CfgGacha _cfgGacha;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_packId = (int)userDatas[0];
			}
			_cfgGacha = ConfigLoader.Instance.Tables.TbCfgGacha.Get(_packId);
			UpdateInfo();
		}
		private void UpdateInfo()
		{
			mTitle.SetLanguage(_cfgGacha.TipsName);
			mCurPackTips.SetLanguage(_cfgGacha.Probability);
			mPackIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(_cfgGacha.TipsIcon);
		}
		protected override void RegisterEvent()
		{
		}

		protected override void Close()
		{
			base.Close();
		}
	}
}
