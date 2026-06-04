using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Home
	{

		protected GameObject mPhotoLoad;
		protected GameObject mAVProVideoUI;
		protected GameObject mShopRedNot;
		protected GameObject mPhotoRedNot;
		protected GameObject mAnimmask;
		protected MediaPlayer mvideoPLayer;
		protected RectTransform mTopUI;
		protected RectTransform mAllAct;
		protected RectTransform mtopLeft;
		protected RectTransform mtopRifgt;
		protected RectTransform mBottomUI;
		protected UIButton mGameDiamond;
		protected UIButton mSetting;
		protected UIButton mPhotots;
		protected UIButton mLevel;
		protected UIButton mtest;
		protected UIButton mtest2;
		protected UIButton mtest3;
		protected TextMeshProUGUI mGameGoldCount;
		protected TextMeshProUGUI mcanRemakeCount;
		protected TextMeshProUGUI mCurLevel;
		protected TextMeshProUGUI mtestTxt;

		protected override void ScriptGenerator()
		{
			mPhotoLoad = FindChild("bg/Obj_PhotoLoad").gameObject;
			mAVProVideoUI = FindChild("bg/Obj_AVProVideoUI").gameObject;
			mShopRedNot = FindChild("Rect_TopUI/Btn_GameDiamond/Obj_ShopRedNot").gameObject;
			mPhotoRedNot = FindChild("Rect_BottomUI/Btn_Photots/Obj_PhotoRedNot").gameObject;
			mAnimmask = FindChild("Obj_Animmask").gameObject;
			mvideoPLayer = FindChildComponent<MediaPlayer>("bg/AV_videoPLayer");
			mTopUI = FindChildComponent<RectTransform>("Rect_TopUI");
			mAllAct = FindChildComponent<RectTransform>("Rect_TopUI/Rect_AllAct");
			mtopLeft = FindChildComponent<RectTransform>("Rect_TopUI/Rect_AllAct/ActBtnNode/Rect_topLeft");
			mtopRifgt = FindChildComponent<RectTransform>("Rect_TopUI/Rect_AllAct/ActBtnNode/Rect_topRifgt");
			mBottomUI = FindChildComponent<RectTransform>("Rect_BottomUI");
			mGameDiamond = FindChildComponent<UIButton>("Rect_TopUI/Btn_GameDiamond");
			mSetting = FindChildComponent<UIButton>("Rect_TopUI/Btn_Setting");
			mPhotots = FindChildComponent<UIButton>("Rect_BottomUI/Btn_Photots");
			mLevel = FindChildComponent<UIButton>("Rect_BottomUI/Btn_Level");
			mtest = FindChildComponent<UIButton>("Btn_test");
			mtest2 = FindChildComponent<UIButton>("Btn_test2");
			mtest3 = FindChildComponent<UIButton>("Btn_test3");
			mGameGoldCount = FindChildComponent<TextMeshProUGUI>("Rect_TopUI/Btn_GameDiamond/TMP_GameGoldCount");
			mcanRemakeCount = FindChildComponent<TextMeshProUGUI>("Rect_BottomUI/Btn_Photots/Obj_PhotoRedNot/TMP_canRemakeCount");
			mCurLevel = FindChildComponent<TextMeshProUGUI>("Rect_BottomUI/Btn_Level/TMP_CurLevel");
			mtestTxt = FindChildComponent<TextMeshProUGUI>("TMP_testTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private Item_PhotoLoad _photoLoad;
		private void InitCodeBlock()
		{
			_photoLoad = mPhotoLoad.GetComponent<Item_PhotoLoad>();
			_topCanvasGroup = mTopUI.GetComponent<CanvasGroup>();
			_bottomCanvasGroup = mBottomUI.GetComponent<CanvasGroup>();
			mLevel.AddListener(ClickLevel);
			mSetting.AddListener(ClickSetting);
			mPhotots.AddListener(ClickPhotots);
			mGameDiamond.AddListener(ClickGameDiamond);
			
			mtest.AddListener(ClickTest);
			mtest2.AddListener(ClickTest2);
			mtest3.AddListener(ClickTest3);
			mvideoPLayer.Events.AddListener(OnMediaPlayerEvent);
		}

		private void ClickLevel(GameObject obj)
		{
			GuideController.Instance.NextGuide();
			OpenLevelInfo();
		}

		private void ClickSetting(GameObject obj)
		{
			GameModule.UI.ShowUI<Panel_Setting>();
		}
		private void ClickGameDiamond(GameObject obj)
		{
			//主界面钻石按钮入口：打开后自动滚动到底部钻石领取区
			GameModule.UI.ShowUI<Panel_Shop>(true);
		}
		private void ClickPhotots(GameObject obj)
		{
			GuideController.Instance.NextGuide();
			GameModule.UI.ShowUI<Panel_Photo>();
		}
		private void ClickTest(GameObject obj)
		{
			GameModule.UI.ShowUI<Panel_Gacha>();
			//GameModule.UI.ShowUI<Panel_GetReward>(GameScene.Instance.RewardId);

			//PropsController.FlyReward(ERewardType.GoldCube,1,2,itemFlyType.EachFly,transform.position,null,isWriteData:true);

			/*var _rewardDatas = ConfigLoader.Instance.Tables.TbCfgReward.Get(10001).Reward;
			PropsController.Instance.AddRewardData(_rewardDatas,OneRewardData.NoneData,false);
			PropsController.Instance.FlyReward(transform.position);*/

			//RechargeController.Instance.SendReward("com.fantasytile.starter_pack",true);

			/*var old = ConfigLoader.Instance.Tables.TbCfgScene.Get(UserController.Instance.SceneID);
			var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(old.FinishReId).Reward;
			PropsController.Instance.AddRewardData(rewardList, OneRewardData.NoneData,false);
			GameModule.UI.ShowUI<Panel_GetReward_1>();*/



			/*List<int> tempList = new List<int>();
			tempList.Add(20001);
			GameModule.UI.ShowUI<Panel_GetReward>(tempList,true);
			*/

			//GameEvent.Send(EventType.DreamRedPoint,true,"1");

			//UserController.Instance.AddAccruePassKey(11, false);

			//SDKManager.Instance.Button_ShowIV(HCIVPositionName.IV_Help,HCIVADType.IV2);
			//GameModule.UI.ShowUI<Panel_Fail>(false,0,0);

			//GameEvent.Send("MainHomeAni",false);
			//GameEvent.Send(EventType.MainHomeAnim,false);


		}

		private void ClickTest2(GameObject obj)
		{
			//UserController.Instance.ResetGoldRushData(12,65);
			//UserController.Instance.GetOneActivityData(ActivityType.GoldRush).MGoldRushData.CurrGoldRushLevel = 24;
			/*UserController.Instance.AddLevelChestKey(0,true);
			UserController.Instance.SetDreamPassKey(0, 1);
			UserController.Instance.AddAccruePassKey(0, true);
			UserController.Instance.GetOneActivityData(ActivityType.DreamPass).IsOpenDreamPass = false;*/

			/*foreach (var info in UserController.Instance.GetAllPropsInfo)
			{
				if (info.mType == PropType.AddSolt)
				{
					info.Num = 0;
				}
			}*/
			
		}

		private void ClickTest3(GameObject obj)
		{
			
		}
		#endregion
	}
}
