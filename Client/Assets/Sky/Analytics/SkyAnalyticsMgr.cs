using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Sky
{

    public enum BlueSkyLogType
    {
        GA = 1,
        Umeng = 1 << 1,
        Adjust = 1 << 2,
        Reyun = 1 << 3,
        Firebase = 1 << 4,
        BlueSkySdk = 1 << 5,

    }
    public class SkyAnalyticsMgr
    {
        static public void init()
        {
            Sky.SkyGAMgr.Instance.Init();
        }
        static BlueSkyLogType _logType = BlueSkyLogType.GA;
        static public void DesignEvent(string sort, string type, string position, bool onlyFirst = false, string extra = "")
        {
            if (onlyFirst)
            {
                if (CryptoPrefs.HasKey(GetString(sort, type, position, extra)))
                {
                    LogTool.DebugLog(GetString(sort, type, position, extra) + "  not first ", LogTool.Color.lightblue);
                    return;
                }
                else
                {
                    CryptoPrefs.SetInt(GetString(sort, type, position, extra), 1);
                }
            }
            LogTool.DebugLog(" _logType = " + _logType + "  " + GetString(sort, type, position, extra), LogTool.Color.lightblue);
            if ((_logType & BlueSkyLogType.GA) > 0)
            {
                string str = GetString(sort, type, position, extra);
                if (!string.IsNullOrEmpty(str))
                    Sky.SkyGAEvents.TrackEvent(str);
            }
        }
        static string GetString(string sort, string type, string position, string extra = "")
        {
            string str;
            if (string.IsNullOrEmpty(extra))
            {
                str = string.Format("{0}:{1}:{2}", sort, type, position);
            }
            else
            {
                str = string.Format("{0}:{1}:{2}:{3}", sort, type, position, extra);
            }
            return str;
        }
    }
}





