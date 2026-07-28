using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif


namespace CoreLite
{
    public static class CoreEmailFeedback
    {   
        public static void SendFeedbackEmail(string email)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            SendEmailAndroid(email);
            #elif UNITY_IOS && !UNITY_EDITOR
            SendEmailIOS(email);
            #else
            SendEmailEditor(email);
            #endif
        }

        #if UNITY_ANDROID
        private static void SendEmailAndroid(string email)
        {
            try
            {
                AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
                
                string emailSubject = "Feedback";
                string emailBody = GenerateEmailBody();
                
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_EMAIL"), new string[] { email });
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), emailSubject);
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), emailBody);
                
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                
                currentActivity.Call("startActivity", intentObject);
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Error($"[EmailFeedback] Android email send failed: {e.Message}");
                FallbackEmailMethod(email);
            }
        }
        #endif

        #if UNITY_IOS
        private static void SendEmailIOS(string email)
        {
            try
            {
                string emailSubject = "Feedback";
                string emailBody = GenerateEmailBody();
                
                // 调用 iOS 原生插件
                ShowMailComposer(email, emailSubject, emailBody);
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Error($"[EmailFeedback] iOS email send failed: {e.Message}");
                FallbackEmailMethod(email);
            }
        }
        #endif

        private static void SendEmailEditor(string email)
        {
            string emailSubject = "Feedback";
            string emailBody = GenerateEmailBody();
            string mailtoLink = $"mailto:{email}?subject={emailSubject}&body={System.Uri.EscapeDataString(emailBody)}";
            
            CoreLogUtils.Debug($"[EmailFeedback] Editor mode - Would send email:\nTo: {email}\nSubject: {emailSubject}\nBody: {emailBody}");
            Application.OpenURL(mailtoLink);
        }

        private static void FallbackEmailMethod(string email)
        {
            string emailSubject = "Feedback";
            string emailBody = GenerateEmailBody();
            string mailtoLink = $"mailto:{email}?subject={emailSubject}&body={System.Uri.EscapeDataString(emailBody)}";
            Application.OpenURL(mailtoLink);
        }

        private static string GenerateEmailBody()
        {
            var body = new System.Text.StringBuilder();
            
            body.AppendLine("Please describe your feedback or issue below:");
            body.AppendLine();
            body.AppendLine();
            body.AppendLine("=== Device Information ===");
            body.AppendLine($"Product Name: {Application.productName}");
            body.AppendLine($"Game Version: {Application.version}");
            body.AppendLine($"User ID: {CorePrefs.UserId}");
            body.AppendLine($"Device: {SystemInfo.deviceModel}");
            body.AppendLine($"OS: {SystemInfo.operatingSystem}");
            body.AppendLine($"Unity Version: {Application.unityVersion}");
            body.AppendLine($"SDK Version: {CoreSdkMgr.GetSDKVersion()}");
            
            return body.ToString();
        }

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _EmailComposer_ShowMailComposer(string to, string subject, string body);
                
        public static void ShowMailComposer(string to, string subject, string body)
        {
            _EmailComposer_ShowMailComposer(to, subject, body);
        }

        [DllImport("__Internal")]
        private static extern int IsiPhone();

        [DllImport("__Internal")]
        private static extern float GetScreenScale();

        [DllImport("__Internal")]
        private static extern void TransformationToOCCoordinates_Out(
            float inX, float inY, float inW, float inH,
            out float outX, out float outY, out float outW, out float outH);
#endif

        public static bool CoreLiteIsIPhone()
        {
#if UNITY_IOS
            return IsiPhone() == 1; 
#else
            return false;
#endif
        }

        public static float CoreLiteGetScreenScale()
        {
#if UNITY_IOS
            return GetScreenScale();
#else
            return 1.0f;
#endif
        }

        public static Rect TransformRectOut(Rect rect)
        {
#if UNITY_IOS && !UNITY_EDITOR             
            TransformationToOCCoordinates_Out(
                rect.x, rect.y, rect.width, rect.height,
                out float x, out float y, out float w, out float h);
            return new Rect(x, y, w, h);
        
#else

            return rect;
#endif
        }

    }
 
}

