using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class AudioDef 
    {
        #region UI

        public const string BGM1 = "GameBGM01";//主界面背景音乐

        public const string CartoonBGM1 = "sound_cartoon_bgm";//开场动画背景音乐
        public const string BtnClick = "sound_btnclick";//按钮点击音效
        public const string UI_chestopen = "sound_chestopen";//宝箱打开音效
        public const string UI_gaincoins = "sound_gaincoins";//获得金币音效
        public const string UI_gainKey = "sound_gainkey";//获得钥匙音效
        public const string UI_gainprops = "sound_gainprops";//获得道具音效
        public const string UI_gainStar = "sound_gainstar";//获得星星音效
        public const string UI_keyfly = "sound_keyfly";//钥匙飞入音效
        public const string UI_propfly = "sound_profly";//道具飞到按钮上音效
        
        public const string UI_clickLove = "sound_clickLove";//点击喜欢音效
        public const string UI_closeLove = "sound_closeLove";//关闭喜欢音效
        public const string UI_showWaterMark = "sound_watermark";//展示水印音效
        public const string UI_winPhoto = "sound_winPhoto";//成功获得照片音效
        #region 场景音效

        public const string Rensuccessful = "sound_rensuccessful";//装修成功音效
        public const string UI_Rendisappear = "sound_rendisappear";//装修按钮消失音效
        public const string UI_btnrenovation = "sound_btnrenovation";//装修按钮按下音效
        public const string UI_scenelight = "sound_scenelight";//旧场景扫光音效
        public const string UI_starfly = "sound_starfly";//星星飞入装修按钮音效
        public const string UI_rendown = "sound_rendown";//装修成功√音效
        public const string UI_Cry = "GameBGM01_sad";// 场景内哭泣音效
        #endregion
        #endregion

        #region 游戏内

        public const string ChainClick = "sound_chainclick";//锁链点击音效
        public const string ChainDie = "sound_chaindie";//锁链破碎音效
        public const string FruitClick = "sound_fruitclick";//棋子点击音效
        public const string FruitDie = "sound_fruitdie";//棋子破碎音效
        public const string IceClick = "sound_iceclick";//冰块点击音效
        public const string IceDie = "sound_icedie";//冰块破碎音效
        public const string LockPropClick = "sound_lockproclick";//未解锁的道具按钮点击音效
        public const string ShellClick = "sound_shellclick";//贝壳点击音效
        public const string ShellDie = "sound_shelldie";//贝壳消失音效
        public const string ShellTu= "sound_shelltu";//贝壳吐棋子音效
        public const string Success = "sound_success";//成功音效
        public const string Fail = "sound_fail";//失败音效
        public const string UseClownHat = "sound_useclownhat";//使用小丑帽子音效
        public const string UseRevoke = "sound_userevoke";//使用撤销音效
        public const string UseLots = "sound_useslots";//使用收集槽音效
        public const string UseSpring = "sound_usespring";//使用弹簧音效
        public const string UseTips = "sound_usetips";//使用提示音效
        public const string UseLaba = "sound_uselaba";//使用拉霸音效
        public const string UseWand = "sound_usewand";//使用魔杖音效
        public const string ZipperDie = "sound_zipperdie";//拉链破碎音效
        public const string Labafruit = "sound_labafruit";//拉霸出现棋子音效
        public const string tipsClick = "sound_tipclick";//提示点击音效

        public const string GoldBreak = "sound_gold_break";//金块碎裂音效
        public const string GoldGet = "sound_gold_get";//金块获得音效
        public const string BombTime = "sound_boom_time";//炸弹缩放音效
        public const string BombTime0 = "sound_boom_time0";//炸弹触发失败
        public const string BombDie = "sound_boom_die";//炸弹消失音效
        public const string butterflyDown = "sound_butterfly_down";//蝴蝶飞到目标位置音效
        public const string butterflyFly = "sound_butterfly_fly";//蝴蝶飞行音效
        public const string vineDie1 = "sound_vine_die1";//藤蔓前两次缩短音效
        public const string vineDie2 = "sound_vine_die2";//藤蔓最后缩短音效
        public const string vineClick = "sound_vine_click";//藤蔓点击音效
        public const string HoneyClick = "sound_honey_click";//蜂蜜点击音效
        public const string HoneyDie = "sound_honey_break";//蜂蜜消失音效
        public const string StoneClick = "sound_stone_click";//石头点击音效
        public const string StoneDie = "sound_stone_die";//石头破碎音效
       
        
        public const string clockSlow = "sound_countdown_slow";//时钟慢速音效
        public const string clockRed= "sound_countdown_red";//时钟报警音效

        #endregion
    }
}
