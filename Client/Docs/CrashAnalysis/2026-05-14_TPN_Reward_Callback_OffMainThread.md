# 崩溃分析：TopOn 激励视频回调在子线程触发 IL2CPP fail-fast

> 单次设备崩溃报告。日志来源：`Xiaomi-23053RN02A-Android-13_2026-05-14_143616.txt`（filter: `level:error package:com.heartbloom.magic.sweet.match.free`）。

## 一句话结论

TopOn (`com.secmtp.sdk.unitybridge`) 在自己的 Worker 线程池上同步执行激励视频回调，C# 端 `CoreTpnReward` 的 `OnRewardEvent / OnAdVideoClose` 以及业务侧 `_rvCloseCallback`（最终调到 `Panel_LevelPhoto.StartGame` 等 Unity API）全部跑在子线程，IL2CPP 检测到非主线程访问引擎 API 后主动 `tgkill(SIGTRAP)` 自杀。**唯一修复点是把 TPN 回调切回主线程**。

---

## 一、崩溃元信息

| 项 | 值 |
|---|---|
| 包名 | `com.heartbloom.magic.sweet.match.free`（Heartbloom Magic: Sweet Match） |
| 设备 | Xiaomi 23053RN02A（Redmi fire_global） |
| 系统 | Android 13（fingerprint `Redmi/fire_global/fire:13/TP1A.220624.014/V14.0.12.0.TMXMIXM`） |
| ABI | arm64 |
| 进程 pid | 2652 |
| 崩溃 tid / 线程名 | **3902 / `pool-62-thread-`**（非主线程；主线程 tid = 2652） |
| 崩溃信号 | **`signal 5 (SIGTRAP), code -6 (SI_TKILL)`**（**主动 abort**，不是被动崩溃） |
| 触发模块 | `libil2cpp.so` (BuildId `03600b8e09fc920e4456202904a41f07763797d9`) → `libunity.so` (BuildId `85c102b5ddea0024`) → `libc.so::tgkill` |
| 崩溃时间 | 2026-05-14 14:35:12.735731418 +0800 |
| 进程已运行 | 70s |

> `SI_TKILL` 表示是进程内的某个线程主动调 `tgkill` 杀自己——这是 Unity IL2CPP fail-fast 的标志，**不会**出现在野指针/越界这类被动崩溃中。

---

## 二、崩溃 backtrace（自底向上）

```
#52  libc.so          __pthread_start
#51  libart.so        art::Thread::CreateCallback
#48  boot.oat         java.lang.Thread.run
#47  boot.oat         java.util.concurrent.ThreadPoolExecutor$Worker.run
#46  boot.oat         java.util.concurrent.ThreadPoolExecutor.runWorker
#45  base.vdex        com.secmtp.sdk.unitybridge.utils.Worker.run             ← TopOn Worker 线程池
#43  base.vdex        com.secmtp.sdk.unitybridge.utils.TaskManager$1.work
#41  base.vdex        com.secmtp.sdk.unitybridge.videoad.VideoHelper$1$11.run ← 视频广告回调 Runnable
#38  libart.so        artQuickProxyInvokeHandler
#35  boot.oat         java.lang.reflect.Proxy.invoke
#34  base.vdex        com.unity3d.player.L.invoke                              ← Unity AndroidJavaProxy
#32  base.vdex        com.unity3d.player.ReflectionHelper.nativeProxyInvoke    ← JNI 入 Native
#29~#27 libunity.so
#26~#05 libil2cpp.so                                                            ← 约 22 帧 C# 回调链
#04~#01 libunity.so   (IL2CPP fail-fast)
#00  libc.so          tgkill+8                                                   ← 主动自杀
```

`secmtp` = TopOn / AnyThinkAds 的内部包名；`VideoHelper` 是其 Unity Bridge 的视频广告辅助类。

---

## 三、业务时间线（带日志证据）

| 时间 | 线程 | 事件 | 日志位置（行） |
|---|---|---|---|
| 14:34:13 | 主线程 2652 | 游戏正常进入选关 | — |
| 14:34:28 | 主线程 2652 | `Panel_LevelPhoto.EnterGame` 点击 → `SDKManager.Button_ShowRV(RV_CrushLevel / RV_LevelSelect / RV_LevelSelect_2)` | 45620 ~ 45935 |
| 14:34:28 | 主线程 2652 | TPN 与 Mintegral 竞价；TPN(`0.0055425`) 胜 Mintegral(`0.0053575`)，调用 `ATRewardedVideo.showAd("n69ff1bee70ca8")` | 45860 ~ 46220 |
| 14:34:30 ~ 14:35:10 | — | Mintegral 通过 `MBRewardVideoActivity` 播放视频 | 50870、95000 |
| **14:35:10.804** | 主线程 2652 | TopOn Java：`onReward: n69ff1bee70ca8` | 205175 |
| **14:35:10.806** | **tid 3902** | C# 进入：`AnyThinkAds.ATRewardedVideoAdClient:onReward` → `CoreTpnReward.OnRewardEvent` | 205250 ~ 205265 |
| 14:35:10.810 | **tid 3902** | C# 进入：`ATRewardedVideoAdClient:onRewardedVideoAdClosed` → `CoreTpnReward.OnAdVideoClose` | 205325 ~ 205340 |
| 14:35:10.813 | **tid 3902** | **`_rvCloseCallback` 闭包在子线程执行**：`CoreAdsMgr.<>c__DisplayClass16_0:<ShowRewardedVideo>b__1` | 205355 |
| 14:35:10.816 | **tid 3902** | `CoreAdjustEvents.TrackEvent("RV_Close")` 仍在子线程 | 205370 |
| **14:35:12.391** | **tid 3902** | IL2CPP fail-fast → `tgkill(3902, SIGTRAP)` | 226865 |
| 14:35:12.703 | crash_dump64 | tombstoned 收到 pid 3902 的崩溃请求 | 222875 |

关键证据原文（崩溃前最后一段 C# 调用都标记 `tid: 3902`，参见 line 205180 ~ 205370）：

```
pid: 2652, tid: 3902, tag: "Unity"
  [CoreLite] [CoreLiteTpn] Reward OnRewardEvent: n69ff1bee70ca8
  [CoreLite] [CoreLiteTpn] Reward OnAdVideoClose: n69ff1bee70ca8
    CoreLite.CoreTpnReward:OnAdVideoClose(Object, ATAdEventArgs)
  [CoreLite]  [ads] rewarded video close callback
    CoreLite.<>c__DisplayClass16_0:<ShowRewardedVideo>b__1(Dictionary`2)
  [CoreLite] CoreLite adjust track event: RV_Close
```

---

## 四、根因

TopOn 把 `onReward / onRewardedVideoAdClosed / ...` 通过 `AndroidJavaProxy` 直接在自己的 `Worker` 线程池上同步代理到 C# 端。**项目侧没有把这些回调 marshal 回 Unity 主线程**，导致下面整条链路全部在子线程：

```104:113:Assets/CoreLite/Base/Ads/Tpn/Ads/CoreTpnReward.cs
private void RegisterCallbacks()
{
    ATRewardedVideo.Instance.client.onAdLoadEvent += OnAdLoad;
    ATRewardedVideo.Instance.client.onAdLoadFailureEvent += OnAdLoadFail;
    ATRewardedVideo.Instance.client.onAdVideoStartEvent += OnAdVideoStart;
    ATRewardedVideo.Instance.client.onAdVideoFailureEvent += OnAdVideoPlayFail;
    ATRewardedVideo.Instance.client.onRewardEvent += OnRewardEvent;
    ATRewardedVideo.Instance.client.onAdVideoCloseEvent += OnAdVideoClose;
    ATRewardedVideo.Instance.client.onAdClickEvent += OnAdClick;
}
```

```243:274:Assets/CoreLite/Base/Ads/Tpn/Ads/CoreTpnReward.cs
private void OnAdVideoClose(object sender, ATAdEventArgs e)
{
    ...
    CoreAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object> { ... });
    ...
    LoadReward(placementId);
}
```

业务侧的 `_rvCloseCallback` 落到玩家点击的那个广告位回调上：

```198:220:Assets/GameScripts/Game/GameLogic/UI/Panel_LevelPhoto.cs
SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_CrushLevel, (success) =>
{
    if (success)
    {
        StartGame(info);   // CloseUI<Panel_Home>() / Button_HideBanner() / ShowUI<Panel_Game>()
    }
});
```

`StartGame` 内部全是 `GameModule.UI.CloseUI / ShowUI / Button_HideBanner` 等 **Unity 主线程独占 API**。子线程一调，IL2CPP 触发 fail-fast → SIGTRAP。

---

## 五、影响面

1. **TPN 全部广告回调**：`CoreTpnReward.OnAdLoad / OnAdLoadFail / OnAdVideoStart / OnAdVideoPlayFail / OnRewardEvent / OnAdVideoClose / OnAdClick` 全部存在该隐患（任何一处后续访问了 Unity API 都会触发崩溃）。
2. **Mintegral 直连**：`Assets/CoreLite/Base/Ads/MTG/Ads/CoreMtgRewardListener.cs` 同样直接在 SDK 回调里调 `CoreAdsMgr.Instance._rvCloseCallback / _rvShowCallback / _rvShowFailCallback`，未保证主线程，**同样有此风险**。
3. **业务回调位点**（已检索到 9 处 `Button_ShowRV` 调用）全部依赖回调在主线程执行：
   - `Panel_LevelPhoto.cs`、`Panel_FailView.cs`、`Panel_SucceedView.cs`、`Panel_PhotoDetails.cs`、`Panel_BuyOnePropView.cs`、`Panel_GetRewardView.cs`、`Panel_CollectDiamondView.cs`、`Panel_Gacha.cs`、`Widget_SlotPosView.cs`、`Widget_OneDiaProductView.cs`、`RechargeController.cs`、`PropsController.cs`。

---

## 六、修复方案

遵循奥卡姆剃刀 —— 只动一处，覆盖所有平台、所有业务。

### 方案 B（推荐）：在 `CoreAdsMgr` 层统一切主线程

理由：`CoreAdsMgr._rvShowCallback / _rvCloseCallback / _rvShowFailCallback` 是所有广告平台（TPN/MTG/...）的公共出口，**只此一处统一 marshal 到主线程**，所有平台自动免疫。

实施要点：
- 在 `CoreAdsMgr.Init` 阶段创建一个轻量 `MainThreadDispatcher`（`MonoBehaviour.Update` + `ConcurrentQueue<Action>`），或直接用 `Cysharp.Threading.Tasks.UniTask.Post / SwitchToMainThread`（项目已用 UniTask）。
- 改造 `_rvShowCallback / _rvCloseCallback / _rvShowFailCallback` 的 setter 或在 `Invoke` 处包一层，强制在主线程触发。

示意（UniTask 版本）：

```csharp
private static void InvokeOnMain(Action action)
{
    if (action == null) return;
    if (PlayerLoopHelper.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId)
        action();
    else
        UniTask.Post(action);   // 切回 Unity 主线程下一帧调用
}

private static void InvokeOnMain<T>(Action<T> action, T arg)
{
    if (action == null) return;
    if (PlayerLoopHelper.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId)
        action(arg);
    else
        UniTask.Post(() => action(arg));
}
```

然后把 `CoreTpnReward / CoreMtgRewardListener` 内所有 `CoreAdsMgr.Instance._rvXxxCallback?.Invoke(...)` 替换为 `CoreAdsMgr.Instance.InvokeRvXxx(...)`，由 `CoreAdsMgr` 内部统一过 `InvokeOnMain`。

### 方案 A（备选）：在 `CoreTpnReward` 每个回调入口切主线程

只解决 TPN 一家，但侵入面小，改动局限。

```csharp
private async void OnAdVideoClose(object sender, ATAdEventArgs e)
{
    await UniTask.SwitchToMainThread();
    // ...原 OnAdVideoClose 主体...
}
```

对 7 个 `OnXxx` 全部如此处理。**注意 MTG 一侧需要同样改造。**

### 不推荐

- 改广告 SDK：黑盒成本高，升级一次又破。
- 在每个业务回调里加 `UnityMainThreadDispatcher`：散落在十几处面板里，难以维护。

---

## 七、验证思路

1. **复现**：用 Mintegral/TPN 实际激励视频，连续看 5 ~ 10 次"看广告 → 完整看完 → 关闭" 流程，覆盖 `RV_CrushLevel / RV_Revive / RV_DownloadPicture / RV_Chest / RV_Slot` 等多个位点。Heartbloom 当前版本必崩，修复后应稳定不崩。
2. **断言**：在 `_rvCloseCallback` 入口加一行临时断言 `Debug.Assert(System.Threading.Thread.CurrentThread.ManagedThreadId == PlayerLoopHelper.MainThreadId)`；修复前会命中，修复后不会。
3. **回归**：保证激励视频"看完发奖→`StartGame` 进入下一关 / `SaveVideoAndPhotoToGallery`"等关键路径功能正常。

---

## 八、归档信息

- 日志原文：`d:\Download\Xiaomi-23053RN02A-Android-13_2026-05-14_143616.txt`
- 相关代码：
  - `Assets/CoreLite/Base/Ads/CoreAdsMgr.cs`
  - `Assets/CoreLite/Base/Ads/Tpn/Ads/CoreTpnReward.cs`
  - `Assets/CoreLite/Base/Ads/MTG/Ads/CoreMtgRewardListener.cs`
  - `Assets/GameScripts/Game/GameLogic/CommonSystem/SDKSys/SDKManager.Ads.cs`
  - `Assets/GameScripts/Game/GameLogic/UI/Panel_LevelPhoto.cs`（以及上文 §五.3 列出的其它面板）
- 关键 placementId：`n69ff1bee70ca8`（Rewarded，Mintegral 中介 unitid `6007679`）
