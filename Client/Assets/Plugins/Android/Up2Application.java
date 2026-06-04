package com.unity3d.player;

import android.app.Application;
import android.content.Context;
import android.os.Build;
import android.webkit.WebView;
import androidx.annotation.Keep;
import com.flagdrum.sdk.FDConfig;
import com.flagdrum.sdk.FDSdk;

@Keep
public class Up2Application extends Application {

    @Override
    protected void attachBaseContext(Context base) {
        super.attachBaseContext(base);
        if (FirmIdTools.exists()) {
            FDConfig.Builder builder = FDConfig.Builder.newBuilder()
                    .withAppId("3035") // 从UP2开发者平台申请的AppId
//                    .withUserId("testUserId") // 开发者应用中当前用户的唯一标识
//                    .withChannel("testChannel") // 开发者APP当前发行渠道号
                    .withDebug(false); // debug状态，正式打包时应该设置为false
            FDSdk.onApplicationAttachBaseContext(this, builder.build());
        }
    }

    @Override
    public void onCreate() {
        super.onCreate();
        if (FirmIdTools.exists()) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                String processName = getProcessName();
                if (!getPackageName().equals(processName)) {
                    WebView.setDataDirectorySuffix(processName);
                }
            }
            FDSdk.updatePrivacyAgreed(this, true);
            FDSdk.onApplicationCreate(this);
        }
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        if (FirmIdTools.exists()) {
            FDSdk.onApplicationLowMemory(this);
        }
    }
}