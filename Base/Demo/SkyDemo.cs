using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Sky;
using UnityEngine;
using UnityEngine.UI;
#if IAP_ENABLE
using static Sky.SkyIapH5Mgr;
#endif
using UnityEngine.Purchasing;

public class SkyDemo : MonoBehaviour
{

    public Image nativeImage;
    public Image smallNativeImage;
    public SkyGameConfigData gameConfigData;
    // Start is called before the first frame update
    void Start()
    {
        SkyLogUtils.Enabled = true;
        SkySdkMgr.RegisterPurchaseInitHandler((result, errorMsg) =>
        {
            if (result)
            {
                SkyLogUtils.Debug("Iap init success");
            }
            else
            {
                SkyLogUtils.Debug("Iap init fail, errorMsg:" + errorMsg);
            }
        });

        SkySdkMgr.RegisterAsnUserHandler((result) =>
        {
            SkyLogUtils.Debug("AsnUserReceived result:" + result);
        });
#if IAP_ENABLE
        SkySdkMgr.RegisterPurchaseCompletionHandler((res) =>
        {
            // 购买成功
            if (res.purchase_result)
            {

                SkyLogUtils.Debug("购买成功");

                // 上报下发奖励
                SkySdkMgr.LogCheckingOrder(res.query_order_reuslt, (res, msg) =>
                {
                    SkyLogUtils.Debug("下发奖励请求结果："+res+" msg:"+msg);
                });
            }
            else
            {
                // 购买失败
                SkyLogUtils.Debug("购买失败：");
            }

            SkyLogUtils.Debug("PurchaseCallback :"+ JsonUtility.ToJson(res));
        });
#endif
#if IAP_ENABLE
        SkySdkMgr.AddProductsStatic(new Dictionary<string, ProductType>()
        {
#if UNITY_ANDROID
            { "com.fantasytile.coins_2",ProductType.Consumable},
#else
            { "com.oopscroco.1280diamonds",ProductType.Consumable},
#endif
        });
#endif
        SkySdkMgr.RegisterFirebaseActivateHandler(() =>{
            
            SkyLogUtils.Debug("Sky SDK 收到firebase激活");
        });

        SkySdkMgr.Instance.RegisterUserAttributionHandler((organicUser,adnetwork,clickLabel) =>
        {
            SkyLogUtils.Debug("Sky SDK 收到用户属性"+organicUser+" adnetwork:"+adnetwork+" clickLabel:"+clickLabel);
        });

        SkySdkMgr.InitSDK(() =>
        {
            SkyLogUtils.Debug("Sky SDK 初始化完成");
        }, privacy);

    }
    string privacy = "";//"https://dtsg.grz14games.com";
    public void QueryProducts()
    {
#if IAP_ENABLE
        SkySdkMgr.GetProductList((res,msg,orderList) =>
        {
            SkyLogUtils.Debug("GetProductList result:"+res+" msg:"+msg+" orderList:"+ JsonUtility.ToJson(orderList));
        });
#endif
    }

    public void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }
#if IAP_ENABLE
    private void PurchaseCallback(SkyPurchase.Result res)
    {
        if (res.purchase_result)
        {

            SkyLogUtils.Debug("下发奖励成功");
            SkySdkMgr.LogCheckingOrder(res.query_order_reuslt, (res, msg) =>
            {
                SkyLogUtils.Debug("下发奖励请求结果："+res+" msg:"+msg);
            });
        }
        else
        {
            SkyLogUtils.Debug("下发奖励失败");
        }

        SkyLogUtils.Debug("PurchaseCallback :"+ JsonUtility.ToJson(res));
    }
#endif
    // Update is called once per frame
    void Update()
    {
        
    }

    public void Buy()
    {
#if IAP_ENABLE
        var pricacy =  SkySdkMgr.GetConfigStr("PictureURL");
        SkyLogUtils.Debug("在线参数 PictureURL："+pricacy);
#if UNITY_ANDROID
        var req = new SkyCreateOrder.Request("diamond1","com.fantasytile.coins_2","1 diamond",40,1,"USD","{\"GamedDiamond\":1}","0");
#else
        var req = new SkyCreateOrder.Request("diamond1280","com.oopscroco.1280diamonds","1 diamond1280",128,1,"USD","{\"GamedDiamond\":1}","0");
#endif
        SkyLogUtils.Debug("ButtonBuyCoins req:"+req.unit_price);
        SkySdkMgr.CreatePaymentOrder(req, (res,msg,paymentInfos) =>
        {
            SkyLogUtils.Debug("CreatePaymentOrder result:"+res+" msg:"+msg+" paymentInfos:"+ JsonUtility.ToJson(paymentInfos));
            
        });
#endif
    }

    public void GetUserInfo()
    {
        SkyLogUtils.Debug("Sky get user info USERID:" + SkySdkMgr.GetCurrentUserId());
        SkySdkMgr.GameConfig((appState,msg) =>
        {
            SkyLogUtils.Debug("Sky get game config appState:" + appState+" msg:"+msg);
        });
    }

    public void Login()
    {
        SkySdkMgr.Login((Result, Msg,uId) =>
        {
            SkyLogUtils.Debug("login result: "+Result+" msg: "+Msg+" uId: "+uId);
        });
        var remoteConfigInt = SkySdkMgr.GetConfigInt("key", 0);
    }

    public void Logout()
    {
        SkySdkMgr.Logout();
        // SkySdkMgr.QueryOrderList(new SkyQueryOrderList.Request(SkyPrefs.UserId,-1,-1), (Result, Msg, orderList) =>
        // {
        //     SkyLogUtils.Debug("query order list result: "+Result+" msg: "+Msg+" orderList: "+ orderList+" count: "+orderList.order_list.Count);
        // });
    }

    public void ShowInterstitial()
    {
        SkySdkMgr.ShowInterstitial("finish_close", IvType.IV1, () =>
        {
            SkyLogUtils.Debug("Sky 插屏广告关闭了");
        }, (callbackInfo, action) =>
        {
            if (callbackInfo.isReady)
            {
                SkyLogUtils.Debug("Sky 插屏广告准备好了,展示去广告");//为什么要插屏广告准备好了再展示去广告？没准备好不能展示吗
                action?.Invoke();
            }else
            {
                SkyLogUtils.Debug("Sky 插屏广告未准备好");
            }
        });
    }

    public void ShowRewarded()
    {
        SkyLogUtils.Debug("Sky show reward");
        var isRewardedAvailable = SkySdkMgr.IsRewardedVideoAvailable();
        SkySdkMgr.ShowRewardedVideo("GetCoin", () =>
        {
            SkyLogUtils.Debug("Sky 关闭了激励视频，下发奖励");
        }, () =>
        {
            SkyLogUtils.Debug("Sky 激励视频显示失败");
        });
    }

    public void ShowBanner()
    {
        SkySdkMgr.ShowBanner();
    }

    public void HidBanner()
    {
        SkySdkMgr.HideBanner();
    }

    public void SendEmail()
    {
        SkySdkMgr.SendFeedbackEmail("617088122@qq.com");
    }

    /// <summary> Small 模板 Native（admob_native_small_units），锚在屏幕底部 banner 大小区域。 </summary>
    public void ShowNativeSmall()
    {
        if (smallNativeImage == null)
        {
            SkyLogUtils.Debug("SkyDemo ShowNativeSmall: smallNativeImage is null");
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
            SkyLogUtils.Debug("SkyDemo ShowNativeMedium: nativeImage is null");
            return;
        }

    }

    /// <summary> 移除 Small / Fullscreen / Medium 三类 Native 展示。 </summary>
    public void HideAllNativeAds()
    {
        SkyLogUtils.Debug("SkyDemo HideAllNativeAds");
    }
}
