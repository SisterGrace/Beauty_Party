using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_OneSelectPhoto
	{

		protected GameObject mFrame;
		protected GameObject mBoosLock;
		protected GameObject mBM;
		protected GameObject mAdNode;
		protected GameObject mVipNode;
		protected GameObject mPitchOn;
		protected GameObject mloading;
		protected GameObject mAdRefresh;
		protected GameObject mVfx;
		protected Image mLevelIcon;
		protected Transform mloadIcon;
		protected UIButton mRefresh;

		protected override void ScriptGenerator()
		{
			mFrame = FindChild("SelectBtn/Obj_Frame").gameObject;
			mBoosLock = FindChild("SelectBtn/Obj_Frame/Obj_BoosLock").gameObject;
			mBM = FindChild("SelectBtn/Obj_BM").gameObject;
			mAdNode = FindChild("SelectBtn/Obj_BM/Obj_AdNode").gameObject;
			mVipNode = FindChild("SelectBtn/Obj_BM/Obj_VipNode").gameObject;
			mPitchOn = FindChild("SelectBtn/Obj_PitchOn").gameObject;
			mloading = FindChild("SelectBtn/Obj_loading").gameObject;
			mAdRefresh = FindChild("SelectBtn/Btn_Refresh/Obj_AdRefresh").gameObject;
			mVfx = FindChild("SelectBtn/Obj_Vfx").gameObject;
			mLevelIcon = FindChildComponent<Image>("SelectBtn/Obj_Frame/Img_LevelIcon");
			mloadIcon = FindChild("SelectBtn/Obj_loading/Trans_loadIcon");
			mRefresh = FindChildComponent<UIButton>("SelectBtn/Btn_Refresh");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mRefresh.AddListener(ClickRefresh);
		}
		private void ClickRefresh(GameObject obj)
		{
			GameEvent.Send(EventType.LevelPhotoRefresh,_infoData.Index);
		}
		#endregion
	}
}
