using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneShrinkPic : UILoopItemWidget, IListDataItem<OneShrinkPicInfo>
	{
		private Item_PhotoLoad _curPicLoad;
		public void SetItemData(OneShrinkPicInfo data)
		{
			_curPicLoad.LoadPhoto(data.Id,0);
		}
	}
	public struct OneShrinkPicInfo
	{
		public int Id;
		public OneShrinkPicInfo(int id)
		{
			Id = id;
		}
	}
}
