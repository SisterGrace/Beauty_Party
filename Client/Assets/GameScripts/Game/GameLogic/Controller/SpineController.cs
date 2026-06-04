using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameBase;
using Spine.Unity;
using static Spine.AnimationState;
using System;
using TK_Engine;

namespace GameLogic
{
    public class SpineController : Singleton<SpineController>
    {
        /// <summary>
        /// 设置Spine动画数据
        /// </summary>
        /// <param name="spine"></param>
        /// <param name="dataName"></param>
        public static void SetSpineData(SkeletonAnimation spine, string dataName)
        {
            SkeletonDataAsset sda = GameModule.Resource.LoadAsset<SkeletonDataAsset>(dataName);
            if (sda != null)
            {
                spine.skeletonDataAsset = sda;
                spine.Initialize(true);
            }
            else
            {
                Log.Error("Spine data not found: " + dataName);
            }
        }
        /// <summary>
        /// 设置Spine动画数据（UI）
        /// </summary>
        /// <param name="spine"></param>
        /// <param name="dataName"></param>
        public static void SetSpineData(SkeletonGraphic spine, string dataName)
        {
            SkeletonDataAsset sda = GameModule.Resource.LoadAsset<SkeletonDataAsset>(dataName);
            if (sda != null)
            {
                spine.skeletonDataAsset = sda;
                spine.Initialize(true);
            }
            else
            {
                Log.Error("Spine data not found: " + dataName);
            }
        }
        /// <summary>
        /// 播放Spine动画
        /// </summary>
        /// <param name="spine"></param>
        /// <param name="trackIndex"></param>
        /// <param name="animName"></param>
        /// <param name="loop"></param>
        /// <param name="skinName"></param>
        /// <param name="callBack"></param>
        public static void PlayAnim(SkeletonAnimation spine, int trackIndex, string animName, bool loop, string skinName = "", Action callBack = null)
        {
            Spine.Skeleton skeleton = spine.skeleton;
            Spine.AnimationState state = spine.AnimationState;
 
            if (spine != null)
            {
                if (!string.IsNullOrEmpty(skinName))
                    skeleton.SetSkin(skinName);
 
                state.SetAnimation(trackIndex, animName, loop);
            }
 
            TrackEntryDelegate animCallBack = null;
 
            animCallBack = delegate
            {
                if (callBack != null)
                {
                    callBack.Invoke();
                }
 
                state.Complete -= animCallBack;
 
                animCallBack = null;
            };
 
            state.Complete += animCallBack;
        }

        /// <summary>
        /// 播放Spine动画（UI）
        /// </summary>
        /// <param name="spine"></param>
        /// <param name="trackIndex"></param>
        /// <param name="animName"></param>
        /// <param name="loop"></param>
        /// <param name="skinName"></param>
        /// <param name="callBack"></param>
        public static void PlayAnim(SkeletonGraphic spine, int trackIndex, string animName, bool loop, string skinName = "",Action callBack = null)
        {
            Spine.Skeleton skeleton = spine.Skeleton;
            Spine.AnimationState state = spine.AnimationState;
 
            if (spine != null)
            {
                if (!string.IsNullOrEmpty(skinName))
                    skeleton.SetSkin(skinName);
 
                state.SetAnimation(trackIndex, animName, loop);
            }
 
            TrackEntryDelegate animCallBack = null;
 
            animCallBack = delegate
            {
                if (callBack != null)
                {
                    callBack.Invoke();
                }
 
                state.Complete -= animCallBack;
 
                animCallBack = null;
            };
 
            state.Complete += animCallBack;
        }

        /// <summary>
        /// 停止Spine动画
        /// </summary>
        /// <param name="spine"></param>
        /// <param name="trackIndex"></param>
        /// <param name="mixDuration"></param>
        public static void StopAnim(SkeletonAnimation spine, int trackIndex, float mixDuration)
        {
            Spine.AnimationState state = spine.AnimationState;
 
            state.SetEmptyAnimation(trackIndex, mixDuration);
        }
 
        /// <summary>
        /// 停止Spine动画（UI）
        /// </summary>
        /// <param name="spine"></param>
        /// <param name="trackIndex"></param>
        /// <param name="mixDuration"></param>
        public static void StopAnim(SkeletonGraphic spine, int trackIndex, float mixDuration)
        {
            Spine.AnimationState state = spine.AnimationState;
 
            state.SetEmptyAnimation(trackIndex, mixDuration);
        }
        
    }
}
