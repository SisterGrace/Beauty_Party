using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Setting
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mContactUs;
		protected UIButton mSetLanguage;
		protected UIButton mEvaluateUs;
		protected UIButton mFavor;
		protected UIButton mPrivacy;
		protected UIButton mUseRedemption;
		protected UIButton mCopyUID;
		protected GameObject mS1;
		protected TMP_InputField mRedemptionTxt;
		protected TextMeshProUGUI mVersion;
		protected TextMeshProUGUI mUserId;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("Obj_S1/Btn_Close");
			mContactUs = FindChildComponent<UIButton>("Obj_S1/Feedback/Btn_ContactUs");
			mSetLanguage = FindChildComponent<UIButton>("Obj_S1/Btn_SetLanguage");
			mEvaluateUs = FindChildComponent<UIButton>("Obj_S1/Btn_EvaluateUs");
			mFavor = FindChildComponent<UIButton>("Obj_S1/Btn_Favor");
			mPrivacy = FindChildComponent<UIButton>("Obj_S1/Btn_Privacy");
			mUseRedemption = FindChildComponent<UIButton>("Obj_S1/RedemptionCode/Btn_UseRedemption");
			mCopyUID = FindChildComponent<UIButton>("Obj_S1/Btn_CopyUID");
			mS1 = FindChild("Obj_S1").gameObject;
			mRedemptionTxt = FindChildComponent<TMP_InputField>("Obj_S1/RedemptionCode/Input_RedemptionTxt");
			mVersion = FindChildComponent<TextMeshProUGUI>("Obj_S1/TMP_Version");
			mUserId = FindChildComponent<TextMeshProUGUI>("Obj_S1/Btn_CopyUID/TMP_UserId");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mSetLanguage.AddListener(ClickSetLanguage);
			mPrivacy.enabled = false;
            mPrivacy.AddListener(ClickPrivacy);
			mContactUs.enabled = false;
            mContactUs.AddListener(ClickContactUs);
			mMask.AddListener(ClickClose);
			mUseRedemption.AddListener(ClickUseRedemption);
			mCopyUID.AddListener(ClickCopyUID);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickSetLanguage(GameObject obj)
		{
			GameModule.UI.ShowUI<Panel_Language>();
		}

		private void ClickPrivacy(GameObject obj)
		{
			SDKManager.OpenPrivacy();
		}

		private void ClickContactUs(GameObject obj)
		{
			/* //邮箱预设信息
			string subInfo = "Feedback";
			string bodyInfo = $"{Application.productName}%0A" +
							  $"Version:%20{PlayGameExtensions.Encode(Application.version)}%0A" +
							  $"UserID:%20{PlayGameExtensions.Encode(SDKManager.GetUserId())}%0A" +
							  $"Device:%20{PlayGameExtensions.Encode(SystemInfo.deviceModel)}%0A" +
							  $"OS:%20{PlayGameExtensions.Encode(SystemInfo.operatingSystem)}%0A" +
							  $"SDK:%20{PlayGameExtensions.Encode(SDKManager.GetSDKVersion())}%0A";
			Application.OpenURL(string.Format("mailto:{0}?subject={1}&body={2}", ConfigLoader.Constant.Email, PlayGameExtensions.Encode(subInfo), bodyInfo)); */
		    Application.OpenURL(ConfigLoader.Constant.DiscordUrl);
		}
		private void ClickUseRedemption(GameObject obj)
		{
			UseRedemption();
		}
		private void ClickCopyUID(GameObject obj)
		{
			//将UID复制进用户的设备
			GUIUtility.systemCopyBuffer = _uid;
			GameEvent.Send(EventType.ShowTips, "uitps_setting_copyuid_success", true);
		}
		#endregion
	}
}
