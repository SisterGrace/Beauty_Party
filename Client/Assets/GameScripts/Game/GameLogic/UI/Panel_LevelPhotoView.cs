using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_LevelPhoto
	{

		protected UIButton mmask;
		protected UIButton mClose;
		protected UIButton mStartGame;
		protected Image mTitleBM;
		protected TextMeshProUGUI mTitleTxt;
		protected TextMeshProUGUI mStartInfo;
		protected GameObject mSelect;
		protected GameObject mCommon;
		protected GameObject mAD;
		protected GameObject mVIP;
		protected RectTransform mPhotoView;
		protected Transform mContent;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mClose = FindChildComponent<UIButton>("AnimNode/Bg/Btn_Close");
			mStartGame = FindChildComponent<UIButton>("AnimNode/Obj_Select/Btn_StartGame");
			mTitleBM = FindChildComponent<Image>("AnimNode/Bg/Img_TitleBM");
			mTitleTxt = FindChildComponent<TextMeshProUGUI>("AnimNode/Bg/Img_TitleBM/TMP_TitleTxt");
			mStartInfo = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_Select/Btn_StartGame/TMP_StartInfo");
			mSelect = FindChild("AnimNode/Obj_Select").gameObject;
			mCommon = FindChild("AnimNode/Obj_Select/Btn_StartGame/Obj_Common").gameObject;
			mAD = FindChild("AnimNode/Obj_Select/Btn_StartGame/Obj_AD").gameObject;
			mVIP = FindChild("AnimNode/Obj_Select/Btn_StartGame/Obj_VIP").gameObject;
			mPhotoView = FindChildComponent<RectTransform>("AnimNode/Obj_Select/Rect_PhotoView");
			mContent = FindChild("AnimNode/Obj_Select/Rect_PhotoView/Viewport/Trans_Content");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mStartGame.AddListener(ClickStartGame);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickStartGame(GameObject obj)
		{
			EnterGame();
		}
		#endregion
	}
}
