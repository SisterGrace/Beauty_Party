using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Sky.SkyLoginEnum;

namespace Sky
{
    public class SkyAccountMgr: SkySingleton<SkyAccountMgr>
    {
        private const int GUEST_USER_TYPE = 0;
        private const int DEFAULT_MAX_RETRY_COUNT = 3;
        private const float RETRY_DELAY = 1.0f;
        private int _currentRetryCount = 0;
        private int _maxRetryCount = DEFAULT_MAX_RETRY_COUNT;
        private Action<bool, string, string> _currentLoginCallback;
        private float _lastLoginTime;

        public string GetCurrentUserId()
        {
            return SkyPrefs.UserId;
        }

        public async void Login(Action<bool, string, string> onComplete = null)
        {
#if IAP_ENABLE
			// ⭐ 无服务器模式：直接返回成功，用设备 ID 作为 UserId
			if (!SkyIapH5Mgr.HasServerBackend)
#else
            if (true)//无服务器模式
#endif
            {
				string localUserId = SkyPrefs.UserId;
				if (string.IsNullOrEmpty(localUserId))
				{
					localUserId = SystemInfo.deviceUniqueIdentifier;
					SkyPrefs.UserId = localUserId;
				}
				SkyLogUtils.Debug($"[无服务器模式] 登录跳过，使用本地 ID: {localUserId}");
				onComplete?.Invoke(true, "无服务器模式（本地登录）", localUserId);
				return;
			}
			_currentRetryCount = 0;

            _maxRetryCount = SkyAnalyticsMgr.GetConfigInt("login_max_retry_count",DEFAULT_MAX_RETRY_COUNT);

            _currentLoginCallback = onComplete;

            await AttemptLoginAsync();
        }

        private async Task  AttemptLoginAsync()
        {
            try
            {
                _lastLoginTime = Time.time;
                string deviceId = await SkyPrefs.GetOrCreateDeviceId();
                SkyAdjustPrefs.DeviceId = deviceId;
#if SSThinkingData
                SkyThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "device_num", deviceId } });
#endif
                var loginRequest = CreateLoginRequest(deviceId);
                SkyAnalyticsMgr.TrackEvent("User_Start_Login");
                SkyLogUtils.Debug("deviceid:"+SkyAdjustPrefs.DeviceId);
                SkyServerMgr.Instance.Login(loginRequest, OnLoginResponse);
            }
            catch(Exception ex)
            {
                SkyLogUtils.Error($"AttemptLoginAsync failed: {ex.Message}");
            }
        }

        private void OnLoginResponse(int code, string msg, ServerBackLoginInfo data)
        {
            bool isSuccess = IsLoginSuccess(code);
            
            if (isSuccess)
            {

                OnLoginSuccess(data);
                
                string resultMessage = "Login successful";

                SkyLogUtils.Debug($"Login succeeded after {_currentRetryCount} retries");
                
                _currentLoginCallback?.Invoke(true, resultMessage, data.user_id);

                _currentLoginCallback = null;

                SkyAnalyticsMgr.TrackEvent("User_Login_Success",new Dictionary<string, object>{
                    {"user_id", data.user_id},
                    {"device_id",SkyAdjustPrefs.DeviceId},
                    {"retry_count",_currentRetryCount},
                    {"Time", Mathf.CeilToInt(Time.time - _lastLoginTime)}
                });

                _currentRetryCount = 0;
            }
            else
            {
                OnLoginFailed(code, msg);
                
                _currentRetryCount++;
                
                if (_currentRetryCount <= _maxRetryCount)
                {
                    SkyLogUtils.Warning($"Login attempt {_currentRetryCount} failed (Code: {code}), retrying in {RETRY_DELAY} seconds...");
                    
                    Invoke(nameof(RetryLoginAsync), RETRY_DELAY);
                }
                else
                {
                    SkyLogUtils.Error($"Login failed after {_maxRetryCount} retries. Giving up.");
                    
                    SkyAnalyticsMgr.TrackEvent("User_Login_Fail",new Dictionary<string, object>{
                        {"user_id", ""},
                        {"device_id",SkyAdjustPrefs.DeviceId},
                        {"retry_count",_currentRetryCount},
                        {"Time", Mathf.CeilToInt(Time.time - _lastLoginTime)}
                    });

                    string resultMessage = $"Login failed: {msg} (Code: {code})";
                    _currentRetryCount = 0;
                    
                    _currentLoginCallback?.Invoke(false, resultMessage, string.Empty);
                    _currentLoginCallback = null; 
                }
            }
        }

        private async void RetryLoginAsync()
        {
            await AttemptLoginAsync();
        }

        public void Logout()
        {
            SkyPrefs.UserId = string.Empty;
            SkyAnalyticsMgr.Logout();
            _currentRetryCount = 0;
            _currentLoginCallback = null;
            CancelInvoke(nameof(RetryLoginAsync));
        }

        private void OnLoginSuccess(ServerBackLoginInfo data)
        {
            SkyLogUtils.Debug("Login successful");
            SkyPrefs.UserId = data.user_id;
            SkyAnalyticsMgr.SetLoginAccount(data.user_id);
            SkyAnalyticsMgr.SetSuperProperties(new Dictionary<string, object>{{"user_id", data.user_id}});
        }
        
        private void OnLoginFailed(int code, string msg)
        {
            SkyLogUtils.Error($"Login failed - Code: {code}, Message: {msg}");
        }

        private bool IsLoginSuccess(int code) => code == 200 || code == 0;

        private Dictionary<string, object> CreateLoginRequest(string deviceId)
        {
            return new Dictionary<string, object>(7) // 指定初始容量提高性能
            {
                ["user_type"] = GUEST_USER_TYPE,
                ["third_uid"] = deviceId,
                ["user_id"] = SkyPrefs.UserId,
                ["third_token"] = string.Empty,
                ["email"] = string.Empty,
                ["photo_url"] = string.Empty,
                ["display_name"] = string.Empty
            };
        }
        

    }
}

