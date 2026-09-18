using System.Text.RegularExpressions;

namespace Sky
{
    public static class SkyStringUtils
    {
        // 宽松模式：保留中文字符（适用于 GA、数数等支持 Unicode 的平台）
        private static readonly Regex InvalidCharsLoose = new Regex(@"[^a-zA-Z0-9\u4e00-\u9fa5]", RegexOptions.Compiled);

        // 严格模式：只允许字母、数字、下划线（适用于 Firebase / Adjust，它们不支持中文和特殊符号）
        private static readonly Regex InvalidCharsStrict = new Regex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

        private static readonly Regex MultipleUndersSkys = new Regex(@"_+", RegexOptions.Compiled);

        private const int DefaultMaxLength = 49;  // GA 上限 49
        private const int FirebaseMaxLength = 40; // Firebase 上限 40

        /// <summary>
        /// 清洗事件名（宽松模式，保留中文，默认最大长度 49，适合 GA/数数）
        /// </summary>
        public static string SanitizeEventName(string name)
        {
            return SanitizeEventName(name, allowUnicode: true, maxLength: DefaultMaxLength);
        }

        /// <summary>
        /// 清洗事件名（可配置是否保留 Unicode 和最大长度）
        /// </summary>
        /// <param name="name">原始事件名</param>
        /// <param name="allowUnicode">true=保留中文等字符；false=仅保留 A-Z a-z 0-9 _（Firebase 要求）</param>
        /// <param name="maxLength">最大长度，超出截断</param>
        public static string SanitizeEventName(string name, bool allowUnicode, int maxLength)
        {
            if (string.IsNullOrEmpty(name)) return name;

            string sanitized = allowUnicode
                ? InvalidCharsLoose.Replace(name, "_")
                : InvalidCharsStrict.Replace(name, "_");

            // 合并连续下划线
            sanitized = MultipleUndersSkys.Replace(sanitized, "_");

            // 去除首尾下划线
            sanitized = sanitized.Trim('_');

            // Firebase 要求事件名不能以数字开头（严格模式下处理）
            if (!allowUnicode && sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            {
                sanitized = "_" + sanitized;
            }

            // 长度截断
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            // 如果清洗后为空，给一个安全默认值
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "event";

            return sanitized;
        }

        /// <summary>
        /// 清洗参数字符串（移除控制字符、首尾空格，避免无效字符导致上报失败）
        /// </summary>
        public static string SanitizeParameter(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // 移除 ASCII 控制字符（0x00-0x1F, 0x7F）
            var cleaned = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");

            // 移除首尾空格
            return cleaned.Trim();
        }
    }
}