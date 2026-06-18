using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
using Coffee.UIExtensions;
namespace GameLogic
{
	public partial class Panel_Collectphotos
	{

		protected UIButton mMask;
		protected UIButton mGet;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode ;
		protected GameObject mchouka01;
		protected GameObject mchouka02;
		protected Transform mloadIcon;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mGet = FindChildComponent<UIButton>("AnimNode/Btn_Get");
			mPhotoNode = FindChild("AnimNode/Bg/Obj_PhotoNode").gameObject;
			mLoadingNode  = FindChild("AnimNode/Bg/Obj_PhotoNode/Obj_LoadingNode ").gameObject;
			mchouka01 = FindChild("AnimNode/Obj_chouka01").gameObject;
			mchouka02 = FindChild("AnimNode/Obj_chouka02").gameObject;
			mloadIcon = FindChild("AnimNode/Bg/Obj_PhotoNode/Obj_LoadingNode /Trans_loadIcon");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_normalPhotoEffect = mchouka01.GetComponent<UIParticle>();
			_pityPhotoEffect = mchouka02.GetComponent<UIParticle>();
			_photoLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
			mGet.AddListener(ClickGet);
		}

		private void ClickGet(GameObject obj)
		{
			Close();
		}
		#endregion
	}
}
