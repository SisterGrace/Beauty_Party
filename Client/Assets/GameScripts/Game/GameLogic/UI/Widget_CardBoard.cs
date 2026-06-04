using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_CardBoard : UIWidget
	{
		public void Open(Widget_EliminateSlot slot,Widget_StoreSlot storeSlot,Transform cardMoving)
		{
			//节点偏移
			Vector2 newPos = new Vector2(0, LevelController.GetCurrentLevelInfo().CardBoardOffestY);
			rectTransform.anchoredPosition = newPos;
			//初始化卡牌控制器，加载关卡生成器
			CardsController.Instance.Init(slot, storeSlot,cardMoving);
			LevelController.Instance.LoadMap(mLayerNode, mStackNode);
		}

		public void Hide()
		{
		}
	}
}
