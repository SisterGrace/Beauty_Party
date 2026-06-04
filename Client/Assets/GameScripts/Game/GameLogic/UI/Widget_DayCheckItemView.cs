using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_DayCheckItem
	{

		protected Image mBg;
		protected Image mRewIcon;
		protected TextMeshProUGUI mItemDay;
		protected TextMeshProUGUI mnorRewCount;
		protected TextMeshProUGUI mRedCount;
		protected GameObject mnorRew;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected GameObject mmask;
		protected GameObject mRedNot;
		protected Transform mloadIcon;

		protected override void ScriptGenerator()
		{
			mBg = FindChildComponent<Image>("Img_Bg");
			mRewIcon = FindChildComponent<Image>("Obj_norRew/Img_RewIcon");
			mItemDay = FindChildComponent<TextMeshProUGUI>("Img_Bg/TMP_ItemDay");
			mnorRewCount = FindChildComponent<TextMeshProUGUI>("Obj_norRew/TMP_norRewCount");
			mRedCount = FindChildComponent<TextMeshProUGUI>("Obj_RedNot/TMP_RedCount");
			mnorRew = FindChild("Obj_norRew").gameObject;
			mPhotoNode = FindChild("Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mmask = FindChild("Obj_mask").gameObject;
			mRedNot = FindChild("Obj_RedNot").gameObject;
			mloadIcon = FindChild("Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_itemPhotoLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
		}
		#endregion
	}
}
