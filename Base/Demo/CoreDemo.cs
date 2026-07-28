using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CoreLite;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using static CoreLite.CoreIapH5Mgr;

public class CoreDemo : MonoBehaviour
{

    public Image nativeImage;
    public Image smallNativeImage;
    public CoreGameConfigData gameConfigData;
    // Start is called before the first frame update
    void Start()
    {
        CoreLogUtils.Enabled = true;
        CoreSdkMgr.RegisterPurchaseInitHandler((result, errorMsg) =>
        {
            if (result)
            {
                CoreLogUtils.Debug("Iap init success");
            }
            else
            {
                CoreLogUtils.Debug("Iap init fail, errorMsg:" + errorMsg);
            }
        });

        CoreSdkMgr.RegisterAsnUserHandler((result) =>
        {
            CoreLogUtils.Debug("AsnUserReceived result:" + result);
        });

        CoreSdkMgr.RegisterPurchaseCompletionHandler((res) =>
        {
            // 购买成功
            if (res.purchase_result)
            {

                CoreLogUtils.Debug("购买成功");

                // 上报下发奖励
                CoreSdkMgr.LogCheckingOrder(res.query_order_reuslt, (res, msg) =>
                {
                    CoreLogUtils.Debug("下发奖励请求结果："+res+" msg:"+msg);
                });
            }
            else
            {
                // 购买失败
                CoreLogUtils.Debug("购买失败：");
            }

            CoreLogUtils.Debug("PurchaseCallback :"+ JsonUtility.ToJson(res));
        });
#if IAP_ENABLE
        CoreSdkMgr.AddProductsStatic(new Dictionary<string, ProductType>()
        {
#if UNITY_ANDROID
            { "com.fantasytile.coins_2",ProductType.Consumable},
#else
            { "com.oopscroco.1280diamonds",ProductType.Consumable},
#endif
        });
#endif
        CoreSdkMgr.RegisterFirebaseActivateHandler(() =>{
            
            CoreLogUtils.Debug("CoreLite SDK 收到firebase激活");
        });

        CoreSdkMgr.Instance.RegisterUserAttributionHandler((organicUser,adnetwork,clickLabel) =>
        {
            CoreLogUtils.Debug("CoreLite SDK 收到用户属性"+organicUser+" adnetwork:"+adnetwork+" clickLabel:"+clickLabel);
        });

        CoreSdkMgr.InitSDK(() =>
        {
            CoreLogUtils.Debug("CoreLite SDK 初始化完成");
        }, privacy);

    }
    string privacy = "";//"https://dtsg.grz14games.com";
    public void QueryProducts()
    {
        CoreSdkMgr.GetProductList((res,msg,orderList) =>
        {
            CoreLogUtils.Debug("GetProductList result:"+res+" msg:"+msg+" orderList:"+ JsonUtility.ToJson(orderList));
        });
    }

    public void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }

    private void PurchaseCallback(CoreLitePurchase.Result res)
    {
        if (res.purchase_result)
        {

            CoreLogUtils.Debug("下发奖励成功");
            CoreSdkMgr.LogCheckingOrder(res.query_order_reuslt, (res, msg) =>
            {
                CoreLogUtils.Debug("下发奖励请求结果："+res+" msg:"+msg);
            });
        }
        else
        {
            CoreLogUtils.Debug("下发奖励失败");
        }

        CoreLogUtils.Debug("PurchaseCallback :"+ JsonUtility.ToJson(res));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Buy()
    {
        var pricacy =  CoreSdkMgr.GetConfigStr("PictureURL");
        CoreLogUtils.Debug("在线参数 PictureURL："+pricacy);
#if UNITY_ANDROID
        var req = new CoreLiteCreateOrder.Request("diamond1","com.fantasytile.coins_2","1 diamond",40,1,"USD","{\"GamedDiamond\":1}","0");
#else
        var req = new CoreLiteCreateOrder.Request("diamond1280","com.oopscroco.1280diamonds","1 diamond1280",128,1,"USD","{\"GamedDiamond\":1}","0");
#endif
        CoreLogUtils.Debug("ButtonBuyCoins req:"+req.unit_price);
        CoreSdkMgr.CreatePaymentOrder(req, (res,msg,paymentInfos) =>
        {
            CoreLogUtils.Debug("CreatePaymentOrder result:"+res+" msg:"+msg+" paymentInfos:"+ JsonUtility.ToJson(paymentInfos));
            
        });
    }

    public void GetUserInfo()
    {
        CoreLogUtils.Debug("CoreLite get user info USERID:" + CoreSdkMgr.GetCurrentUserId());
        CoreSdkMgr.GameConfig((appState,msg) =>
        {
            CoreLogUtils.Debug("CoreLite get game config appState:" + appState+" msg:"+msg);
        });
    }

    public void Login()
    {
        CoreSdkMgr.Login((Result, Msg,uId) =>
        {
            CoreLogUtils.Debug("login result: "+Result+" msg: "+Msg+" uId: "+uId);
        });
        var remoteConfigInt = CoreSdkMgr.GetConfigInt("key", 0);
    }

    public void Logout()
    {
        CoreSdkMgr.Logout();
        // CoreSdkMgr.QueryOrderList(new CoreLiteQueryOrderList.Request(CorePrefs.UserId,-1,-1), (Result, Msg, orderList) =>
        // {
        //     CoreLogUtils.Debug("query order list result: "+Result+" msg: "+Msg+" orderList: "+ orderList+" count: "+orderList.order_list.Count);
        // });
    }

    public void ShowInterstitial()
    {
        CoreSdkMgr.ShowInterstitial("finish_close", IvType.IV1, () =>
        {
            CoreLogUtils.Debug("CoreLite 插屏广告关闭了");
        }, (callbackInfo, action) =>
        {
            if (callbackInfo.isReady)
            {
                CoreLogUtils.Debug("CoreLite 插屏广告准备好了,展示去广告");//为什么要插屏广告准备好了再展示去广告？没准备好不能展示吗
                action?.Invoke();
            }else
            {
                CoreLogUtils.Debug("CoreLite 插屏广告未准备好");
            }
        });
    }

    public void ShowRewarded()
    {
        CoreLogUtils.Debug("CoreLite show reward");
        var isRewardedAvailable = CoreSdkMgr.IsRewardedVideoAvailable();
        CoreSdkMgr.ShowRewardedVideo("GetCoin", () =>
        {
            CoreLogUtils.Debug("CoreLite 关闭了激励视频，下发奖励");
        }, () =>
        {
            CoreLogUtils.Debug("CoreLite 激励视频显示失败");
        });
    }

    public void ShowBanner()
    {
        CoreSdkMgr.ShowBanner();
    }

    public void HidBanner()
    {
        CoreSdkMgr.HideBanner();
    }

    public void SendEmail()
    {
        CoreSdkMgr.SendFeedbackEmail("617088122@qq.com");
    }

    /// <summary> Small 模板 Native（admob_native_small_units），锚在屏幕底部 banner 大小区域。 </summary>
    public void ShowNativeSmall()
    {
        if (smallNativeImage == null)
        {
            CoreLogUtils.Debug("CoreDemo ShowNativeSmall: smallNativeImage is null");
            return;
        }

    }

    /// <summary> 全屏 Native 遮罩场景（admob_native_fullscreen_units）。 </summary>
    public void ShowNativeFull()
    {

    }

    /// <summary> Medium 模板 Native 内嵌展示（竞价维度 Native）。 </summary>
    public void ShowNativeMedium()
    {
        if (nativeImage == null)
        {
            CoreLogUtils.Debug("CoreDemo ShowNativeMedium: nativeImage is null");
            return;
        }

    }

    /// <summary> 移除 Small / Fullscreen / Medium 三类 Native 展示。 </summary>
    public void HideAllNativeAds()
    {
        CoreLogUtils.Debug("CoreDemo HideAllNativeAds");
    }
}
