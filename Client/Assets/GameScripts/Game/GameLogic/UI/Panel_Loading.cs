using DG.Tweening;
using Sky;
using System.Diagnostics;
using TK_Engine;
using TMPro;
using UnityEngine;
using System.Collections;
namespace GameLogic
{
	[Window(UILayer.System)]
	public partial class Panel_Loading : UIWindow
	{
		private bool IsLoadSuccess;
		private float value;
		private Stopwatch _LoadSw;

        private bool _isWaitingForSDK = false;      // 是否正在等待 SDK 初始化
        private float _waitSDKTimer = 0f;            // 已等待的时间
        private float _loadingStartTime = 0f;        // 记录准备上报时的时间戳
        protected override void OnRefresh()
		{
			_LoadSw = new Stopwatch();
			_LoadSw.Start();
			Log.Info("loading开始");
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
            try
            {
                LoadingSystem.Instance.SetUpdateCb(UpdateProgress);
            }
            catch (System.Exception e)
            {
                Log.Error($"LoadingSystem init error: {e}");
            }
            // 替换协程调用为设置等待标志
            _loadingStartTime = Time.realtimeSinceStartup;   // 记录时间
            _isWaitingForSDK = true;
            _waitSDKTimer = 0f;
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
            // ===== 原有逻辑：检测是否关闭 Loading =====
            if (IsLoadSuccess && LoadingSystem.Instance.loadType == LoadType.None)
            {
                Close();
            }

            // ===== 新增逻辑：等待 SDK 就绪后上报 =====
            if (_isWaitingForSDK)
            {
                _waitSDKTimer += Time.deltaTime;

                // 条件1：SDK 已可用（通过你的 SkyAnalyticsMgr.TrackingEnable()）
                // 条件2：等待超过 3 秒（防止卡死，超时也强行上报）
                if (SkyAnalyticsMgr.TrackingEnable() || _waitSDKTimer >= 3f)
                {
                    // 上报 Loading_Start
                    SDKManager.Instance.LogEvent("Loading_Start", "Start_Time", _loadingStartTime);

                    // 如果超时但 SDK 依然不可用，打个警告方便排查
                    if (!SkyAnalyticsMgr.TrackingEnable())
                    {
                        Log.Warning($"SDK 初始化超时（已等 {_waitSDKTimer:F1}s），强行上报 Loading_Start");
                    }

                    // 结束等待，避免重复上报
                    _isWaitingForSDK = false;
                }
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
