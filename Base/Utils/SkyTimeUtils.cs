using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public static class SkyTimeUtils
    {
        private const string _installTime = "install_time";
        public static void SetInstallTime()
        {
            if (!SkyPrefs.HasKey(_installTime))
            {
                SkyPrefs.SetInt(_installTime, (int)CurrentTimestamp());
            }
        }

        public static int GetInstallTime()
        {
            return SkyPrefs.GetInt(_installTime, 0);
        }

        public static int GetIntervalDays()
        {
            var firstTime = SkyPrefs.GetInt(_installTime);
            return DaysBetweenTimestamps(firstTime, CurrentTimestamp());
        }

        public static long CurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static int DaysBetweenTimestamps(long timestamp1, long timestamp2)
        {
            // 将时间戳转换为 DateTime 对象
            var dateTime1 = DateTimeOffset.FromUnixTimeSeconds(timestamp1).DateTime.Date;
            var dateTime2 = DateTimeOffset.FromUnixTimeSeconds(timestamp2).DateTime.Date;

            var timeSpan = dateTime2 - dateTime1;
            var days = timeSpan.Days;

            return days;
        }

        public static int SecondBetweenTimestamps(long timestamp1, long timestamp2)
        {
            // 将时间戳转换为 DateTime 对象
            var dateTime1 = DateTimeOffset.FromUnixTimeSeconds(timestamp1).DateTime.Date;
            var dateTime2 = DateTimeOffset.FromUnixTimeSeconds(timestamp2).DateTime.Date;

            var timeSpan = dateTime2 - dateTime1;
            int secondsDifference = (int)timeSpan.TotalSeconds; // 转换为秒数

            return secondsDifference;
        }

        public static string FormatDateTime(long timestamp, string format)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timestamp);
            var formattedDateTime = dateTime.ToString(format);
            return formattedDateTime;
        }
    }
}