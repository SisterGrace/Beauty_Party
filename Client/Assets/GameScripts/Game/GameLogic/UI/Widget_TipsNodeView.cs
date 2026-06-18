using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_TipsNode
	{

		protected Transform mnode1;
		protected Transform mnode2;
		protected TextMeshProUGUI mTipsTxt;
		protected TextMeshProUGUI mTipsTxt2;
		protected TextMeshProUGUI mnode3Tips;
		protected TextMeshProUGUI mnode3Title;
		protected RectTransform mtipsBg2;
		protected RectTransform mnode3;
		protected UIButton mClose;

		protected override void ScriptGenerator()
		{
			mnode1 = FindChild("Trans_node1");
			mnode2 = FindChild("Trans_node2");
			mTipsTxt = FindChildComponent<TextMeshProUGUI>("Trans_node1/TMP_TipsTxt");
			mTipsTxt2 = FindChildComponent<TextMeshProUGUI>("Trans_node2/Rect_tipsBg2/TMP_TipsTxt2");
			mnode3Tips = FindChildComponent<TextMeshProUGUI>("Rect_node3/Bg/frame/TMP_node3Tips");
			mnode3Title = FindChildComponent<TextMeshProUGUI>("Rect_node3/Bg/TMP_node3Title");
			mtipsBg2 = FindChildComponent<RectTransform>("Trans_node2/Rect_tipsBg2");
			mnode3 = FindChildComponent<RectTransform>("Rect_node3");
			mClose = FindChildComponent<UIButton>("Rect_node3/Btn_Close");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
