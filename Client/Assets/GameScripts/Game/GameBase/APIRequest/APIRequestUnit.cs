using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using TK_Engine;
using Newtonsoft.Json;

namespace GameBase
{
    public class APIRequestUnit : MonoBehaviour
    {
        public Action<APIRequestUnit> DelOver;

        public string RequestUrl;
        private bool _requestStarted = false;
        private float _rTimer = 0;
        private const float TIME_OUT_SECONDS = 15;
        private Action<string> _errorCallback;

        private void Update()
        {
            if (_requestStarted)
            {
                _rTimer += Time.unscaledDeltaTime;
                if (_rTimer >= TIME_OUT_SECONDS)
                {
                    TimeOut();
                }
            }
        }

        public void SendRequest<T, U>(T pRequestData, Action<U> pRespondCallback = null, Action<string> pErrorCallback = null) where T : APIBase
        {
#if UNITY_EDITOR
            Log.Info("Request URL: " + RequestUrl);
            Log.Info("API{0} request data:{1}", typeof(T).Name, pRequestData.RequestJson());
            Log.Info("API{0} request text:{1}", typeof(T).Name, Utility.EncryptionHelper.XXTEAEncrypt(pRequestData.RequestJson()));
#endif
            _errorCallback = pErrorCallback;
            _requestStarted = true;
            _rTimer = 0;
            RequestUrl = pRequestData.RequestURL;
            if (pRequestData.Method == HttpMethod.GET)
            {
                StartCoroutine(GetRequest(pRequestData.RequestURL, pRequestData.RequestJson(), pRequestData.UseRawResponse, pRespondCallback, pErrorCallback));
            }
            else
            {
                StartCoroutine(PostRequest(pRequestData.RequestURL, pRequestData.RequestJson(), pRequestData.UseRawResponse, pRespondCallback, pErrorCallback));
            }
        }

        IEnumerator PostRequest<U>(string pUrl, string pRequestJson, bool pUseRawResponse, Action<U> pRespondCallback = null, Action<string> pErrorCallback = null)
        {
            UnityWebRequest tRequest = new UnityWebRequest(pUrl, "POST");
            tRequest.downloadHandler = new DownloadHandlerBuffer();
            tRequest.SetRequestHeader("Content-Type", "application/json");

#if UNITY_EDITOR
            Log.Info("Request URL : " + pUrl);
#endif
            Log.Info("API request text:{0}", pRequestJson);
            if (SettingsUtils.FrameworkGlobalSettings.HttpDataEncrypt)
            {
                string tEncryptStr = Utility.EncryptionHelper.XXTEAEncrypt(pRequestJson);
                tRequest.SetRequestHeader("params", tEncryptStr);
#if UNITY_EDITOR
                Log.Info("API request encrypt text:{0}", tEncryptStr);
#endif
            }
            else
            {
                byte[] tPostBytes = Utility.EncryptionHelper.StringToBytes(pRequestJson);
                tRequest.uploadHandler = new UploadHandlerRaw(tPostBytes);
            }

            tRequest.SetRequestHeader("encrypt", SettingsUtils.FrameworkGlobalSettings.HttpDataEncrypt ? "true" : "false");

            yield return tRequest.SendWebRequest();
            Log.Info("Status Code: " + tRequest.responseCode);
#if UNITY_2020_1_OR_NEWER
            if (tRequest.result == UnityWebRequest.Result.ConnectionError || tRequest.result == UnityWebRequest.Result.ProtocolError || tRequest.result == UnityWebRequest.Result.DataProcessingError)
#else
            if (tRequest.isNetworkError || tRequest.isHttpError)
#endif
            {
                pErrorCallback?.Invoke("Network error!");
                Log.Error(tRequest.error);
            }
#if UNITY_2020_1_OR_NEWER
            else if (tRequest.result == UnityWebRequest.Result.Success)
#else
            else
#endif
            {
                string tRespondTxt = tRequest.downloadHandler.text;

                Log.Info("API respond text:{0}", tRespondTxt);

                if (SettingsUtils.FrameworkGlobalSettings.HttpDataEncrypt)
                {
                    tRespondTxt = Utility.EncryptionHelper.XXTEADecrypt(tRespondTxt);

                    Log.Info("API respond decrypt text:{0}", tRespondTxt);

                }

                if (pUseRawResponse)
                {
                    // 原始响应格式：直接解析为目标类型
                    U tData = Utility.Json.ToObject<U>(tRespondTxt);
                    pRespondCallback?.Invoke(tData);
                }
                else
                {
                    // 标准响应格式：code/msg/data 包装
                    APIRespond<U> tData = Utility.Json.ToObject<APIRespond<U>>(tRespondTxt);
                    if (tData.code == 0)
                    {
                        pRespondCallback?.Invoke(tData.data);
                    }
                    else
                    {
                        pErrorCallback?.Invoke(tData.msg);
                    }
                }
            }

            Over();
        }
        IEnumerator GetRequest<U>(string pUrl, string pRequestJson, bool pUseRawResponse, Action<U> pRespondCallback = null, Action<string> pErrorCallback = null)
        {
            // 将 JSON 参数转换为 URL 查询参数
            string tFullUrl = BuildGetUrl(pUrl, pRequestJson);

#if UNITY_EDITOR
            Log.Info("GET Request URL : " + tFullUrl);
#endif

            UnityWebRequest tRequest = new UnityWebRequest(tFullUrl,"GET");
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            tRequest.SetRequestHeader("Timestamp", timestamp);
            tRequest.SetRequestHeader("Sign", Utility.EncryptionHelper.GetMD5(timestamp));

            tRequest.downloadHandler = new DownloadHandlerBuffer();
           
            yield return tRequest.SendWebRequest();
            Log.Info("Status Code: " + tRequest.responseCode);

#if UNITY_2020_1_OR_NEWER
            if (tRequest.result == UnityWebRequest.Result.ConnectionError || tRequest.result == UnityWebRequest.Result.ProtocolError || tRequest.result == UnityWebRequest.Result.DataProcessingError)
#else
            if (tRequest.isNetworkError || tRequest.isHttpError)
#endif
            {
                pErrorCallback?.Invoke("Network error!");
                Log.Error(tRequest.error);
            }
#if UNITY_2020_1_OR_NEWER
            else if (tRequest.result == UnityWebRequest.Result.Success)
#else
            else
#endif
            {
                string tRespondTxt = tRequest.downloadHandler.text;
                Log.Info("API respond text:{0}", tRespondTxt);

                // GET 请求默认不加密，直接解析
                if (pUseRawResponse)
                {
                    // 原始响应格式：直接解析为目标类型
                    U tData = Utility.Json.ToObject<U>(tRespondTxt);
                    pRespondCallback?.Invoke(tData);
                }
                else
                {
                    // 标准响应格式：code/msg/data 包装
                    APIRespond<U> tData = Utility.Json.ToObject<APIRespond<U>>(tRespondTxt);
                    if (tData.code == 0)
                    {
                        pRespondCallback?.Invoke(tData.data);
                    }
                    else
                    {
                        pErrorCallback?.Invoke(tData.msg);
                    }
                }
            }

            Over();
        }

        /// <summary>
        /// 构建 GET 请求 URL，将 JSON 转换为查询参数
        /// </summary>
        private string BuildGetUrl(string pBaseUrl, string pRequestJson)
        {
            if (string.IsNullOrEmpty(pRequestJson) || pRequestJson == "{}")
            {
                return pBaseUrl;
            }

            // 使用简单的 JSON 解析提取键值对
            var tParams = new System.Collections.Generic.List<string>();

            // 移除首尾的大括号
            string tJsonContent = pRequestJson.Trim();
            if (tJsonContent.StartsWith("{") && tJsonContent.EndsWith("}"))
            {
                tJsonContent = tJsonContent.Substring(1, tJsonContent.Length - 2);
            }

            // 简单解析 JSON 键值对
            string[] tPairs = tJsonContent.Split(',');
            foreach (string tPair in tPairs)
            {
                int tColonIndex = tPair.IndexOf(':');
                if (tColonIndex > 0)
                {
                    string tKey = tPair.Substring(0, tColonIndex).Trim().Trim('"');
                    string tValue = tPair.Substring(tColonIndex + 1).Trim().Trim('"');
                    tParams.Add($"{UnityWebRequest.EscapeURL(tKey)}={UnityWebRequest.EscapeURL(tValue)}");
                }
            }

            if (tParams.Count > 0)
            {
                string tSeparator = pBaseUrl.Contains("?") ? "&" : "?";
                return pBaseUrl + tSeparator + string.Join("&", tParams);
            }

            return pBaseUrl;
        }
    

        /// <summary>
        /// 超时处理
        /// </summary>
        private void TimeOut()
        {
            StopAllCoroutines();
            _errorCallback?.Invoke("Request timeout");
            Over();
        }

        private void Over()
        {
            _requestStarted = false;
            DelOver?.Invoke(this);
        }

        public class APIRespond<U>
        {
            public int code;
            public string msg;
            public U data;
        }
    }

}
