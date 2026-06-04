using Config.common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_BuyOneProp
	{

		protected UIButton mMask;
		protected UIButton mClose;
		protected UIButton mAdToGet;
		protected UIButton mGoldToGet;
		protected UIButton mAdOpen;
		protected UIButton mCurGold;
		protected TextMeshProUGUI mPropName;
		protected TextMeshProUGUI mPropCount;
		protected TextMeshProUGUI mPropTipsTxt;
		protected TextMeshProUGUI mCost;
		protected TextMeshProUGUI mGoldCount;
		protected Image mPropIcon;
		protected Transform mGiftNode;
		protected RectTransform mnative;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mAdToGet = FindChildComponent<UIButton>("AnimNode/Btn_AdToGet");
			mGoldToGet = FindChildComponent<UIButton>("AnimNode/Btn_GoldToGet");
			mAdOpen = FindChildComponent<UIButton>("AnimNode/Trans_GiftNode/Btn_AdOpen");
			mCurGold = FindChildComponent<UIButton>("AnimNode/Btn_CurGold");
			mPropName = FindChildComponent<TextMeshProUGUI>("AnimNode/Bg/TMP_PropName");
			mPropCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Bg/Frame/TMP_PropCount");
			mPropTipsTxt = FindChildComponent<TextMeshProUGUI>("AnimNode/Bg/TMP_PropTipsTxt");
			mCost = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_GoldToGet/TMP_Cost");
			mGoldCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Btn_CurGold/TMP_GoldCount");
			mPropIcon = FindChildComponent<Image>("AnimNode/Bg/Frame/Img_PropIcon");
			mGiftNode = FindChild("AnimNode/Trans_GiftNode");
			mnative = FindChildComponent<RectTransform>("AnimNode/Rect_native");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mClose.AddListener(ClickClose);
			mAdToGet.AddListener(ClickAdToGet);
			mGoldToGet.AddListener(ClickGoldToGet);
			mMask.AddListener(ClickClose);
			mAdOpen.AddListener(ClickAdOpen);
		}

		private void ClickClose(GameObject obj)
		{
			Close();
		}

		private void ClickAdToGet(GameObject obj)
		{
			//SDKManager.Instance.Button_ShowNative(mnative);
			SDKManager.Instance.Button_ShowRV(curAdPos, (success) =>
			{
				if (success)
				{
					var endTr = FlySystem.Instance.GetPos((FlyReType)((int)_cfgGamePropInfo.Type + 4));
					PropsController.FlyReward(ERewardType.Prop, (int)_cfgGamePropInfo.Type,
						_cfgGamePropInfo.GetCount,itemFlyType.EachFly, mPropIcon.transform.position,endTr,isWriteData:true);
					Close();
					if ((int)_cfgGamePropInfo.Type < 5)
					{
						LevelController.Instance.GameRvCount++;
					}
				}
			});
			
		}

		private void ClickGoldToGet(GameObject obj)
		{
			if (UserController.Instance.CheckConsume(ERewardType.Currency, 0, _cfgGamePropInfo.Cost))
			{
				UserController.Instance.UserConsume(ERewardType.Currency, 0, _cfgGamePropInfo.Cost);
				var endTr = FlySystem.Instance.GetPos((FlyReType)((int)_cfgGamePropInfo.Type + 4));
				
				PropsController.FlyReward(ERewardType.Prop, (int)_cfgGamePropInfo.Type,
					_cfgGamePropInfo.GetCount,itemFlyType.EachFly, mPropIcon.transform.position,endTr, callback:() =>
					{
						GameEvent.Send(EventType.SetTipsMask,false);
					},isWriteData:true);
				Close();
			}
			else
			{
				//打开商店
				//GameModule.UI.ShowUI<Panel_Shop>(1);
				GameEvent.Send(EventType.ShowTips, "uitps_floattip_notcoins",true);
			}
		}

		private void ClickAdOpen(GameObject obj)
		{
			var cfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.Adfree);
			//GameModule.UI.ShowUI<Panel_NoAd>(cfg);
		}
		#endregion
	}
}
