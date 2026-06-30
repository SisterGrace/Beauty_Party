using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using UnityEngine;
using Y_Base;
using System;
using TK_Engine;
using TMPro;
using UnityEngine.Purchasing;


namespace GameLogic
{
    public class RechargeController : Singleton<RechargeController>
    {
        private Dictionary<string, ProductType> _products = new Dictionary<string, ProductType>();
        private Dictionary<string, Vector3> _sendPos = new Dictionary<string, Vector3>();
        private Dictionary<string, Action<string>> _sendEvent = new Dictionary<string, Action<string>>();
        private List<BuyData> _temp = new List<BuyData>();
        public void InitAllProduct()
        {
#if ENABLE_IAP
            _products.Clear();
            var list = ConfigLoader.Instance.Tables.TbCfgRecharge.DataList;
            //添加内购商品
            foreach (var data in list)
            {
                if (data.Price > 0)
                {
                    var id = GetId(data);
                    _products.Add(id, (ProductType)data.MyProductType);
                }
            }
            SDKManager.Instance.AddProducts(_products);
            Log.Info($"添加了商品数量：{_products.Count}");
#else
            Log.Info("[IAP] 内购功能未启用 (ENABLE_IAP 未定义)，跳过商品初始化");
#endif
        }

        /// <summary>
        /// 获取内购商品id
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetId(CfgRecharge data)
        {
            string name = "";
#if UNITY_ANDROID || UNITY_EDITOR
            name = data.IAPAndroid;
#elif UNITY_IPHONE
            name = data.IAPIOS;
#endif
            return name;
        }
        /// <summary>
        /// 获取商品购买价格
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public static string GetPriceByID(int id)
        {
            bool isShowpriceInEditor = false;
#if UNITY_EDITOR
            isShowpriceInEditor = true;
#endif
            var rechargeInfo = GetRechargeInfoByShop(id);
            var curPrive = SDKManager.GetProductPrice(rechargeInfo.IAPAndroid);
            var price = rechargeInfo.Price == 0 ? "Free" : $"{(!isShowpriceInEditor ? curPrive : $"${rechargeInfo.Price}")}";
            //购买按钮事件上报
            return price;
        }

        /// <summary>
        /// 通过充值信息获取商品购买价格
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static string GetPriceByRechargeId(CfgRecharge rechargeInfo)
        {
            bool isShowpriceInEditor = false;
#if UNITY_EDITOR
            isShowpriceInEditor = true;
#endif
            var curPrive = SDKManager.GetProductPrice(rechargeInfo.IAPAndroid);
            var price = rechargeInfo.Price == 0 ? "Free" : $"{(!isShowpriceInEditor ? curPrive : $"${rechargeInfo.Price}")}";
            //购买按钮事件上报
            return price;
        }

        public static string GetAdByCfgRecharge(CfgRecharge rechargeInfo, out bool isShowAd)
        {
            int curAdTimes = UserController.Instance.GetAcItemUnLock(rechargeInfo.Id);
            int adTimes = rechargeInfo.AdUnlockTimes;
            if (curAdTimes >= adTimes)//此时解锁
            {
                isShowAd = false;
                return "btn_get";
            }
            else
            {
                isShowAd = true;
                return $"{curAdTimes}/{adTimes}";
            }
        }
        /// <summary>
        /// 是否还有等待的充值礼包（仅限商城的礼包）
        /// </summary>
        /// <returns></returns>
        public bool IsHavePack()
        {
            var allPacksInfo =
                ConfigLoader.Instance.Tables.TbCfgShop.DataList.FindAll(x => x.GoodType == Config.common.mProductType.GiftPack);
            return allPacksInfo.Count > UserController.Instance.GetShopPackIds.Count;
        }
        /// <summary>
        /// 在商城返回礼包信息
        /// </summary>
        /// <param name="isConcise">是否精简模式</param>
        /// <returns></returns>
        public List<CfgShop> GetShopPacksInfo(bool isConcise)
        {
            var allPacksInfo = new List<CfgShop>();
            List<int> allPackIds = new List<int>();
            //已经购买的礼包id
            List<int> shopPackIds = UserController.Instance.GetShopPackIds;
            var allPacks = ConfigLoader.Instance.Tables.TbCfgShop.DataList.FindAll(x => x.GoodType == Config.common.mProductType.GiftPack);
            for (int i = 0; i < allPacks.Count; i++)
            {
                if (!shopPackIds.Contains(allPacks[i].Id))
                {
                    allPackIds.Add(allPacks[i].Id);
                }
            }
            if (allPackIds.Count == 0) return allPacksInfo;
            if (isConcise)
            {
                bool isHaveOne = shopPackIds.Count >= allPacks.Count - 1;
                for (int i = 0; isHaveOne ? i < 1 : i < 2; i++)
                {
                    allPacksInfo.Add(allPacks.Find(x => x.Id == allPackIds[i]));
                }
            }
            else
            {
                foreach (var id in allPackIds)
                {
                    allPacksInfo.Add(allPacks.Find(x => x.Id == id));
                }
            }
            return allPacksInfo;
        }

        public List<CfgShop> GetShopGoldCoinsInfo(bool isConcise)
        {
            List<CfgShop> goldCoinsInfo = new List<CfgShop>();
            var allShopInfos = ConfigLoader.Instance.Tables.TbCfgShop.DataList.FindAll(x => x.GoodType == Config.common.mProductType.GoldCoin); ;
            if (isConcise)
            {
                goldCoinsInfo.Add(allShopInfos.Find(x => x.GoodType == Config.common.mProductType.GoldCoin && x.Id == 5));
            }
            else
            {
                foreach (var shopInfo in allShopInfos)
                {
                    goldCoinsInfo.Add(shopInfo);
                }
            }
            return goldCoinsInfo;
        }
        /// <summary>
        /// 获取充值信息
        /// </summary>
        /// <param name="rechargeId"></param>
        /// <returns></returns>
        public static CfgRecharge GetRechargeInfo(int rechargeId)
        {
            return ConfigLoader.Instance.Tables.TbCfgRecharge.GetById(rechargeId);
        }
        /// <summary>
        /// 获取充值信息
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <returns></returns>
        public static CfgRecharge GetRechargeInfo(string productId)
        {
            return ConfigLoader.Instance.Tables.TbCfgRecharge.GetByIAPAndroid(productId);
        }
        /// <summary>
        /// 根据商品id获取充值信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static CfgRecharge GetRechargeInfoByShop(int shopId)
        {
            var shopInfo = ConfigLoader.Instance.Tables.TbCfgShop.Get(shopId);
            var RechargeInfo = GetRechargeInfo(shopInfo.Price.ID);
            return RechargeInfo;
        }
        /// <summary>
        /// 获取礼包奖励
        /// </summary>
        /// <param name="packId">礼包id</param>
        /// <returns></returns>
        public static List<TBRewardData> GetPackReward(int packId)
        {
            var RechargeInfo = GetRechargeInfoByShop(packId);
            return ConfigLoader.Instance.Tables.TbCfgReward.Get(RechargeInfo.RewardId).Reward;
        }

        public static List<TBRewardData> GetPackRewardToProductId(string ProductId)
        {
            var RechargeInfo = GetRechargeInfo(ProductId);
            return ConfigLoader.Instance.Tables.TbCfgReward.Get(RechargeInfo.RewardId).Reward;
        }
        public void Buy(CfgRecharge cfg, Vector3 pos, Action<string> callback = null)
        {
#if !ENABLE_IAP
            Log.Info($"[IAP] 内购功能未启用 (ENABLE_IAP 未定义)，跳过 Buy: cfg.Id={cfg?.Id}");
            return;
#else
#if UNITY_EDITOR
            callback?.Invoke(GetId(cfg));
            return;
#endif
            if (SettingsUtils.FrameworkGlobalSettings.CloseIAP)
            {
                callback?.Invoke(GetId(cfg));
                return;
            }
            var id = GetId(cfg);
            //判断是否获取到商品信息，如果没有就加一个提示
            var product = SDKManager.GetProductById(id);
            if (product == null)
            {
                Log.Error($"没有找到商品信息：{id}");
                //这里可以加一个提示，需要提示用户是否正常
                GameEvent.Send(EventType.ShowTips, "uitips_loadingdata", true);
                return;
            }
             SDKManager.Instance.LogEvent("Game_IAP_BUTTON_CLICK", "Postion", 1);
            _sendPos[id] = pos;
            _sendEvent[id] = callback;
            GameEvent.Send(EventType.SetWaitingUIAndTips, true, "uitips_buy_tips3");

            Log.Info("点击了购买按钮");
            SDKManager.Instance.BuyProduct(GetId(cfg), cfg.EventID);
#endif
        }

        public void SendReward(string pId, bool orderAlreadyExists = false, bool isInvokeCallback = true)
        {
            if (string.IsNullOrEmpty(pId))
            {
                Log.Warning("发送商品奖励的id为空");
                return;
            }
            if (!UserController.Instance.IsInit)
            {
                _temp.Add(new BuyData(pId, true));
                return;
            }
            var cfg = GetCfg(pId);
            if (isInvokeCallback && _sendEvent.TryGetValue(pId, out var action))
            {
                action?.Invoke(pId);
                _sendEvent[pId] = null;
            }
            //补单操作,只发送数据，不做动画
            if (orderAlreadyExists)
            {
                //设置有奖励id的充值信息
                if (cfg.RewardId != 0)
                {
                    var endlessCfg = FindEndlessTreasureConfig(cfg.Id);
                    if (endlessCfg != null)
                    {
                        HandleEndlessTreasureReOrder(cfg);
                        return;
                    }
                    //对于礼包要记录
                    if (cfg.Giftid != 0)
                    {
                        if (cfg.Giftid < 100)
                        {
                            //添加已经购买过的礼包id，商城界面就不再显示
                            UserController.Instance.AddeShopPackId(cfg.Giftid);
                        }
                        else
                        {
                            //获取当前配置的照片ID和OtherID
                            var rewardList = ConfigLoader.Instance.Tables.TbCfgReward.Get(cfg.RewardId).Reward;
                            List<int> photoIds = new List<int>();
                            foreach (var reward in rewardList)
                            {
                                if (reward.Type == ERewardType.Photo)
                                {
                                    if (reward.ID > 0) photoIds.Add(reward.ID);
                                    if (reward.OtherID > 0) photoIds.Add(reward.OtherID);
                                }
                            }
                            UserController.Instance.AddShopPhotoWithConfig(cfg.Giftid, photoIds);
                        }

                    }
                    PropsController.Instance.SendRewardData(cfg.RewardId);
                }
            }
        }

        private CfgEndlessTreasure FindEndlessTreasureConfig(int rechargeId)
        {
            return ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.DataList
                .Find(x => x.Price.ID == rechargeId);
        }

        private void HandleEndlessTreasureReOrder(CfgRecharge cfg)
        {
            var allInfos = ConfigLoader.Instance.Tables.TbCfgEndlessTreasure.DataList;
            ActivityType actType = ActivityType.UntoldTreasure;
            var getList = UserController.Instance.GetCurRewList(actType);

            var targetItem = allInfos.Find(x =>
                x.Price.ID == cfg.Id && (getList == null || !getList.Contains(x.Id)));
            if (targetItem == null)
            {
                PropsController.Instance.SendRewardData(cfg.RewardId);
                return;
            }

            var reward = ConfigLoader.Instance.Tables.TbCfgReward.Get(cfg.RewardId);

            if (reward.IsRandom)
            {
                bool isHaveCurRandom = PhotoController.Instance.IsHaveRandomPhotos(actType);
                bool isHaveLastRandom = PhotoController.Instance.IsHaveLastRandomPhotos(actType);
                int curIndex = (isHaveCurRandom || isHaveLastRandom) ? 0 : 1;
                var rewCfg = reward.Reward[curIndex];

                if (isHaveCurRandom || isHaveLastRandom)
                {
                    var randomPhotoId = isHaveCurRandom
                        ? PhotoController.Instance.GetCurActivityPhotoIds(actType)[Mathf.Abs(rewCfg.ID)]
                        : PhotoController.Instance.GetLastActivityPhotoIds(actType)[Mathf.Abs(rewCfg.ID)];
                    UserController.Instance.SendReward(ERewardType.Photo, randomPhotoId, rewCfg.Count,
                        rewCfg.FlyType == itemFlyType.AddTimeFly);
                }
                else
                {
                    UserController.Instance.SendReward(rewCfg.Type, rewCfg.ID, rewCfg.Count,
                        rewCfg.FlyType == itemFlyType.AddTimeFly);
                }
            }
            else
            {
                PropsController.Instance.SendRewardData(cfg.RewardId);
            }

            int claimedCountBefore = getList != null ? getList.Count : 0;
            UserController.Instance.AddRewGetList(actType, targetItem.Id);

            int totalItems = allInfos.Count;
            int displaySize = Mathf.Min(6, totalItems);
            int unclaimedBefore = totalItems - claimedCountBefore;
            int unclaimedAfter = unclaimedBefore - 1;
            if (unclaimedAfter > 0 && unclaimedAfter < displaySize && unclaimedBefore >= displaySize)
            {
                UserController.Instance.AddActivityLoopCount(actType);
                PhotoController.Instance.SaveLastGetPhoto(actType);
                PhotoController.Instance.SetCurActivityPhotoIds(actType);
            }
        }

        //检测补单下发
        public void CheckOrder()
        {
            //检测补单价格是否满足切换到B类用户
            var totalCost = SDKManager.Instance.GetIAPTotalCost;
            foreach (var buyData in _temp)
            {
                SendReward(buyData.id, buyData.orderAlreadyExists);
            }
            _temp.Clear();
        }
        /// <summary>
        /// 移除一个发送事件
        /// </summary>
        /// <param name="pId"></param>
        public void RemoveOneSendEvent(string pId)
        {
            if (string.IsNullOrEmpty(pId)) return;
            if (_sendEvent.TryGetValue(pId, out var action))
            {
                _sendEvent[pId] = null;
            }
        }
        public CfgRecharge GetCfg(string id)
        {
            CfgRecharge cfg = null;
#if UNITY_ANDROID || UNITY_EDITOR
            cfg = ConfigLoader.Instance.Tables.TbCfgRecharge.GetByIAPAndroid(id);
#elif UNITY_IPHONE
             cfg = ConfigLoader.Instance.Tables.TbCfgRecharge.GetByIAPIOS(id);
#endif
            return cfg;
        }

        /// <summary>
        /// 判断该充值项是否处于 IAA（看广告）模式。
        /// 优先看配置自带的 AdUnlockTimes，其次看远程参数是否标记了该商品。
        /// </summary>
        public static bool IsIAAMode(CfgRecharge cfg)
        {
            if (cfg == null) return false;
            return cfg.AdUnlockTimes > 0 || ActivityPhotoRemoteHelper.IsRemoteIAAOverride(cfg.Id);
        }

        /// <summary>
        /// 获取该充值项在 IAA 模式下需要看广告的次数。
        /// 配置自带 AdUnlockTimes > 0 时用配置值，否则用 Ceiling(Price)。
        /// </summary>
        public static int GetIAAAdTimes(CfgRecharge cfg)
        {
            if (cfg.AdUnlockTimes > 0) return cfg.AdUnlockTimes;
            int times = (int)Math.Ceiling(cfg.Price);
            return times > 0 ? times : 1;
        }

        /// <summary>
        /// 获取按钮显示文本，同时返回当前按钮模式枚举。
        /// IAP / IAA：返回值为纯文本，直接设置 text。
        /// Free / IAAComplete：返回值为多语言 key，需用 SetLanguage 解析。
        /// </summary>
        public static string GetBuyButtonText(CfgRecharge cfg, out EBuyBtnMode mode)
        {
            if (IsIAAMode(cfg))
            {
                int adTimes = GetIAAAdTimes(cfg);
                int curAdTimes = UserController.Instance.GetAcItemUnLock(cfg.Id);
                if (curAdTimes >= adTimes)
                {
                    mode = EBuyBtnMode.IAAComplete;
                    return "btn_get";
                }
                mode = EBuyBtnMode.IAA;
                return $"{curAdTimes}/{adTimes}";
            }

            if (cfg.Price > 0)
            {
                mode = EBuyBtnMode.IAP;
                return GetPriceByRechargeId(cfg);
            }

            mode = EBuyBtnMode.Free;
            return "btn_free";
        }

        /// <summary>
        /// 通用按钮 UI 刷新：根据 EBuyBtnMode 设置文本 / 多语言 / 广告图标 / 事件上报。
        /// 返回当前是否处于 IAA 看广告状态（可用于外部记录 _isShowAd）。
        /// </summary>
        public static bool UpdateBuyButtonUI(TextMeshProUGUI priceTxt, CfgRecharge cfg,
            GameObject adIcon = null, bool isLogEvent = false)
        {
            string btnTxt = GetBuyButtonText(cfg, out EBuyBtnMode mode);
            bool isShowAd = mode == EBuyBtnMode.IAA;
            switch (mode)
            {
                case EBuyBtnMode.IAA:
                    priceTxt.text = btnTxt;
                    if (isLogEvent)
                        SDKManager.Instance.LogEvent("RV_Button_Show", "Position", cfg.EventID);
                    break;
                case EBuyBtnMode.IAAComplete:
                    priceTxt.SetLanguage(btnTxt);
                    break;
                case EBuyBtnMode.IAP:
                    priceTxt.text = btnTxt;
                    if (isLogEvent)
                        IAPBtnShowLogEvent(rechargeId: cfg.Id);
                    break;
                case EBuyBtnMode.Free:
                    priceTxt.SetLanguage(btnTxt);
                    break;
            }
        if (adIcon != null) adIcon.SetActive(isShowAd);
        
        // 控制扫光效果：IAA模式下启用扫光
        // priceTxt 路径: Btn_PackPrice/Hor/TMP_PackPriceTxt
        // 所以按钮是 parent.parent
        var btnTransform = priceTxt.transform.parent?.parent;
        if (btnTransform != null)
        {
            var shineEffect = btnTransform.GetComponent<Item_ShineEffect>();
            if (shineEffect != null)
            {
                shineEffect.enabled = isShowAd;
            }
        }
        
        return isShowAd;
    }

        /// <summary>
        /// 通用点击处理：IAA 模式下弹出广告（广告位使用 cfg.EventID）并更新进度，IAP 模式下直接购买。
        /// onStateChanged: 每次广告观看成功或状态变化后回调（用于刷新按钮文本）
        /// onRewardReady: 广告看完 / IAP 支付成功后回调（用于发放奖励）
        /// </summary>
        public void HandleBuyOrAd(CfgRecharge cfg, Vector3 flyPos,
            Action onStateChanged, Action<string> onRewardReady)
        {
            if (IsIAAMode(cfg))
            {
                int adTimes = GetIAAAdTimes(cfg);
                int curAdTimes = UserController.Instance.GetAcItemUnLock(cfg.Id);
                if (curAdTimes < adTimes)
                {
                    SDKManager.Instance.Button_ShowRV(cfg.EventID, (success) =>
                    {
                        if (success)
                        {
                            curAdTimes++;
                            UserController.Instance.SetAcItemUnLock(cfg.Id, curAdTimes);
                            onStateChanged?.Invoke();
                        }
                    });
                }
                else
                {
                    UserController.Instance.SetAcItemUnLock(cfg.Id, 0);
                    onStateChanged?.Invoke();
                    onRewardReady?.Invoke(GetId(cfg));
                }
            }
            else if (cfg.Price > 0)
            {
                Buy(cfg, flyPos, onRewardReady);
            }
            else
            {
                onRewardReady?.Invoke("");
            }
        }

        public static void IAPBtnShowLogEvent(int shopID = -1, int rechargeId = -1)
        {
            CfgRecharge rechargeInfo = null;
            if (shopID != -1)
            {
                rechargeInfo = GetRechargeInfoByShop(shopID);
            }
            else if (rechargeId != -1)
            {
                rechargeInfo = GetRechargeInfo(rechargeId);
            }
            SDKManager.Instance.LogEvent("IAP_Button_Show", "IAP", rechargeInfo.EventID);
        }
    }

    public enum EBuyBtnMode
    {
        IAP,
        IAA,
        IAAComplete,
        Free,
    }

    public class BuyData
    {
        public string id;
        public bool orderAlreadyExists;
        public BuyData(string id, bool order)
        {
            this.id = id;
            orderAlreadyExists = order;
        }
    }
}
