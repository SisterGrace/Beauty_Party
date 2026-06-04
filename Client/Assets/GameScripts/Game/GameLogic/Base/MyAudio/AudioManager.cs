using UnityEngine;
using TK_Engine;

namespace GameLogic
{
    public class AudioManager 
    {
        /// <summary>
        /// 播放，如果超过最大发声数采用fadeout的方式复用最久播放的AudioSource。
        /// </summary>
        /// <param name="type">声音类型</param>
        /// <param name="path">声音文件路径</param>
        /// <param name="bLoop">是否循环播放</param>>
        public static void Play(TK_Engine.AudioType type, string path,bool bAsync = false, bool bLoop = false)
        {
            GameModule.Audio.Play(type, path, bLoop, 1.0f, bAsync, true);
        }

        public static void Stop(TK_Engine.AudioType type, bool fadeout)
        {
            GameModule.Audio.Stop(type, fadeout);
        }
    }
}
