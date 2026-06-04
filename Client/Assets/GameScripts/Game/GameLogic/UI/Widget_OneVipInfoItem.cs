using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using GameBase;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneVipInfoItem : UIWidget, IListDataItem<VIPInfoData>
	{
		private VIPInfoData _data;
		public void SetItemData(VIPInfoData data)
		{
			_data = data;
			UpdateView();
		}
		
		private void UpdateView()
		{
			mCurbg.sprite = GameModule.Resource.LoadAsset<Sprite>(_data.Bg);
			mIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(_data.Icon);
			mCurInfoTxt.SetLanguage(_data.InfoTxt);
		}

		public void Hide()
		{
		}
	}
	public struct VIPInfoData
	{
		public string Bg;
		public string Icon;
		public string InfoTxt;
		public VIPInfoData(string bg, string icon, string infoTxt)
		{
			Bg = bg;
			Icon = icon;
			InfoTxt = infoTxt;
		}
	}
}
