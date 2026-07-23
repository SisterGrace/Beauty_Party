using UnityEngine;
using TK_Engine;
using System.Collections.Generic;

namespace GameLogic
{
    public class AudioManager
    {
        // 记录每个 AudioType 是否处于播放状态（仅用于 Music 等，简单场景）
        private static Dictionary<TK_Engine.AudioType, bool> _playingStatus = new Dictionary<TK_Engine.AudioType, bool>();

        public static void Play(TK_Engine.AudioType type, string path, bool bAsync = false, bool bLoop = false)
        {
            GameModule.Audio.Play(type, path, bLoop, 1.0f, bAsync, true);
            // 标记为播放中（对于 Music 类型，假设一定成功）
            if (type == TK_Engine.AudioType.Music)
                _playingStatus[type] = true;
        }

        public static void Stop(TK_Engine.AudioType type, bool fadeout)
        {
            GameModule.Audio.Stop(type, fadeout);
            // 标记为停止（注意：fadeout 期间实际仍在播放，但此处立即标记为 false）
            if (type == TK_Engine.AudioType.Music)
                _playingStatus[type] = false;
        }

        public static bool IsPlaying(TK_Engine.AudioType type)
        {
            if (_playingStatus.TryGetValue(type, out bool playing))
                return playing;
            return false; // 默认未播放
        }
    }
}