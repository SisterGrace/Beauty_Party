using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_Guide
	{

		protected GameObject mbg;
		protected GameObject mUIGuideNode;
		protected GameObject mUImask;
		protected GameObject mBattleGuideNode;
		protected GameObject mFormask;
		protected Image mLookAt;
		protected Transform mBtnNode;
		protected Transform mForNode;
		protected RectTransform mUITips;
		protected RectTransform mBattleTips;
		protected TextMeshProUGUI mUITipsTxt;
		protected TextMeshProUGUI mBattleTipsTxt;
		protected UIButton mContinue;

		protected override void ScriptGenerator()
		{
			mbg = FindChild("Obj_bg").gameObject;
			mUIGuideNode = FindChild("Obj_UIGuideNode").gameObject;
			mUImask = FindChild("Obj_UIGuideNode/Obj_UImask").gameObject;
			mBattleGuideNode = FindChild("Obj_BattleGuideNode").gameObject;
			mFormask = FindChild("Obj_BattleGuideNode/Obj_Formask").gameObject;
			mLookAt = FindChildComponent<Image>("Obj_UIGuideNode/Obj_UImask/Img_LookAt");
			mBtnNode = FindChild("Obj_UIGuideNode/Trans_BtnNode");
			mForNode = FindChild("Obj_BattleGuideNode/Trans_ForNode");
			mUITips = FindChildComponent<RectTransform>("Obj_UIGuideNode/Rect_UITips");
			mBattleTips = FindChildComponent<RectTransform>("Obj_BattleGuideNode/Rect_BattleTips");
			mUITipsTxt = FindChildComponent<TextMeshProUGUI>("Obj_UIGuideNode/Rect_UITips/TMP_UITipsTxt");
			mBattleTipsTxt = FindChildComponent<TextMeshProUGUI>("Obj_BattleGuideNode/Rect_BattleTips/TMP_BattleTipsTxt");
			mContinue = FindChildComponent<UIButton>("Btn_Continue");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mContinue.AddListener(ClickContinue);
		}

		private void ClickContinue(GameObject obj)
		{
			if (GuideController.Instance.IsTriggerGuide)
			{
				HideGuide();
				GuideController.Instance.NextTriggerGuide();
			}

			if (GuideController.Instance.IsGuideId)
			{
				GuideController.Instance.NextGuide();
			}
			
		}
		#endregion
	}
}
