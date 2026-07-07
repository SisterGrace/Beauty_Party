using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreLite
{
    public class CoreInterstitialRules : CoreSingleton<CoreInterstitialRules>
    {
        private Dictionary<int, int> _ivsCount = new Dictionary<int, int>();
        public Dictionary<IvType, float> _adTypeTimeInterVal = new Dictionary<IvType, float>();
        public IvType _currentShowIvType = IvType.NULL;
        public float _videoStartTime = 0;
        public string CanShowInter(IvType _type)
        {
            // 【1】前N次不展示插屏
            var checkIfReachFirstNTimes = CheckIfReachFirstNTimes(_type);
            if (checkIfReachFirstNTimes != null)
            {
                return checkIfReachFirstNTimes;
            }

            // 到达N次后，第N+1次直接展示插屏
            if (string.IsNullOrEmpty(IfFirstCheckPlayerPrefs(_type)))
            {
                CoreLogUtils.Debug(" [ads] 到达第N+1次且时间满足");
                return null;
            }
            else if (IfFirstCheckPlayerPrefs(_type).Equals(1))
            {
                CoreLogUtils.Debug(" [ads] 到达第N+1次但是时间未到");
                return "到达N次,但是时间未到";
            }

            // 非第N+1次，不能直接展示广告； 【2】要判断是否达到了间隔次数；
            int innerIvType = GetInterRules(_type).Type;
            int currTimes; // 当前达到的间隔次数
            int needTimes = GetInterRules(_type).Over_Level; // 需要达到的间隔次数

            if (_ivsCount.ContainsKey(innerIvType))
            {
                // 读取字典里插屏类型的次数
                currTimes = _ivsCount[innerIvType] + 1;
                _ivsCount[innerIvType] = currTimes;
            }
            else
            {
                // 字典里没有数据，添加数据
                currTimes = 1;
                _ivsCount.Add(innerIvType, currTimes);
            }

            CoreLogUtils.Debug(" [ads] 当前间隔次数：" + currTimes + "远端次数：" + needTimes + "IV Type:" + _type);
            if (currTimes <= needTimes)
            {
                // 没到频次控制时间
                return "当前间隔次数：" + currTimes + "远端次数：" + needTimes + "IV Type:" + _type;
            }

            //【3】判断是否达到最小时间间隔
            int timeInterVal = GetInterRules(_type).Interval_Time;
            var mIvStartTime = _videoStartTime; //GetCurrentTimeInterVal(_type);
            var adInterTime = Time.realtimeSinceStartup - mIvStartTime;
            bool canShowFlag = adInterTime >= timeInterVal;
            var msg = "插屏时间间隔：当前时间：" + adInterTime + "远端时间：" + timeInterVal + "IV Type:" + _type + " canShowFlag:" +
                    canShowFlag;
            CoreLogUtils.Debug(" [ads]" + msg);

            if (canShowFlag)
            {
                // 重置次数
                _ivsCount[innerIvType] = 0;
            }

            if (!canShowFlag)
            {
                return msg;
            }

            return null;
        }

        /// <summary>
        /// 仅检查当前是否满足插屏展示规则，不修改任何计数或 Prefs（查询插屏规则时不计次）。
        /// 用于外部在决定展示前先判断是否会被规则拦截；多次调用不会增加插屏调用次数。
        /// 返回 null 表示规则允许展示，非 null 为拦截原因。
        /// </summary>
        public string CanShowInterPeek(IvType _type)
        {
            var checkFirstN = CheckIfReachFirstNTimesPeek(_type);
            if (checkFirstN != null)
                return checkFirstN;

            if (string.IsNullOrEmpty(IfFirstCheckPlayerPrefsPeek(_type)))
                return null;
            if (IfFirstCheckPlayerPrefsPeek(_type).Equals(1))
                return "到达N次,但是时间未到";

            int innerIvType = GetInterRules(_type).Type;
            int needTimes = GetInterRules(_type).Over_Level;
            int currTimes = (_ivsCount.ContainsKey(innerIvType) ? _ivsCount[innerIvType] : 0) + 1;

            CoreLogUtils.Debug(" [ads] peek 当前间隔次数：" + currTimes + " 远端次数：" + needTimes + " IV Type:" + _type);
            if (currTimes <= needTimes)
                return "当前间隔次数：" + currTimes + "远端次数：" + needTimes + "IV Type:" + _type;

            int timeInterVal = GetInterRules(_type).Interval_Time;
            var adInterTime = Time.realtimeSinceStartup - _videoStartTime;
            bool canShowFlag = adInterTime >= timeInterVal;
            if (!canShowFlag)
                return "插屏时间间隔：当前时间：" + adInterTime + "远端时间：" + timeInterVal + "IV Type:" + _type + " canShowFlag:" + canShowFlag;

            return null;
        }

        /// <summary>仅读检查“前N次不展示”，不增加 Prefs 中的次数。</summary>
        public string CheckIfReachFirstNTimesPeek(IvType _type)
        {
            string reachKey = "ivReachTimes" + _type.ToString();
            int reachTime = CorePrefs.GetInt(reachKey, 0);
            bool canShow = reachTime >= GetInterRules(_type).Skip_Level;
            var msg = "前N次不展示插屏,本地次数:" + reachTime + "远端参数：" + GetInterRules(_type).Skip_Level + "IV Type:" + _type;
            CoreLogUtils.Debug(" [ads]" + msg + " canshow:" + canShow);
            return canShow ? null : msg;
        }

        /// <summary>仅读检查首次/时间，不写入 Prefs。</summary>
        public string IfFirstCheckPlayerPrefsPeek(IvType _type)
        {
            string key = "first_Show_iv" + _type.ToString();
            string values = CorePrefs.GetString(key, "");
            if (values.Length <= 0)
            {
                int timeInterVal = GetInterRules(_type).Interval_Time;
                if (_videoStartTime == 0)
                    return null;
                var adInterTime = Time.realtimeSinceStartup - _videoStartTime;
                bool canShowFlag = adInterTime >= timeInterVal;
                CoreLogUtils.Debug(" [ads] 到达N+1次了,插屏类型:" + _type + " 远端时间间隔:" + timeInterVal + " 当前时间：" + adInterTime);
                return canShowFlag ? null : "1";
            }
            return "2";
        }

        public string CheckIfReachFirstNTimes(IvType _type)
        {
            bool canShow = false;

            string reachKey = "ivReachTimes" + _type.ToString();
            //  取出本地插屏播放的次数
            int reachTime = CorePrefs.GetInt(reachKey, 0);
            // 是否到达可播放时机
            if (reachTime >= GetInterRules(_type).Skip_Level)
            {
                canShow = true;
            }

            reachTime += 1;
            CorePrefs.SetInt(reachKey, reachTime);
            var msg = "前N次不展示插屏,本地次数:" + reachTime + "远端参数：" + GetInterRules(_type).Skip_Level +
                    "IV Type:" + _type;
            CoreLogUtils.Debug(" [ads]" + msg + "canshow:" + canShow);
            if (!canShow)
            {
                return msg;
            }

            return null;
        }

        public string IfFirstCheckPlayerPrefs(IvType _type)
        {
            string key = "first_Show_iv" + _type.ToString();
            string values = CorePrefs.GetString(key, "");
            if (values.Length <= 0)
            {
                int timeInterVal = GetInterRules(_type).Interval_Time;
                if (_videoStartTime == 0)
                {
                    CorePrefs.SetString(key, key);
                    return null;
                }

                var adInterTime = Time.realtimeSinceStartup - _videoStartTime;
                bool canShowFlag = adInterTime >= timeInterVal;
                CoreLogUtils.Debug(" [ads]" + "到达N +1次了,插屏类型:" + _type + " 远端时间间隔:" + timeInterVal + " 当前时间：" + adInterTime);
                if (canShowFlag)
                {
                    CorePrefs.SetString(key, key);
                    return null;
                }
                else
                {
                    return "1";
                }
            }
            else
            {
                return "2";
            }
        }

        public ConfigItem GetInterRules(IvType index)
        {
            string jsonData = CoreFirebaseMgr.Instance.GetConfigStr("IV_Switch", INTER_RULES_DEFAULT_VALUE);
            CoreLogUtils.Debug($"CoreLite IV rule jsondata: {jsonData}");
            
            ConfigItem CreateDefaultConfig()
            {
                return new ConfigItem
                {
                    Type = 1,
                    Over_Level = 0,
                    Interval_Time = 0,
                    Skip_Level = 1
                };
            }

            try
            {
                ConfigItem[] configItems = CoreDataUtils.FromJsonArray<ConfigItem>(jsonData);
                return configItems.FirstOrDefault(item => item.Type == (int)index) ?? CreateDefaultConfig();
            }
            catch (Exception ex)
            {
                CoreLogUtils.Debug($"解析IV规则配置失败: {ex.Message}");
                return CreateDefaultConfig();
            }
        }

        private static string INTER_RULES_DEFAULT_VALUE = "[{\"Type\":1,\"Over_Level\":0,\"Interval_Time\":0,\"Skip_Level\":1}]";

        [System.Serializable]
        public class ConfigItem
        {
            public int Type;
            public int Over_Level;
            public int Interval_Time;
            public int Skip_Level;
        }
}
}

