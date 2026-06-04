using System;
using System.Collections.Generic;
using Config.common;
using Config.config;
using SimpleJSON;
using TK_Engine;

namespace GameLogic
{
    /// <summary>
    /// 远程配置覆盖工具。
    /// 1. 活动奖励照片：遍历有 PhotoRemoteKey 的活动，精准定位 CfgReward 并替换 Photo / PhotoGroup 照片数据。
    /// 2. 关卡照片：覆盖 LevelPhotosA / LevelPhotosB。
    /// 3. IAP→IAA：记录需要从内购切换为看广告模式的商品 ID 集合，供 RechargeController 在运行时判断。
    ///
    /// 活动奖励 JSON 示例：
    /// [
    ///   {
    ///     "rewardId": 101,
    ///     "photo": { "id": 5001, "otherId": 6001 },
    ///     "photoGroup": { "aPhotos": [3001, 3002], "bPhotos": [4001, 4002] }
    ///   },
    ///   {
    ///     "rewardId": 102,
    ///     "photo": { "id": 7001, "otherId": 8001 }
    ///   }
    /// ]
    ///
    /// 关卡照片 JSON 示例（通用 key "level_photo_config"）：
    /// {
    ///   "5":  { "levelPhotosA": [1001, 1002, 1003], "levelPhotosB": [2001, 2002, 2003] },
    ///   "10": { "levelPhotosA": [4001, 4002] }
    /// }
    ///
    /// IAP→IAA JSON 示例（通用 key "iap_to_iaa_config"）：
    /// [1001, 1002, 1005]
    /// </summary>
    public static class ActivityPhotoRemoteHelper
    {


        private static readonly HashSet<int> _iaaOverrideIds = new HashSet<int>();

        /// <summary>
        /// 判断某个 CfgRecharge 是否被远程配置切换为 IAA 模式
        /// </summary>
        public static bool IsRemoteIAAOverride(int rechargeId) => _iaaOverrideIds.Contains(rechargeId);

        public static void ApplyAllOverrides(string levelPhotoKey, string iapToIAAKey)
        {
            ApplyPhotoRewardOverrides();
            ApplyLevelPhotoOverrides(levelPhotoKey);
            ApplyIAPToIAAOverrides(iapToIAAKey);
        }

        #region 活动奖励照片替换

        public static void ApplyPhotoRewardOverrides()
        {
            var activityList = ConfigLoader.Instance.Tables.TbCfgActivity.DataList;
            foreach (var activity in activityList)
            {
                if (string.IsNullOrEmpty(activity.PhotoRemoteKey))
                    continue;

                string json = SDKManager.GetRemoteConfigString(activity.PhotoRemoteKey, "");
                if (string.IsNullOrEmpty(json))
                    continue;

                var root = JSON.Parse(json);
                if (root == null)
                    continue;

                var items = root.IsArray ? root.AsArray : new JSONArray { [0] = root };

                foreach (JSONNode item in items)
                {
                    int rewardId = item["rewardId"].AsInt;
                    var cfgReward = ConfigLoader.Instance.Tables.TbCfgReward.GetOrDefault(rewardId);
                    if (cfgReward == null)
                    {
                        Log.Warning($"活动 {activity.ActType} 远程配置 rewardId={rewardId} 在奖励表中不存在");
                        continue;
                    }

                    OverridePhotoRewards(cfgReward.Reward, item);
                    OverridePhotoGroupRewards(cfgReward.Reward, item);

                    Log.Info($"活动 {activity.ActType} 照片奖励已从远程配置替换，key={activity.PhotoRemoteKey}, rewardId={rewardId}");
                }
            }
        }

        /// <summary>
        /// 直接传入 JSON 字符串执行照片奖励覆盖，用于 GM 测试。
        /// </summary>
        public static void ApplyPhotoRewardOverridesFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var root = JSON.Parse(json);
            if (root == null)
            {
                Log.Error("[PhotoRemote] JSON 解析失败，请检查格式");
                return;
            }

            var items = root.IsArray ? root.AsArray : new JSONArray { [0] = root };

            foreach (JSONNode item in items)
            {
                int rewardId = item["rewardId"].AsInt;
                var cfgReward = ConfigLoader.Instance.Tables.TbCfgReward.GetOrDefault(rewardId);
                if (cfgReward == null)
                {
                    Log.Warning($"[PhotoRemote] rewardId={rewardId} 在奖励表中不存在");
                    continue;
                }

                OverridePhotoRewards(cfgReward.Reward, item);
                OverridePhotoGroupRewards(cfgReward.Reward, item);

                Log.Info($"[PhotoRemote] rewardId={rewardId} 照片奖励已从测试JSON替换");
            }
        }

        private static void OverridePhotoRewards(List<TBRewardData> rewardList, JSONNode root)
        {
            var photoNode = root["photo"];
            if (photoNode == null || photoNode.IsNull)
                return;

            foreach (var reward in rewardList)
            {
                if (reward.Type != ERewardType.Photo)
                    continue;

                if (photoNode["id"] != null && !photoNode["id"].IsNull)
                    reward.OverridePhotoId(photoNode["id"].AsInt);

                if (photoNode["otherId"] != null && !photoNode["otherId"].IsNull)
                    reward.OverrideOtherId(photoNode["otherId"].AsInt);
            }
        }

        private static void OverridePhotoGroupRewards(List<TBRewardData> rewardList, JSONNode root)
        {
            var groupNode = root["photoGroup"];
            if (groupNode == null || groupNode.IsNull)
                return;

            foreach (var reward in rewardList)
            {
                if (reward.Type != ERewardType.PhotoGroup)
                    continue;

                var groupCfg = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.GetOrDefault(reward.ID);
                if (groupCfg == null)
                {
                    Log.Warning($"奖励中 PhotoGroup id={reward.ID} 在照片组表中不存在");
                    continue;
                }

                if (groupNode["aPhotos"] != null && !groupNode["aPhotos"].IsNull)
                {
                    var newList = new List<int>();
                    foreach (var item in groupNode["aPhotos"].AsArray)
                        newList.Add(item.Value.AsInt);
                    groupCfg.OverrideAPhotoRews(newList);
                }

                if (groupNode["bPhotos"] != null && !groupNode["bPhotos"].IsNull)
                {
                    var newList = new List<int>();
                    foreach (var item in groupNode["bPhotos"].AsArray)
                        newList.Add(item.Value.AsInt);
                    groupCfg.OverrideBPhotoRews(newList);
                }
            }
        }

        #endregion

        #region 关卡照片替换


        public static void ApplyLevelPhotoOverrides(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            string json = SDKManager.GetRemoteConfigString(key, "");
            if (string.IsNullOrEmpty(json))
                return;

            var root = JSON.Parse(json);
            if (root == null)
                return;

            foreach (var kvp in root.AsObject)
            {
                if (!int.TryParse(kvp.Key, out int levelId))
                    continue;

                var level = ConfigLoader.Instance.Tables.TbCfgLevelInfo.GetOrDefault(levelId);
                if (level == null)
                {
                    Log.Warning($"远程配置关卡 id={levelId} 在关卡表中不存在");
                    continue;
                }

                var data = kvp.Value;

                if (data["levelPhotosA"] != null && !data["levelPhotosA"].IsNull)
                {
                    var newList = new List<int>();
                    foreach (var item in data["levelPhotosA"].AsArray)
                        newList.Add(item.Value.AsInt);
                    level.OverrideLevelPhotosA(newList);
                }

                if (data["levelPhotosB"] != null && !data["levelPhotosB"].IsNull)
                {
                    var newList = new List<int>();
                    foreach (var item in data["levelPhotosB"].AsArray)
                        newList.Add(item.Value.AsInt);
                    level.OverrideLevelPhotosB(newList);
                }

                Log.Info($"关卡 {levelId} 照片已从远程配置替换");
            }
        }

        #endregion

        #region IAP→IAA 切换

        /// <summary>
        /// 从远程配置获取需要切换为 IAA 的商品 ID 列表，仅存储到 _iaaOverrideIds 集合中，
        /// 不修改 CfgRecharge 配置数据。RechargeController 在运行时动态判断。
        /// </summary>
        private static void ApplyIAPToIAAOverrides(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            string json = SDKManager.GetRemoteConfigString(key, "");
            if (string.IsNullOrEmpty(json)) return;

            JSONNode root;
            try { root = JSON.Parse(json); }
            catch (Exception e)
            {
                Log.Warning($"IAP→IAA 远程 JSON 解析失败: {e.Message}");
                return;
            }

            if (root == null || !root.IsArray) return;

            _iaaOverrideIds.Clear();

            foreach (var item in root.AsArray)
            {
                int rechargeId = item.Value.AsInt;
                _iaaOverrideIds.Add(rechargeId);
                Log.Info($"IAP→IAA: rechargeId={rechargeId} 已标记为 IAA 模式");
            }
        }

        /// <summary>
        /// 直接传入 JSON 字符串执行 IAP→IAA 切换，用于 GM 测试。
        /// JSON 格式：[1001, 1002, 1005]
        /// </summary>
        public static void ApplyIAPToIAAOverridesFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            JSONNode root;
            try { root = JSON.Parse(json); }
            catch (Exception e)
            {
                Log.Error($"[IAPToIAA] JSON 解析失败: {e.Message}");
                return;
            }

            if (root == null || !root.IsArray)
            {
                Log.Error("[IAPToIAA] JSON 格式错误，需要数组格式，如 [1001, 1002]");
                return;
            }

            _iaaOverrideIds.Clear();

            foreach (var item in root.AsArray)
            {
                int rechargeId = item.Value.AsInt;
                _iaaOverrideIds.Add(rechargeId);
                Log.Info($"[IAPToIAA] rechargeId={rechargeId} 已标记为 IAA 模式");
            }

            Log.Info($"[IAPToIAA] 测试完成，共标记 {_iaaOverrideIds.Count} 个商品为 IAA 模式");
        }

        #endregion
    }
}
