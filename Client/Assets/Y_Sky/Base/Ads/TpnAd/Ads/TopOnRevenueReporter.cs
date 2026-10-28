using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// TopOn 广告展示收益上报器。
    /// 
    /// 职责：
    ///   1. 接收 TopOn 专用收入回调 didRevenueForPlacementID:extra: 的数据
    ///   2. 按在线参数 rev_adj 的 rate 权重，决定本次收益归类到哪个 mediationPlatform
    ///   3. 调用 SkySolarEngineEvents 上报热力引擎 _appImp 事件
    /// 
    /// 重要：必须在 TopOn 的【专用收入回调】中调用，不要在展示成功回调中调用，
    ///       否则会因重复触发导致数据比 TopOn 后台多。
    /// </summary>
    public static class TopOnRevenueReporter
    {
        private const string RevAdjKey = "rev_adj";

        // 在线参数 name → 热力引擎 mediationPlatform 自定义归类名
        private const string ChannelAppLovinMax = "applovin_max_sdk";
        private const string ChannelIronSource = "ironsource_sdk";
        private const string MediationAPL = "APL";
        private const string MediationIS = "IS";

        private static List<(string name, int rate)> _cachedWeights;
        private static string _cachedRaw;
        // ============ 新增：业务上下文缓存 ============
        // key = placementId，value = (pos, formatNumber, positionNumber)
        private static readonly Dictionary<string, (string pos, int formatNumber, int positionNumber)> _revenueContext
            = new Dictionary<string, (string pos, int formatNumber, int positionNumber)>();

        /// <summary>
        /// 展示回调里调用，把本次展示的业务上下文缓存起来，供收入回调使用。
        /// </summary>
        public static void SetRevenueContext(string placementId, string pos, int formatNumber, int positionNumber)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            _revenueContext[placementId] = (pos ?? "", formatNumber, positionNumber);
        }

        /// <summary>
        /// 收入回调里调用，读回展示时缓存的业务上下文。
        /// </summary>
        public static bool TryGetRevenueContext(string placementId,
            out string pos, out int formatNumber, out int positionNumber)
        {
            if (!string.IsNullOrEmpty(placementId) &&
                _revenueContext.TryGetValue(placementId, out var ctx))
            {
                pos = ctx.pos;
                formatNumber = ctx.formatNumber;
                positionNumber = ctx.positionNumber;
                return true;
            }
            pos = "";
            formatNumber = 0;
            positionNumber = 0;
            return false;
        }
        // ============================================


        public static void OnAdRevenuePaid(string placementId, Dictionary<string, object> extra)
        {
            if (extra == null || extra.Count == 0) return;

            // ---------- 1. 提取收益 ----------
            double revenue = ExtractDouble(extra, "publisher_revenue");
            if (revenue < 0) return;

            // ---------- 2. 提取货币 ----------
            string currency = ExtractString(extra, "currency");
            if (string.IsNullOrEmpty(currency))
            {
                LogTool.Error("[TopOnRevenueReporter] missing currency, skip");
                return;
            }

            // ---------- 3. 提取其他字段 ----------
            string networkFirmId = ExtractString(extra, "network_firm_id");
            string networkPlacementId = ExtractString(extra, "network_placement_id");
            string adFormat = ExtractString(extra, "adunit_format");
            // adunitId 删掉（原来没被用到）

            // ---------- 4. 按权重选 mediationPlatform ----------
            string mediation = PickMediationByWeight();
            if (string.IsNullOrEmpty(mediation)) return;

            // ---------- 5. network_firm_id → 真实广告平台名 ----------
            string adNetworkName = "";
            if (int.TryParse(networkFirmId, out int firmId))
                adNetworkName = SkySolarEngineEvents.MapFirmIdToPlatform(firmId);

            if (string.IsNullOrEmpty(adNetworkName))
            {
                LogTool.Error($"[TopOnRevenueReporter] unknown network_firm_id={networkFirmId}, skip");
                return;
            }

            // ---------- 6. 从缓存取业务上下文 ----------
            string pos;
            int formatNumber, positionNumber;
            if (!TryGetRevenueContext(placementId, out pos, out formatNumber, out positionNumber))
            {
                // 拿不到缓存：可能是收入回调先于展示回调触发，用兜底值，并打日志
                LogTool.DebugLog($"[TopOnRevenueReporter] no context for placement={placementId}, using fallback (pos empty)");
            }

            // ---------- 7. 上报所有平台 ----------
            SkyAnalyticsMgr.TrackAdImpressionEvent(
                revenue: revenue,
                network: adNetworkName,
                unit: networkPlacementId,
                placementID: placementId,
                format: adFormat,
                platform: "topon",
                pos: pos,
                formatNumber: formatNumber,
                positionNumber: positionNumber,
                currency: currency,
                customMediationPlatform: mediation);
        }

        // ============================================================
        //  在线参数解析 & 权重选择
        // ============================================================
        /// <summary>
        /// 拿不到在线参数 / 参数无效时的兜底归类。
        /// 
        /// 注意：使用 "default_topon" 而不是 "topon"。
        /// 因为改动之前旧逻辑是固定上报 "topon"，历史库里已有脏数据。
        /// 用 default_topon 可以和历史数据区分，也便于排查当前兜底频率。
        /// </summary>
        private const string MediationDefault = "default_topon";
        /// <summary>
        /// 按 rev_adj 的 rate 随机选一项，返回对应的 mediationPlatform。
        /// 无有效项 / 未命中 → 返回 null（不上报）。
        /// </summary>
        private static string PickMediationByWeight()
        {
            var weights = GetWeights();

            // 拿不到参数 / 参数无效 → 兜底为 default_topon，不丢收入
            if (weights == null || weights.Count == 0)
            {
                LogTool.DebugLog("[TopOnRevenueReporter] rev_adj unavailable or invalid, fallback to default_topon");
                return MediationDefault;
            }

            int roll = Random.Range(0, 100); // 0-99
            int cumulative = 0;
            foreach (var w in weights)
            {
                cumulative += w.rate;
                if (roll < cumulative)
                {
                    var mediation = MapChannelToMediation(w.name);
                    // name 是未知渠道（配置里出现了 APL / IS 之外的 name）→ 也兜底
                    if (string.IsNullOrEmpty(mediation))
                    {
                        LogTool.DebugLog($"[TopOnRevenueReporter] unknown channel name={w.name}, fallback to default_topon");
                        return MediationDefault;
                    }
                    return mediation;
                }
            }

            // 累计不足 100，随机落在剩余部分 → 按业务定义不上报
            return null;
        }

        private static string MapChannelToMediation(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return null;

            // 新增：如果配置里包含 topon 字样，正常归类为 topon
            if (channelName.IndexOf("topon", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "TPN";
            switch (channelName)
            {
                case ChannelAppLovinMax: return MediationAPL;
                case ChannelIronSource: return MediationIS;
                default: return null; // 未定义的 name 不上报
            }
        }

        private static List<(string name, int rate)> GetWeights()
        {
            string raw = GetRemoteConfigValue(RevAdjKey);
            if (string.IsNullOrEmpty(raw)) return null;

            if (raw == _cachedRaw && _cachedWeights != null)
                return _cachedWeights;

            try
            {
                _cachedWeights = ParseWeights(raw);
                _cachedRaw = raw;
                return _cachedWeights;
            }
            catch (System.Exception e)
            {
                LogTool.Error("[TopOnRevenueReporter] parse rev_adj failed: " + e);
                return null;
            }
        }

        /// <summary>
        /// 解析：[{"name":"applovin_max_sdk","rate":70},{"name":"ironsource_sdk","rate":30}]
        /// rate 缺失 / 非 0-100 → 跳过该项。
        /// </summary>
        private static List<(string name, int rate)> ParseWeights(string json)
        {
            var result = new List<(string, int)>();
            int idx = 0;
            while (true)
            {
                int s = json.IndexOf('{', idx);
                if (s < 0) break;
                int e = json.IndexOf('}', s);
                if (e < 0) break;

                string obj = json.Substring(s + 1, e - s - 1);
                idx = e + 1;

                string name = ExtractJsonString(obj, "name");
                int rate = ExtractJsonInt(obj, "rate");

                if (!string.IsNullOrEmpty(name) && rate >= 0 && rate <= 100)
                    result.Add((name, rate));
            }
            return result;
        }

        // ============================================================
        //  通用工具
        // ============================================================

        private static double ExtractDouble(Dictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v) || v == null) return -1;
            switch (v)
            {
                case double d: return d;
                case float f: return f;
                case int i: return i;
                case long l: return l;
                case string s: return double.TryParse(s, out double r) ? r : -1;
                default: return -1;
            }
        }

        private static string ExtractString(Dictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v) || v == null) return null;
            string s = v.ToString();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static string ExtractJsonString(string obj, string key)
        {
            int k = obj.IndexOf("\"" + key + "\"");
            if (k < 0) return null;
            int colon = obj.IndexOf(':', k);
            if (colon < 0) return null;
            int q1 = obj.IndexOf('"', colon);
            if (q1 < 0) return null;
            int q2 = obj.IndexOf('"', q1 + 1);
            if (q2 < 0) return null;
            return obj.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static int ExtractJsonInt(string obj, string key)
        {
            int k = obj.IndexOf("\"" + key + "\"");
            if (k < 0) return -1;
            int colon = obj.IndexOf(':', k);
            if (colon < 0) return -1;
            int p = colon + 1;
            while (p < obj.Length && (obj[p] == ' ' || obj[p] == '\t')) p++;
            int start = p;
            while (p < obj.Length && (char.IsDigit(obj[p]) || obj[p] == '-')) p++;
            if (p == start) return -1;
            return int.TryParse(obj.Substring(start, p - start), out int v) ? v : -1;
        }

        /// <summary>
        /// 读取在线参数。TODO: 替换成你们项目的实际接口。
        /// </summary>
        /// <summary>
        /// 读取在线参数。替换成你们项目的实际接口。
        /// </summary>
        private static string GetRemoteConfigValue(string key)
        {
            // TODO: 换成实际接口，例如：
            return SkyAnalyticsMgr.GetConfigStr(key);
        }
    }
}
