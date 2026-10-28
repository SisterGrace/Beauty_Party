using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// 全局货币转换器，用于将 AdMob 和 TopOn 回调中的各种货币统一转换为 USD 进行竞价比价。
    /// 汇率数据基于市场平均值，支持 20+ 常见货币。
    /// </summary>
    public static class SkyCurrencyConverter
    {
        // 缓存所有货币到 USD 的汇率（1 单位目标货币 = ? USD）
        private static readonly Dictionary<string, double> _toUsdRates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "USD", 1.0 },
            { "CNY", 0.138 },       // 1 人民币 ≈ 0.138 美元
            { "IDR", 0.0000625 },   // 1 印尼盾 ≈ 0.0000625 美元 (1 USD ≈ 16000 IDR)
            { "EUR", 1.08 },        // 1 欧元 ≈ 1.08 美元
            { "GBP", 1.27 },        // 1 英镑 ≈ 1.27 美元
            { "JPY", 0.0066 },      // 1 日元 ≈ 0.0066 美元 (1 USD ≈ 150 JPY)
            { "KRW", 0.00075 },     // 1 韩元 ≈ 0.00075 美元 (1 USD ≈ 1300 KRW)
            { "AUD", 0.66 },        // 1 澳元 ≈ 0.66 美元
            { "CAD", 0.74 },        // 1 加元 ≈ 0.74 美元
            { "HKD", 0.128 },       // 1 港币 ≈ 0.128 美元
            { "TWD", 0.031 },       // 1 新台币 ≈ 0.031 美元
            { "INR", 0.012 },       // 1 印度卢比 ≈ 0.012 美元
            { "RUB", 0.011 },       // 1 俄罗斯卢布 ≈ 0.011 美元
            { "BRL", 0.20 },        // 1 巴西雷亚尔 ≈ 0.20 美元
            { "VND", 0.00004 },     // 1 越南盾 ≈ 0.00004 美元 (1 USD ≈ 25000 VND)
            { "THB", 0.028 },       // 1 泰铢 ≈ 0.028 美元
            { "MYR", 0.21 },        // 1 马来西亚林吉特 ≈ 0.21 美元
            { "SGD", 0.74 },        // 1 新加坡元 ≈ 0.74 美元
            { "PHP", 0.018 },       // 1 菲律宾比索 ≈ 0.018 美元
            { "TRY", 0.031 },       // 1 土耳其里拉 ≈ 0.031 美元
            { "SAR", 0.26 },        // 1 沙特里亚尔 ≈ 0.26 美元
            { "AED", 0.27 }         // 1 阿联酋迪拉姆 ≈ 0.27 美元
        };

        /// <summary>
        /// 将任意货币转换为 USD。
        /// 如果遇到字典里没有的货币，会采用兜底策略，防止竞价系统被未知数值污染。
        /// </summary>
        /// <param name="amount">原始金额</param>
        /// <param name="currencyCode">货币代码（如 "IDR", "CNY", "USD"）</param>
        /// <returns>转换后的 USD 金额</returns>
        public static double ToUSD(double amount, string currencyCode)
        {
            if (amount <= 0) return 0;
            if (string.IsNullOrEmpty(currencyCode)) return amount; // 默认按 USD 处理

            if (_toUsdRates.TryGetValue(currencyCode, out double rate))
            {
                double usdAmount = amount * rate;
                SkyLogUtils.Debug($"[Currency] 货币转换: {amount:F6} {currencyCode} → ${usdAmount:F6} USD (汇率={rate:F6})");
                return usdAmount;
            }

            // 🚨 兜底策略：遇到未知货币（比如 BHD、KWD 这种），千万不能直接当成 USD！
            // 如果金额很大（>100），说明大概率是低价值货币（如印尼盾、越南盾），为了防止它作弊，直接按极低价值折算。
            // 如果金额很小，那就当它作废，返回 0。
            SkyLogUtils.Warning($"[Currency] 未知的货币代码: {currencyCode}，金额: {amount}。为了防止竞价异常，将强制按兜底规则处理。");
            
            if (amount >= 100)
            {
                // 大额未知货币按极低汇率处理（防止越南盾、印尼盾这种"数值大但价值低"的货币作弊）
                double fallbackUsd = amount * 0.0001;
                SkyLogUtils.Debug($"[Currency] 大额未知货币兜底: {amount:F6} {currencyCode} → ${fallbackUsd:F6} USD (按 0.0001 汇率)");
                return fallbackUsd;
            }
            
            // 小额未知货币直接归零（可能是测试数据或无效回调）
            SkyLogUtils.Debug($"[Currency] 小额未知货币归零: {amount:F6} {currencyCode} → $0 USD");
            return 0;
        }

        /// <summary>
        /// 可选：利用免费的公开 API 在后台静默更新最新汇率（推荐在游戏启动时调用一次）
        /// 这样即使几个月后汇率大变，你的本地字典也能保持最新。
        /// </summary>
        /// <remarks>
        /// 使用开源免费汇率 API（https://open.er-api.com/v6/latest/USD）
        /// 每天 1500 次免费额度，足够游戏启动使用。
        /// 需要配合你们项目的 HTTP 请求方法实现（如 UnityWebRequest）。
        /// </remarks>
        public static void FetchAndUpdateRates()
        {
            // TODO: 可选实现，在游戏启动时调用一次，动态更新汇率表
            // string url = "https://open.er-api.com/v6/latest/USD";
            // SkyHttp.Get(url, (success, json) => {
            //     if (success && !string.IsNullOrEmpty(json)) {
            //         // 解析 JSON 拿到 rates，把 1 USD = X 转换成 1 X = ? USD
            //         // 更新到 _toUsdRates 字典中，并存到 PlayerPrefs 里供下次启动直接用
            //     }
            // });
        }
    }
}
