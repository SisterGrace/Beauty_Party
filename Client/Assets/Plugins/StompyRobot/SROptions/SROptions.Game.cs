using Config.common;
using CoreLite;
using GameLogic;
using Sky;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TK_Engine;
using UnityEngine;
using Config;
using EventType = GameLogic.EventType;

public partial class SROptions
{
    public int Levelid;
    [Category("SetDataByID")]
    [DisplayName("ID")]
    [Sort(000)]
    public int ID
    {
        get { return Levelid; }
        set { Levelid = value; }
    }
    [Category("SetDataByID")]
    [DisplayName("SetStage")]
    [Sort(001)]
    public void SetLevel()
    {
        GameEvent.Send(EventType.SetLevel, Levelid);
    }

    [Category("SetData")]
    [DisplayName("金币+1000")]
    [Sort(001)]
    public void SetGold()
    {
        GameEvent.Send(EventType.SetGold);
    }
    [Category("SetData")]
    [DisplayName("改造体力+100")]
    [Sort(003)]
    public void SetStamina()
    {
        GameEvent.Send(EventType.SetStamina);
    }

    [Category("SetData")]
    [DisplayName("无敌时间")]
    [Sort(004)]
    public void SetLife()
    {
        GameEvent.Send(EventType.SetInvincibleLife);
    }
    [Category("SetData")]
    [DisplayName("撤回道具+3")]
    [Sort(005)]
    public void AddRevoke()
    {
        GameEvent.Send(EventType.SetProps, PropType.Revoke);
    }
    [Category("SetData")]
    [DisplayName("打乱道具+3")]
    [Sort(006)]
    public void AddMessUp()
    {
        GameEvent.Send(EventType.SetProps, PropType.MessUp);
    }
    [Category("SetData")]
    [DisplayName("销毁道具+3")]
    [Sort(007)]
    public void AddDestoryGroup()
    {
        GameEvent.Send(EventType.SetProps, PropType.DestoryGruop);
    }
    [Category("SetData")]
    [DisplayName("移出道具+3")]
    [Sort(008)]
    public void AddMoveOut()
    {
        GameEvent.Send(EventType.SetProps, PropType.MoveOut);
    }
    [Category("SetData")]
    [DisplayName("提示道具+3")]
    [Sort(009)]
    public void AddPrompt()
    {
        GameEvent.Send(EventType.SetProps, PropType.Prompt);
    }

    [Category("SetData")]
    [DisplayName("加槽道具+3")]
    [Sort(010)]
    public void AddAddSolt()
    {
        GameEvent.Send(EventType.SetProps, PropType.AddSolt);
    }


    [Category("ADFree")]
    [DisplayName("免广告")]
    [Sort(001)]
    public void SetADFree()
    {
        GameEvent.Send(EventType.SetAdFree);
    }
    [Category("ADFree")]
    [DisplayName("打开广告")]
    [Sort(002)]
    public void OpenAD()
    {
        GameEvent.Send(EventType.OpenAd);
    }

    private int _curGoldNum;

    [Category("黄金狂潮")]
    [DisplayName("金块数量")]
    [Sort(001)]
    public int CurGoldNum
    {
        get { return _curGoldNum; }
        set { _curGoldNum = value; }
    }

    [Category("黄金狂潮")]
    [DisplayName("设置数量")]
    [Sort(002)]
    public void SetGoldNum()
    {
        GameEvent.Send(EventType.AddGoldRush, CurGoldNum);
    }

    private int _sroPayType;
    [Category("内购设置")]
    [DisplayName("1-谷歌，2-第三方")]
    [Sort(001)]
    public int SroPayType
    {
        get { return _sroPayType; }
        set { _sroPayType = value; }
    }
    [Category("内购设置")]
    [DisplayName("设置支付方式")]
    [Sort(002)]
    public void SetPayType()
    {
        GameEvent.Send(EventType.setPayType, SroPayType);
    }
    private string _sroPayCountry;

    [Category("内购设置")]
    [DisplayName("支付国家")]
    [Sort(003)]
    public string SroPayCountry
    {
        get { return _sroPayCountry; }
        set { _sroPayCountry = value; }
    }
    [Category("内购设置")]
    [DisplayName("设置支付国家")]
    [Sort(004)]
    public void SetPayCountry()
    {
        GameEvent.Send(EventType.SetPayCountry, SroPayCountry);
    }

    [Category("开关UI")]
    [DisplayName("打开UI")]
    [Sort(001)]
    public void OpenGameUI()
    {
        GameEvent.Send(EventType.SetGameUIShow, false);
    }
    [Category("开关UI")]
    [DisplayName("关闭UI")]
    [Sort(002)]
    public void CloseGameUI()
    {
        GameEvent.Send(EventType.SetGameUIShow, true);
    }

    [Category("活动")]
    [DisplayName("打开所有活动")]
    [Sort(001)]
    public void OpenAllActivity()
    {
        GameEvent.Send(EventType.SetAllActivity, true);
    }
    [Category("活动")]
    [DisplayName("关闭所有活动")]
    [Sort(002)]
    public void CloseAllActivity()
    {
        GameEvent.Send(EventType.SetAllActivity, false);
    }

    [Category("活动")]
    [DisplayName("黄金狂潮结束")]
    [Sort(003)]
    public void EndGoldRush()
    {
        GameEvent.Send(EventType.QuicklyEndGoldRush);
    }

    [Category("活动")]
    [DisplayName("通行证结束")]
    [Sort(004)]
    public void EndPassport()
    {
        GameEvent.Send(EventType.QuicklyEndPassport);
    }

    /* private int _userType;

     [Category("A/B用户")]
     [DisplayName("用户类型设置")]
     [Sort(000)]
     public int MyUserType
     {
         get { return _userType; }
         set { _userType = value; }
     }

     [Category("A/B用户")]
     [DisplayName("用户类型设置")]
     [Sort(001)]
     public void SetUserType()
     {
         GameEvent.Send(EventType.SetUserType, MyUserType);
     }*/
    [Category("SDK")]
    // 点击计数 & 解锁状态
    private int _abUnlockClicks = 0;
    private bool _abUnlocked = false;
    private SROptions_AB _abOptions;
    private float _lastClickTime = -10f;          // 上次点击时间
    private const float INTERVAL_LIMIT = 1.3f;    // 允许的最大间隔（秒）

    // 不显示
    public string 解锁AB面进度
    {
        get => _abUnlocked ? "已解锁" : $"{_abUnlockClicks}/5";
    }

    // 解锁按钮
    [Category("SDK")]
    public void TEST()
    {
        if (_abUnlocked) return;

        float now = Time.realtimeSinceStartup;
        // 如果距离上次点击超过 1.5 秒，重置计数
        if (now - _lastClickTime > INTERVAL_LIMIT)
        {
            _abUnlockClicks = 0;
        }
        _lastClickTime = now;

        _abUnlockClicks++;
        if (_abUnlockClicks >= 7)
        {
            _abUnlocked = true;
            if (_abOptions == null) _abOptions = new SROptions_AB();
            SRDebug.Instance.AddOptionContainer(_abOptions);
            SRDebug.Instance?.ShowDebugPanel(true);
        }
    }
    private string _testPhotoRewardJson = "";

    [Category("远程配置测试")]
    [DisplayName("奖励JSON")]
    [Sort(001)]
    public string TestPhotoRewardJson
    {
        get { return _testPhotoRewardJson; }
        set { _testPhotoRewardJson = value; }
    }

    [Category("远程配置测试")]
    [DisplayName("测试照片奖励覆盖")]
    [Sort(002)]
    public void TestPhotoRewardOverride()
    {
        GameEvent.Send(EventType.TestPhotoRewardOverride, TestPhotoRewardJson);
    }

    private string _testIAPToIAAJson = "";

    [Category("远程配置测试")]
    [DisplayName("IAP→IAA JSON")]
    [Sort(003)]
    public string TestIAPToIAAJson
    {
        get { return _testIAPToIAAJson; }
        set { _testIAPToIAAJson = value; }
    }

    [Category("远程配置测试")]
    [DisplayName("测试IAP转IAA")]
    [Sort(004)]
    public void TestIAPToIAAOverride()
    {
        GameEvent.Send(EventType.TestIAPToIAAOverride, TestIAPToIAAJson);
    }

    [Category("七日签到")]
    [DisplayName("跳过今日签到（模拟下一天）")]
    [Sort(001)]
    public void SkipCheckInDay()
    {
        GameEvent.Send(EventType.TestSkipCheckInDay);
    }

    private bool _isASN;
    [Category("SDK")]
    [DisplayName("ASN")]
    [Sort(002)]
    public bool IsASN
    {
        get { return _isASN; }
        set { _isASN = value; }
    }
    [Category("SDK")]
    [DisplayName("设置ASN")]
    [Sort(003)]
    public void SetUserASN()
    {
        GameEvent.Send(EventType.SetUserASN, IsASN);
    }

    [Category("插屏广告")]
    [DisplayName("插屏1")]
    [Sort(001)]
    public void ShowInter1()
    {
        CoreSdkMgr.ShowInterstitial("IV_Back", CoreLite.IvType.IV1, () =>
        {
        });
    }
    [Category("插屏广告")]
    [DisplayName("插屏2")]
    [Sort(002)]
    public void ShowInter2()
    {
        CoreSdkMgr.ShowInterstitial("IV_Fail", CoreLite.IvType.IV2, () =>
        {
        });
    }

/*#if UNITY_EDITOR
    // 方式一：通过右键组件菜单触发（Inspector 中右键点击组件）
    [Category("发送买量用户信息")]
    public void Editor_ForcePaidUser()
    {
        // 1. 重置持久化状态，防止被 IsFinalized 拦截至（如果已经归因过）
        SkySolarEnginePrefs.HasAttribution = false;
        SkySolarEnginePrefs.IsFinalized = false;
        SkySolarEnginePrefs.IsPaidUser = false;
        SkySolarEnginePrefs.Network = "";
        SkySolarEnginePrefs.Campaign = "";
        SkySolarEnginePrefs.Adgroup = "";
        SkySolarEnginePrefs.Creative = "";
        SkySolarEnginePrefs.ClickLabel = "";
        // 3. 模拟一个典型的买量渠道（例如：巨量引擎、Meta、Unity Ads 等）
        string mockNetwork = "TestNetwork_Meta";
        string mockCampaign = "Test_Campaign_2026";
        string mockAdgroup = "Test_Adgroup_01";
        string mockCreative = "Test_Creative_Red";
        string mockClickId = "click_test_abc123";

        // 4. 调用私有方法标记为付费用户
        // 由于 FinalizeAsPaid 是 private，但在同一个类中可以直接调用
        SkySolarEngineMgr.Instance.FinalizeAsPaid(mockNetwork, mockCampaign, mockAdgroup, mockCreative, mockClickId);

        LogTool.DebugLog("[SolarEngine Test] 强制模拟为买量用户（非自然量）已生效！", LogTool.Color.green);
    }
#endif*/
}

public class SROptions_AB
{
    EABUserType _type = (EABUserType)UserController.Instance.GetCurUserType;
    [Category("SDK")]
    public EABUserType AB面
    {
        get => _type;
        set
        {
            if (value == _type) return;
            _type = value;
            GameEvent.Send(EventType.SetUserType, (int)_type);
        }
    }
}
namespace Config
{
    public enum EABUserType
    {
        A = 0,
        B = 1,
    }

}