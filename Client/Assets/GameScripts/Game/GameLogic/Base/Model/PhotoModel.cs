using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Config.common;

namespace GameLogic
{
    /// <summary>
    /// 用于用户随机存储照片信息类
    /// </summary>
    public class PhotoModel 
    {
        //当前关卡所需的图片id,用于记录当前关卡随机出来的图片id
        public List<int> CurLevelPhotoIds;
        //用于记录当前活动的随机奖励信息
        public Dictionary<ActivityType,List<int>> AllActivityRandomIds;
        //用于记录当前活动的下一波随机奖励信息
        public Dictionary<ActivityType, List<int>> NextActivityRandomIds;
        //用于记录当前活动上一轮的随机奖励信息
        public Dictionary<ActivityType, List<int>> LastActivityRandomIds;
        public PhotoModel()
        {
            
        }

        public PhotoModel(bool isNew)
        {
            if (isNew)
            {
                CurLevelPhotoIds = new List<int>();
                AllActivityRandomIds = new Dictionary<ActivityType, List<int>>();
                NextActivityRandomIds = new Dictionary<ActivityType, List<int>>();
                LastActivityRandomIds = new Dictionary<ActivityType, List<int>>();
            }
        }
    }
}
