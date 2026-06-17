using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    // 热云 SolarEngine SDK 管理器（重写版）。
    // 数据区域：海外 oversea；AppKey 来源：SkyConfigData.analyticsConfig.reyun_ID
    //
    // 职责：
    //   1) 初始化（preInit + initialize 连续调）
    //   2) 把 GameObject 名字注入 Java，让归因/事件回调能 UnitySendMessage 回来
    //   3) 接收归因回调（Java → C# 通过 UnitySendMessage），落到 SkySolarEnginePrefs
    //   4) 提供 TrackEvent / TrackPresetEvent 给上层调用
    //
    // 与 Adjust 的关系：互不抢占；归因结果只落 SkySolarEnginePrefs，不触发 CoreSdkMgr.OnUserAttributionReceived
    public class SkySolarEngineMgr : SkySingleton<SkySolarEngineMgr>
    {
        // 灰度阶段先打开 SDK 本地日志，方便 Logcat 自检；上线前应改为 false
        private const bool EnableLog = true;

        public bool IsInitialized { get; private set; }

        public bool TrackingEnable() => IsInitialized;

        // 从 SkyConfigData（Resources/SkyConfigData.asset）读取热云 AppKey
        private static string GetAppKey()
        {
            var cfg = SkyConfigMgr.Instance != null ? SkyConfigMgr.Instance.gameConfig : null;
            if (cfg == null || cfg.analyticsConfig == null) return string.Empty;
            return cfg.analyticsConfig.reyun_ID ?? string.Empty;
        }

        // 单独暴露 PreInit，便于以后接「同意隐私政策后再 Initialize」流程
        public void PreInit()
        {
            var appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey)) return;
            SkySolarEngineBridge.PreInit(appKey);
        }

        // SDK 初始化：preInit 后立即正式 initialize
        public void Init()
        {
            if (IsInitialized) return;
            var appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey))
            {
                LogTool.Error("[SolarEngine] AppKey empty (SkyConfigData.reyun_ID), skip init");
                return;
            }

            // 必须在 Initialize 之前告诉 Java 把回调发回哪个 GameObject。
            // SkySingleton 创建的名字含命名空间，例如 "Sky.SkySolarEngineMgr"，直接取 gameObject.name 最稳
            SkySolarEngineBridge.SetUnityCallbackTarget(gameObject.name);

            SkySolarEngineBridge.PreInit(appKey);
            SkySolarEngineBridge.Initialize(appKey, EnableLog);

            IsInitialized = true;
            LogTool.DebugLog("[SolarEngine] Init triggered, appKey=" + appKey + ", log=" + EnableLog,
                LogTool.Color.yellow);
        }

        #region 事件上报

        public void TrackEvent(string eventName)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackEvent(eventName, null);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackEvent(eventName, ToJson(properties));
        }

        // 上报热云预置事件（_appStart / _appLogin / _appPur 等）
        public void TrackPresetEvent(string eventName)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackPresetEvent(eventName, null);
        }

        public void TrackPresetEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackPresetEvent(eventName, ToJson(properties));
        }

        #endregion

        #region 归因回调（Java → C# 通过 UnitySendMessage 调用，方法名不可改）

        // 归因成功：参数为热云返回的 JSON 字符串
        public void OnAttributionSuccess(string json)
        {
             LogTool.DebugLog("[SolarEngine] OnAttributionSuccess: " + json);
            try
            {
                // 字段名对照真机日志确认（SDK 1.3.1.4 海外，2026-06-12 真机验证）
                var network  = ExtractString(json, "channel_name", "cid_name", "network");
                var campaign = ExtractString(json, "adplan_name", "turl_campaign_name", "campaign_name", "campaign");
                var adgroup  = ExtractString(json, "adgroup_name", "aid_name", "adgroup");
                var creative = ExtractString(json, "adcreative_name", "creative_name", "creative");
                var clickId  = ExtractString(json, "click_id", "clickId", "click_label");

                if (string.IsNullOrEmpty(network)) network = "Organic";

                SkySolarEnginePrefs.HasAttribution = true;
                SkySolarEnginePrefs.Network    = network;
                SkySolarEnginePrefs.Campaign   = campaign;
                SkySolarEnginePrefs.Adgroup    = adgroup;
                SkySolarEnginePrefs.Creative   = creative;
                SkySolarEnginePrefs.ClickLabel = clickId;
                SkySolarEnginePrefs.IsPaidUser = !IsOrganic(network);

                // 走项目统一上报通道，自动分发到 GA + 热云本身（事件名约定）
                Sky.SkyAnalyticsMgr.DesignEvent("SE", "Attribution", "Success", true, network);
            }
            catch (Exception e)
            {
                LogTool.Error("[SolarEngine] parse attribution failed: " + e);
            }
        }

        // 归因失败：参数为错误码字符串
        public void OnAttributionFail(string code)
        {
            LogTool.DebugLog("[SolarEngine] OnAttributionFail, code=" + code, LogTool.Color.yellow);
            Sky.SkyAnalyticsMgr.DesignEvent("SE", "Attribution", "Fail", true, code ?? "");
        }

        private static bool IsOrganic(string network)
        {
            if (string.IsNullOrEmpty(network)) return true;
            return network.Equals("Organic", StringComparison.OrdinalIgnoreCase)
                || network.Equals("organic", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 平铺 JSON 工具：避免引入 Newtonsoft，仅处理顶层字符串字段

        // 按 keys 顺序在 JSON 中查找，第一个非空值返回；找不到返回空串
        private static string ExtractString(string json, params string[] keys)
        {
            if (string.IsNullOrEmpty(json) || keys == null) return string.Empty;
            foreach (var key in keys)
            {
                var v = TryGetField(json, key);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return string.Empty;
        }

        // 仅支持 "key":"value" 或 "key":value(数字/bool) 形式的顶层字段
        private static string TryGetField(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
            var pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0) return null;
            int p = colon + 1;
            while (p < json.Length && (json[p] == ' ' || json[p] == '\t')) p++;
            if (p >= json.Length) return null;
            if (json[p] == '"')
            {
                int end = p + 1;
                while (end < json.Length && json[end] != '"')
                {
                    if (json[end] == '\\' && end + 1 < json.Length) end += 2;
                    else end++;
                }
                if (end >= json.Length) return null;
                return json.Substring(p + 1, end - p - 1);
            }
            // 数字 / bool / null：截到 , } ] 之前
            int e = p;
            while (e < json.Length && json[e] != ',' && json[e] != '}' && json[e] != ']') e++;
            return json.Substring(p, e - p).Trim();
        }

        // 简易 Dictionary → JSON（仅支持 string/数字/bool，避免引外部库）
        private static string ToJson(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0) return null;
            var sb = new System.Text.StringBuilder(64);
            sb.Append('{');
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(EscapeJson(kv.Key)).Append('"').Append(':');
                AppendJsonValue(sb, kv.Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendJsonValue(System.Text.StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            switch (v)
            {
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int _:
                case long _:
                case float _:
                case double _:
                case decimal _:
                    sb.Append(System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture));
                    break;
                default:
                    sb.Append('"').Append(EscapeJson(v.ToString())).Append('"');
                    break;
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        #endregion

        void OnDestroy()
        {
            Debug.LogWarning("SkySolarEngineMgr 被销毁了！");
        }
    }
}
