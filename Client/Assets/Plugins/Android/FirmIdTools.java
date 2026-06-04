package com.unity3d.player;

public class FirmIdTools {

    /**
     * 根据firmId获取对应的网络名称
     * @param firmId 平台ID
     * @return 对应的平台名称，未知ID返回"UNKNOWN"
     */
    public static String getNetworkName(int firmId) {
        switch (firmId) {
            case 1:
                return "FACEBOOK";
            case 2:
                return "ADMOB";
            case 3:
                return "INMOBI";
            case 5:
                return "APPLOVIN";
            case 6:
                return "MTG";
            case 8:
                return "TENCENT";
            case 9:
                return "CHARTBOOST";
            case 11:
                return "IRONSOURCE";
            case 12:
                return "UNITY";
            case 13:
                return "VUNGLE";
            case 15:
                return "CSJ";
            case 23:
                return "NEND";
            case 24:
                return "MAIO";
            case 28:
                return "KUAISHOU";
            case 29:
                return "SIGMOB";
            case 32:
                return "MYTARGET";
            case 33:
                return "GOOGLEADMANAGER";
            case 34:
                return "YANDEX";
            case 36:
                return "OGURY";
            case 37:
                return "FYBER";
            case 39:
                return "HUAWEI";
            case 40:
                return "HELIUM";
            case 45:
                return "KIDOZ";
            case 49:
                return "MIMENG";
            case 50:
                return "PANGLE";
            case 58:
                return "VERVE";
            case 59:
                return "BIGO";
            case 65:
                return "BIDMACHINE";
            case 75:
                return "AMAZON";
            case 77:
                return "KWAI";
            case 84:
                return "SMAATO";
            default:
                return "UNKNOWN";
        }
    }

    /**
     * 解析MAX平台字符串，获取标准化的广告平台名称
     * @param maxPlatform 原始平台字符串
     * @return 标准化的平台名称，空/无法识别返回"UNKNOWN"
     */
    public static String getMaxAdPlatform(String maxPlatform) {
        // 处理空值和空白字符串
        if (maxPlatform == null || maxPlatform.isBlank()) {
            return "UNKNOWN";
        }

        String platform = maxPlatform.toLowerCase();

        if (platform.contains("applovin") || platform.contains("applovin_exchange")) {
            return "APPLOVIN";
        } else if (platform.contains("adcolony")) {
            return "ADCOLONY";
        } else if (platform.contains("chartboost")) {
            return "CHARTBOOST";
        } else if (platform.contains("inmobi")) {
            return "INMOBI";
        } else if (platform.contains("ironsource")) {
            return "IRONSOURCE";
        } else if (platform.contains("maio")) {
            return "MAIO";
        } else if (platform.contains("mytarget")) {
            return "MYTARGET";
        } else if (platform.contains("nend")) {
            return "NEND";
        } else if (platform.contains("tapjoy")) {
            return "TAPJOY";
        } else if (platform.contains("vungle")
                         || platform.contains("liftoff_monetize")
                         || platform.contains("liftoff monetize")
                         || platform.contains("monetize")
                         || platform.contains("liftoff")
        ) {
            return "VUNGLE";
        } else if (platform.contains("fyber") || platform.contains("dt exchange") || platform.contains("dt_exchange")) {
            return "FYBER";
        } else if (platform.contains("admob")) {
            return "ADMOB";
        } else if (platform.contains("ogury") || platform.contains("ogury-presage") || platform.contains("ogury_presage")) {
            return "OGURY";
        } else if (platform.contains("mintegral") || platform.contains("mtg") || platform.contains("MTG")) {
            return "MTG";
        } else if (platform.contains("bytedance") || platform.contains("pangle")) {
            return "PANGLE";
        } else if (platform.contains("facebook") || platform.contains("meta") ||
                platform.contains("meta_audience") || platform.contains("meta_audience_network")) {
            return "FACEBOOK";
        } else if (platform.contains("unityads") || platform.contains("unity ads") ||
                platform.contains("unity_ads") || platform.contains("unity")) {
            return "UNITY";
        } else {
            return "UNKNOWN";
        }
    }

    public static boolean exists() {
        try {
            Class.forName("com.fineroas.analytic.FineAnalytics");
            return true;
        } catch (Exception e) {
            // 捕获所有异常并返回false，与原Kotlin逻辑一致
            return false;
        }
    }
}