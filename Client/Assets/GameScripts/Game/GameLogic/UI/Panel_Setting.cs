using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
using SRDebugger;
using GameBase;
using System.Text;

namespace GameLogic
{
	[Window(UILayer.UI, PlayAnim = true)]
	public partial class Panel_Setting : UIWindow
	{
		private bool _homeSetting = false;
		protected CanvasGroup _canvasGroup;
		private string _uid = "";
		protected override void OnRefresh()
		{
			//mVersion.text = "Version: " + Application.version;
			mVersion.text = $"Version: {Application.version}\n" +
							$"{GameModule.Resource.GetPackageVersion()}";
			_uid = SDKManager.GetUserId();
			mUserId.text = $"UID: {_uid}";
			InitRedemptionInput();
		}

		protected override void RegisterEvent()
		{
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
			if (string.IsNullOrEmpty(mRedemptionTxt.text))
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
				var request = new RedeemCodeRequest(mRedemptionTxt.text,
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
								   RechargeController.Instance.SendReward(productId, true,false);
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
			if (mRedemptionTxt == null) return;
			mRedemptionTxt.characterLimit = 9;
			mRedemptionTxt.onValueChanged.RemoveAllListeners();
			mRedemptionTxt.onValueChanged.AddListener(OnRedemptionInputChanged);
		}
		//过滤非英文数字
		private void OnRedemptionInputChanged(string input)
		{
			string filtered = FilterAlphanumeric(input);
			if (mRedemptionTxt != null)
			{
				mRedemptionTxt.text = filtered;
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
			base.Close();

		}
	}
}
