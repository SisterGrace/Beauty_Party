package com.unity3d.player;

import android.content.Context;
import android.text.TextUtils;
import android.util.Log;
import androidx.annotation.Keep;

import com.reyun.solar.engine.OnInitializationCallback;
import com.reyun.solar.engine.SolarEngineConfig;
import com.reyun.solar.engine.SolarEngineManager;

/**
 * 【新增】热力引擎（SolarEngine / 热云）Android 桥接类。
 * 数据区域：海外（com.reyun.solar.engine.oversea）  SDK 版本：1.3.1.4
 *
 * 用途：
 *   - 给 Unity 端 SolarEngineBridge.cs 提供 preInit / initialize 入口
 *   - 内部统一处理空值、Context 获取、异常兜底，避免 Unity 端反射调用崩溃
 *
 * 调用顺序（参考热云官方文档）：
 *   1. preInit(appKey)               — 启动后立即调用，仅初始化内部状态，不上报数据
 *   2. initialize(appKey, enableLog) — 用户同意《隐私政策》后调用，开始正式上报
 *
 * 当前为最小验证版本：preInit 与 initialize 在 SDKManager.Init() 中连续调用。
 * 文档：https://docs.solar-engine.com
 */
@Keep
public final class SolarEngineProxy {
    private static final String TAG = "[SolarEngineProxy]";

    private SolarEngineProxy() {
        throw new AssertionError("Utility class cannot be instantiated");
    }

    /**
     * 预初始化 SDK。需要在用户同意《隐私政策》前就可以调用，此阶段不会上报任何数据。
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
            // 兜底：任何异常都吞掉，避免影响游戏主流程
            Log.e(TAG, "preInit threw exception", t);
        }
    }

    /**
     * 正式初始化 SDK。须在用户同意《隐私政策》后调用。
     *
     * @param appKey    后台「资产管理 - 应用管理」获取的 16 位 AppKey
     * @param enableLog 是否打开 SDK 本地调试日志（灰度阶段建议打开）
     */
    public static void initialize(String appKey, boolean enableLog) {
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
            SolarEngineConfig config = builder.build();
            // 通过回调 code 判断初始化结果，code == 0 视为成功，其他 code 含义见官方文档
            SolarEngineManager.getInstance().initialize(ctx, appKey, config,
                new OnInitializationCallback() {
                    @Override
                    public void onInitializationCompleted(int code) {
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
