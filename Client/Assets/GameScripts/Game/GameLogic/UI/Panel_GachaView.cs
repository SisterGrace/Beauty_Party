using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Gacha
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mPackInfo;
		protected UIButton mGameDiamond;
		protected UIButton mSwitch;
		protected UIButton mAdDraw;
		protected UIButton mDiamondDraw;
		protected UIButton mOpenVip;
		protected UIButton mcommonPack;
		protected UIButton msuperPack;
		protected TextMeshProUGUI mAllDiamondCount;
		protected TextMeshProUGUI mCurPackName;
		protected TextMeshProUGUI mAdCost;
		protected TextMeshProUGUI mCurCost;
		protected ScrollRect mAllPacks;
		protected Transform mContent;
		protected Transform manimNode;
		protected GameObject mcommonMask;
		protected GameObject msupperMask;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("Bg/TopUI/Btn_Close");
			mPackInfo = FindChildComponent<UIButton>("Bg/TopUI/Btn_PackInfo");
			mGameDiamond = FindChildComponent<UIButton>("Bg/TopUI/Btn_GameDiamond");
			mSwitch = FindChildComponent<UIButton>("Bg/Btn_Switch");
			mAdDraw = FindChildComponent<UIButton>("Bg/AllDrawBtns/Btn_AdDraw");
			mDiamondDraw = FindChildComponent<UIButton>("Bg/AllDrawBtns/Btn_DiamondDraw");
			mOpenVip = FindChildComponent<UIButton>("Bg/AllDrawBtns/Btn_OpenVip");
			mcommonPack = FindChildComponent<UIButton>("Bg/Btn_commonPack");
			msuperPack = FindChildComponent<UIButton>("Bg/Btn_superPack");
			mAllDiamondCount = FindChildComponent<TextMeshProUGUI>("Bg/TopUI/Btn_GameDiamond/TMP_AllDiamondCount");
			mCurPackName = FindChildComponent<TextMeshProUGUI>("Bg/TMP_CurPackName");
			mAdCost = FindChildComponent<TextMeshProUGUI>("Bg/AllDrawBtns/Btn_AdDraw/TMP_AdCost");
			mCurCost = FindChildComponent<TextMeshProUGUI>("Bg/AllDrawBtns/Btn_DiamondDraw/TMP_CurCost");
			mAllPacks = FindChildComponent<ScrollRect>("Bg/Scroll_AllPacks");
			mContent = FindChild("Bg/Scroll_AllPacks/Viewport/Trans_Content");
			manimNode = FindChild("Bg/Trans_animNode");
			mcommonMask = FindChild("Bg/Btn_commonPack/Obj_commonMask").gameObject;
			msupperMask = FindChild("Bg/Btn_superPack/Obj_supperMask").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(OnClickClose);
			mPackInfo.AddListener(OnClickPackInfo);
			mSwitch.AddListener(OnClickSwitch);
			mAdDraw.AddListener(OnClickAdDraw);
			mDiamondDraw.AddListener(OnClickDiamondDraw);
			mOpenVip.AddListener(OnClickOpenVip);
			mcommonPack.AddListener(OnClickCommonPack);
			msuperPack.AddListener(OnClickSuperPack);
			mGameDiamond.AddListener(OnClickGameDiamond);
		}

		public void OnClickClose(GameObject obj)
		{
			Close();
		}
		private void OnClickPackInfo(GameObject obj)
		{
			// TODO: 显示卡包详情/保底进度信息
			GameModule.UI.ShowUI<Panel_GachaTip>(_curPackId);
		}
		private void OnClickSwitch(GameObject obj)
		{
			ShowPhotoPack();
		}
		private void OnClickOpenVip(GameObject obj)
		{
			// 打开VIP购买界面
			GameModule.UI.ShowUI<Panel_VIP>(2); // 假设1是VIP商店类型
		}
		private void OnClickCommonPack(GameObject obj)
		{
			SwitchToPack(0);
		}
		private void OnClickSuperPack(GameObject obj)
		{
			SwitchToPack(1);
		}
		private void OnClickGameDiamond(GameObject obj)
		{
			GameModule.UI.ShowUI<Panel_Shop>(true);
		}
		#endregion
	}
}
