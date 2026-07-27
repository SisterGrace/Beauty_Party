
using UnityEngine;
using System.Runtime.CompilerServices;
namespace Sky
{
    public class LogTool : MonoBehaviour
    {
        static bool logFlg = false;
        public enum Color
        {
            none,
            red,
            yellow,
            green,
            blue,
            lightblue,
            purple,
            silver,
        }
        static string[] m_colorS = { "{0}", "<color=#ff0000>{0}</color>", "<color=#ffff00ff>{0}</color>", "<color=#008000ff>{0}</color>", "<color=#0000ffff>{0}</color>", "<color=#add8e6ff>{0}</color>",
      "<color=#800080ff>{0}</color>","<color=#c0c0c0ff>{0}</color>","{0}" };

        public static void Init(bool enableFullLog)
        {
            logFlg = enableFullLog;
            if (enableFullLog)
            {
                Debug.unityLogger.logEnabled = true;
                Debug.unityLogger.filterLogType = LogType.Log;   // 显示所有
            }
            else
            {
                Debug.unityLogger.logEnabled = true;             // 保持启用，只过滤级别
                Debug.unityLogger.filterLogType = LogType.Error; // 仅显示 Error
            }
        }

        public static void DebugLog(string log, Color color = Color.none, bool showPath = false, [CallerFilePath] string path = null, [CallerMemberName] string name = null)
        {
            if (logFlg == false)
            {
                return;
            }
            string colorStr = m_colorS[(int)color];
            if(showPath==true)
            {
                string[] str = null;
                if (path != null)
                {
                    str = path.Split('/');
                }
                if (str != null)
                {
                    Debug.Log(string.Format(colorStr, "[" + str[str.Length - 1] + "_" + name + "]:" + log));
                }
                else
                {
                    Debug.Log(string.Format(colorStr, "[" + path + "_" + name + "]:" + log));
                }
                str = null;
            }
            else
            {
                Debug.Log(string.Format(colorStr, log));
            }
            
        }
        public static void Error(string log, [CallerFilePath] string path = null, [CallerMemberName] string name = null)
        {
            if (logFlg == false)
            {
                return;
            }
            string[] str = null;
            if (path != null)
            {
                str = path.Split('/');
            }
            if (str != null)
            {
                Debug.LogError("[" + str[str.Length - 1] + "_" + name + "]:" + log);
            }
            else
            {
                Debug.LogError("Error!!! [" + path + "_" + name + "]:" + log);
            }
            str = null;
        }



    }
}



