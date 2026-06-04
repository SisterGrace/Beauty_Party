using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	/// <summary>
	/// 卡槽位置组件，负责管理游戏中"广告加槽"按钮的显示/隐藏及埋点上报。
	/// View 部分（Widget_SlotPosView.cs）处理按钮绑定与广告回调逻辑。
	/// </summary>
	[Window(UILayer.UI)]
	public partial class Widget_SlotPos : UIWidget
	{
		// 临时占位字段（无实际业务用途）
		private int mGarbageCounter = 0;

		/// <summary>
		/// 打开卡槽组件。
		/// </summary>
		/// <param name="showAdd">是否显示"广告加槽"按钮，默认隐藏</param>
		public void Open(bool showAdd =false)
		{
			mADSlot.gameObject.SetActive(showAdd);

			// 当广告按钮可见时，上报曝光埋点
			if (showAdd)
			{
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_Slot.ToString());
			}
		}

		/// <summary>
		/// 隐藏/关闭卡槽组件。
		/// </summary>
		public void Hide()
		{
			
		}

		
	}
}
