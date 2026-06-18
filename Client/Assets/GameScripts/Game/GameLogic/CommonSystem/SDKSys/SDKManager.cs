using System;
using System.Collections.Generic;
using Y_Base;
using UnityEngine;
using TK_Engine;
using Config;
using Sky;

#if USE_UP2SDK
using CoreLite;
#endif

namespace GameLogic
{
    public partial class SDKManager : Singleton<SDKManager>
    {
        #region sdk属性
        private SDKModel _sdkModel;
        private bool _isPromote;
        private bool _isASN;
        public bool IsASN
        {
            get => _isASN;
            set
            {
                _isASN = value;
            }
        }

        public bool IsPromote
        {
            get
            {
                Log.Info($"此时是否为买量用户:{_isPromote}");
                TryUpgradeToTypeB(_isPromote);
                return _isPromote;
            }
        }
        private int _myPayType;

        public int MyPayType
        {
            get => _myPayType;
            set
            {
                _myPayType = value;
            }
        }
        private string _payCountry;

        public string PayCountry
        {
            get => _payCountry;
            set
            {
                _payCountry = value;
            }
        }
        //是否已经下架
        public bool IsRemovedApp;
        //支付在线参数结构体
        public PayConfig MyPayConfig;
        public int GetPayDailyTimes
        {
            get => _sdkModel.PaydailyTimes;
            set
            {
                _sdkModel.PaydailyTimes = value;
                Save();
            }
        }
        public int GetPayTotalTimes
        {
            get => _sdkModel.PaytotalTimes;
            set
            {
                _sdkModel.PaytotalTimes = value;
                Save();
            }
        }
        public int GetInterstitialTimes
        {
            get => _sdkModel.InterstitialPlayTimes;
            set
            {
                _sdkModel.InterstitialPlayTimes = value;
                //如果插屏次数大于等于3，则提示用户
                if (_sdkModel.InterstitialPlayTimes >= 3)
                {
                    if (!UserController.Instance.AdFree)
                    {
                        ActivityController.Instance.OpenNoADActivity();
                    }
                    _sdkModel.InterstitialPlayTimes = 0;
                }
                Save();
            }
        }
        //是否新用户
        public bool GetIsNewUser
        {
            get => _sdkModel.IsNewUser;
            set
            {
                _sdkModel.IsNewUser = value;
                Save();
            }
        }

        public float GetIAPTotalCost
        {
            get
            {
                //TryUpgradeToTypeB(_sdkModel.IAPTotalCost > ConfigLoader.Constant.AUnlockIAP);
                return _sdkModel.IAPTotalCost;
            }
            set
            {
                _sdkModel.IAPTotalCost = value;
                //TryUpgradeToTypeB(_sdkModel.IAPTotalCost > ConfigLoader.Constant.AUnlockIAP);
                Save();
            }
        }

        public bool GetLevelChooseReported
        {
            get => _sdkModel.IsLevelChooseReported;
            set
            {
                _sdkModel.IsLevelChooseReported = value;
                Save();
            }
        }
#if USE_UP2SDK && ENABLE_IAP
        //获取支付列表
        private CoreProductListRequest.ProductListData _productListData;
#endif
        #endregion

        public void Init(Action initSdkCallback)
        {
            _sdkModel = ES3.Load("SDKModel", new SDKModel(true));
            _myPayType = 1;
            _payCountry = "usd";
            _isASN = false;
            RechargeController.Instance.InitAllProduct();
#if USE_UP2SDK
#if ENABLE_IAP
            CoreSdkMgr.RegisterPurchaseInitHandler(OnPurchaseInitLinstener);
            CoreSdkMgr.RegisterPurchaseCompletionHandler(PurchaseCallback);
#endif
            CoreSdkMgr.Instance.RegisterUserAttributionHandler(UserSourceListenerCallback);
            CoreSdkMgr.GameConfig(GetAPPState);
            CoreSdkMgr.RegisterFirebaseActivateHandler(GetRemoteConfigCall);
            CoreSdkMgr.RegisterAsnUserHandler(SetUserASN);
#endif
            SkyAnalyticsMgr.init();
            GameModule.Timer.AddTimer(tiems =>
            {
                initSdkCallback?.Invoke();
            }, 0.5f);
#if USE_UP2SDK && !UNITY_EDITOR
            CoreSdkMgr.InitSDK(() =>
                {
                    OnSplashClosed();
                });
#endif
            Vibration.Init();
        }

        /// <summary>IV 关闭时的外部回调，由 Button_ShowIV 设置并在关闭时回传 true</summary>
        private Action<bool> _currentIvCloseCallback;

        public void Save()
        {
            ES3.Save("SDKModel", _sdkModel);
        }

        /// <summary>
        /// AB 分组升级判断：满足条件时将用户升级为 B 面用户
        /// </summary>
        private void TryUpgradeToTypeB(bool condition)
        {
            int abControl = GetRemoteConfigInt("AB_control", 1);
            if (abControl != 2 || !UserController.Instance.IsInit) return;
            if (condition && UserController.Instance.GetCurUserType != 1 && !IsASN)
                UserController.Instance.GetCurUserType = 1;
        }
        public bool IsEditor =>
#if UNITY_EDITOR
    true;
#else
    false;
#endif
 void OnSplashClosed() => Log.Info("开屏广告关闭");
    }
    
}
