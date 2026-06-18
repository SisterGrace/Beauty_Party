using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Y_Base;
using UnityEditor;
using Config.common;
namespace GameLogic
{
	[Window(UILayer.UI, PlayAnim = true)]
	public partial class Panel_Discard : UIWindow
	{
		//记录加入时间（系统时间，后台不暂停）
		private System.DateTime _timeJoinUtc;
		//判定的时间差
		private const long TIME_DIFF = 3;
		//奖励
		private List<TBRewardData> _rewardDatas;
		private Item_PhotoLoad _itemPhotoLoad;
		protected override void OnRefresh()
		{
			_timeJoinUtc = default;
			var acCfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(ActivityType.Discard);
			if (acCfg != null)
			{
				_rewardDatas = ConfigLoader.Instance.Tables.TbCfgReward.Get(acCfg.RandomGroupID).Reward;
			}
			int abType = UserController.Instance.GetCurUserType;
			var photoId = abType == 1 ? _rewardDatas[0].OtherID : _rewardDatas[0].ID;
			_itemPhotoLoad.LoadUIImg(photoId);
			InitRedemptionInput();
			SDKManager.Instance.LogEvent("Discord_Show");
		}

		protected override void RegisterEvent()
		{
		}

		private bool _isWaitingReturn;
		private void JoinDiscord()
		{
			if (_isWaitingReturn) return;
			Application.OpenURL(ConfigLoader.Constant.DiscordUrl);
			SDKManager.Instance.LogEvent("Discord_Click");
			_timeJoinUtc = System.DateTime.UtcNow;
			Log.Info($"加入discard时间: {_timeJoinUtc}");
			_isWaitingReturn = true;
			Utility.Unity.AddOnApplicationFocusListener(OnAppFocusChanged);
		}
		private void OnAppFocusChanged(bool hasFocus)
		{
			Log.Info($"OnAppFocusChanged: {hasFocus}, _timeJoinUtc: {_timeJoinUtc}");
			if (!hasFocus || _timeJoinUtc == default) return;

			double timeDiff = (System.DateTime.UtcNow - _timeJoinUtc).TotalSeconds;
			Log.Info($"timeDiff: {timeDiff}");
			if (timeDiff < 2)
			{
				return;
			}
			ClearFocusListener();

			if (timeDiff >= TIME_DIFF)
			{
				UserController.Instance.SetDiscardOpen(true);
				//发送奖励
				PropsController.Instance.ShowRewardCollectPanels(_rewardDatas);
				Close();
			}
		}
		private void ClearFocusListener()
		{
			_timeJoinUtc = default;
			_isWaitingReturn = false;
			Utility.Unity.RemoveOnApplicationFocusListener(OnAppFocusChanged);
		}
		/// <summary>
		/// 使用兑换码
		/// </summary>
		private void UseRedemption()
		{
			//先检测网络情况
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				GameEvent.Send(EventType.ShowTips, "uitps_noInternet_tips", true);
				return;
			}
			//检测兑换码是否为空
			if (string.IsNullOrEmpty(mRedemptionCode.text))
			{
				GameEvent.Send(EventType.ShowTips, "uitps_setting_redemptionCodetips1", true);
				return;
			}
			//打开加载界面
			GameEvent.Send(EventType.SetWaitingUI, true);
			try
			{
				string countryCode = SDKManager.GetStoreCountryCode();
#if UNITY_EDITOR
				countryCode = "CN";
#endif
				var request = new RedeemCodeRequest(mRedemptionCode.text,
				UUIDTool.DeviceID,
				 Application.identifier,
				  countryCode);
				APIRequestManager.Instance.SendRequest<RedeemCodeRequest, RedeemCodeResponse>(
				 	request,
				   (response) =>
				   {
					   GameEvent.Send(EventType.SetWaitingUI, false);
					   //0: 成功，1：无效，2：已使用
					   Log.Info($"兑换结果: {response.result}");
					   SDKManager.Instance.LogEvent("Discord_Exchange", "Exchange_Type", response.result);
					   switch (response.result)
					   {
						   case 0:
							   Close();
							   //内购
							   if (response.type == 0)
							   {
								   int pId = int.Parse(response.content);
								   var productInfo = RechargeController.GetRechargeInfo(pId);
								   if (productInfo == null)
								   {
									   GameEvent.Send(EventType.ShowTips, "uitps_setting_redemptionCodetips7", true);
									   Log.Error($"兑换码兑换的商品信息为空: {pId}");
									   return;
								   }
								   Log.Info("开始获取商品内购ID");
								   string productId = RechargeController.Instance.GetId(productInfo);
								   Log.Info($"兑换码兑换的商品内购ID: {productId}");
								   RechargeController.Instance.SendReward(productId, true, false);
								   Log.Info("兑换码兑换的商品内购ID发送奖励成功");
								   //提示已发送兑换码奖励，请查收
								   GameEvent.Send(EventType.ShowTips, "uitps_setting_redemptionCodetips3", true);
							   }
							   else if (response.type == 1)
							   {
								   //图片
								   int photoId = int.Parse(response.content);
								   //添加到相册
								   UserController.Instance.AddUserPhotoData(photoId);
								   //弹出图片收集界面
								   GameModule.UI.ShowUI<Panel_Collectphotos>(photoId, 1);
							   }
							   break;
						   case 1:
							   //提示兑换码无效
							   GameEvent.Send(EventType.ShowTips, "uitps_setting_redemptionCodetips4", true);
							   break;
						   case 2:
							   //提示兑换码已使用
							   GameEvent.Send(EventType.ShowTips, "uitps_setting_redemptionCodetips5", true);
							   break;
					   }

				   }
					   ,
					(error) =>
					{
						Log.Error($"兑换失败结果: {error}");
						GameEvent.Send(EventType.SetWaitingUI, false);
						GameEvent.Send(EventType.ShowTips, "uitps_setting_redemptionCodetips2", true);
					}
);
			}
			catch (System.Exception)
			{
				GameEvent.Send(EventType.SetWaitingUI, false);

				throw;
			}
		}

		//设置输入限制
		private void InitRedemptionInput()
		{
			if (mRedemptionCode == null) return;
			mRedemptionCode.characterLimit = 9;
			mRedemptionCode.onValueChanged.RemoveAllListeners();
			mRedemptionCode.onValueChanged.AddListener(OnRedemptionInputChanged);
		}
		//过滤非英文数字
		private void OnRedemptionInputChanged(string input)
		{
			string filtered = FilterAlphanumeric(input);
			if (mRedemptionCode != null)
			{
				mRedemptionCode.text = filtered;
			}
		}
		//国立字符串，只保留英文和数字
		private string FilterAlphanumeric(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;
			return System.Text.RegularExpressions.Regex.Replace(input, "[^a-zA-Z0-9]", "");
		}
		protected override void Close()
		{
			ClearFocusListener();
			base.Close();
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameModule.UI.RemoveOrderPanel<Panel_Discard>(true);
		}
	}
}
