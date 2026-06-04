package com.unity3d.player;

import android.util.Log;
import androidx.annotation.Keep;

import com.flagdrum.sdk.FDSdk;
import com.flagdrum.sdk.api.event.FDEvent;

import java.util.Arrays;

/**
 * 广告和用户事件埋点工具类
 * 负责处理 Topon/Max 广告的展示、点击埋点，以及用户事件（注册/登录）、渠道、AB测试的埋点
 */
@Keep
public final class Up {
    private static final String TAG = "[UpUpUp]";

    // 私有构造方法，确保无法实例化（模拟 Kotlin 的 object 单例）
    private Up() {
        throw new AssertionError("Utility class cannot be instantiated");
    }

    /**
     * Topon 广告展示埋点
     * @param toponFirmId Topon 广告平台ID
     * @param adType 广告类型（NATIVE/BANNER/INTERSTITIAL/SPLASH/REWARD_VIDEO）
     * @param adId 广告位ID
     * @param ecpm 广告千次展示收益
     * @param showId 展示ID（可选）
     */
    public static void onToponAdShow(
            int toponFirmId,
            String adType,
            String adId,
            double ecpm,
            String showId
    ) {
        logAdEvent("onToponAdShow",
                "toponFirmId", toponFirmId,
                "adType", adType,
                "adId", adId,
                "ecpm", ecpm,
                "showId", showId);
        onAdShow(FirmIdTools.getNetworkName(toponFirmId), adType, adId, ecpm, showId);
    }

    /**
     * Max 广告展示埋点
     * @param maxPlatform Max 广告平台名称
     * @param adType 广告类型（NATIVE/BANNER/INTERSTITIAL/SPLASH/REWARD_VIDEO）
     * @param adId 广告位ID
     * @param ecpm 广告千次展示收益
     */
    public static void onMaxAdShow(
            String maxPlatform,
            String adType,
            String adId,
            double ecpm
    ) {
        logAdEvent("onMaxAdShow",
                "maxPlatform", maxPlatform,
                "adType", adType,
                "adId", adId,
                "ecpm", ecpm);
        onAdShow(FirmIdTools.getMaxAdPlatform(maxPlatform), adType, adId, ecpm, "");
    }

    /**
     * Topon 广告点击埋点
     * @param toponFirmId Topon 广告平台ID
     * @param adType 广告类型（NATIVE/BANNER/INTERSTITIAL/SPLASH/REWARD_VIDEO）
     * @param adId 广告位ID
     * @param showId 展示ID（可选）
     */
    public static void onToponAdClick(
            int toponFirmId,
            String adType,
            String adId,
            String showId
    ) {
        logAdEvent("onToponAdClick",
                "toponFirmId", toponFirmId,
                "adType", adType,
                "adId", adId,
                "showId", showId);
        onAdClick(FirmIdTools.getNetworkName(toponFirmId), adType, adId, showId);
    }

    /**
     * Max 广告点击埋点
     * @param maxPlatform Max 广告平台名称
     * @param adType 广告类型（NATIVE/BANNER/INTERSTITIAL/SPLASH/REWARD_VIDEO）
     * @param adId 广告位ID
     */
    public static void onMaxAdClick(
            String maxPlatform,
            String adType,
            String adId
    ) {
        logAdEvent("onMaxAdClick",
                "maxPlatform", maxPlatform,
                "adType", adType,
                "adId", adId);
        onAdClick(FirmIdTools.getMaxAdPlatform(maxPlatform), adType, adId, "");
    }

    /**
     * 用户事件埋点（注册/登录）
     * @param eventName 事件名称（REGISTER/LOGIN）
     */
    public static void onUserEvent(String eventName) {
        Log.d(TAG, "onUserEvent => eventName:" + eventName);
        if (!FirmIdTools.exists()) return;

        try {
            FDEvent.UserEvent userEvent = FDEvent.UserEvent.valueOf(eventName);
            FDSdk.getEventManager().onUserEvent(userEvent);
        } catch (IllegalArgumentException e) {
            Log.e(TAG, "无效的用户事件名称: " + eventName, e);
        }
    }

    /**
     * 设置渠道
     * @param channel 渠道名称
     */
    public static void setChannel(String channel) {
        Log.d(TAG, "setChannel => channel:" + channel);
        if (!FirmIdTools.exists()) return;

        FDSdk.getAdManager().setChannel(channel);
    }

    /**
     * 设置AB测试信息
     * @param testName 测试名称
     * @param testVersion 测试版本号
     * @param userTags 用户标签（可变参数）
     */
    public static void setABTest(String testName, int testVersion, String... userTags) {
        Log.d(TAG, "setABTest => testName:" + testName + ", testVersion:" + testVersion + ", userTags:" + Arrays.toString(userTags));
        if (!FirmIdTools.exists()) return;

        FDSdk.getEventManager().setABTest(testName, testVersion, userTags);
    }

    /**
     * 通用广告展示埋点（内部调用）
     * @param adPlatform 广告平台名称
     * @param adType 广告类型
     * @param adId 广告位ID
     * @param ecpm 广告千次展示收益
     * @param showId 展示ID（可选）
     */
    private static void onAdShow(
            String adPlatform,
            String adType,
            String adId,
            double ecpm,
            String showId
    ) {
        if (!FirmIdTools.exists()) return;

        try {
            FDEvent.AdPlatform platform = FDEvent.AdPlatform.valueOf(adPlatform);
            FDEvent.AdType type = FDEvent.AdType.valueOf(adType);

            if (showId.isEmpty()) {
                FDSdk.getEventManager().onAdShow(platform, adId, type, ecpm);
            } else {
                FDSdk.getEventManager().onAdShow(platform, adId, type, ecpm, showId);
            }
        } catch (IllegalArgumentException e) {
            Log.e(TAG, "广告展示埋点失败：无效的平台/类型参数 | adPlatform:" + adPlatform + ", adType:" + adType, e);
        }
    }

    /**
     * 通用广告点击埋点（内部调用）
     * @param adPlatform 广告平台名称
     * @param adType 广告类型
     * @param adId 广告位ID
     * @param showId 展示ID（可选）
     */
    private static void onAdClick(
            String adPlatform,
            String adType,
            String adId,
            String showId
    ) {
        if (!FirmIdTools.exists()) return;

        try {
            FDEvent.AdPlatform platform = FDEvent.AdPlatform.valueOf(adPlatform);
            FDEvent.AdType type = FDEvent.AdType.valueOf(adType);

            if (showId.isEmpty()) {
                FDSdk.getEventManager().onAdClick(platform, adId, type);
            } else {
                FDSdk.getEventManager().onAdClick(platform, adId, type, showId);
            }
        } catch (IllegalArgumentException e) {
            Log.e(TAG, "广告点击埋点失败：无效的平台/类型参数 | adPlatform:" + adPlatform + ", adType:" + adType, e);
        }
    }

    /**
     * 统一的广告事件日志打印方法
     * @param eventName 事件名称
     * @param params 日志参数（键值对，需成对传入）
     */
    private static void logAdEvent(String eventName, Object... params) {
        StringBuilder paramsStr = new StringBuilder();
        for (int i = 0; i < params.length; i += 2) {
            if (i > 0) {
                paramsStr.append(" ");
            }
            String key = params[i].toString();
            Object value = (i + 1 < params.length) ? params[i + 1] : "";
            paramsStr.append(key).append(":").append(value);
        }
        Log.d(TAG, eventName + " => " + paramsStr);
    }
}