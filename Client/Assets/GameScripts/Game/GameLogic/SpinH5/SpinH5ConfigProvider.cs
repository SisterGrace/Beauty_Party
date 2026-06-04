using System;

/// <summary>
/// SpinH5 模块的在线参数获取接口。由宿主（如 SDKMgr）在 Init 时注入实现。
/// 用于 RemoteConfig 覆写、底价等配置的拉取。
/// </summary>
public static class SpinH5ConfigProvider
{
    /// <summary>获取字符串类型在线参数。未设置时返回默认值。</summary>
    public static Func<string, string, string> GetConfigStr { get; set; }

    /// <summary>获取整型在线参数。未设置时返回默认值。</summary>
    public static Func<string, int, int> GetConfigInt { get; set; }

    /// <summary>获取浮点型在线参数。未设置时返回默认值。</summary>
    public static Func<string, float, float> GetConfigFloat { get; set; }

    /// <summary>获取布尔型在线参数。未设置时返回默认值。</summary>
    public static Func<string, bool, bool> GetConfigBool { get; set; }

    /// <summary>安全获取字符串，未设置实现时返回默认值</summary>
    public static string SafeGetStr(string key, string defaultValue)
        => GetConfigStr != null ? GetConfigStr(key, defaultValue) : defaultValue;

    /// <summary>安全获取整型，未设置实现时返回默认值</summary>
    public static int SafeGetInt(string key, int defaultValue)
        => GetConfigInt != null ? GetConfigInt(key, defaultValue) : defaultValue;

    /// <summary>安全获取浮点型，未设置实现时返回默认值</summary>
    public static float SafeGetFloat(string key, float defaultValue)
        => GetConfigFloat != null ? GetConfigFloat(key, defaultValue) : defaultValue;

    /// <summary>安全获取布尔型，未设置实现时返回默认值</summary>
    public static bool SafeGetBool(string key, bool defaultValue)
        => GetConfigBool != null ? GetConfigBool(key, defaultValue) : defaultValue;
}
