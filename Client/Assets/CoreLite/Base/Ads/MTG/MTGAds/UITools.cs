using System;
using CoreLite;


namespace MTGAds
{
    public class UITools
    {
        public static void RunUnityUI(Action action)
        {
            Loom.QueueOnMainThread(obj => action.Invoke(), "");
        }
    }
}