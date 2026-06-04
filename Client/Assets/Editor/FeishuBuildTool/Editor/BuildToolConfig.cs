using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FeishuBuildTool.Editor
{
    /// <summary>
    /// 打包工具配置持久化。
    /// 数据保存在 ToolBuild/editor_config.json（已被 .gitignore 忽略）。
    /// </summary>
    [Serializable]
    public class BuildToolConfig
    {
        public string feishuAppId = "";
        public string feishuAppSecret = "";
        public string keystorePass = "";
        public string keyAliasPass = "";
        public bool rememberPassword = true;
        public string lastUser = "";
        public List<DriveFileRecord> driveFileTokens = new List<DriveFileRecord>();
        public List<FeishuUser> users = new List<FeishuUser>();

        [Serializable]
        public class FeishuUser
        {
            public string name = "";
            public string openId = "";
        }

        [Serializable]
        public class DriveFileRecord
        {
            public string bundleId = "";
            public string fileToken = "";
        }

        private static string ConfigDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../ToolBuild"));

        private static string ConfigPath => Path.Combine(ConfigDir, "editor_config.json");

        private static string OpenClawConfigPath
        {
            get
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".openclaw", "openclaw.json");
            }
        }

        public static BuildToolConfig Load()
        {
            BuildToolConfig config;

            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    config = JsonUtility.FromJson<BuildToolConfig>(json) ?? new BuildToolConfig();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BuildToolConfig] Failed to load config: {e.Message}");
                    config = new BuildToolConfig();
                }
            }
            else
            {
                config = new BuildToolConfig();
            }

            if (!config.rememberPassword)
            {
                config.keystorePass = "";
                config.keyAliasPass = "";
            }

            if (string.IsNullOrEmpty(config.feishuAppId) || string.IsNullOrEmpty(config.feishuAppSecret))
            {
                TryLoadFromOpenClaw(config);
            }

            return config;
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);

                var toSave = JsonUtility.ToJson(this, true);
                File.WriteAllText(ConfigPath, toSave);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildToolConfig] Failed to save config: {e.Message}");
            }
        }

        public FeishuUser FindUser(string name)
        {
            return users.Find(u => u.name == name);
        }

        public void AddOrUpdateUser(string name, string openId)
        {
            var existing = FindUser(name);
            if (existing != null)
            {
                existing.openId = openId;
            }
            else
            {
                users.Add(new FeishuUser { name = name, openId = openId });
            }
        }

        public void RemoveUser(string name)
        {
            users.RemoveAll(u => u.name == name);
            if (lastUser == name)
                lastUser = users.Count > 0 ? users[0].name : "";
        }

        public string[] GetUserNames()
        {
            var names = new string[users.Count];
            for (int i = 0; i < users.Count; i++)
                names[i] = users[i].name;
            return names;
        }

        public int GetUserIndex(string name)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].name == name) return i;
            }
            return -1;
        }

        public string GetDriveToken(string bundleId)
        {
            var record = driveFileTokens.Find(r => r.bundleId == bundleId);
            return record?.fileToken ?? "";
        }

        public void SetDriveToken(string bundleId, string fileToken)
        {
            var record = driveFileTokens.Find(r => r.bundleId == bundleId);
            if (record != null)
            {
                record.fileToken = fileToken;
            }
            else
            {
                driveFileTokens.Add(new DriveFileRecord { bundleId = bundleId, fileToken = fileToken });
            }
        }

        public void RemoveDriveToken(string bundleId)
        {
            driveFileTokens.RemoveAll(r => r.bundleId == bundleId);
        }

        private static void TryLoadFromOpenClaw(BuildToolConfig config)
        {
            if (!File.Exists(OpenClawConfigPath)) return;

            try
            {
                string json = File.ReadAllText(OpenClawConfigPath);

                string appId = ExtractNestedValue(json, "channels", "feishu", "appId");
                string appSecret = ExtractNestedValue(json, "channels", "feishu", "appSecret");

                if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(appSecret))
                {
                    config.feishuAppId = appId;
                    config.feishuAppSecret = appSecret;
                    Debug.Log("[BuildToolConfig] Loaded Feishu credentials from OpenClaw config");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BuildToolConfig] Could not read OpenClaw config: {e.Message}");
            }
        }

        private static string ExtractNestedValue(string json, string key1, string key2, string key3)
        {
            int idx = json.IndexOf($"\"{key1}\"", StringComparison.Ordinal);
            if (idx < 0) return "";
            idx = json.IndexOf($"\"{key2}\"", idx, StringComparison.Ordinal);
            if (idx < 0) return "";
            idx = json.IndexOf($"\"{key3}\"", idx, StringComparison.Ordinal);
            if (idx < 0) return "";

            idx = json.IndexOf(':', idx);
            if (idx < 0) return "";
            idx++;
            while (idx < json.Length && (json[idx] == ' ' || json[idx] == '"')) idx++;
            int end = json.IndexOf('"', idx);
            return end < 0 ? "" : json.Substring(idx, end - idx);
        }
    }
}
