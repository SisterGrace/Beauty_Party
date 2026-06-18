using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_CardBoard
	{

		protected RectTransform mStackNode;
		protected RectTransform mLayerNode;

		protected override void ScriptGenerator()
		{
			mStackNode = FindChildComponent<RectTransform>("Rect_StackNode");
			mLayerNode = FindChildComponent<RectTransform>("Rect_LayerNode");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
