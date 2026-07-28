using UnityEngine;

public static class CryptoPrefs
{
    // ---------- Bool ----------
    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    // ---------- Int ----------
    public static void SetInt(string key, int val)
    {
        PlayerPrefs.SetInt(key, val);
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    // ---------- Float ----------
    public static void SetFloat(string key, float val)
    {
        PlayerPrefs.SetFloat(key, val);
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    // ---------- String ----------
    public static void SetString(string key, string val)
    {
        if (val == null) val = "";
        PlayerPrefs.SetString(key, val);
    }

    public static string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }
    // ---------- Long ----------
    public static void SetLong(string key, long value)
    {
        PlayerPrefs.SetString(key, value.ToString());
    }

    public static long GetLong(string key, long defaultValue = 0)
    {
        string val = PlayerPrefs.GetString(key, null);
        if (string.IsNullOrEmpty(val))
            return defaultValue;
        if (long.TryParse(val, out long result))
            return result;
        return defaultValue;
    }
    // ---------- Management ----------
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}