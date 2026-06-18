using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_PhotoPackItem
	{

		protected UIButton mOpenPics;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected GameObject mPackThumbnail;
		protected GameObject mAllPackPics;
		protected GameObject mOneShrinkPic;
		protected Transform mloadIcon;
		protected Transform mCurContent;
		protected TextMeshProUGUI mTitle;

		protected override void ScriptGenerator()
		{
			mOpenPics = FindChildComponent<UIButton>("Btn_OpenPics");
			mPhotoNode = FindChild("Btn_OpenPics/Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("Btn_OpenPics/Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mPackThumbnail = FindChild("Obj_PackThumbnail").gameObject;
			mAllPackPics = FindChild("Obj_PackThumbnail/Obj_AllPackPics").gameObject;
			mOneShrinkPic = FindChild("Obj_PackThumbnail/Obj_AllPackPics/Viewport/Trans_CurContent/Item_OneShrinkPic").gameObject;
			mloadIcon = FindChild("Btn_OpenPics/Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mCurContent = FindChild("Obj_PackThumbnail/Obj_AllPackPics/Viewport/Trans_CurContent");
			mTitle = FindChildComponent<TextMeshProUGUI>("Btn_OpenPics/TMP_Title");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_itemPhotoLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
			// 已移除点击卡包弹出缩略图循环列表的功能
			// 现在改为轮播切换时自动弹出
		}
		#endregion
	}
}
