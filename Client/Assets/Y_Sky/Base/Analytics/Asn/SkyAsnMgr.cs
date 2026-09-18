/*using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Sky
{
    public class SkyAsnMgr : SkySingleton<SkyAsnMgr>
    {
        public void StartReqAsn()
        {
            var asnUrl = SkyConfigMgr.Instance.GetAsnUrl();

            if(string.IsNullOrEmpty(asnUrl))
            {
                SkySdkMgr.OnAsnUserReceived?.Invoke(false);
                return;
            }

            RequestAsn(asnUrl,(state)=>
            {
                SkySdkMgr.OnAsnUserReceived?.Invoke(state);
            });
        }

        private void RequestAsn(string url, Action<bool> callback)
        {
            StartCoroutine(DoRequestWithRetry(url, callback));
        }

        private static IEnumerator DoRequestWithRetry(string url, Action<bool> callback)
        {
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                SkyLogUtils.Debug($"asn Request attempt #{attempt} to {url}");

                using (var request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    // 检查请求结果
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        if (request.responseCode == 200)
                        {
                            SkyLogUtils.Debug("asn Request succeeded with status 200");
                            callback(false);
                            yield break; 
                        }
                        // 204: 立即返回 false
                        else if (request.responseCode == 204)
                        {
                            SkyLogUtils.Debug("asn Request returned status 204");
                            callback(true);
                            yield break; 
                        }
                        else // 其他成功状态码需要重试
                        {
                            SkyLogUtils.Warning($"asn Unexpected status code: {request.responseCode}. Retrying...");
                        }
                    }
                    else // 请求失败需要重试
                    {
                        SkyLogUtils.Warning($"asn Request failed: {request.error}. Retrying...");
                    }
                }
                if (attempt < maxRetries)
                {
                    float waitTime = Mathf.Pow(2, attempt) * 0.5f; // 0.5s, 1s, 2s
                    SkyLogUtils.Debug($"asn Waiting {waitTime}s before next retry");
                    yield return new WaitForSeconds(waitTime);
                }
            }
            SkyLogUtils.Debug("asn All retries completed, returning true");
            callback(false);
        }
    }

}

*/