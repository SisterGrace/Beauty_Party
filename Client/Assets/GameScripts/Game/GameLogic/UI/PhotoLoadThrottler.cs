using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 图片加载节流器：按调用顺序排队，每帧最多加载 N 张。
    /// 内部会先等几帧，让 UI 布局稳定后再开始出队。
    /// </summary>
    public static class PhotoLoadThrottler
    {
        private const int PER_FRAME = 3;
        private static int STARTUP_DELAY_FRAMES = 3;   // 等布局稳定
        private static bool first = true;

        private static readonly Queue<(int photoId, Item_PhotoLoad target)> _queue = new();
        private static bool _running = false;

        public static void Reset()
        {
            _queue.Clear();
        }

        public static void RequestLoad(int photoId, Item_PhotoLoad target)
        {
            if (target == null) return;
            _queue.Enqueue((photoId, target));

            if (!_running)
            {
                _running = true;
                Process().Forget();
            }
        }

        private static async UniTaskVoid Process() 
        {
            if(first)
            {
                first = false;
            }
            else
            {
                STARTUP_DELAY_FRAMES = 0;
            }
            // 等布局稳定
            for (int i = 0; i < STARTUP_DELAY_FRAMES; i++)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            int count = 0;
            int frame = -1;

            while (_queue.Count > 0)
            {
                if (frame != Time.frameCount)
                {
                    frame = Time.frameCount;
                    count = 0;
                }

                if (count >= PER_FRAME)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    continue;
                }

                var (photoId, target) = _queue.Dequeue();
                if (target != null) target.LoadUIImg(photoId);
                count++;
            }

            _running = false;
        }
    }
}