using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OnePhotoItem
	{

		protected Image mPhotoIcon;
		protected UIButton mLike;
		protected GameObject mLikeNode;
		protected GameObject mLoadingNode;
		protected GameObject mRedNot;
		protected Transform mloadIcon;
		protected TextMeshProUGUI mRedCount;

		protected override void ScriptGenerator()
		{
			mPhotoIcon = FindChildComponent<Image>("SelectBtn/Mask/Img_PhotoIcon");
			mLike = FindChildComponent<UIButton>("SelectBtn/Btn_Like");
			mLikeNode = FindChild("SelectBtn/Btn_Like/Obj_LikeNode").gameObject;
			mLoadingNode = FindChild("SelectBtn/Obj_LoadingNode").gameObject;
			mRedNot = FindChild("SelectBtn/Obj_RedNot").gameObject;
			mloadIcon = FindChild("SelectBtn/Obj_LoadingNode/Trans_loadIcon");
			mRedCount = FindChildComponent<TextMeshProUGUI>("SelectBtn/Obj_RedNot/TMP_RedCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mLike.AddListener(ClickLiked);
		}

		private void ClickLiked(GameObject obj)
		{
			SetPhotoLike();
		}
		#endregion
	}
}
