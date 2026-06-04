#if USE_UP2SDK
using CoreLite;
#endif
using TK_Engine;

namespace GameLogic
{
    public partial class SDKManager
    {
        public bool IsLogin;
        public bool IsLoginSuccess;

        public void Login()
        {
#if UNITY_EDITOR

            IsLogin = true;
            IsLoginSuccess = true;
            return;
#endif
#if USE_UP2SDK
            /// <summary>
            /// 登录接口
            /// </summary>
            /// <param name="onComplete">bool:是否登录成功，string:登录失败原因，string:登录成功后的userid</param>
            CoreSdkMgr.Login((result, msg, uId) =>
            {
                Log.Debug("login result: " + result + " msg: " + msg + " uId: " + uId);
                if (result)
                {
                    ReadFailOrderId();
                }
                IsLogin = true;
                IsLoginSuccess = true;
                return;
            });
#else
            // 等待新 SDK 接入：未接入时直接置为已登录，避免业务流程卡住
            IsLogin = true;
            IsLoginSuccess = true;
#endif
        }

        public static string GetUserId()
        {
#if USE_UP2SDK
            return CoreSdkMgr.GetCurrentUserId();
#else
            return string.Empty;
#endif
        }
    }
}
