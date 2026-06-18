using System;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using DG.Tweening;
using UnityEngine;
using Y_Base;
using TK_Engine;

namespace GameLogic
{
    public class GameScene : SingletonBehaviour<GameScene>
    {
        public Transform StoryNode;
        public Camera GameCamera;
        [FieldName("是否跳过开场动画")] public bool IsSkipOpenCartoon = false;
        private bool _isLogEvent_HomeShow = false;
        public List<CardCell> TestCards = new List<CardCell>();
        #region 测试
        public ERewardType rewardType;
        public int id;
        public int Count;
        public bool IsAddTime;
        public int RewardId;

        public Ease easeType;
        #endregion

        #region Panel_Succeed动画测试
        [Header("=== Panel_Succeed 动画配置 ===")]
        public SucceedAnimConfig SucceedAnimConfig = SucceedAnimConfig.Default;

        [FieldName("测试钻石动画")]
        public bool TestDiamondAnim;
        [FieldName("测试照片动画")]
        public bool TestPhotoAnim;
        [FieldName("测试完整流程")]
        public bool TestFullAnim;
        [FieldName("重置动画")]
        public bool TestResetAnim;
        [FieldName("应用配置")]
        public bool ApplyConfig;

        private void OnValidate()
        {
#if UNITY_EDITOR
            //只在运行时且是用户主动点击时才执行（避免进入PlayMode时自动触发）
            if (!Application.isPlaying) return;

            //应用配置
            if (ApplyConfig)
            {
                ApplyConfig = false;
                Panel_Succeed.SetAnimConfig(SucceedAnimConfig);
                Debug.Log("[GameScene] 动画配置已应用到 Panel_Succeed");
            }

            //测试钻石动画
            if (TestDiamondAnim)
            {
                TestDiamondAnim = false;
                Panel_Succeed.SetAnimConfig(SucceedAnimConfig);
                var panel = GameModule.UI.GetWindow(typeof(Panel_Succeed).FullName) as Panel_Succeed;
                if (panel != null)
                {
                    panel.TestDiamondAnimation();
                }
                else
                {
                    Debug.LogWarning("[GameScene] Panel_Succeed 未打开，请先打开结算界面");
                }
            }

            //测试照片动画
            if (TestPhotoAnim)
            {
                TestPhotoAnim = false;
                Panel_Succeed.SetAnimConfig(SucceedAnimConfig);
                var panel = GameModule.UI.GetWindow(typeof(Panel_Succeed).FullName) as Panel_Succeed;
                if (panel != null)
                {
                    panel.TestPhotoAnimation();
                }
                else
                {
                    Debug.LogWarning("[GameScene] Panel_Succeed 未打开，请先打开结算界面");
                }
            }

            //测试完整流程
            if (TestFullAnim)
            {
                TestFullAnim = false;
                Panel_Succeed.SetAnimConfig(SucceedAnimConfig);
                var panel = GameModule.UI.GetWindow(typeof(Panel_Succeed).FullName) as Panel_Succeed;
                if (panel != null)
                {
                    panel.TestFullAnimation();
                }
                else
                {
                    Debug.LogWarning("[GameScene] Panel_Succeed 未打开，请先打开结算界面");
                }
            }

            //重置动画
            if (TestResetAnim)
            {
                TestResetAnim = false;
                var panel = GameModule.UI.GetWindow(typeof(Panel_Succeed).FullName) as Panel_Succeed;
                if (panel != null)
                {
                    panel.TestResetAnimation();
                }
                else
                {
                    Debug.LogWarning("[GameScene] Panel_Succeed 未打开，请先打开结算界面");
                }
            }
#endif
        }
        #endregion
        private void Awake()
        {
            GameModule.Timer.AddTimer(time =>
            {
                Init();
            }, .002f);
        }

        private void Init()
        {
            _isLogEvent_HomeShow = false;
            RechargeController.Instance.CheckOrder();
            //RechargeController.Instance.SendReward("com.dreamtile.endless_treasure1", true, false);
            //如果没有通过第一关，冷启动都会是进入关卡
            if (UserController.Instance.Level > 1)
            {
                GameModule.UI.ShowUI<Panel_Home>(false, null);
                GameModule.UI.CheckOrderPanel();
            }
            else
            {
                int curId = UserController.Instance.GetCurUserPhotoID;
                UserController.Instance.AddUserPhotoData(curId, 0);
                var phCfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(curId);
                //需要判断此时的AB面
                string phUrl = PropsController.GetImgUrl(phCfg.ImageID, phCfg.ImageType, phCfg.ABGroup);
                string viUrl = PropsController.GetVideoUrl(phCfg.ImageID, phCfg.ABGroup, phCfg.IsVideo);
                OneSelectPhotoInfo info = new OneSelectPhotoInfo(0, phCfg.ID, phCfg.ABGroup, phUrl, viUrl, false, 1, phCfg.IsVideo, false, 0);
                GameModule.UI.ShowUI<Panel_Game>(info);
                LevelController.Instance.CurLevelSelectPhotoInfo = info;
            }

        }

        public void LogEventHomeShow()
        {
            if (!_isLogEvent_HomeShow)
            {
                _isLogEvent_HomeShow = true;
                SDKManager.Instance.LogEvent("Home_Show");
            }
        }


    }
}
