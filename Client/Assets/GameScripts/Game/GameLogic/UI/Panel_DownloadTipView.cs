using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_DownloadTip
	{

		protected UIButton mmask;
		protected UIButton mAdDownload;
		protected UIButton mClose;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected GameObject mAdicn;
		protected Transform mloadIcon;
		protected TextMeshProUGUI mFreeDowCount;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mAdDownload = FindChildComponent<UIButton>("AnimNode/Btn_AdDownload");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mPhotoNode = FindChild("AnimNode/Bg/Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("AnimNode/Bg/Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mAdicn = FindChild("AnimNode/Btn_AdDownload/Obj_Adicn").gameObject;
			mloadIcon = FindChild("AnimNode/Bg/Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mFreeDowCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_AdDownload/TMP_FreeDowCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_photoLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
			mClose.AddListener(ClickClose);
			mAdDownload.AddListener(ClickDownload);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickDownload(GameObject obj)
		{
			Download();
		}
		#endregion
	}
}
