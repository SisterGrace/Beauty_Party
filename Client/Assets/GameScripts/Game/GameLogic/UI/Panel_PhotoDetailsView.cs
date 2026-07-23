using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
using Config.common;
namespace GameLogic
{
	public partial class Panel_PhotoDetails
	{

		protected UIButton mMask;
		protected UIButton mSetBack;
		protected UIButton mLikeNode;
		protected UIButton mClose;
		protected UIButton mAdToGet;
		protected UIButton mGoldToGet;
		protected UIButton mCurGold;
		protected GameObject mImgNode;
		protected GameObject mVideoNode;
		protected GameObject mWatermark;
		protected GameObject mSetBtns;
		protected GameObject mlikeIcon;
		protected GameObject mVideoLoading;
		protected GameObject mAdicn;
		protected Image mPhotoIcon;
		protected AspectRatioFitter aspectRatioFitter;
        protected MediaPlayer mPhotoAVPro;
		protected Transform mloadingIcon;
		protected TextMeshProUGUI mFreeDowCount;
		protected TextMeshProUGUI mCost;
		protected TextMeshProUGUI mFreeCount;
		protected TextMeshProUGUI mGoldCount;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mSetBack = FindChildComponent<UIButton>("AnimNode/PicNode/Obj_SetBtns/Btn_SetBack");
			mLikeNode = FindChildComponent<UIButton>("AnimNode/PicNode/Obj_SetBtns/Btn_LikeNode");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mAdToGet = FindChildComponent<UIButton>("AnimNode/Btn_AdToGet");
			mGoldToGet = FindChildComponent<UIButton>("AnimNode/Btn_GoldToGet");
			mCurGold = FindChildComponent<UIButton>("AnimNode/Btn_CurGold");
			mImgNode = FindChild("AnimNode/PicNode/mask/Obj_ImgNode").gameObject;
			mVideoNode = FindChild("AnimNode/PicNode/mask/Obj_VideoNode").gameObject;
			mWatermark = FindChild("AnimNode/PicNode/Obj_Watermark").gameObject;
			mSetBtns = FindChild("AnimNode/PicNode/Obj_SetBtns").gameObject;
			mlikeIcon = FindChild("AnimNode/PicNode/Obj_SetBtns/Btn_LikeNode/Obj_likeIcon").gameObject;
			mVideoLoading = FindChild("AnimNode/PicNode/Obj_VideoLoading").gameObject;
			mAdicn = FindChild("AnimNode/Btn_AdToGet/Obj_Adicn").gameObject;
			mPhotoIcon = FindChildComponent<Image>("AnimNode/PicNode/mask/Obj_ImgNode/Img_PhotoIcon");
			aspectRatioFitter = mPhotoIcon.GetComponent<AspectRatioFitter>();
            mPhotoAVPro = FindChildComponent<MediaPlayer>("AnimNode/PicNode/AV_PhotoAVPro");
			mloadingIcon = FindChild("AnimNode/PicNode/Obj_VideoLoading/Trans_loadingIcon");
			mFreeDowCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_AdToGet/TMP_FreeDowCount");
			mCost = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_GoldToGet/Btntxts/TMP_Cost");
			mFreeCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_GoldToGet/Btntxts/TMP_FreeCount");
			mGoldCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_CurGold/TMP_GoldCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mLikeNode.AddListener(Clicklike);
			mSetBack.AddListener(ClickSetBack);
			mAdToGet.AddListener(ClickAdToGet);
			mGoldToGet.AddListener(ClickGoldToGet);
			mPhotoAVPro.Events.AddListener(OnMediaPlayerEvent);
		}

		
		private void ClickClose(GameObject obj)
		{
			Close();
		}
		private void Clicklike(GameObject obj)
		{
			SetPhotoLike();
		}
		private void ClickSetBack(GameObject obj)
		{
			SetVideoToMain();
		}

		private void ClickAdToGet(GameObject obj)
		{
			AdDownloadPic();
		}

		private void ClickGoldToGet(GameObject obj)
		{
			if (!_isFreeDownload)
			{
				//需要检测金币是否足够
				if(UserController.Instance.CheckConsume(ERewardType.Currency,0,300))
				{
					if (_PhotoItemInfo.ABType == 1)
					{
						SaveVideoToGallery(true);
					}
					else
					{
						SavePhotoToGallery(true);
					}
					
				}else
				{
					//弹出提示框
					GameModule.UI.ShowUI<Panel_Shop>(true);
					Close();
				}
			}
			else
			{
				if (_PhotoItemInfo.ABType == 1)
				{
					SaveVideoToGallery(true);
				}
				else
				{
					SavePhotoToGallery(true);
				}
				
			}
			
			
		}
		#endregion
	}
}
