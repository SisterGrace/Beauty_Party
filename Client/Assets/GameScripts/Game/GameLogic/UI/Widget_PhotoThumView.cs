using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_PhotoThum
	{

		protected UIButton mmask;
		protected Transform mnode1;
		protected Transform mCurContent;
		protected GameObject mAllPackPics;
		protected GameObject mOneShrinkPic;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mnode1 = FindChild("Trans_node1");
			mCurContent = FindChild("Trans_node1/Obj_AllPackPics/Viewport/Trans_CurContent");
			mAllPackPics = FindChild("Trans_node1/Obj_AllPackPics").gameObject;
			mOneShrinkPic = FindChild("Trans_node1/Obj_AllPackPics/Viewport/Trans_CurContent/Item_OneShrinkPic").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mmask.AddListener(ClickMask);
		}
		private void ClickMask(GameObject obj)
		{
			Hide();
		}
		#endregion
	}
}
