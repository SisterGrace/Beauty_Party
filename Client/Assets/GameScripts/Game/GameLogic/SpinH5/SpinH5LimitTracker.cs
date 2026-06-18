using System;
using Y_Base;
using UnityEngine;

/// <summary>
/// SpinH5 竞价展示限额追踪。按 IV/RV 分别记录日计数与总计数，支持跨日清零与持久化。
/// </summary>
public class SpinH5LimitTracker : Singleton<SpinH5LimitTracker>
{
    private const string KeyPrefix = "SpinH5_Limit_";
    private const string KeyLastDate = "SpinH5_LastDate";
    private const string KeyLastH5ShowTime = "SpinH5_LastH5ShowTime";
    private const string DateFormat = "yyyy-MM-dd";

    private int _ivDailyCount;
    private int _rvDailyCount;
    private int _ivTotalCount;
    private int _rvTotalCount;
    private string _lastDate;
    private double _lastH5ShowUtcSeconds;

    public int IvDailyCount => _ivDailyCount;
    public int RvDailyCount => _rvDailyCount;
    public int IvTotalCount => _ivTotalCount;
    public int RvTotalCount => _rvTotalCount;

    public SpinH5LimitTracker()
    {
        Load();
        CheckAndResetDaily();
    }

    /// <summary>检查是否跨日，跨日则清零日计数</summary>
    private void CheckAndResetDaily()
    {
        var today = DateTime.Now.ToString(DateFormat);
        if (_lastDate != today)
        {
            _ivDailyCount = 0;
            _rvDailyCount = 0;
            _lastDate = today;
            Save();
        }
    }

    /// <summary>是否处于 bid_cd_second 内（H5 展示后 CD 内不参与竞价）</summary>
    public bool IsInBidCd(int bidCdSecond)
    {
        if (bidCdSecond <= 0 || _lastH5ShowUtcSeconds <= 0) return false;
        var elapsed = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - _lastH5ShowUtcSeconds;
        return elapsed < bidCdSecond;
    }

    /// <summary>IV 是否可参与竞价（未达日限与总限）</summary>
    public bool CanShowIv(int ivDailyLimit, int ivTotalLimit)
    {
        CheckAndResetDaily();
        if (ivDailyLimit > 0 && _ivDailyCount >= ivDailyLimit) return false;
        if (ivTotalLimit > 0 && _ivTotalCount >= ivTotalLimit) return false;
        return true;
    }

    /// <summary>RV 是否可参与竞价（未达日限与总限）</summary>
    public bool CanShowRv(int rvDailyLimit, int rvTotalLimit)
    {
        CheckAndResetDaily();
        if (rvDailyLimit > 0 && _rvDailyCount >= rvDailyLimit) return false;
        if (rvTotalLimit > 0 && _rvTotalCount >= rvTotalLimit) return false;
        return true;
    }

    /// <summary>IV 竞价成功展示后调用</summary>
    public void RecordIvShown()
    {
        CheckAndResetDaily();
        _ivDailyCount++;
        _ivTotalCount++;
        _lastH5ShowUtcSeconds = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        Save();
    }

    /// <summary>RV 竞价成功展示后调用</summary>
    public void RecordRvShown()
    {
        CheckAndResetDaily();
        _rvDailyCount++;
        _rvTotalCount++;
        _lastH5ShowUtcSeconds = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        Save();
    }

    private void Load()
    {
        _ivDailyCount = PlayerPrefs.GetInt(KeyPrefix + "IvDaily", 0);
        _rvDailyCount = PlayerPrefs.GetInt(KeyPrefix + "RvDaily", 0);
        _ivTotalCount = PlayerPrefs.GetInt(KeyPrefix + "IvTotal", 0);
        _rvTotalCount = PlayerPrefs.GetInt(KeyPrefix + "RvTotal", 0);
        _lastDate = PlayerPrefs.GetString(KeyLastDate, DateTime.Now.ToString(DateFormat));
        _lastH5ShowUtcSeconds = double.TryParse(PlayerPrefs.GetString(KeyLastH5ShowTime, "0"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t)
            ? t : 0;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KeyPrefix + "IvDaily", _ivDailyCount);
        PlayerPrefs.SetInt(KeyPrefix + "RvDaily", _rvDailyCount);
        PlayerPrefs.SetInt(KeyPrefix + "IvTotal", _ivTotalCount);
        PlayerPrefs.SetInt(KeyPrefix + "RvTotal", _rvTotalCount);
        PlayerPrefs.SetString(KeyLastDate, _lastDate);
        PlayerPrefs.SetString(KeyLastH5ShowTime, _lastH5ShowUtcSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }
}
