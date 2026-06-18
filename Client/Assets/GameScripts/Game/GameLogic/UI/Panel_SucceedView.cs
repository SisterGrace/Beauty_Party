using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Succeed
	{

		protected Image mMask;
		protected GameObject mDiamondShow;
		protected GameObject mPhotoShow;
		protected GameObject mAdicn;
		protected GameObject mPhotoLoad;
		protected GameObject mLoadingNode;
		protected GameObject mAVProVideoUI;
		protected GameObject mLikeIcon;
		protected RectTransform mDiaTitle;
		protected RectTransform mDiaIcon;
		protected RectTransform mDiaBtns;
		protected RectTransform mPhotoBg;
		protected RectTransform mTitlebg;
		protected RectTransform mPhotBtns;
		protected RectTransform mFrame;
		protected UIButton mContinue;
		protected UIButton mAdGet;
		protected UIButton mAdDownload;
		protected UIButton mNextLevel;
		protected UIButton mBackHome;
		protected UIButton mLike;
		protected TextMeshProUGUI mCurGetDiaCount;
		protected TextMeshProUGUI mAdMultiple;
		protected TextMeshProUGUI mFreeDowCount;
		protected Transform mloadIcon;
		protected MediaPlayer mPhotoAVPro;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<Image>("Img_Mask");
			mDiamondShow = FindChild("AnimNode/Obj_DiamondShow").gameObject;
			mPhotoShow = FindChild("AnimNode/Obj_PhotoShow").gameObject;
			mAdicn = FindChild("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_PhotBtns/Btn_AdDownload/Obj_Adicn").gameObject;
			mPhotoLoad = FindChild("AnimNode/Obj_PhotoShow/Rect_Frame/mask/Obj_PhotoLoad").gameObject;
			mLoadingNode = FindChild("AnimNode/Obj_PhotoShow/Rect_Frame/mask/Obj_PhotoLoad/Obj_LoadingNode").gameObject;
			mAVProVideoUI = FindChild("AnimNode/Obj_PhotoShow/Rect_Frame/mask/Obj_AVProVideoUI").gameObject;
			mLikeIcon = FindChild("AnimNode/Obj_PhotoShow/Btn_Like/Obj_LikeIcon").gameObject;
			mDiaTitle = FindChildComponent<RectTransform>("AnimNode/Obj_DiamondShow/Rect_DiaTitle");
			mDiaIcon = FindChildComponent<RectTransform>("AnimNode/Obj_DiamondShow/Rect_DiaIcon");
			mDiaBtns = FindChildComponent<RectTransform>("AnimNode/Obj_DiamondShow/Rect_DiaBtns");
			mPhotoBg = FindChildComponent<RectTransform>("AnimNode/Obj_PhotoShow/Rect_PhotoBg");
			mTitlebg = FindChildComponent<RectTransform>("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_Titlebg");
			mPhotBtns = FindChildComponent<RectTransform>("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_PhotBtns");
			mFrame = FindChildComponent<RectTransform>("AnimNode/Obj_PhotoShow/Rect_Frame");
			mContinue = FindChildComponent<UIButton>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_Continue");
			mAdGet = FindChildComponent<UIButton>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_AdGet");
			mAdDownload = FindChildComponent<UIButton>("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_PhotBtns/Btn_AdDownload");
			mNextLevel = FindChildComponent<UIButton>("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_PhotBtns/Btn_NextLevel");
			mBackHome = FindChildComponent<UIButton>("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_PhotBtns/Btn_BackHome");
			mLike = FindChildComponent<UIButton>("AnimNode/Obj_PhotoShow/Btn_Like");
			mCurGetDiaCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_Continue/TMP_CurGetDiaCount");
			mAdMultiple = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_AdGet/TMP_AdMultiple");
			mFreeDowCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_PhotoShow/Rect_PhotoBg/Rect_PhotBtns/Btn_AdDownload/TMP_FreeDowCount");
			mloadIcon = FindChild("AnimNode/Obj_PhotoShow/Rect_Frame/mask/Obj_PhotoLoad/Obj_LoadingNode/Trans_loadIcon");
			mPhotoAVPro = FindChildComponent<MediaPlayer>("AnimNode/Obj_PhotoShow/AV_PhotoAVPro");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private CanvasGroup mPhotoBgCanvasGroup;
		private Item_PhotoLoad _photoLoad;
		private void InitCodeBlock()
		{
			_photoLoad = mPhotoLoad.GetComponent<Item_PhotoLoad>();
			mAdDownload.AddListener(ClickAdDownload);
			mAdGet.AddListener(ClickAdGet);
			mContinue.AddListener(ClickContinue);
			mNextLevel.AddListener(ClickNextLevel);
			mBackHome.AddListener(ClickBackHome);
			mLike.AddListener(ClickLike);
			mPhotoAVPro.Events.AddListener(OnMediaPlayerEvent);
			if (mPhotoBgCanvasGroup == null)
			{
				mPhotoBgCanvasGroup = mPhotoBg.gameObject.GetComponent<CanvasGroup>();
			}
		}

		private void ClickAdDownload(GameObject obj)
		{
			if (_isLoading)
			{
				GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				return;
			}
			//下载图片到相册，分AD或者VIP次数免费下载
			if (!_isFreeDownload)
			{
				SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_DownloadPicture, (succees) =>
				{
					if (succees)
					{
						if (_selectPhotoInfo.IsVideo)
						{
							SaveVideoAndPhotoToGallery();
						}
						else
						{
							SavePhotoToGallery();
						}

					}
				});
			}
			else
			{
				if (_selectPhotoInfo.IsVideo)
				{
					SaveVideoAndPhotoToGallery(true);
				}
				else
				{
					SavePhotoToGallery(true);
				}

			}

		}

		private void ClickAdGet(GameObject obj)
		{
			if (_isAnim) return; //动画中不响应点击

			SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_SuccMore, (bool success) =>
			{
				if (success)
				{
					//发送奖励
					LevelSendReward(false);
					//切换到照片界面
					SwitchToPhotoShowAnimation();
				}
			});
		}

		private void ClickContinue(GameObject obj)
		{
			if (_isAnim) return; //动画中不响应点击
								 //切换到照片界面
			SwitchToPhotoShowAnimation();
		}

		private void ClickNextLevel(GameObject obj)
		{
			if (_isAnim) return; //动画中不响应点击
			Close();
			GameModule.UI.CloseUI<Panel_Game>();
			GameModule.UI.ShowUI<Panel_Home>(false, null, true, false);
			//自动打开关卡照片选择界面
			GameModule.UI.AddOrderPanel<Panel_LevelPhoto>(99, false);
			GameEvent.Send(EventType.SetAnimMask, false);
		}

		private void ClickBackHome(GameObject obj)
		{
			if (_isAnim) return; //动画中不响应点击

			GameModule.UI.CloseUI<Panel_Game>();
			GameModule.UI.ShowUI<Panel_Home>(false, null, true, false);
			Close();
			//加载下一关的图
			PhotoController.Instance.PhotoPreloading(true, true);
			GameEvent.Send(EventType.SetAnimMask, false);
		}
		private void ClickLike(GameObject obj)
		{
			SetLikeIcon();
		}
		#endregion
	}
}
