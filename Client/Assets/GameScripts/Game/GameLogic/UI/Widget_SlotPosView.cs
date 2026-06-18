using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_SlotPos
	{

		protected UIButton mADSlot;

		protected override void ScriptGenerator()
		{
			mADSlot = FindChildComponent<UIButton>("Btn_ADSlot");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			// 第二个参数为按下时的回调，用于阻止棋子同时响应
			mADSlot.AddListener(ClickADSlot, OnADSlotPointerDown);
		}
		
		private void OnADSlotPointerDown(GameObject obj)
		{
			if (CardsController.Instance.CurPlayCardType == playCardType.clikcard) return;
			// 按下时立刻设置使用道具状态
			CardsController.Instance.CurPlayCardType = playCardType.useprop;
		}

		private void ClickADSlot(GameObject obj)
		{
			if (CardsController.Instance.CurPlayCardType == playCardType.clikcard) return;
			// 执行加槽逻辑
			SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_Slot, (success) =>
			{
				if (success)
				{
					// 执行加槽逻辑
					CardsController.Instance.AddSolt();
					mADSlot.gameObject.Hide();

				}
				CardsController.Instance.CurPlayCardType = playCardType.None;
			},
			() =>
			{
				GameEvent.Send(EventType.SetGameMask, false);
				CardsController.Instance.CurPlayCardType = playCardType.None;
			});
			
		}
		#endregion
	}
}
