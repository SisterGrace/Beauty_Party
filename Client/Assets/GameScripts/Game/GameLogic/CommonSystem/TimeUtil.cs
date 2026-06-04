using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using TK_Engine;
using UnityEngine.Networking;

namespace GameLogic
{
    public static class TimeUtil 
    {
        public static bool IsGetComplete;
        /// <summary>
        /// 初始时间戳
        /// </summary>
        private static long _mTimeStampStart;

        /// <summary>
        /// 初始化DateTime
        /// </summary>
        private static DateTime _mDateTimeNow;
        /// <summary>
        /// 当前时间戳
        /// </summary>
        public static long TimeStampNow => _mTimeStampStart + (int)Time.unscaledTime;
        public static DateTime DateTimeNow => _mDateTimeNow.AddSeconds(Time.unscaledTime);
        #region 本地时间戳

        public static readonly DateTime UtcTim 
            = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        private static long GetLocalTimeStamp()
        {
            var ts = DateTime.UtcNow - UtcTim;
            return (long)ts.TotalSeconds;
        }
        #endregion
        /// <summary>
        /// 获取服务器时间(异步获取)
        /// </summary>
        public static async UniTask GetServerTime()
        {
            if (!SettingsUtils.FrameworkGlobalSettings.IsServerTime)
            {
                _mTimeStampStart = GetLocalTimeStamp();
                SetTimeDate();
                return;
            }
            //使用using或finally来释放UnityWebRequest。
            using (UnityWebRequest Uwr = UnityWebRequest.Get("https://udt.myntpsv.com/mfun/server-time"))
            {
                Uwr.timeout = 10;
                try
                {
                    await Uwr.SendWebRequest().ToUniTask();
                    if (Uwr.result != UnityWebRequest.Result.Success)
                    {
                        //无法获取服务器时间，就使用本地时间
                        Log.Warning("Get server time error: " + Uwr.error);
                        _mTimeStampStart = GetLocalTimeStamp();
                    }
                    else
                    {
                        try
                        {
                            string rawText = Uwr.downloadHandler.text;
                            string timeStamp = rawText.Split(',')[2].Substring(5, 10);
                            _mTimeStampStart = long.Parse(timeStamp);
                            Log.Info("Get server time success: " + _mTimeStampStart);
                        }
                        catch (Exception e)//捕获数据解析异常
                        {
                            _mTimeStampStart = GetLocalTimeStamp();
                            Log.Warning("Get server time Parse error: " + e);
                        }
                    }
                }
                catch (Exception e)//网络错误或捕获网络层异常（如请求超时）
                {
                    _mTimeStampStart = GetLocalTimeStamp();
                    Log.Warning("Get server time Network error: " + e);
                }
                finally
                {
                    SetTimeDate();
                }
            }
        }
        /// <summary>
        /// 每天零点
        /// </summary>
        /// <returns>当前时间戳,秒</returns>
        public static long GetTimeStampToZero()
        {
            long utcTimestamp = (long)(DateTimeNow.Date - UtcTim).TotalSeconds;
            return utcTimestamp;
        }
        /// <summary>
        /// 后某一天零点
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStampToNextZero(int next=1)
        {
            long utcTimestamp = (long)(DateTimeNow.Date - UtcTim).TotalSeconds;
            utcTimestamp+=(86400*next);
            return utcTimestamp;
        }
        private static void SetTimeDate()
        {
            DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            //这里的七个零的作用是将秒级时间戳转换为10进制的纳秒级时间戳（因为 TimeSpan 的构造函数需要一个表示纳秒的长整型参数
            long lTime = long.Parse(_mTimeStampStart + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            _mDateTimeNow = dtStart.Add(toNow);
            IsGetComplete = true;
        }
        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            return TimeStampNow;
        }
        /// <summary>
        /// 获取时间戳差值,只计算秒
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static int GetSecondDiffer(long timeStamp)
        {
            if(timeStamp == 0) return 0;
            return (int)(TimeStampNow - timeStamp);
        }

        public static long GetDifferenceInDays(long oldTime)
        {
            long tDays = GetTimeStampToZero() - oldTime;
            return tDays / 86400;
        }
        /// <summary>
        /// 总秒数转成 H:M:S格式
        /// </summary>
        /// <param name="second"></param>
        /// <returns></returns>
        public static string SecondToTime(long second)
        {
            second = (long)Mathf.Max(second, 0);
            long hour = second / 3600;
            long minute = (second % 3600) / 60;
            long sec = second % 60;
            return $"{hour:D2}:{minute:D2}:{sec:D2}";
        }
        /// <summary>
        /// 总秒数 转成 d:H:M(时间字符格式化)
        /// </summary>
        /// <param name="second"></param>
        /// <param name="isDay"></param>
        /// <param name="formatWithLeadingZero">是否控制参数补零操作</param>
        /// <returns></returns>
        public static string SecondToTime2(long second, bool isDay = true,bool formatWithLeadingZero =true)
        {
            second = (long)Mathf.Max(second, 0);
            long day = second / 86400;
            long hour = (isDay?second % 86400:second) / 3600;
            long minute = (isDay?second % 86400 % 3600:second % 3600) / 60;
            long sec = (isDay?second % 86400 % 3600:second % 3600) % 60;
            if (isDay && day > 0)
            {
                return formatWithLeadingZero ? $"{day:D2}d:{hour:D2}h" : $"{day}d{hour}h";
            }
            else if (hour > 0)
            {
                return formatWithLeadingZero ? $"{hour:D2}h:{minute:D2}" : $"{hour}:{minute}";
            }
            else 
            {
                return formatWithLeadingZero ? $"{minute:D2}:{sec:D2}" : $"{minute}:{sec}";
            }
        }
        /// <summary>
        /// 总秒数 转成 分钟数
        /// </summary>
        /// <param name="second"></param>
        /// <returns></returns>
        public static string GetSecToMin(long second)
        {
            return (second / 60).ToString();
        }
        /// <summary>
        /// 总秒数 转成 小时数
        /// </summary>
        /// <param name="second"></param>
        /// <returns></returns>
        public static string GetSecToHour(long second,bool isSet = false)
        {
            if (!isSet)
            {
                return (second / 3600).ToString();
            }
            else
            {
                long tTime = second - 1;
                return $"{tTime / 3600}H+";
            }
            
        }
        /// <summary>
        /// 总秒数 转成小时h，当小于1小时，转成m:s格式
        /// </summary>
        /// <param name="second"></param>
        /// <returns></returns>
        public static string GetHourOrMinute(long second, bool isSet = false)
        {
            if (second >= 3600)
            {
                return $"{GetSecToHour(second)}h+";
            }
            else
            {
                return $"{SecondToTime2(second,false,false)}";
            }
        }

       
    }
}
