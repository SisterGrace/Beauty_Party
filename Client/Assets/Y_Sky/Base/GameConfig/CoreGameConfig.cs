using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{
    public class CoreGameConfig : MonoBehaviour
    {
        public static void GameConfig(Action<AppStatus,string> action)
        {
            CoreServerMgr.Instance.Post(CoreConfigMgr.Instance.GetGameConfigUrl(), null, (int code, string msg, GameConfigInfo info) =>
            {
                if (code == 0)
                {
                    action?.Invoke((AppStatus)info.app_status, msg);
                }
                else
                {
                    action?.Invoke(AppStatus.Other, msg);
                }
            });
        }

        [System.Serializable]
        public struct GameConfigInfo
        {
            public int app_status;
        }
        
        /// <summary>
        /// App状态枚举（与后端返回的整数状态码对应）
        /// </summary>
        public enum AppStatus
        {
            // 解析失败？
            Other = 0,

            /// <summary>
            /// 未上线（对应状态码 1）
            /// </summary>
            NotLaunched = 1,

            /// <summary>
            /// 已上架（对应状态码 2）
            /// </summary>
            Launched = 2,

            /// <summary>
            /// 被下架（对应状态码 3）
            /// </summary>
            Removed = 3,

            /// <summary>
            /// 不适用（对应状态码 4）
            /// </summary>
            NotApplicable = 4
        }
    }
}

