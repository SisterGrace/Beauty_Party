using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CoreLite
{
    public class CoreServerRequest : CoreSingleton<CoreServerRequest>
    {
        public void SendPostRequest(string url, string jsonRequestBody, Dictionary<string, string> headers = null, Action<int, string> callback = null)
        {
            StartCoroutine(PostRequestCoroutine(url, jsonRequestBody, headers, callback));
        }

        private static IEnumerator PostRequestCoroutine(string url, string jsonRequestBody, Dictionary<string, string> headers, Action<int, string> callback)
        {
            url = "1111";//避免用其服务器
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.timeout = 15;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(jsonRequestBody))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonRequestBody));
                request.uploadHandler.contentType = "application/json";
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var startTime = Time.realtimeSinceStartup;
#endif
            yield return request.SendWebRequest();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var endTime = Time.realtimeSinceStartup;
            var sb = new StringBuilder();
            sb.AppendLine($"POST Request URL: {url}");
            sb.AppendLine($"POST Request Headers: {GetHeadersAsString(headers)}");
            sb.AppendLine($"POST Request Body: {jsonRequestBody}");
            sb.AppendLine($"Response Code: {request.responseCode}");
            sb.AppendLine($"Response Time: {endTime - startTime:F2} seconds");
            sb.AppendLine($"Response Headers: \n {GetHeadersAsString(request.GetResponseHeaders())}");
            var downloadHandlerText = request.downloadHandler != null ? request.downloadHandler.text : "";
            sb.AppendLine($"Response: {downloadHandlerText}");
            Debug.Log(sb.ToString());
#endif

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(0, request.downloadHandler?.text);
            }
            else
            {
                callback?.Invoke(-1, request.error ?? $"request fail, result = {request.result}");
            }

            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
        }

        private static string GetHeadersAsString(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return "\tN/A";
            }

            return headers.Aggregate("", (current, header) => current + "\t" + header.Key + ": " + header.Value + "\n");
        }
    }
}