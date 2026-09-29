using Config.common;
using Config.config;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Purchasing;
using Y_Base;

namespace GameLogic
{
    /// <summary>
    /// 活动照片预加载管理器（仅磁盘缓存，避免内存卡顿）
    /// </summary>
    public class ActivityPhotoPreloadManager
    {
        private static ActivityPhotoPreloadManager _instance;
        public static ActivityPhotoPreloadManager Instance => _instance ??= new ActivityPhotoPreloadManager();

        private Dictionary<ActivityType, bool> _preloadStatus = new Dictionary<ActivityType, bool>();
        private CancellationTokenSource _cts;

        /// <summary>
        /// 预加载所有活动页面的图片（按优先级顺序）
        /// </summary>
        public async UniTask PreloadAllActivitiesAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                Debug.Log("[ActivityPreload] 开始预加载所有活动图片（仅磁盘缓存）...");

                // 按优先级顺序预加载
                await PreloadShopPhotosAsync();           // 🔴 福利中心（最高优先级）
                //await PreloadGachaPhotosAsync();          // 🟠 抽卡
                //await PreloadEndlessTreasureAsync();      // 🟡 无尽寻宝
                //await PreloadDreamPassAsync();            // 🟡 梦想通行证

                Debug.Log("[ActivityPreload] 所有活动图片预加载完成！");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ActivityPreload] 预加载被取消");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActivityPreload] 预加载失败: {e.Message}");
            }
        }

        #region 各活动预加载实现

        /// <summary>
        /// 1. 福利中心（Panel_Shop）
        /// </summary>
        public async UniTask PreloadShopPhotosAsync()
        {
            if (IsPreloaded(ActivityType.Shop)) return;

            try
            {
                Debug.Log("[ActivityPreload] 预加载福利中心图片...");


                var allShopCfgs = ConfigLoader.Instance.Tables.TbCfgShop.DataList;
                var photoCfgs = allShopCfgs.FindAll(x => x.GoodType == mProductType.PhotoPack || x.GoodType == mProductType.GiftPack);
                List<string> photoUrls = new List<string>();

                int AbType = UserController.Instance.GetCurUserType;

                foreach (var cfg in photoCfgs)
                {
                    var rechargeInfo = RechargeController.GetRechargeInfoByShop(cfg.Id);
                    if (rechargeInfo == null) continue;

                    var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId)?.Reward;
                    if (rewardList == null) continue;
                    List<int> photoIds = new List<int>();
                    foreach (var reward in rewardList)
                    {
                        if (reward.Type == ERewardType.Photo)
                        {
                            if (reward.ID > 0) photoIds.Add(reward.ID);
                            if (reward.OtherID > 0) photoIds.Add(reward.OtherID);
                        }
                        else if (reward.Type == ERewardType.PhotoGroup)
                        {
                            var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(reward.ID);
                            if(AbType!=1)
                            {
                                photoIds.AddRange(group.APhotoRews);
                            }else
                            {
                                photoIds.AddRange(group.BPhotoRews);
                            }
                        }
                    }
                    foreach (var photoId in photoIds)
                    {
                        int targetPhotoId =/* AbType == 1 ? photoId : */photoIds[0];
                        string url = PropsController.GetImgUrl(targetPhotoId);
                        if (!string.IsNullOrEmpty(url) && !photoUrls.Contains(url))
                        {
                            photoUrls.Add(url);
                        }
                    }
                }

                int count = await ImgUtil.PreloadToDiskBatchAsync(photoUrls, 3, _cts.Token);
                MarkPreloaded(ActivityType.Shop);
                Debug.Log($"[ActivityPreload] 福利中心预加载完成: {count}/{photoUrls.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActivityPreload] 福利中心预加载失败: {e.Message}");
            }
        }

        /// <summary>
        /// 2. 抽卡（Panel_Gacha）
        /// </summary>
        public async UniTask PreloadGachaPhotosAsync()
        {
            if (IsPreloaded(ActivityType.Gacha)) return;

            try
            {
                Debug.Log("[ActivityPreload] 预加载抽卡图片...");

                var gachaController = GachaController.Instance;
                if (gachaController == null) return;

                gachaController.InitPhotoPacks();
                var packList = gachaController.PhotoPackList;
                List<string> photoUrls = new List<string>();

                int AbType = UserController.Instance.GetCurUserType;

                foreach (var pack in packList)
                {
                    if (pack.PhotoIDs == null || pack.PhotoIDs.Count == 0) continue;

                    int targetPhotoId = AbType == 1 ? pack.PhotoIDs[1] : pack.PhotoIDs[0];
                    string url = PropsController.GetImgUrl(targetPhotoId);

                    if (!string.IsNullOrEmpty(url) && !photoUrls.Contains(url))
                    {
                        photoUrls.Add(url);
                    }
                }

                int count = await ImgUtil.PreloadToDiskBatchAsync(photoUrls, 3, _cts.Token);
                MarkPreloaded(ActivityType.Gacha);
                Debug.Log($"[ActivityPreload] 抽卡预加载完成: {count}/{photoUrls.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActivityPreload] 抽卡预加载失败: {e.Message}");
            }
        }

        /// <summary>
        /// 3. 无尽寻宝（Panel_EndlessTreasure）
        /// </summary>
        public async UniTask PreloadEndlessTreasureAsync()
        {
            if (IsPreloaded(ActivityType.UntoldTreasure)) return;

            try
            {
                Debug.Log("[ActivityPreload] 预加载无尽寻宝图片...");

                var allEndlessCfgs = ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.DataList;
                List<string> photoUrls = new List<string>();

                foreach (var cfg in allEndlessCfgs)
                {
                    var rechargeInfo = RechargeController.GetRechargeInfo(cfg.Id);
                    if (rechargeInfo == null) continue;

                    var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId)?.Reward;
                    if (rewardList == null) continue;

                    foreach (var reward in rewardList)
                    {
                        if (reward.Type == ERewardType.Photo || reward.Type == ERewardType.PhotoGroup)
                        {
                            int photoId = reward.ID;
                            string url = PropsController.GetImgUrl(photoId);
                            if (!string.IsNullOrEmpty(url) && !photoUrls.Contains(url))
                            {
                                photoUrls.Add(url);
                            }
                        }
                    }
                }

                int count = await ImgUtil.PreloadToDiskBatchAsync(photoUrls, 3, _cts.Token);
                MarkPreloaded(ActivityType.UntoldTreasure);
                Debug.Log($"[ActivityPreload] 无尽寻宝预加载完成: {count}/{photoUrls.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActivityPreload] 无尽寻宝预加载失败: {e.Message}");
            }
        }

        /// <summary>
        /// 4. 梦想通行证（Panel_DreamPass）
        /// </summary>
        public async UniTask PreloadDreamPassAsync()
        {
            if (IsPreloaded(ActivityType.DreamPass)) return;

            try
            {
                Debug.Log("[ActivityPreload] 预加载梦想通行证图片...");

                var allPassList = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
                List<string> photoUrls = new List<string>();
                for (int i = 0; i < allPassList.Count; i++)
                {
                    var Rwcfg = ConfigLoader.Instance.Tables.TbCfgReward.Get(allPassList[i].RewardId);
                    for (int j = 0; j < Rwcfg.Reward.Count; j++)
                    {
                        if (Rwcfg.Reward[j].Type == ERewardType.Photo)
                        {
                            string url = PropsController.GetImgUrl(Rwcfg.Reward[j].ID);
                            if (!string.IsNullOrEmpty(url) && !photoUrls.Contains(url))
                            {
                                photoUrls.Add(url);
                            }
                        }
                    }
                }

             /*   foreach (var cfg in allPassList)
                {
                    foreach (var rewardList in cfg.RewardList)
                    {
                        foreach (var reward in rewardList)
                        {
                            if (reward.Type == ERewardType.Photo || reward.Type == ERewardType.PhotoGroup)
                            {
                                string url = PropsController.GetImgUrl(reward.ID);
                                if (!string.IsNullOrEmpty(url) && !photoUrls.Contains(url))
                                {
                                    photoUrls.Add(url);
                                }
                            }
                        }
                    }
                }*/

                int count = await ImgUtil.PreloadToDiskBatchAsync(photoUrls, 3, _cts.Token);
                MarkPreloaded(ActivityType.DreamPass);
                Debug.Log($"[ActivityPreload] 梦想通行证预加载完成: {count}/{photoUrls.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActivityPreload] 梦想通行证预加载失败: {e.Message}");
            }
        }

        #endregion

        #region 辅助方法

        private bool IsPreloaded(ActivityType type)
        {
            return _preloadStatus.ContainsKey(type) && _preloadStatus[type];
        }

        private void MarkPreloaded(ActivityType type)
        {
            _preloadStatus[type] = true;
        }

        public void CancelPreload()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void ResetPreloadStatus()
        {
            _preloadStatus.Clear();
        }

        #endregion
    }
}
