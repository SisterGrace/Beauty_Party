using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_AllPhotoItems
	{

		protected GameObject mallINode;
		protected GameObject mOnePhotoItem;

		protected override void ScriptGenerator()
		{
			mallINode = FindChild("Obj_allINode").gameObject;
			mOnePhotoItem = FindChild("Obj_allINode/Viewport/Content/Item_OnePhotoItem").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
		}
		#endregion
	}
}
