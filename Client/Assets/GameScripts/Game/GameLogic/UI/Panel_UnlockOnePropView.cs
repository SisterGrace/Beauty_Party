using Config.common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_UnlockOneProp
	{
		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mPickUp;
		protected Transform mBg;
		protected Image mPropIcon;
		protected TextMeshProUGUI mPropCount;
		protected TextMeshProUGUI mPropTipsTxt;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("Trans_Bg/Btn_Close");
			mPickUp = FindChildComponent<UIButton>("Trans_Bg/Btn_PickUp");
			mBg = FindChild("Trans_Bg");
			mPropIcon = FindChildComponent<Image>("Trans_Bg/Frame/Img_PropIcon");
			mPropCount = FindChildComponent<TextMeshProUGUI>("Trans_Bg/Frame/TMP_PropCount");
			mPropTipsTxt = FindChildComponent<TextMeshProUGUI>("Trans_Bg/TMP_PropTipsTxt");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mPickUp.AddListener(ClickGet);
			mClose.AddListener(ClickClose);
			mMask.AddListener(ClickClose);
		}

		private void ClickGet(GameObject obj)
		{
			HidePanel();
			var endTr = FlySystem.Instance.GetPos(FlyReType.prop);
			PropsController.FlyReward(ERewardType.Prop, (int)_cfgGamePropInfo.Type, _cfgGamePropInfo.GetCount, itemFlyType.EachFly,
				mPropIcon.transform.position, endTr, callback:() =>
				{
					Close();
				},isWriteData:true);
		}

		private void ClickClose(GameObject obj)
		{
			HidePanel();
			var endTr = FlySystem.Instance.GetPos(FlyReType.prop);
			PropsController.FlyReward(ERewardType.Prop, (int)_cfgGamePropInfo.Type, _cfgGamePropInfo.GetCount, itemFlyType.EachFly,
				mPropIcon.transform.position, endTr, callback:() =>
				{
					Close();
				},isWriteData:true);
		}
		#endregion
	}
}
