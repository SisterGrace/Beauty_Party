using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using Y_Base;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    [Window(UILayer.UI)]
    public partial class Panel_Shop : UIWindow
    {
        //礼包列表
        private UIListWidget<Widget_OneProduct, OneProductInfo> _allProductList;
        //照片列表
        private UIListWidget<Widget_OneExcItem, OneExcItemInfo> _allExcList;
        //钻石列表(只包含付费的)
        private UIListWidget<Widget_OneDiaProduct, OneDiaProductInfo> _allDiaList;
        //钻石广告Item
        private Widget_OneDiaProduct _adDiaProduct;
        private List<CfgShop> _allShopCfgs;

        #region ===== 数据预热缓存（静态，跨实例共享）=====
        private static List<OneExcItemInfo> _cachedPhotoInfos;
        private static Dictionary<int, List<int>> _cachedAllCurrentConfigs;
        private static bool _cachedAllConfigPurchased;
        private static bool _dataReady;

        /// <summary>
        /// 数据预热：主界面稳定后调用，只做纯数据计算，不创建 UI
        /// </summary>
        public static async UniTask PreloadDataAsync()
        {
            if (_dataReady) return;

            try
            {
                // 让一帧再开始，避免阻塞调用方
                await UniTask.Yield();

                var allShopCfgs = ConfigLoader.Instance.Tables.TbCfgShop.DataList;
                _cachedPhotoInfos = BuildPhotoInfos(allShopCfgs);
                _dataReady = true;

                Debug.Log($"[Panel_Shop] 数据预热完成: {_cachedPhotoInfos.Count} 项");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Panel_Shop] 数据预热失败: {e.Message}");
            }
        }

        /// <summary>
        /// 缓存失效：购买、刷新、货币变化时调用
        /// </summary>
        public static void InvalidateCache()
        {
            _dataReady = false;
            _cachedPhotoInfos = null;
            _cachedAllCurrentConfigs = null;
            _cachedAllConfigPurchased = false;
        }

        /// <summary>
        /// 纯数据构建：从原 CreatPhotoList 抽出，不涉及 UI
        /// </summary>
        private static List<OneExcItemInfo> BuildPhotoInfos(List<CfgShop> allShopCfgs)
        {
            var photoCfgs = allShopCfgs.FindAll(x => x.GoodType == mProductType.PhotoPack);
            List<OneExcItemInfo> photoInfos = new List<OneExcItemInfo>();
            Dictionary<int, List<int>> allCurrentConfigs = new Dictionary<int, List<int>>();
            bool allConfigPurchased = UserController.Instance.IsAllConfigPhotosPurchased();

            if (UserController.Instance.GetCurUserType == 0 || !allConfigPurchased)
            {
                // 配表模式
                for (int i = 0; i < photoCfgs.Count; i++)
                {
                    var rechargeInfo = RechargeController.GetRechargeInfoByShop(photoCfgs[i].Id);
                    var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId).Reward;
                    List<int> currentPhotoIds = UserController.GetPhotoIdsFromRewardList(rewardList);
                    bool isPurchased = UserController.Instance.CheckShopPhotoConfig(rechargeInfo.Giftid, currentPhotoIds);
                    bool isUpdated = UserController.Instance.IsPhotoProductUpdated(rechargeInfo.Giftid, currentPhotoIds);
                    photoInfos.Add(new OneExcItemInfo(photoCfgs[i].Id, rechargeInfo.Giftid, isPurchased, isUpdated, currentPhotoIds));
                    allCurrentConfigs[rechargeInfo.Giftid] = currentPhotoIds;
                }
            }
            else
            {
                // 动态模式
                var dynamicIds = UserController.Instance.RefreshDynamicShopPhotos();
                for (int i = 0; i < dynamicIds.Count; i++)
                {
                    bool obtained = UserController.Instance.IsGetPhoto(dynamicIds[i]);
                    photoInfos.Add(new OneExcItemInfo(dynamicIds[i], obtained));
                }
            }

            _cachedAllCurrentConfigs = allCurrentConfigs;
            _cachedAllConfigPurchased = allConfigPurchased;
            return photoInfos;
        }
        #endregion

        protected override void OnRefresh()
        {
            // 标记当前页面为活跃页面
            ImgUtil.SetActivePage(this.GetType().Name);
            PhotoLoadThrottler.Reset();
            _allShopCfgs = ConfigLoader.Instance.Tables.TbCfgShop.DataList;
            UpdateInfo();
            //创建商品列表
            CreatePruductList();
            CreatPhotoList();
            CreatDiaList();
            //从钻石按钮入口进入时滚动到底部，方便玩家直接看到钻石领取
            bool scrollToBottom = userDatas.Length > 0 && userDatas[0] is bool b && b;
            if (scrollToBottom)
            {
                ScrollToBottomAsync().Forget();
            }
        }

        private async UniTaskVoid ScrollToBottomAsync()
        {
            //ScrollRect 优先尝试 mAllProducts 本身，找不到则在子节点兜底查找
            var scrollRect = mAllProducts.GetComponent<ScrollRect>()
                ?? mAllProducts.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect == null)
            {
                Log.Error("[Panel_Shop] 未找到 ScrollRect 组件，无法滚动到底部");
                return;
            }
            //等待 2 帧，确保 Widget 实例化完成且 Layout 计算稳定
            await UniTask.DelayFrame(2);
            if (scrollRect == null || scrollRect.content == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0f;
            Log.Info($"[Panel_Shop] 滚动到底部完成 content.height={scrollRect.content.rect.height}");
        }

        protected override void RegisterEvent()
        {
            GameEvent.AddEventListener<int>(EventType.RefreshCurrency, UpdateInfo);
        }

        private void UpdateInfo(int id = 1)
        {
            //刷新当前金币数量
            mAllDiaCount.text = PropsController.FormatCurrencyAuto(UserController.Instance.Diamond);

            // 货币变化意味着可能有购买行为，数据缓存必须失效
            InvalidateCache();
        }

        private void CreatePruductList()
        {
#if ENABLE_IAP
            //所有礼包配置
            var packCfgs = _allShopCfgs.FindAll(x => x.GoodType == mProductType.GiftPack);
            List<OneProductInfo> packInfos = new List<OneProductInfo>();
            //创建礼品包
            for (int i = 0; i < packCfgs.Count; i++)
            {
                //先判断当前礼包是否已经购买，如购买不需要生成
                var rechargeInfo = RechargeController.GetRechargeInfoByShop(packCfgs[i].Id);
                if (UserController.Instance.GetShopPackIds.Contains(rechargeInfo.Giftid)) continue;
                var allRews = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId).Reward;
                packInfos.Add(new OneProductInfo(packCfgs[i].Id, rechargeInfo.Giftid, allRews));
            }
            if (packInfos.Count == 0)
            {
                mpacksNode.Hide();
                return;
            }
            if (_allProductList == null)
            {
                _allProductList = CreateWidget<UIListWidget<Widget_OneProduct, OneProductInfo>>(mPack.gameObject);
            }
            _allProductList.SetDatas(packInfos);
#else
			mpacksNode.Hide();
#endif
        }

        private void CreatPhotoList()
        {
            // 🔴 优先用预热缓存，没有就现算（保底）
            List<OneExcItemInfo> photoInfos = _cachedPhotoInfos;
            if (photoInfos == null)
            {
                photoInfos = BuildPhotoInfos(_allShopCfgs);
                _cachedPhotoInfos = photoInfos;
                _dataReady = true;
            }

            if (photoInfos.Count == 0)
            {
                mExcNode.Hide();
                return;
            }
            mExcNode.Show();

            if (_allExcList == null)
            {
                _allExcList = CreateWidget<UIListWidget<Widget_OneExcItem, OneExcItemInfo>>(mAllExc.gameObject);
            }
            _allExcList.SetDatas(photoInfos);

            if (!_cachedAllConfigPurchased && _cachedAllCurrentConfigs != null)
            {
                UserController.Instance.MarkPhotoProductsSeen(_cachedAllCurrentConfigs);
            }
        }

        private void CreatDiaList()
        {
            //所有钻石配置
            var diaCfgs = _allShopCfgs.FindAll(x => x.GoodType == mProductType.GoldCoin);
            OneDiaProductInfo adDiaProduct = new OneDiaProductInfo(0, "", new List<TBRewardData>());
#if ENABLE_IAP
            List<OneDiaProductInfo> diaInfos = new List<OneDiaProductInfo>();
#endif
            for (int i = 0; i < diaCfgs.Count; i++)
            {
                var rechargeInfo = RechargeController.GetRechargeInfoByShop(diaCfgs[i].Id);
                if (rechargeInfo.Price == 0)
                {
                    var AdrewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId).Reward;
                    adDiaProduct = new OneDiaProductInfo(diaCfgs[i].Id, "", AdrewardList);
                    continue;
                }
#if ENABLE_IAP
                string price = RechargeController.GetPriceByRechargeId(rechargeInfo);
                var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(rechargeInfo.RewardId).Reward;
                diaInfos.Add(new OneDiaProductInfo(diaCfgs[i].Id, price, rewardList));
#endif
            }
            if (adDiaProduct.Id != 0)
            {
                _adDiaProduct = CreateWidgetByType<Widget_OneDiaProduct>(mAllDia);
                _adDiaProduct.transform.SetSiblingIndex(0);
                _adDiaProduct.SetItemData(adDiaProduct);
            }
#if ENABLE_IAP
            if (diaInfos.Count == 0) return;
            if (_allDiaList == null)
            {
                _allDiaList = CreateWidget<UIListWidget<Widget_OneDiaProduct, OneDiaProductInfo>>(motherDia.gameObject);
            }
            _allDiaList.SetDatas(diaInfos);
#else
			motherDia.gameObject.Hide();
#endif
        }

        protected override void Close()
        {
            // 清除活跃页面标记
            ImgUtil.ClearActivePage(this.GetType().Name);

            base.Close();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvent.RemoveEventListener<int>(EventType.RefreshCurrency, UpdateInfo);
        }
    }
}