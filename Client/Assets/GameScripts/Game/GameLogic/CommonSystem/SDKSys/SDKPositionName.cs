using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 插屏广告类型，与底层 SDK 的 IvType 枚举值保持一致（IV1=1 ... IV8=8）。
    /// 抽离到业务层是为了让 SDKManager 公开接口与具体 SDK 命名空间解耦，方便切换 A18/Up2 等不同 SDK。
    /// </summary>
    public enum IvType
    {
        NULL = -1,
        IV1 = 1,
        IV2 = 2,
        IV3 = 3,
        IV4 = 4,
        IV5 = 5,
        IV6 = 6,
        IV7 = 7,
        IV8 = 8,
    }

    public enum HCIVPositionName
    {
        IV_Start,
        IV_Back,
        IV_Victory,
        IV_Leave,
        IV_Fail,
    }
    public enum HCRVPositionName
    {
        RV_SuccMore,
        RV_Tip,
        RV_Puzzle,
        RV_Slot,
        RV_Revive,
        RV_Chest,
        RV_Energy,
        RV_Coins,
        RV_DownloadPicture,//看视频下载图片
        RV_LevelSelect,//看视频挑战精选关卡
        RV_CrushLevel,//看视频挑战BOSS关卡
        RV_PropA,//看视频获得关卡道具1
        RV_PropB,//看视频获得关卡道具2
        RV_PropC,//看视频获得关卡道具3
        RV_PropD,//看视频获得关卡道具4
        RV_GiftPicture,//看视频获得礼品包活动激励视频图片
        RV_Endless,//无尽寻宝活动看视频获得奖励
        RV_CommonCard,//看视频获得普通抽卡
        RV_VIPCard,//看视频获得VIP抽卡
        RV_Refresh,//看视频刷新图片
        RV_Daily,//看视频签到奖励翻倍
        RV_IAPToIAA,//IAP转IAA看视频
        RV_LevelSelect_2,//看视频挑战第三个精选关卡
        RV_PhotoSpecial,//看视频获取梦想通行证的高级图片
        RV_PhotoNormal//看视频获取梦想通行证的普通图片

    }
}
