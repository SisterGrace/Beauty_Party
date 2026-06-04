using TK_Engine;
using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Diagnostics;
namespace GameLogic
{
	[Window(UILayer.System)]
	public partial class Panel_Loading : UIWindow
	{
		private bool IsLoadSuccess;
		private float value;
		private Stopwatch _LoadSw;
		protected override void OnRefresh()
		{
			_LoadSw = new Stopwatch();
			_LoadSw.Start();
			value = 0.1f;
			IsLoadSuccess = false;
			
			mLoadProgressBar.DOFillAmount(value, 0.2f).OnComplete(() =>
			{
				GameModule.UI.ShowUI<Panel_FloatTips>();
				//检查设备是否能够访问互联网
				if (Application.internetReachability == NetworkReachability.NotReachable)
				{
					GameEvent.Send(EventType.ShowTips,"uitps_noInternet_tips",true);
				}
			}).OnUpdate(() =>
			{
				int fill = (int)(mLoadProgressBar.fillAmount * 100);
				mLoadingTxt.text = fill + "%";
			});
			//mLoadTips.gameObject.SetActive(!false);
			//更新进度条
			LoadingSystem.Instance.SetUpdateCb(UpdateProgress);
			SDKManager.Instance.LogEvent("Loading_Start", "Start_Time", Time.realtimeSinceStartup);
		}
		protected override void RegisterEvent()
		{
			GameEvent.AddEventListener<bool>(EventType.SetLoadNetTips,SetLoadNetTips);
		}
		protected override void Close()
		{
			base.Close();
			_LoadSw.Stop();
			Log.Info($"加载总时长：{_LoadSw.ElapsedMilliseconds}ms");
			SDKManager.Instance.LogEvent("Loading_End","Time",$"{_LoadSw.ElapsedMilliseconds*0.001f}");
		}
		
		private void UpdateProgress(LoadType type)
		{
			value += 0.15f;
			mLoadProgressBar.DOKill(true);
			if (type == LoadType.LoadScene)
			{
				mLoadProgressBar.DOFillAmount(value, 0.1f).OnStepComplete(() =>
				{
					IsLoadSuccess = true;
				}).OnUpdate(() =>
				{
					int fill = (int)(mLoadProgressBar.fillAmount * 100);
					mLoadingTxt.text = fill + "%";
				});
			}
			else
			{
				mLoadProgressBar.DOFillAmount(value, 0.1f).OnUpdate(() =>
				{
					int fill = (int)(mLoadProgressBar.fillAmount * 100);
					mLoadingTxt.text = fill + "%";
				});
			}
		}
		protected override void OnUpdate()
		{
			//base.OnUpdate();
			if (IsLoadSuccess && LoadingSystem.Instance.loadType == LoadType.None)
			{
				Close();
			}
		}

		private void SetLoadNetTips(bool isShow)
		{
			mNetTips.SetActive(isShow);
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			//判断是否买量用户
			var isPromote = SDKManager.Instance.IsPromote;
			GameEvent.RemoveEventListener<bool>(EventType.SetLoadNetTips,SetLoadNetTips);
		}
	}
}
