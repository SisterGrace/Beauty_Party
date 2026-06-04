using System.Collections;
using System.Collections.Generic;
using Config.config;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GameBase;
using Config.common;
using TK_Engine;

namespace GameLogic
{
    public class PhotoController : Singleton<PhotoController>
    {
        private PhotoModel _photoModel;
        public async UniTask InitPhoto()
        { 
            _photoModel = ES3.Load("PhotoModel", new PhotoModel(true));
            int curId = UserController.Instance.GetCurUserPhotoID;
            
            //预加载初始默认图
            await PhotoPreloading(curId);
            //预加载关卡图
            PhotoPreloading(false, true);
            //预加载通行证奖励图,需要加载36张图
            //ActivityPhotoPreloading(ActivityType.DreamPass);
        }
        public void Save()
        {
            ES3.Save("PhotoModel", _photoModel);
        }
        #region 关卡图预加载
        /// <summary>
        /// 设置关卡随机图
        /// </summary>
        /// <param name="isRefresh">是否刷新</param>
        /// <param name="refreshIndex">刷新指定索引的图，-1表示刷新全部，>=0表示只刷新该索引位置的图</param>
        public void SetLevelUsePhotoId(bool isRefresh = false, int refreshIndex = -1)
        {
            var levelInfo = LevelController.GetCurrentLevelInfo(); 
            int Ab = UserController.Instance.GetCurUserType;
            int needCount = levelInfo.Difficulty > 0 ? 1 : 3;
            //当此时所需的不同类型的图数量与当前所拥有的不同类型的图数量不一致时，需要刷新
            bool Correct = needCount!= GetCurLevelPhotoIds.Count;
            
            // 独立刷新单张图的逻辑（Boss关卡不支持单独刷新）
            if (isRefresh && refreshIndex >= 0 && needCount != 1 && !Correct && GetCurLevelPhotoIds != null && GetCurLevelPhotoIds.Count > refreshIndex)
            {
                RefreshSinglePhotoId(refreshIndex, Ab);
                return;
            }
            
            if (isRefresh||Correct)
            {
                //清空
               SetCurLevelPhotoIds(null);
            }
            if(GetCurLevelPhotoIds!=null&& GetCurLevelPhotoIds.Count>0) return;
            
            List<int> Curphotoids = new List<int>();
            if (needCount == 1)//Boos关卡生成3号位的图
            {
                var saclelist = ConfigLoader.GetOnekindPhotoList((ConfigLoader.MyPhotoType)(3), Ab);
                var saclelist1 = saclelist. FindAll(a => !UserController.Instance.IsGetPhoto(a.ID));
                CfgPhotos cfg = null;
                if (saclelist1 != null && saclelist1.Count > 0)
                {
                    cfg = saclelist1.RandomFromAllList(false);
                }
                else
                {
                    //没有符合要求的图，就大列表随机取一个
                    cfg = saclelist.RandomFromAllList(false);
                }
                Curphotoids.Add(cfg.ID);
            }
            else
            {
                for (int i = 0; i < needCount; i++)
                {
                    var saclelist = ConfigLoader.GetOnekindPhotoList((ConfigLoader.MyPhotoType)(i + 1), Ab);
                    var saclelist1 = saclelist. FindAll(a => !UserController.Instance.IsGetPhoto(a.ID));
                    CfgPhotos cfg = null;
                    if (saclelist1 != null && saclelist1.Count > 0)
                    {
                        cfg = saclelist1.RandomFromAllList(false);
                    }
                    else
                    {
                        //没有符合要求的图，就大列表随机取一个
                        cfg = saclelist.RandomFromAllList(false);
                    }
                    Curphotoids.Add(cfg.ID);
                }
               
            }
            SetCurLevelPhotoIds(Curphotoids);
        }
        
        /// <summary>
        /// 独立刷新单张图的ID（仅用于普通关卡，Boss关卡不支持）
        /// </summary>
        /// <param name="refreshIndex">要刷新的索引</param>
        /// <param name="Ab">用户类型</param>
        private void RefreshSinglePhotoId(int refreshIndex, int Ab)
        {
            // 收集当前列表中其他位置已存在的ID（排除要刷新的索引位置）
            HashSet<int> existingIds = new HashSet<int>();
            for (int i = 0; i < GetCurLevelPhotoIds.Count; i++)
            {
                if (i != refreshIndex)
                {
                    existingIds.Add(GetCurLevelPhotoIds[i]);
                }
            }
            
            // 普通关卡按索引+1确定图片类型
            int photoType = refreshIndex + 1;
            var saclelist = ConfigLoader.GetOnekindPhotoList((ConfigLoader.MyPhotoType)photoType, Ab);
            
            // 筛选出：1.未获得的图 2.不在当前其他位置已存在的ID中
            var saclelist1 = saclelist.FindAll(a => !UserController.Instance.IsGetPhoto(a.ID) && !existingIds.Contains(a.ID));
            
            CfgPhotos cfg = null;
            if (saclelist1 != null && saclelist1.Count > 0)
            {
                cfg = saclelist1.RandomFromAllList(false);
            }
            else
            {
                // 没有符合要求的图，就从大列表中排除已存在ID后随机取一个
                var fallbackList = saclelist.FindAll(a => !existingIds.Contains(a.ID));
                if (fallbackList != null && fallbackList.Count > 0)
                {
                    cfg = fallbackList.RandomFromAllList(false);
                }
                else
                {
                    // 实在没有可选的，就从大列表随机取一个
                    cfg = saclelist.RandomFromAllList(false);
                }
            }
            
            // 更新指定索引位置的ID
            _photoModel.CurLevelPhotoIds[refreshIndex] = cfg.ID;
            Save();
        }
        
        /// <summary>
        /// 这是通过关卡配置设置的关卡图
        /// </summary>
        public void SetLevelCfgToPhotoId()
        {
            var levelInfo = LevelController.GetCurrentLevelInfo();
            int Ab = UserController.Instance.GetCurUserType;
            int needCount = levelInfo.Difficulty > 0 ? 1 : 3;
            int curLevelRefresh = UserController.Instance.CurAdLevelRefreshState;
            if (curLevelRefresh>0)//表示当前配置关卡已经刷新过了，就不用读取配置，直接使用当前的存储的ID
            {
                return;
            }
            else
            {
                SetCurLevelPhotoIds(null);
            }
            List<int> Curphotoids = new List<int>();
            if (needCount == 1) //Boos关卡生成3号位的图
            {
                var IdList = Ab == 0 ? levelInfo.LevelPhotosA : levelInfo.LevelPhotosB;
                if (IdList.Count > 0)
                {
                    Curphotoids.Add(IdList[0]);
                }
            }
            else
            {
                for (int i = 0; i < needCount; i++)
                {
                    var IdList = Ab == 0 ? levelInfo.LevelPhotosA : levelInfo.LevelPhotosB;
                    if (IdList.Count > i)
                    {
                        Curphotoids.Add(IdList[i]);
                    }
                }
            }
            SetCurLevelPhotoIds(Curphotoids);
        }
        //预加载图到内存中，没有的就网络下载
        public async UniTask  PhotoPreloading(bool isRefresh,bool isPreload)
        {
            if(_photoModel == null) return;
            var levelInfo = LevelController.GetCurrentLevelInfo(); 
            //判断是否随机关卡图
            bool isRandom = levelInfo.LevelPhotosA.Count==0&&levelInfo.LevelPhotosB.Count==0;
            if (isRandom)
            {
                SetLevelUsePhotoId(isRefresh);
            }
            else
            {
                //虽然是固定图，但是还是要判断是否需要刷新，如果用户刷新过，则使用用户刷新的ID，否则使用配置的ID
                SetLevelCfgToPhotoId();
            }
            if (isPreload)
            {
                var dataInfo = GetCurLevelPhotoIds;
                if (dataInfo == null || dataInfo.Count == 0) return;
                for (int i = 0; i < dataInfo.Count; i++)
                {
                    var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(dataInfo[i]);
                    string imgurl = PropsController.GetImgUrl(cfg.ImageID, cfg.ImageType, cfg.ABGroup);
                    await ImgUtil.GetTextureAsync(imgurl);
                }
            }
        }

        /// <summary>
        /// 预加载一些图片
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        public async UniTask PhotoPreloading(List<string> urls)
        {
            for (int i = 0; i < urls.Count; ++i)
            { 
                await ImgUtil.GetTexturesAsync(urls[i]);
            }
        }
        /// <summary>
        /// 通过一个id预加载图片
        /// </summary>
        /// <param name="id"></param>
        public async UniTask PhotoPreloading(int id)
        {
            var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(id); 
            string imgurl = PropsController.GetImgUrl(cfg.ImageID, cfg.ImageType, cfg.ABGroup);
            Log.Info($"预加载图片: {imgurl}");
            await ImgUtil.GetTextureAsync(imgurl);
        } 
        #endregion

        #region 关卡随机图相关
        public List<int> GetCurLevelPhotoIds
        {
            get=> _photoModel.CurLevelPhotoIds;
        }
        /// <summary>
        /// 设置当前关卡的照片id集合
        /// </summary>
        /// <param name="ids"></param>
        public void SetCurLevelPhotoIds(List<int> ids)
        {
            _photoModel.CurLevelPhotoIds.Clear();
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                _photoModel.CurLevelPhotoIds.Add(ids[i]);
            }
            Save();
        }
        #endregion

        #region 活动随机图相关
        //TODO:当玩家开启某活动的时候，如果该活动需要随机图奖励，则需要在这里设置
        //TODO:当玩家改变用户类型的时候，需要刷新活动随机图
        /// <summary>
        /// 获取当前活动的随机照片id集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<int> GetCurActivityPhotoIds(ActivityType type)
        {
            return _photoModel.AllActivityRandomIds.MyTryGetValue(type);
        }
        /// <summary>
        /// 获取上一次活动的随机照片id集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<int> GetLastActivityPhotoIds(ActivityType type)
        {
            return _photoModel.LastActivityRandomIds.MyTryGetValue(type);
        }
        /// <summary>
        /// 当前活动是否有随机图奖励
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsHaveRandomPhotos(ActivityType type)
        {
            return GetCurActivityPhotoIds(type)!= default&& GetCurActivityPhotoIds(type).Count>0;
        }

        public bool IsHaveLastRandomPhotos(ActivityType type)
        {
            return GetLastActivityPhotoIds(type) != null && GetLastActivityPhotoIds(type).Count > 0;
        }
        /// <summary>
        /// 保存当前活动的未获得的照片id集合
        /// </summary>
        /// <param name="type"></param>
        public void SaveLastGetPhoto(ActivityType type)
        {
            var curRandomIds = GetCurActivityPhotoIds(type);
            if (curRandomIds == default || curRandomIds.Count == 0) return;
            var lastRandomIds = GetLastActivityPhotoIds(type);
            //清空上一次活动的未获得的照片id集合
            if (lastRandomIds == default)
            {
                _photoModel.LastActivityRandomIds[type] = new List<int>();
            }
            else
            {
                lastRandomIds.Clear();
            }
            for (int i = 0; i < curRandomIds.Count; i++)
            {
                int curId = curRandomIds[i];
                _photoModel.LastActivityRandomIds[type].Add(curId);
            }
        }
        /// <summary>
        /// 清空上一次活动的未获得的照片id集合
        /// </summary>
        /// <param name="type"></param>
        public void ClearLastGetPhoto(ActivityType type)
        {
            if (IsHaveLastRandomPhotos(type))
            {
                _photoModel.LastActivityRandomIds[type].Clear();
                Save();
            }
        }
        /// <summary>
        /// 设置当前活动的随机照片id集合
        /// </summary>
        /// <param name="type">活动枚举</param>
        public void SetCurActivityPhotoIds(ActivityType type,bool isPreload=true)
        {
            var acCfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(type);
            int needCount = acCfg.RandomCount;
            if (needCount <= 0)
            {
                //没有随机就去预加载固定的随机奖励图
                ActivityPhotoPreloading(type);
                return;
            }
            int rewId = acCfg.RandomGroupID;
            //0-A，1-B
            var rewCfg = ConfigLoader.Instance.Tables.TbCfgReward.GetOrDefault(rewId);
            if (rewCfg == null)
            {
                Log.Error($"活动 {type} 随机奖励 {rewId} 在奖励表中不存在");
                return;
            }
            var curPhotoRew = rewCfg.Reward.Find(a => a.Type == ERewardType.PhotoGroup);
            if (curPhotoRew == null)
            {
                Log.Error($"活动 {type} 随机奖励组ID {rewId} 在照片奖励组表中不存在");
                return;
            }
            var group = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(curPhotoRew.ID);
            int ABgrounp = UserController.Instance.GetCurUserType;
            List<int> photoIds = new();
            if (ABgrounp == 0)
            {
                photoIds.AddRange(group.APhotoRews);
            }
            else
            {
                photoIds.AddRange(group.BPhotoRews);
            }
           
            if (needCount > photoIds.Count) //不满足随机数量要求，此时就不需要设置id，或者当前集合清空
            {
                if (_photoModel.AllActivityRandomIds.TryGetValue(type, out var list))
                {
                    list.Clear();
                }
                else
                {
                    _photoModel.AllActivityRandomIds[type] = new List<int>();
                }
            }
            else
            {
                if (!_photoModel.AllActivityRandomIds.TryGetValue(type, out var list))
                {
                    list = new List<int>();
                    _photoModel.AllActivityRandomIds[type] = list;
                }
                list.Clear();
                for (int i = 0; i < needCount; i++)
                {
                    var cfg = photoIds.RandomSwapFromList(true);
                    list.Add(cfg);
                }
            }
            Save();
            if (isPreload)
            {
                //预加载图片到内存中
                ActivityPhotoPreloading(type);
            }
        }
        //预加载活动图片到内存中
        public async UniTask ActivityPhotoPreloading(ActivityType type)
        {
            List<int> dataInfo;
            var acCfg = ConfigLoader.Instance.Tables.TbCfgActivity.GetByActType(type);
            int needCount = acCfg.RandomCount;
            if (needCount > 0)//如果时随机奖励直接读取随机列表
            {
                dataInfo = GetCurActivityPhotoIds(type);
            }
            else
            {
                dataInfo = GetOtherActivityPhotoIds(type);
            }
           
            if (dataInfo == default || dataInfo.Count == 0) return;
            for (int i = 0; i < dataInfo.Count; i++)
            {
                var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(dataInfo[i]);
                string imgurl = PropsController.GetImgUrl(cfg.ImageID, cfg.ImageType, cfg.ABGroup);
                await ImgUtil.GetTextureAsync(imgurl);
            }
        }
        /// <summary>
        /// 其它活动固定的照片id集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<int> GetOtherActivityPhotoIds(ActivityType type)
        {
            List<int> ids = new();
            List<TBRewardData> rewardDatas = new();
            switch (type)
            {
                case ActivityType.DreamPass:
                    var Dpcfglist = ConfigLoader.Instance.Tables.TbCfgDreamPass.DataList;
                    for (int i = 0; i < Dpcfglist.Count; i++)
                    {
                        var Rwcfg = ConfigLoader.Instance.Tables.TbCfgReward.Get(Dpcfglist[i].RewardId);
                        for (int j = 0; j < Rwcfg.Reward.Count; j++)
                        {
                            if (Rwcfg.Reward[j].Type == ERewardType.Photo)
                            {
                                ids.Add(Rwcfg.Reward[j].ID);
                                ids.Add(Rwcfg.Reward[j].OtherID);
                            }
                        }
                    }
                    break;
            }
            return ids;
        }
        //切换用户类型时，刷新活动随机图
        public void RefreshActivityRandomPhoto()
        {
            if(_photoModel == null) return;
            foreach (var item in _photoModel.AllActivityRandomIds)
            {
                if (item.Value.Count > 0&&(item.Key==ActivityType.UntoldTreasure))
                {
                    item.Value.Clear();
                    SetCurActivityPhotoIds(item.Key,true);
                }
            }
        }

        #endregion
    }
}
