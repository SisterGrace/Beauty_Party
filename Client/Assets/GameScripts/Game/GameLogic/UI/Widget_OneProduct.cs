using TK_Engine;
using UnityEngine;
using Config.common;
using System.Collections;
using System.Collections.Generic;
using GameBase;
using UnityEngine.UI;
using TMPro;
using System;
using Config.config;
using DG.Tweening;

namespace GameLogic
{
    [Window(UILayer.UI)]
    public partial class Widget_OneProduct : UIWidget, IListDataItem<OneProductInfo>
    {
        private mProductType _mProductType;
        private CfgShop _mCfgShop;
        private CfgRecharge _mCfgRecharge;
        private OneProductInfo _mData;
        private Item_PhotoLoad _itemPhoto;
        //当前卡包的照片id
        private List<int> _rewPhotoIds;
        private bool _isShowAd;

        public void SetItemData(OneProductInfo data)
        {
            _mCfgRecharge = RechargeController.GetRechargeInfoByShop(data.Id);
            _mData = data;
            UpdatePackInfo(data);

        }
        protected override void OnUpdate()
        {

        }
        private void UpdatePackInfo(OneProductInfo data)
        {
            _mCfgShop = ConfigLoader.Instance.Tables.TbCfgShop.Get(data.Id);
            UpdateBuyButton(true);
            mPackNameTxt.SetLanguage(_mCfgShop.PackName);
            mpackBg.sprite = GameModule.Resource.LoadAsset<Sprite>(_mCfgShop.Frame);
            ShowRewardInfo();
        }

        private void UpdateBuyButton(bool isLogEvent = false)
        {
            _isShowAd = RechargeController.UpdateBuyButtonUI(mPackPriceTxt, _mCfgRecharge, mAdicon, isLogEvent);
        }

        private void ShowRewardInfo()
        {
            int AbType = UserController.Instance.GetCurUserType;
            //礼包都是展示一个照片和道具奖励
            var photoRew = _mData.rewardDatas.Find(x => x.Type == ERewardType.PhotoGroup);
            if (photoRew != null)
            {
                var groupCfg = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew.Get(photoRew.ID);
                _rewPhotoIds = AbType == 1 ? groupCfg.BPhotoRews : groupCfg.APhotoRews;
                _itemPhoto.LoadUIImg(_rewPhotoIds[0]);
            }
            //展示道具奖励
            var propRew = _mData.rewardDatas.Find(x => x.Type != ERewardType.PhotoGroup && x.Type != ERewardType.Photo);
            if (propRew != null)
            {
                string iconName = PropsController.GetRewardIcon(propRew);
                mcurPropIcon.sprite = GameModule.Resource.LoadAsset<Sprite>(iconName);
                mpropCount.text = PropsController.GetRewardCount(propRew);
                mcurPropIcon.SetNativeSize();
            }
        }

        //支付回调
        private void BuyOneProduct()
        {
            //发送奖励
            PropsController.Instance.ShowRewardCollectPanels(_mData.rewardDatas,false);
            //更新已经购买
            UserController.Instance.AddeShopPackId(_mCfgRecharge.Giftid);
            GameModule.UI.CloseUI<Panel_Shop>();
        }
        //展示当前照片组的照片缩略图
        private void showPics()
        {
            if (_rewPhotoIds == null || _rewPhotoIds.Count == 0) return;
            GameEvent.Send(EventType.ShowPhotoThum, _rewPhotoIds, mPhotoNode.transform);
        }
        public void Hide()
        {
            transform.DOScale(0, 0.3f).SetEase(Ease.InBack);
        }

    }
    public struct OneProductInfo
    {
        public int Id;
        public int Giftid;
        public List<TBRewardData> rewardDatas;


        public OneProductInfo(int id, int giftid, List<TBRewardData> rewardData)
        {
            this.Id = id;
            this.Giftid = giftid;
            this.rewardDatas = rewardData;
        }
    }
}
