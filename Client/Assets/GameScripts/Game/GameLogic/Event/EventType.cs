using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class EventType
    {
        #region 关卡编辑器事件名字
        public const string UpdateLayerUI = "UpdateLayerUI";//更新卡牌层级信息
        public const string UpdateCardUI = "UpdateCardUI";//更新卡牌信息
        public const string UpdateCardTypeCount = "UpdateCardTypeCount";//更新卡牌类型数量
        //作弊器相关事件名字
        public const string SetLevel = "SetLevel";//设置关卡
        public const string SetGold = "SetGold";//设置金币
        public const string SetLifes = "SetLifes";//设置生命值
        public const string SetStamina = "SetStamina";//设置改造体力
        public const string SetProps = "SetProps";//设置道具
        public const string SetSceneID = "SetSceneID";//设置场景ID
        public const string SetInvincibleLife = "SetInvincibleLife";//设置无敌时间
        public const string SetAdFree = "SetAdFree";//设置免广告
        public const string OpenAd = "OpenAd";//打开广告
        public const string SetGameUIShow = "SetGameUIShow";//设置游戏UI显示
        public const string SetAllActivity = "SetAllActivity";//打开所有活动
        public const string CloseAllActivityBtns = "CloseAllActivityBtns";//关闭所有活动按钮
        public const string setDeviceID = "setDeviceID";//设置设备ID
        public const string ClearMMOCountdown = "ClearMMOCountdown";//清除网赚倒计时
        public const string QuicklyEndGoldRush = "QuicklyEndGoldRush";//快速结束黄金狂潮
        public const string QuicklyEndPassport = "QuicklyEndPassport";//快速结束梦想通行证
        public const string setPayType = "setPayType";//设置支付方式
        public const string SetPayCountry = "SetPayCountry";//设置支付国家
        public const string SetUserType = "SetUserType";//设置用户类型
        public const string AddGoldRush = "AddGoldRush";//黄金狂潮添加金块
        public const string SetUserASN = "SetUserASN";//设置用户ASN
        public const string TestPhotoRewardOverride = "TestPhotoRewardOverride";//测试活动照片奖励远程配置
        public const string TestIAPToIAAOverride = "TestIAPToIAAOverride";//测试IAP转IAA远程配置
        public const string TestSkipCheckInDay = "TestSkipCheckInDay";//测试跳过签到天数
        public const string UpdatePackDisplay = "UpdatePackDisplay";//更新卡包显示
            
        #endregion
        #region UI相关事件名
        public const string ShowTips = "ShowTips";
        public const string ShowTips2 = "ShowTips2";
        public const string ShowTips3= "ShowTips3";
        public const string ShowTips4= "ShowTips4";
        public const string HideBubbleTips = "HideBubbleTips";//隐藏气泡提示
        public const string GamePanelShow = "GamePanelShow";//显示游戏面板
        public const string UpdatePropBtnState = "UpdatePropBtnState";//更新道具按钮状态
        public const string DoPropAnim = "DoPropAnim";//播放道具动画
        public const string UpdateNugget = "UpdateNugget";//更新金卡牌数量
        public const string RefreshCurrency = "RefreshCurrency";//刷新货币数量
        public const string RefreshLife = "RefreshLife";//刷新生命值
        public const string RefreshLevel = "RefreshLevel";//刷新关卡值
        public const string FreezeTime = "FreezeTime";//冻结时间
        public const string PauseGameTime = "PauseGameTime";//暂停游戏时间
        public const string SetAnimMask = "SetAnimMask"; //动画遮罩
        public const string SetTipsMask = "SetTipsMask"; //提示遮罩
        public const string SetGameMask = "SetGameMask"; //游戏局内遮罩
        public const string RefreshActivity = "RefreshActivity"; //刷新活动信息
        public const string RefreshEndlessItem = "RefreshEndlessItem"; //刷新无尽宝藏道具
        public const string SetEndlessShowAnim = "SetEndlessShowAnim"; //设置无尽宝藏显示动画
        public const string OpenDreamPass = "OpenDreamPass"; //激活梦想通行证
        public const string RefreshDreamPassAdCount = "RefreshDreamPassAdCount"; //刷新梦想通行证广告进度
        public const string GetDreamReward = "GetDreamReward";//获得梦想奖励
        public const string SetAdEnter = "SetAdEnter"; //设置广告入口状态
        public const string SetCloseBanner = "SetCloseBanner"; //设置关闭banner状态
        public const string SetLoadNetTips = "SetLoadNetTips"; //设置加载提示
        public const string RefreshPhotoTab = "RefreshPhotoTab";//刷新相册选项卡
        public const string DownloadPicture = "DownloadPicture";//下载图片
        public const string LevelPhotoRefresh = "LevelPhotoRefresh"; //刷新关卡照片
        public const string EnterGame = "EnterGame"; //直接进入游戏

        public const string OpenLevelInfo = "OpenLevelInfo"; //打开关卡信息面板
        public const string SetWaitingUI = "SetWaitingUI"; //设置等待界面
        public const string SetWaitingUIAndTips = "SetWaitingUIAndTips"; //设置等待界面和提示
        public const string ShowGameClosePanel = "ShowGameClosePanel"; //显示游戏关闭面板
        public const string ShowPhotoThum = "ShowPhotoThum"; //显示照片缩略图

        public const string OnVipLevelChanged = "OnVipLevelChanged";   // VIP等级改变
        
        public const string RefreshHomeVideo = "RefreshHomeVideo"; //刷新首页视频
        public const string RefreshUserABType = "RefreshUserABType";//刷新AB类型
        public const string OpenGameBgAvPro = "OpenGameBgAvPro";//打开游戏背景视频播放
        
        public const string ReadFailOrder = "ReadFailOrder";//读取失败订单

        public const string ss = "ss"; //测试事件
        #region 红点事件名
        public const string ShopRedPoint = "ShopRedPoint";//商城红点
        public const string EndlessRedPoint = "EndlessRedPoint";//无尽红点
        public const string DreamRedPoint = "DreamRedPoint";//梦想红点
        public const string PhotoRedPoint = "PhotoRedPoint";//相册红点
        public const string SwitchRedPoint = "SwitchRedPoint";//切标签页红点
        public const string SetOnePhotoRedPoint = "SetOnePhotoRedPoint";//设置一张相册红点
        public const string SetGachaRedPoint = "SetGachaRedPoint";//设置抽卡红点
        public const string RefreshPhotoLikeStatus = "RefreshPhotoLikeStatus";//刷新单张照片点赞状态
        public const string NewPlayerPackRedPoint = "NewPlayerPackRedPoint";//新手礼包红点
        public const string DayCheckInRedPoint = "DayCheckInRedPoint";//七日签到红点
        #endregion

        public const string NextGuide = "NextGuide";//下一步引导
        public const string HideGuide = "HideGuide";//隐藏引导
        public const string StartGuide1 = "StartGuide1";//开始引导1
        public const string StartTimeTips = "StartTimeTips";//开始时间引导
        #endregion
    }
}
