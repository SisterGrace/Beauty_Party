package com.unity3d.player;

import android.content.Context;
import android.text.TextUtils;
import android.util.Log;
import androidx.annotation.Keep;

import com.reyun.solar.engine.OnAttributionListener;
import com.reyun.solar.engine.OnInitializationCallback;
import com.reyun.solar.engine.infos.SEAdImpEventModel;
import com.reyun.solar.engine.infos.SEAdClickEventModel;
import com.reyun.solar.engine.infos.SEPurchaseEventModel;
import com.reyun.solar.engine.SolarEngineConfig;
import com.reyun.solar.engine.SolarEngineManager;

import org.json.JSONObject;

import java.util.Iterator;

/**
 * 热云 SolarEngine Android 桥接（重写版）。
 * 数据区域：海外（com.reyun.solar.engine.oversea）  
 *
 * 提供给 Unity 端的入口：
 *   - setUnityCallbackTarget(name)         指定接收回调的 GameObject 名
 *   - preInit(appKey)                      启动时调用，仅初始化内部状态
 *   - initialize(appKey, enableLog)        用户同意隐私后调用，开始正式上报；同时挂归因监听
 *   - trackEvent(name, propsJson)          上报自定义事件
 *   - trackPresetEvent(name, propsJson)    上报预置事件（_appStart 等）
 *   - trackAdImpression(...)               广告展示收益事件（_appImp）
 *   - trackAdClick(...)                    广告点击事件（_appClick）
 *   - trackPurchase(...)                   应用内购买事件（_appPur）
 *
 * 归因回调通过 UnityPlayer.UnitySendMessage 回到 Unity：
 *   GameObject = sUnityCallbackTarget（默认 "Sky.SkySolarEngineMgr"）
 *   方法名     = OnAttributionSuccess(String json) / OnAttributionFail(String code)
 *
 * 文档：https://docs.solar-engine.com
 */
@Keep
public final class SolarEngineProxy {
    private static final String TAG = "[SolarEngineProxy]";
    // 与 SkySingleton<T> 创建的 GameObject 名一致：typeof(T).ToString() 含命名空间
    private static volatile String sUnityCallbackTarget = "Sky.SkySolarEngineMgr";

    private SolarEngineProxy() {
        throw new AssertionError("Utility class cannot be instantiated");
    }
    public static String getDistinctId() {
        try {
            return SolarEngineManager.getInstance().getDistinctId();
        } catch (Throwable t) {
            Log.e(TAG, "getDistinctId failed", t);
            return "";
        }
    }
    /**
     * Unity 端在创建单例后调用，告知 Java 把回调发回哪个 GameObject。
     * 避免在 Java 里硬编码 SkySingleton 生成的名字。
     */
    public static void setUnityCallbackTarget(String name) {
        if (!TextUtils.isEmpty(name)) {
            sUnityCallbackTarget = name;
            Log.i(TAG, "setUnityCallbackTarget=" + name);
        }
    }

    /**
     * 预初始化。隐私同意前可调用，不上报数据。
     */
    public static void preInit(String appKey) {
        if (TextUtils.isEmpty(appKey)) {
            Log.e(TAG, "preInit failed: appKey is empty");
            return;
        }
        try {
            Context ctx = getApplicationContext();
            if (ctx == null) {
                Log.e(TAG, "preInit failed: application context is null");
                return;
            }
            SolarEngineManager.getInstance().preInit(ctx, appKey);
            Log.i(TAG, "preInit called, appKey=" + appKey);
        } catch (Throwable t) {
            Log.e(TAG, "preInit threw exception", t);
        }
    }

    /**
     * 正式初始化。隐私同意后调用，开始上报；同步注册归因监听。
     *
     * @param appKey    后台应用管理获取的 16 位 AppKey
     * @param enableLog 是否打开 SDK 本地调试日志
     */
    public static void initialize(String appKey, boolean enableLog, boolean enableDebugModel) {
        if (TextUtils.isEmpty(appKey)) {
            Log.e(TAG, "initialize failed: appKey is empty");
            return;
        }
        try {
            Context ctx = getApplicationContext();
            if (ctx == null) {
                Log.e(TAG, "initialize failed: application context is null");
                return;
            }
            SolarEngineConfig.Builder builder = new SolarEngineConfig.Builder();
            if (enableLog) {
                builder.logEnabled();
            }
            if (enableDebugModel) {
                builder.isDebugModel(true);
            }
            // SDK 1.3.1.4：setOnAttributionListener 在 SolarEngineConfig 上而非 Builder 上，
            // 需要 build() 之后、initialize() 之前注入
            SolarEngineConfig config = builder.build();
            config.setOnAttributionListener(new OnAttributionListener() {
                @Override
                public void onAttributionSuccess(JSONObject attribution) {
                    String json = attribution == null ? "{}" : attribution.toString();
                    Log.i(TAG, "onAttributionSuccess: " + json);
                    sendToUnity("OnAttributionSuccess", json);
                }

                @Override
                public void onAttributionFail(int errorCode) {
                    Log.w(TAG, "onAttributionFail, code=" + errorCode);
                    sendToUnity("OnAttributionFail", String.valueOf(errorCode));
                }
            });
            SolarEngineManager.getInstance().initialize(ctx, appKey, config,
                new OnInitializationCallback() {
                    @Override
                    public void onInitializationCompleted(int code) {
                        // 0 成功；101 未初始化；102 appKey 非法；103 context 为 null；105 内部异常
                        if (code == 0) {
                            Log.i(TAG, "initialize success, code=0");
                        } else {
                            Log.w(TAG, "initialize failed, code=" + code);
                        }
                    }
                });
            Log.i(TAG, "initialize called, appKey=" + appKey + ", enableLog=" + enableLog);
        } catch (Throwable t) {
            Log.e(TAG, "initialize threw exception", t);
        }
    }

    /**
     * 上报自定义事件。propsJson 为 null/空字符串时不带属性。
     * 对应 SDK 1.3.1.4 的 SolarEngineManager.track(String, JSONObject)。
     */
    public static void trackEvent(String eventName, String propsJson) {
        if (TextUtils.isEmpty(eventName)) return;
        try {
            JSONObject props = parseJson(propsJson);
            // SDK 不接受 null props，没有属性时传一个空 JSONObject
            SolarEngineManager.getInstance().track(eventName, props == null ? new JSONObject() : props);
        } catch (Throwable t) {
            Log.e(TAG, "trackEvent threw, name=" + eventName, t);
        }
    }

    /**
     * 上报预置事件（_appStart / _appLogin / _appPur 等）。
     * SDK 1.3.1.4 没有独立 trackPresetEvent 接口，预置事件走同一个 track()，靠事件名约定。
     */
    public static void trackPresetEvent(String eventName, String propsJson) {
        trackEvent(eventName, propsJson);
    }

    /**
     * 主动拉取一次归因结果（同步），二次启动用得上。
     */
    public static String getAttribution() {
        try {
            JSONObject attr = SolarEngineManager.getInstance().getAttribution();
            return attr == null ? "" : attr.toString();
        } catch (Throwable t) {
            Log.e(TAG, "getAttribution threw", t);
            return "";
        }
    }

    /**
     * 广告展示收益事件（_appImp）
     * 文档：https://help.solar-engine.com/cn/docs/yu-ding-yi-shi-jian#ddf7239b7394338e3d62aa57598f0bb1
     */
    public static void trackAdImpression(
            String adNetworkPlatform,
            String mediationPlatform,
            int adType,
            String adNetworkAppID,
            String adNetworkADID,
            float ecpm,
            String currencyType,
            boolean isRenderSuccess,
            String customPropertiesJson) {
        
        if (TextUtils.isEmpty(adNetworkPlatform) || TextUtils.isEmpty(adNetworkADID)) {
            Log.w(TAG, "trackAdImpression: missing required params");
            return;
        }
        
        try {
            JSONObject customProps = parseJson(customPropertiesJson);
            SEAdImpEventModel model = new SEAdImpEventModel(
                    adNetworkPlatform,
                    TextUtils.isEmpty(mediationPlatform) ? "custom" : mediationPlatform,
                    adType,
                    TextUtils.isEmpty(adNetworkAppID) ? "" : adNetworkAppID,
                    adNetworkADID,
                    ecpm,
                    TextUtils.isEmpty(currencyType) ? "USD" : currencyType,
                    isRenderSuccess,
                    customProps
            );
            SolarEngineManager.getInstance().trackAdImpression(model);
            Log.i(TAG, "trackAdImpression: platform=" + adNetworkPlatform + ", ecpm=" + ecpm);
        } catch (Throwable t) {
            Log.e(TAG, "trackAdImpression threw", t);
        }
    }

    /**
     * 广告点击事件（_appClick）
     * 文档：https://help.solar-engine.com/cn/docs/yu-ding-yi-shi-jian#01134ef29655208d50386a08288e4789
     */
    public static void trackAdClick(
            String adNetworkPlatform,
            String mediationPlatform,
            int adType,
            String adNetworkADID,
            String customPropertiesJson) {
        
        if (TextUtils.isEmpty(adNetworkPlatform) || TextUtils.isEmpty(adNetworkADID)) {
            Log.w(TAG, "trackAdClick: missing required params");
            return;
        }
        
        try {
            JSONObject customProps = parseJson(customPropertiesJson);
            SEAdClickEventModel model = new SEAdClickEventModel(
                    adNetworkPlatform,
                    TextUtils.isEmpty(mediationPlatform) ? "custom" : mediationPlatform,
                    adType,
                    adNetworkADID,
                    customProps
            );
            SolarEngineManager.getInstance().trackAdClick(model);
            Log.i(TAG, "trackAdClick: platform=" + adNetworkPlatform + ", unit=" + adNetworkADID);
        } catch (Throwable t) {
            Log.e(TAG, "trackAdClick threw", t);
        }
    }

    /**
     * 应用内购买事件（_appPur）
     * 文档：https://help.solar-engine.com/cn/docs/yu-ding-yi-shi-jian#0ed9733361cf603f44d3153d31d5fbaa
     */
    public static void trackPurchase(
            String orderId,
            double payAmount,
            String currencyType,
            String payType,
            String productId,
            String productName,
            int productNum,
            int payStatus,
            String failReason,
            String customPropertiesJson) {
        
        if (payAmount < 0) {
            Log.w(TAG, "trackPurchase: payAmount < 0, skip");
            return;
        }
        
        try {
            JSONObject customProps = parseJson(customPropertiesJson);
            SEPurchaseEventModel model = new SEPurchaseEventModel(
                    TextUtils.isEmpty(orderId) ? "" : orderId,
                    payAmount,
                    TextUtils.isEmpty(currencyType) ? "CNY" : currencyType,
                    TextUtils.isEmpty(payType) ? "" : payType,
                    TextUtils.isEmpty(productId) ? "" : productId,
                    TextUtils.isEmpty(productName) ? "" : productName,
                    productNum,
                    payStatus,
                    TextUtils.isEmpty(failReason) ? "" : failReason,
                    customProps
            );
            SolarEngineManager.getInstance().trackPurchase(model);
            Log.i(TAG, "trackPurchase: amount=" + payAmount + ", status=" + payStatus);
        } catch (Throwable t) {
            Log.e(TAG, "trackPurchase threw", t);
        }
    }

    private static JSONObject parseJson(String json) {
        if (TextUtils.isEmpty(json)) return null;
        try {
            JSONObject obj = new JSONObject(json);
            // 空对象当 null 处理，少传一次空 map
            return obj.length() == 0 ? null : obj;
        } catch (Throwable t) {
            Log.w(TAG, "parseJson failed, raw=" + json);
            return null;
        }
    }

    private static void sendToUnity(String method, String message) {
        try {
            UnityPlayer.UnitySendMessage(sUnityCallbackTarget, method, message == null ? "" : message);
        } catch (Throwable t) {
            Log.e(TAG, "UnitySendMessage failed, target=" + sUnityCallbackTarget + " method=" + method, t);
        }
    }

    /**
     * 获取 ApplicationContext。Unity 主线程下 UnityPlayer.currentActivity 可用。
     */
    private static Context getApplicationContext() {
        try {
            if (UnityPlayer.currentActivity != null) {
                return UnityPlayer.currentActivity.getApplicationContext();
            }
        } catch (Throwable ignored) {
        }
        return null;
    }
}
