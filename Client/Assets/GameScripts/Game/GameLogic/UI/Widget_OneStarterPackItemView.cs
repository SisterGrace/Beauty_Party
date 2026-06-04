using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneStarterPackItem
	{

		protected Image mrewardIcon;
		protected TextMeshProUGUI mrewardCount;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected Transform mloadIcon;

		protected override void ScriptGenerator()
		{
			mrewardIcon = FindChildComponent<Image>("Img_rewardIcon");
			mrewardCount = FindChildComponent<TextMeshProUGUI>("Img_rewardIcon/TMP_rewardCount");
			mPhotoNode = FindChild("Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mloadIcon = FindChild("Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_itemPhoto = mPhotoNode.GetComponent<Item_PhotoLoad>();
		}
		#endregion
	}
}
