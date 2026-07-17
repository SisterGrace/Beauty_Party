using System;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using UnityEngine;

namespace GameLogic
{
    public partial class UserModel
    {
        public int Id;
        [ES3Serializable]
        public int Diamond;//钻石数量
        [ES3Serializable]
        public int Level;//关卡
        [ES3Serializable]
        public bool IsLevelloop;//关卡循环
        [ES3Serializable]
        public bool IsLevelRefresh;//关卡刷新
        [ES3Serializable] 
        public int LevelloopId_1;//当前关卡大循环id
        [ES3Serializable] 
        public int LevelloopId_2;//当前关卡小循环id
        
        [ES3Serializable] public Dictionary<int,int> ActivtyItemUnlockDic;//活动道具解锁
        [ES3Serializable] public int CurLevelIsAdRefresh;//当前关卡广告刷新状态

        [ES3Serializable] public int VIPLevel;//vip等级,0-普通用户，1-普通版，2-豪华版，3-终身版

        [ES3Serializable] public long VIPEndTime;//vip到期时间戳。

        [ES3Serializable] public int VIPFreeDownloadCount;//vip每日免费下载次数

        [ES3Serializable] public long VIPFreeDownloadCountRefreshTime;//vip免费下载次数刷新时间戳

        #region 道具、关卡宝箱
        [ES3Serializable]
        public List<PropInfo> Props;
        [ES3Serializable]
        public int CurProgress;//当前关卡进度
        #endregion

        #region 商店
        //当前购买的礼包id
        [ES3Serializable] 
        public long FreeGetGoldTime;//免费领取钻石时间
        [ES3Serializable] 
        public List<int> CurShopPackId;//当前商城购买的礼包id
        [ES3Serializable]
        public Dictionary<int, List<int>> ShopPhotoConfigDic;//商城照片购买配置字典，key=Giftid，value=对应奖励的ID和OtherID列表
        [ES3Serializable]
        public Dictionary<int, List<int>> ShopPhotoSeenConfigDic;//已见的照片商品配置，key=GiftId，value=photoIds
        [ES3Serializable]
        public List<int> ShopDynamicPhotoIds;//当前动态展示的照片ID列表（当前批次）
        [ES3Serializable] public Dictionary<int, int> ShopDynamicPhotoUnlockDic;//商城动态展示的照片解锁次数存档
        #endregion
        #region 活动礼包
        [ES3Serializable]
        public bool AdFree;//永久免广告
        [ES3Serializable] 
        public bool ISOpenDreampass;//是否开通梦想通行证
        [ES3Serializable]
        public long NoAdsEndTime; //去广告到期时间
        public Dictionary<ActivityType,ActityData> ActivityDataDic = new Dictionary<ActivityType, ActityData>();
        //活动是否永久结束
        public Dictionary<ActivityType, bool> ActivityEndDic = new Dictionary<ActivityType, bool>();
        [ES3Serializable]
        public List<ActivityType> RemoveActivityList = new List<ActivityType>();
        #endregion
        
        #region SDK
        public long CurDateTime;//当前零点时间戳
        //每日游戏局数
        public int DayGameCount;
        //每日激励获得生命次数
        public int AdLifeCount;
        //是否购买新手礼包
        public bool IsBuyStarterPack;
        //生涯抽卡次数
        public int LuckyDrawCount;
        
        #endregion

        #region 相册

        public int CurUserType;//当前用户类型0-A，1-B
        //相册数据
        public List<UserPhotoData> UserPhotosData;
        //当前使用的图片Id
        public int CurUserPhotoId;

        /// <summary>
        /// 当日看广告获取卡包次数.
        /// </summary>
        public Dictionary<int, int> PhotoPackAdTimes;

        /// <summary>
        /// 刷新每日看广告获取卡包次数的时间戳.
        /// </summary>
        public long RefreshPhotoPackAdTimesTimestamp;

        /// <summary>
        /// 每个卡包的保底计数（未出L4的连续抽卡次数）
        /// Key: packId, Value: 当前连续未出最高等级次数
        /// </summary>
        [ES3Serializable]
        public Dictionary<int, int> GachaPityCounter;

        #endregion
        
        public UserModel()
        {
            
        }

        public UserModel(bool isNew)
        {
            if (isNew)
            {
                Id = 0;
                Diamond = 0;
                Level = 1;
                IsLevelloop = false;
                LevelloopId_1 = -1;
                LevelloopId_2 = -1;
                Props = new List<PropInfo>();
                CurLevelIsAdRefresh = 0;
                var propsCfg = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.DataList;
                for (int i = 0; i < propsCfg.Count; i++)
                {
                    bool isUnlcok = Level >= propsCfg[i].UnLcokLevel;
                    Props.Add(new PropInfo(propsCfg[i].Type,isUnlcok,isUnlcok?1:0,false));
                }
              
                CurProgress = 1;
                CurShopPackId = new List<int>();
                ShopPhotoConfigDic = new Dictionary<int, List<int>>();
                ShopPhotoSeenConfigDic = new Dictionary<int, List<int>>();
                ShopDynamicPhotoIds = new List<int>();
                CurDateTime = TimeUtil.GetTimeStampToZero();
                ISOpenDreampass = false;
                ActivtyItemUnlockDic = new Dictionary<int, int>();
                ShopDynamicPhotoUnlockDic = new Dictionary<int, int>();
                UserPhotosData = new List<UserPhotoData>();
                VIPLevel = 0;
                CurUserType = 0;
                CurUserPhotoId =CurUserType==0?ConfigLoader.Constant.ARootPicID: ConfigLoader.Constant.BRootPicID;
                IsBuyStarterPack = false;
                LuckyDrawCount = 0;
            }
        }
    }
}
