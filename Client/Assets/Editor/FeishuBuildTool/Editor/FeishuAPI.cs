using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;

namespace FeishuBuildTool.Editor
{
    /// <summary>
    /// 飞书 Open API 的 C# 封装，供 Unity Editor 内使用。
    /// 支持：获取 token、发送文本/富文本消息、消息 API 文件发送（<=30MB）、
    /// 云盘分片上传（大文件）、云盘文件分享、云盘文件管理。
    /// </summary>
    public static class FeishuAPI
    {
        private const string BaseUrl = "https://open.feishu.cn/open-apis";
        private const int MaxMessageFileSize = 30 * 1024 * 1024;
        private const int DriveBlockSize = 4 * 1024 * 1024;

        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        #region Token

        public static string GetTenantAccessToken(string appId, string appSecret)
        {
            string url = $"{BaseUrl}/auth/v3/tenant_access_token/internal";
            string body = $"{{\"app_id\":\"{appId}\",\"app_secret\":\"{appSecret}\"}}";

            var resp = PostJson(url, body);
            if (resp.code != 0)
                throw new Exception($"Failed to get token: code={resp.code}, msg={resp.msg}");

            string token = GetJsonString(resp.raw, "tenant_access_token");
            if (string.IsNullOrEmpty(token))
                throw new Exception("tenant_access_token is empty in response");

            Debug.Log("[Feishu] Token acquired");
            return token;
        }

        #endregion

        #region Send Text Message

        public static void SendText(string token, string receiveId, string text,
            string receiveIdType = "open_id")
        {
            string url = $"{BaseUrl}/im/v1/messages?receive_id_type={receiveIdType}";
            string contentJson = $"{{\"text\":\"{EscapeJson(text)}\"}}";
            string payload = BuildMessagePayload(receiveId, "text", contentJson);

            var resp = PostJsonWithAuth(url, payload, token);
            if (resp.code != 0)
                throw new Exception($"SendText failed: code={resp.code}, msg={resp.msg}");

            Debug.Log("[Feishu] Text message sent");
        }

        #endregion

        #region Send File (auto-select channel)

        /// <summary>
        /// 发送文件给用户。自动判断：&lt;=30MB 走消息 API，&gt;30MB 走云盘分片上传。
        /// 返回云盘 file_token（仅大文件走云盘时有值），用于后续清理旧文件。
        /// </summary>
        public static string SendFile(string token, string receiveId, string filePath,
            string receiveIdType = "open_id", string oldDriveFileToken = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            long fileSize = new FileInfo(filePath).Length;
            string fileName = Path.GetFileName(filePath);
            long sizeMB = fileSize / 1024 / 1024;

            if (fileSize <= MaxMessageFileSize)
            {
                Debug.Log($"[Feishu] File {fileName} ({sizeMB}MB) <= 30MB, using message API");
                SendFileViaMessageAPI(token, receiveId, filePath, fileName, receiveIdType);
                return null;
            }

            Debug.Log($"[Feishu] File {fileName} ({sizeMB}MB) > 30MB, using Drive chunked upload");
            if (!string.IsNullOrEmpty(oldDriveFileToken))
            {
                Debug.Log($"[Feishu] Deleting old Drive file: {oldDriveFileToken}");
                DriveDeleteFile(token, oldDriveFileToken);
            }
            return SendFileViaDrive(token, receiveId, filePath, fileName, receiveIdType);
        }

        #endregion

        #region Message API File Upload (<=30MB)

        private static void SendFileViaMessageAPI(string token, string receiveId, string filePath,
            string fileName, string receiveIdType)
        {
            string fileKey = UploadFileToMessage(token, filePath, fileName);

            string url = $"{BaseUrl}/im/v1/messages?receive_id_type={receiveIdType}";
            string contentJson = $"{{\"file_key\":\"{fileKey}\"}}";
            string payload = BuildMessagePayload(receiveId, "file", contentJson);

            var resp = PostJsonWithAuth(url, payload, token);
            if (resp.code != 0)
                throw new Exception($"Send file message failed: code={resp.code}, msg={resp.msg}");

            Debug.Log("[Feishu] File message sent via message API");
        }

        private static string UploadFileToMessage(string token, string filePath, string fileName)
        {
            string url = $"{BaseUrl}/im/v1/files";

            using (var form = new MultipartFormDataContent())
            {
                form.Add(new StringContent("stream"), "file_type");
                form.Add(new StringContent(fileName), "file_name");

                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                form.Add(fileContent, "file", fileName);

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = Client.SendAsync(request).Result;
                string raw = response.Content.ReadAsStringAsync().Result;
                int code = GetJsonInt(raw, "code");

                if (code != 0)
                    throw new Exception($"Upload file failed: {raw}");

                string fileKey = GetNestedJsonString(raw, "data", "file_key");
                Debug.Log($"[Feishu] File uploaded, key={fileKey}");
                return fileKey;
            }
        }

        #endregion

        #region Drive Chunked Upload (large files)

        private static string SendFileViaDrive(string token, string receiveId, string filePath,
            string fileName, string receiveIdType)
        {
            string folderToken = GetDriveRootFolder(token);
            string fileToken = DriveChunkedUpload(token, filePath, fileName, folderToken);
            DriveShareToUser(token, fileToken, receiveId);

            string driveUrl = $"https://feishu.cn/file/{fileToken}";
            string text = $"📎 APK 已上传到飞书云盘\n" +
                          $"📂 文件: {fileName}\n" +
                          $"🔗 下载: {driveUrl}\n\n" +
                          "也可在飞书「与我共享」中找到此文件";
            SendText(token, receiveId, text, receiveIdType);
            return fileToken;
        }

        public static string GetDriveRootFolder(string token)
        {
            string url = $"{BaseUrl}/drive/explorer/v2/root_folder/meta";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = Client.SendAsync(request).Result;
            string raw = response.Content.ReadAsStringAsync().Result;
            int code = GetJsonInt(raw, "code");

            if (code != 0)
                throw new Exception($"Get Drive root folder failed: {raw}");

            string folderToken = GetNestedJsonString(raw, "data", "token");
            Debug.Log($"[Feishu] Drive root folder: {folderToken}");
            return folderToken;
        }

        public static string DriveChunkedUpload(string token, string filePath, string fileName,
            string folderToken)
        {
            long fileSize = new FileInfo(filePath).Length;
            Debug.Log($"[Feishu] Drive chunked upload: {fileName} ({fileSize / 1024 / 1024}MB)");

            string prepUrl = $"{BaseUrl}/drive/v1/files/upload_prepare";
            string prepBody =
                $"{{\"file_name\":\"{EscapeJson(fileName)}\",\"parent_type\":\"explorer\"," +
                $"\"parent_node\":\"{folderToken}\",\"size\":{fileSize}}}";

            var prepResp = PostJsonWithAuth(prepUrl, prepBody, token);
            if (prepResp.code != 0)
                throw new Exception($"Upload prepare failed: {prepResp.raw}");

            string uploadId = GetNestedJsonString(prepResp.raw, "data", "upload_id");
            int blockSize = GetNestedJsonInt(prepResp.raw, "data", "block_size");
            int blockNum = GetNestedJsonInt(prepResp.raw, "data", "block_num");

            if (blockSize <= 0) blockSize = DriveBlockSize;
            Debug.Log($"[Feishu] Prepared: {blockNum} blocks × {blockSize / 1024 / 1024}MB");

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[blockSize];
                for (int seq = 0; seq < blockNum; seq++)
                {
                    int bytesRead = fs.Read(buffer, 0, blockSize);

                    string partUrl = $"{BaseUrl}/drive/v1/files/upload_part";
                    using (var form = new MultipartFormDataContent())
                    {
                        form.Add(new StringContent(uploadId), "upload_id");
                        form.Add(new StringContent(seq.ToString()), "seq");
                        form.Add(new StringContent(bytesRead.ToString()), "size");

                        var blockContent = new ByteArrayContent(buffer, 0, bytesRead);
                        blockContent.Headers.ContentType =
                            MediaTypeHeaderValue.Parse("application/octet-stream");
                        form.Add(blockContent, "file", "block");

                        var request = new HttpRequestMessage(HttpMethod.Post, partUrl) { Content = form };
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response = Client.SendAsync(request).Result;
                        string partRaw = response.Content.ReadAsStringAsync().Result;
                        int partCode = GetJsonInt(partRaw, "code");

                        if (partCode != 0)
                            throw new Exception($"Upload block {seq} failed: {partRaw}");
                    }

                    int pct = (seq + 1) * 100 / blockNum;
                    Debug.Log($"[Feishu] Upload progress: {seq + 1}/{blockNum} ({pct}%)");
                }
            }

            string finishUrl = $"{BaseUrl}/drive/v1/files/upload_finish";
            string finishBody = $"{{\"upload_id\":\"{uploadId}\",\"block_num\":{blockNum}}}";

            var finishResp = PostJsonWithAuth(finishUrl, finishBody, token);
            if (finishResp.code != 0)
                throw new Exception($"Upload finish failed: {finishResp.raw}");

            string fileToken = GetNestedJsonString(finishResp.raw, "data", "file_token");
            Debug.Log($"[Feishu] Drive upload complete: {fileToken}");
            return fileToken;
        }

        public static void DriveShareToUser(string token, string fileToken, string userOpenId)
        {
            string url = $"{BaseUrl}/drive/v1/permissions/{fileToken}/members?type=file";
            string body =
                $"{{\"member_type\":\"openid\",\"member_id\":\"{userOpenId}\",\"perm\":\"view\"}}";

            try
            {
                var resp = PostJsonWithAuth(url, body, token);
                if (resp.code == 0)
                    Debug.Log("[Feishu] File shared to user");
                else
                    Debug.LogWarning($"[Feishu] Share failed (non-fatal): {resp.raw}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Feishu] Share failed (non-fatal): {e.Message}");
            }
        }

        public struct DriveFileInfo
        {
            public string name;
            public string token;
            public string type;
            public long size;
        }

        public static List<DriveFileInfo> DriveListFiles(string token)
        {
            string folderToken = GetDriveRootFolder(token);
            var result = new List<DriveFileInfo>();
            string pageToken = "";

            while (true)
            {
                string url = $"{BaseUrl}/drive/v1/files?folder_token={folderToken}&page_size=50";
                if (!string.IsNullOrEmpty(pageToken))
                    url += $"&page_token={pageToken}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = Client.SendAsync(request).Result;
                string raw = response.Content.ReadAsStringAsync().Result;
                int code = GetJsonInt(raw, "code");

                if (code != 0)
                    throw new Exception($"List Drive files failed: {raw}");

                ParseDriveFileList(raw, result);

                bool hasMore = raw.Contains("\"has_more\":true");
                if (hasMore)
                {
                    pageToken = GetNestedJsonString(raw, "data", "page_token");
                    if (string.IsNullOrEmpty(pageToken)) break;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private static void ParseDriveFileList(string raw, List<DriveFileInfo> result)
        {
            string dataPattern = "\"files\":[";
            int filesIdx = raw.IndexOf(dataPattern, StringComparison.Ordinal);
            if (filesIdx < 0) return;

            int arrStart = filesIdx + dataPattern.Length - 1;
            string arrJson = ExtractArray(raw, arrStart);
            if (string.IsNullOrEmpty(arrJson)) return;

            int pos = 0;
            while (pos < arrJson.Length)
            {
                int objStart = arrJson.IndexOf('{', pos);
                if (objStart < 0) break;

                string objJson = ExtractObject(arrJson, objStart);
                if (string.IsNullOrEmpty(objJson)) break;

                var info = new DriveFileInfo
                {
                    name = GetJsonString(objJson, "name"),
                    token = GetJsonString(objJson, "token"),
                    type = GetJsonString(objJson, "type"),
                    size = GetJsonLong(objJson, "size")
                };
                result.Add(info);

                pos = objStart + objJson.Length;
            }
        }

        public static void DriveDeleteFile(string token, string fileToken)
        {
            if (string.IsNullOrEmpty(fileToken)) return;

            try
            {
                string url = $"{BaseUrl}/drive/v1/files/{fileToken}?type=file";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = Client.SendAsync(request).Result;
                string raw = response.Content.ReadAsStringAsync().Result;
                int code = GetJsonInt(raw, "code");

                if (code == 0)
                    Debug.Log($"[Feishu] Old file deleted: {fileToken}");
                else
                    Debug.LogWarning($"[Feishu] Delete old file failed (non-fatal): {raw}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Feishu] Delete old file failed (non-fatal): {e.Message}");
            }
        }

        #endregion

        #region HTTP Helpers

        private struct ApiResponse
        {
            public int code;
            public string msg;
            public string raw;
        }

        private static ApiResponse PostJson(string url, string jsonBody)
        {
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = Client.PostAsync(url, content).Result;
            string raw = response.Content.ReadAsStringAsync().Result;
            return new ApiResponse
            {
                code = GetJsonInt(raw, "code"),
                msg = GetJsonString(raw, "msg"),
                raw = raw
            };
        }

        private static ApiResponse PostJsonWithAuth(string url, string jsonBody, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = Client.SendAsync(request).Result;
            string raw = response.Content.ReadAsStringAsync().Result;
            return new ApiResponse
            {
                code = GetJsonInt(raw, "code"),
                msg = GetJsonString(raw, "msg"),
                raw = raw
            };
        }

        private static string BuildMessagePayload(string receiveId, string msgType, string contentJson)
        {
            return $"{{\"receive_id\":\"{receiveId}\",\"msg_type\":\"{msgType}\",\"content\":\"{EscapeJson(contentJson)}\"}}";
        }

        #endregion

        #region Minimal JSON Helpers

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string GetJsonString(string json, string key)
        {
            string pattern = $"\"{key}\":\"";
            int idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return "";
            idx += pattern.Length;
            int end = json.IndexOf('"', idx);
            return end < 0 ? "" : json.Substring(idx, end - idx);
        }

        private static int GetJsonInt(string json, string key)
        {
            string pattern1 = $"\"{key}\":";
            int idx = json.IndexOf(pattern1, StringComparison.Ordinal);
            if (idx < 0) return -1;
            idx += pattern1.Length;
            while (idx < json.Length && json[idx] == ' ') idx++;
            int end = idx;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            if (end == idx) return -1;
            return int.TryParse(json.Substring(idx, end - idx), out int val) ? val : -1;
        }

        private static string GetNestedJsonString(string json, string outerKey, string innerKey)
        {
            string outerPattern = $"\"{outerKey}\":";
            int outerIdx = json.IndexOf(outerPattern, StringComparison.Ordinal);
            if (outerIdx < 0) return "";

            int braceStart = json.IndexOf('{', outerIdx + outerPattern.Length);
            if (braceStart < 0) return "";

            string innerJson = ExtractObject(json, braceStart);
            return GetJsonString(innerJson, innerKey);
        }

        private static int GetNestedJsonInt(string json, string outerKey, string innerKey)
        {
            string outerPattern = $"\"{outerKey}\":";
            int outerIdx = json.IndexOf(outerPattern, StringComparison.Ordinal);
            if (outerIdx < 0) return -1;

            int braceStart = json.IndexOf('{', outerIdx + outerPattern.Length);
            if (braceStart < 0) return -1;

            string innerJson = ExtractObject(json, braceStart);
            return GetJsonInt(innerJson, innerKey);
        }

        private static string ExtractObject(string json, int startBrace)
        {
            int depth = 0;
            for (int i = startBrace; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                if (depth == 0) return json.Substring(startBrace, i - startBrace + 1);
            }
            return json.Substring(startBrace);
        }

        private static string ExtractArray(string json, int startBracket)
        {
            int depth = 0;
            for (int i = startBracket; i < json.Length; i++)
            {
                if (json[i] == '[') depth++;
                else if (json[i] == ']') depth--;
                if (depth == 0) return json.Substring(startBracket, i - startBracket + 1);
            }
            return json.Substring(startBracket);
        }

        private static long GetJsonLong(string json, string key)
        {
            string pattern = $"\"{key}\":";
            int idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return 0;
            idx += pattern.Length;
            while (idx < json.Length && (json[idx] == ' ' || json[idx] == '"')) idx++;
            int end = idx;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            if (end == idx) return 0;
            return long.TryParse(json.Substring(idx, end - idx), out long val) ? val : 0;
        }

        #endregion
    }
}
