using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
using Spine.Unity;
using Config.common;
namespace GameLogic
{
	public partial class Panel_Game
	{

		protected GameObject mPhotoLoad;
		protected GameObject mLoadingNode;
		protected GameObject mAVProVideoUI;
		protected GameObject mtimeNode;
		protected GameObject mclockAnim;
		protected GameObject mtime1Bar;
		protected GameObject mRevokeicon;
		protected GameObject mRevokeAdd;
		protected GameObject mRevokeLock;
		protected GameObject mFreeRevoke;
		protected GameObject mMessUpicon;
		protected GameObject mMessUpAdd;
		protected GameObject mMessUpLock;
		protected GameObject mFreeMessUp;
		protected GameObject mDestoryGruopicon;
		protected GameObject mDestoryGruopAdd;
		protected GameObject mDestoryGruopLock;
		protected GameObject mFreeDestory;
		protected GameObject mMoveOuticon;
		protected GameObject mMoveOutAdd;
		protected GameObject mMoveOutLock;
		protected GameObject mFreeMoveOut;
		protected GameObject mMask;
		protected GameObject mMask2;
		protected Transform mloadIcon;
		protected Transform mtimeBg1;
		protected Transform mStoreSlotNode;
		protected Transform mSlotNode;
		protected Transform mCardMoving;
		protected MediaPlayer mMediaPlayer;
		protected RectTransform mTopsUI;
		protected RectTransform mCardBoardNode;
		protected RectTransform mBottomsUI;
		protected TextMeshProUGUI mLevelName;
		protected TextMeshProUGUI mTime2CountTimeTxt;
		protected TextMeshProUGUI mTime1CountTimeTxt;
		protected TextMeshProUGUI mRevokeCount;
		protected TextMeshProUGUI mMessUpCount;
		protected TextMeshProUGUI mDestoryGruopCount;
		protected TextMeshProUGUI mMoveOutCount;
		protected UIButton mSetting;
		protected UIButton mBackHome;
		protected UIButton mRevoke;
		protected UIButton mMessUp;
		protected UIButton mDestoryGruop;
		protected UIButton mMoveOut;
		protected Slider mTime2Progress;
		protected Slider mTime1Progress;

		protected override void ScriptGenerator()
		{
			mPhotoLoad = FindChild("Bg/Obj_PhotoLoad").gameObject;
			mLoadingNode = FindChild("Bg/Obj_PhotoLoad/Obj_LoadingNode").gameObject;
			mAVProVideoUI = FindChild("Bg/Obj_AVProVideoUI").gameObject;
			mtimeNode = FindChild("Rect_TopsUI/Obj_timeNode").gameObject;
			mclockAnim = FindChild("Rect_TopsUI/Obj_timeNode/Bg2/time2Bar/Obj_clockAnim").gameObject;
			mtime1Bar = FindChild("Rect_TopsUI/Obj_timeNode/Obj_time1Bar").gameObject;
			mRevokeicon = FindChild("Rect_BottomsUI/AllPropBtns/Btn_Revoke/Obj_Revokeicon").gameObject;
			mRevokeAdd = FindChild("Rect_BottomsUI/AllPropBtns/Btn_Revoke/CountTxtImg/Obj_RevokeAdd").gameObject;
			mRevokeLock = FindChild("Rect_BottomsUI/AllPropBtns/Btn_Revoke/Obj_RevokeLock").gameObject;
			mFreeRevoke = FindChild("Rect_BottomsUI/AllPropBtns/Btn_Revoke/Obj_FreeRevoke").gameObject;
			mMessUpicon = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MessUp/Obj_MessUpicon").gameObject;
			mMessUpAdd = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MessUp/CountTxtImg/Obj_MessUpAdd").gameObject;
			mMessUpLock = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MessUp/Obj_MessUpLock").gameObject;
			mFreeMessUp = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MessUp/Obj_FreeMessUp").gameObject;
			mDestoryGruopicon = FindChild("Rect_BottomsUI/AllPropBtns/Btn_DestoryGruop/Obj_DestoryGruopicon").gameObject;
			mDestoryGruopAdd = FindChild("Rect_BottomsUI/AllPropBtns/Btn_DestoryGruop/CountTxtImg/Obj_DestoryGruopAdd").gameObject;
			mDestoryGruopLock = FindChild("Rect_BottomsUI/AllPropBtns/Btn_DestoryGruop/Obj_DestoryGruopLock").gameObject;
			mFreeDestory = FindChild("Rect_BottomsUI/AllPropBtns/Btn_DestoryGruop/Obj_FreeDestory").gameObject;
			mMoveOuticon = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MoveOut/Obj_MoveOuticon").gameObject;
			mMoveOutAdd = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MoveOut/CountTxtImg/Obj_MoveOutAdd").gameObject;
			mMoveOutLock = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MoveOut/Obj_MoveOutLock").gameObject;
			mFreeMoveOut = FindChild("Rect_BottomsUI/AllPropBtns/Btn_MoveOut/Obj_FreeMoveOut").gameObject;
			mMask = FindChild("Obj_Mask").gameObject;
			mMask2 = FindChild("Obj_Mask2").gameObject;
			mloadIcon = FindChild("Bg/Obj_PhotoLoad/Obj_LoadingNode/Trans_loadIcon");
			mtimeBg1 = FindChild("Rect_TopsUI/Obj_timeNode/Trans_timeBg1");
			mStoreSlotNode = FindChild("Rect_BottomsUI/Trans_StoreSlotNode");
			mSlotNode = FindChild("Rect_BottomsUI/Trans_SlotNode");
			mCardMoving = FindChild("Trans_CardMoving");
			mMediaPlayer = FindChildComponent<MediaPlayer>("Bg/AV_MediaPlayer");
			mTopsUI = FindChildComponent<RectTransform>("Rect_TopsUI");
			mCardBoardNode = FindChildComponent<RectTransform>("Rect_CardBoardNode");
			mBottomsUI = FindChildComponent<RectTransform>("Rect_BottomsUI");
			mLevelName = FindChildComponent<TextMeshProUGUI>("Rect_TopsUI/LevelBg/TMP_LevelName");
			mTime2CountTimeTxt = FindChildComponent<TextMeshProUGUI>("Rect_TopsUI/Obj_timeNode/Bg2/time2Bar/TMP_Time2CountTimeTxt");
			mTime1CountTimeTxt = FindChildComponent<TextMeshProUGUI>("Rect_TopsUI/Obj_timeNode/Obj_time1Bar/TMP_Time1CountTimeTxt");
			mRevokeCount = FindChildComponent<TextMeshProUGUI>("Rect_BottomsUI/AllPropBtns/Btn_Revoke/CountTxtImg/TMP_RevokeCount");
			mMessUpCount = FindChildComponent<TextMeshProUGUI>("Rect_BottomsUI/AllPropBtns/Btn_MessUp/CountTxtImg/TMP_MessUpCount");
			mDestoryGruopCount = FindChildComponent<TextMeshProUGUI>("Rect_BottomsUI/AllPropBtns/Btn_DestoryGruop/CountTxtImg/TMP_DestoryGruopCount");
			mMoveOutCount = FindChildComponent<TextMeshProUGUI>("Rect_BottomsUI/AllPropBtns/Btn_MoveOut/CountTxtImg/TMP_MoveOutCount");
			mSetting = FindChildComponent<UIButton>("Rect_TopsUI/Btn_Setting");
			mBackHome = FindChildComponent<UIButton>("Rect_TopsUI/Btn_BackHome");
			mRevoke = FindChildComponent<UIButton>("Rect_BottomsUI/AllPropBtns/Btn_Revoke");
			mMessUp = FindChildComponent<UIButton>("Rect_BottomsUI/AllPropBtns/Btn_MessUp");
			mDestoryGruop = FindChildComponent<UIButton>("Rect_BottomsUI/AllPropBtns/Btn_DestoryGruop");
			mMoveOut = FindChildComponent<UIButton>("Rect_BottomsUI/AllPropBtns/Btn_MoveOut");
			mTime2Progress = FindChildComponent<Slider>("Rect_TopsUI/Obj_timeNode/Bg2/time2Bar/Slider_Time2Progress");
			mTime1Progress = FindChildComponent<Slider>("Rect_TopsUI/Obj_timeNode/Obj_time1Bar/Slider_Time1Progress");
			InitCodeBlock();
		}

		#region 可编辑代码块

		private SkeletonGraphic _clockAnim;
		private Item_PhotoLoad _photoLoad;
		private void InitCodeBlock()
		{
			_photoLoad = mPhotoLoad.GetComponent<Item_PhotoLoad>();
			mSetting.AddListener(ClickSetting);
			mRevoke.AddListener(ClickRevoke);
			_propBtns.Add(PropType.Revoke, new PropBtnInfo(mRevokeCount, mRevokeicon, mRevokeAdd, mRevokeLock, mRevoke, mFreeRevoke));
			mMessUp.AddListener(ClickMessUp);
			_propBtns.Add(PropType.MessUp, new PropBtnInfo(mMessUpCount, mMessUpicon, mMessUpAdd, mMessUpLock, mMessUp, mFreeMessUp));
			mDestoryGruop.AddListener(ClickDestoryGruop);
			_propBtns.Add(PropType.DestoryGruop, new PropBtnInfo(mDestoryGruopCount, mDestoryGruopicon, mDestoryGruopAdd, mDestoryGruopLock, mDestoryGruop, mFreeDestory));
			mMoveOut.AddListener(ClickMoveOut);
			_propBtns.Add(PropType.MoveOut, new PropBtnInfo(mMoveOutCount, mMoveOuticon, mMoveOutAdd, mMoveOutLock, mMoveOut, mFreeMoveOut));
			_clockAnim = mclockAnim.GetComponent<SkeletonGraphic>();
			mMediaPlayer.Events.AddListener(OnMediaPlayerEvent);
			mBackHome.AddListener(ClickBackHome);
		}

		private void ClickSetting(GameObject obj)
		{
			GameModule.UI.ShowUI<Panel_Setting>();
		}
		private void ClickBackHome(GameObject obj)
		{
			ExitLevel();
		}

		private void ClickRevoke(GameObject obj)
		{
			RevokeOneStep();
		}

		private void ClickMessUp(GameObject obj)
		{
			MessUpCards();
		}

		private void ClickDestoryGruop(GameObject obj)
		{
			DestroyCards();
		}

		private void ClickMoveOut(GameObject obj)
		{
			MoveOutCards();
		}
		private void ClickTest(GameObject obj)
		{
			PropsController.FreezeTime(obj.transform.position, (Tr) =>
			{

			});
		}
		#endregion
	}
}
