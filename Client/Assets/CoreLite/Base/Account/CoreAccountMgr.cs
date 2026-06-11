using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static CoreLite.CoreLoginEnum;

namespace CoreLite
{
    public class CoreAccountMgr: CoreSingleton<CoreAccountMgr>
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
            return CorePrefs.UserId;
        }

        public async void Login(Action<bool, string, string> onComplete = null)
        {
            _currentRetryCount = 0;

            _maxRetryCount = CoreAnalyticsMgr.GetConfigInt("login_max_retry_count",DEFAULT_MAX_RETRY_COUNT);

            _currentLoginCallback = onComplete;

            await AttemptLoginAsync();
        }

        private async Task  AttemptLoginAsync()
        {
            try
            {
                _lastLoginTime = Time.time;
                string deviceId = await CorePrefs.GetOrCreateDeviceId();
                CoreAdjustPrefs.DeviceId = deviceId;
                #if SSThinkingData
                CoreThinkingDataMgr.Instance.SetSuperProperties(new Dictionary<string, object> { { "device_num", deviceId } });
#endif
                var loginRequest = CreateLoginRequest(deviceId);
                CoreAnalyticsMgr.TrackEvent("User_Start_Login");
                CoreLogUtils.Debug("deviceid:"+CoreAdjustPrefs.DeviceId);
                CoreServerMgr.Instance.Login(loginRequest, OnLoginResponse);
            }
            catch(Exception ex)
            {
                CoreLogUtils.Error($"AttemptLoginAsync failed: {ex.Message}");
            }
        }

        private void OnLoginResponse(int code, string msg, ServerBackLoginInfo data)
        {
            bool isSuccess = IsLoginSuccess(code);
            
            if (isSuccess)
            {

                OnLoginSuccess(data);
                
                string resultMessage = "Login successful";

                CoreLogUtils.Debug($"Login succeeded after {_currentRetryCount} retries");
                
                _currentLoginCallback?.Invoke(true, resultMessage, data.user_id);

                _currentLoginCallback = null;

                CoreAnalyticsMgr.TrackEvent("User_Login_Success",new Dictionary<string, object>{
                    {"user_id", data.user_id},
                    {"device_id",CoreAdjustPrefs.DeviceId},
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
                    CoreLogUtils.Warning($"Login attempt {_currentRetryCount} failed (Code: {code}), retrying in {RETRY_DELAY} seconds...");
                    
                    Invoke(nameof(RetryLoginAsync), RETRY_DELAY);
                }
                else
                {
                    CoreLogUtils.Error($"Login failed after {_maxRetryCount} retries. Giving up.");
                    
                    CoreAnalyticsMgr.TrackEvent("User_Login_Fail",new Dictionary<string, object>{
                        {"user_id", ""},
                        {"device_id",CoreAdjustPrefs.DeviceId},
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
            CorePrefs.UserId = string.Empty;
            CoreAnalyticsMgr.Logout();
            _currentRetryCount = 0;
            _currentLoginCallback = null;
            CancelInvoke(nameof(RetryLoginAsync));
        }

        private void OnLoginSuccess(ServerBackLoginInfo data)
        {
            CoreLogUtils.Debug("Login successful");
            CorePrefs.UserId = data.user_id;
            CoreAnalyticsMgr.SetLoginAccount(data.user_id);
            CoreAnalyticsMgr.SetSuperProperties(new Dictionary<string, object>{{"user_id", data.user_id}});
        }
        
        private void OnLoginFailed(int code, string msg)
        {
            CoreLogUtils.Error($"Login failed - Code: {code}, Message: {msg}");
        }

        private bool IsLoginSuccess(int code) => code == 200 || code == 0;

        private Dictionary<string, object> CreateLoginRequest(string deviceId)
        {
            return new Dictionary<string, object>(7) // 指定初始容量提高性能
            {
                ["user_type"] = GUEST_USER_TYPE,
                ["third_uid"] = deviceId,
                ["user_id"] = CorePrefs.UserId,
                ["third_token"] = string.Empty,
                ["email"] = string.Empty,
                ["photo_url"] = string.Empty,
                ["display_name"] = string.Empty
            };
        }
        

    }
}

