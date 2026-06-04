using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneShrinkPic
	{

		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected Transform mloadIcon;

		protected override void ScriptGenerator()
		{
			mPhotoNode = FindChild("Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mloadIcon = FindChild("Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_curPicLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
		}
		#endregion
	}
}
