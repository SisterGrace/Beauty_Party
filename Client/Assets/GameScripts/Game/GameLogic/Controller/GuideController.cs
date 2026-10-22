using System.Collections;
using System.Collections.Generic;
using Config.common;
using Config.config;
using UnityEngine;
using Y_Base;
using TK_Engine;

namespace GameLogic
{
    public class GuideController : Singleton<GuideController>
    {
        private int _guideId;
        private int _triggerGuideId;
        
        private CfgGuide _guideCfg;
        private List<int> _guideIdList;
        private List<int> _triggerGuide;
        
        private List<GuideData> _waitClickObjs = new List<GuideData>();
        public int GuideId => _guideId;
        public bool IsGuideId => _guideId > 0;
        public List<GuideData> WaitClickObjs => _waitClickObjs;
        public List<int> GuideIdList=> _guideIdList;
        public List<int> TriggerGuide => _triggerGuide;
        public CfgGuide GetGuideCfg(bool IsGetTriggerGuide =false)
        {
            _guideCfg = ConfigLoader.Instance.Tables.TbCfgGuide.Get(IsGetTriggerGuide? _triggerGuideId : _guideId);
            return _guideCfg;
        }
        #region 触发引导
        public int TriggerGuideId => _triggerGuideId;
        public bool IsTriggerGuide => _triggerGuideId > 0;

        public void CheckTriggerGuide(int guideId)
        {
            if(_triggerGuide.Contains(guideId)) return;
            _triggerGuide.Add(guideId);
            _triggerGuideId = guideId;
            CheckTriggerGuide();
            //Save();
        }

        public void ClearTriggerGuide()
        {
            _triggerGuide.Clear();
            _triggerGuideId = 0;
        }
        private void  CheckTriggerGuide()
        {
            if (IsTriggerGuide)
            {
                switch (_triggerGuideId)
                {
                    case 105:
                        UserController.Instance.SetOneProp(PropType.Revoke,1,false,false,true);
                        break;
                    case 106:
                        UserController.Instance.SetOneProp(PropType.MessUp, 1, false, false,true);
                        break;
                    case 111:
                        UserController.Instance.SetOneProp(PropType.DestoryGruop, 1, false, false,true);
                        break;
                    case 112:
                        UserController.Instance.SetOneProp(PropType.MoveOut, 1, false, false,true);
                        //移动棋子
                        CardsController.Instance.NoClickCard(GetGuideCfg(true).ClickCardIds);
                        break;
                    case 4:
                        /*UserController.Instance.SetOneProp(PropType.DrawCard, 0, false, true,true);
                        UserController.Instance.SetOneProp(PropType.Prompt, 0, false, true,true);
                        UserController.Instance.SetOneProp(PropType.AddSolt, 0, false, true,true);*/
                        break;
                }
                if (GameModule.UI.HasWindow<Panel_Guide>())
                {
                    GameEvent.Send(EventType.NextGuide);
                }
                else
                {
                    GameModule.UI.ShowUI<Panel_Guide>();
                }
            }
            else
            {
                GameModule.UI.CloseUI<Panel_Guide>();
                WaitClickObjs.Clear();
            }
        }

        public void NextTriggerGuide()
        {
            if (IsTriggerGuide)
            {
                OnTriggerGuideSDKEvent(_triggerGuideId);
                _triggerGuideId = GetGuideCfg(true).NextId;
                CheckTriggerGuide();
            }
        }

        public void NextTriggerGuide(int guideId)
        {
            if (IsTriggerGuide && _triggerGuideId == guideId)
            {
                _triggerGuideId = GetGuideCfg(true).NextId;
                CheckTriggerGuide();
            }
        }

        private void OnTriggerGuideSDKEvent(int guideId)
        {
            return;
            switch (guideId)
            {
                case 101:
                    SDKManager.Instance.LogEvent("Guide", "Guide", 1);
                    break;
                case 102:
                    //SDKManager.Instance.LogEvent("Guide","Guide",2);
                    break;
                case 105:
                    SDKManager.Instance.LogEvent("Guide", "Guide", 2);
                    break;
                case 106:
                    SDKManager.Instance.LogEvent("Guide", "Guide", 3);
                    break;
                case 111:
                    SDKManager.Instance.LogEvent("Guide", "Guide", 4);
                    break;
                case 112:
                    SDKManager.Instance.LogEvent("Guide", "Guide", 5);
                    break;
               
            }
        }
        /// <summary>
        /// 当前触发引导是否存在指定引导id
        /// </summary>
        /// <param name="guideId"></param>
        /// <returns></returns>
        public bool IsTriggerGuideId(int guideId)
        {
            return _triggerGuide.Contains(guideId);
        }
        #endregion

        public void CheckStartGuide()
        {
            if (!_guideIdList.Contains(1))
            {
                StartGuide(1);
            }
        }

        public static bool IsGuide1()
        {
            if (!Instance._guideIdList.Contains(1))
            {
                return true;
            }

            return false;
        }

        public static bool IsGuide3()
        {
            int UnLockLevel = UserController.Instance.GetCurUserType == 1 ? 6 : 51;
            if (UserController.Instance.Level == UnLockLevel && !Instance._guideIdList.Contains(3))
            {
                return true;
            }

            return false;
        }
      
        /// <summary>
        /// 立即引导
        /// </summary>
        /// <param name="guideId"></param>
        public void AtOnceGuide(int guideId)
        {
            StartGuide(guideId);
            CheckGuide();
        }
        public void StartGuide(int guideId)
        {
            if (!_guideIdList.Contains(guideId))
            {
                _guideIdList.Add(guideId);
                _guideId = guideId;
                Save();
            }
        }

        public void EndOneGuide(int guideId)
        {
            if (!_guideIdList.Contains(guideId))
            {
                _guideIdList.Add(guideId);
                Save();
            }
        }

        private void RemoveGuide(int guideId)
        {
            if (_guideIdList.Contains(guideId))
            {
                _guideIdList.Remove(guideId);
                Save();
            }
        }
        /// <summary>
        /// 检查引导
        /// </summary>
        public void CheckGuide()
        {
            if (_guideId > 0)
            {
                if (_guideId == 1)
                {
                    /*//第一步引导，只开启主界面的故事按钮
                    GameEvent.Send(EventType.StartGuide1,false);
                    RemoveGuide(1);*/
                }
                if (GameModule.UI.HasWindow<Panel_Guide>())
                {
                    GameEvent.Send(EventType.NextGuide);
                }
                else
                {
                    GameModule.UI.ShowUI<Panel_Guide>();
                }
            }
            else
            {
                GameModule.UI.CloseUI<Panel_Guide>();
            }
        }

        public void NextGuide()
        {
            if (_guideId > 0)
            {
                GuideEvent(_guideId);
                _guideId = GetGuideCfg().NextId;
                CheckGuide();
            }
        }

        private void GuideEvent(int guideId)
        {
            switch (guideId)
            {
                case 1:
                    //SDKManager.Instance.LogEvent("Guide","Guide",4);
                    break;
                case 2:
                    //SDKManager.Instance.LogEvent("Guide","Guide",6);
                    break;
                case 3:
                    //SDKManager.Instance.LogEvent("Guide","Guide",5);
                    break;
            }
        }
        public void Init()
        {
            _guideIdList = ES3.Load("GuideIdList", new List<int>());
            _triggerGuide = ES3.Load("TriggerGuide", new List<int>());
            //CheckStartGuide();
        }

        public void Save()
        {
            ES3.Save("GuideIdList", _guideIdList);
            //ES3.Save("TriggerGuide", _triggerGuide);
        }

       
        
    }
    public struct GuideData
    {
        public Transform OldParent;
        public  Transform Tr;
        public int SiblingIndex;

        public GuideData(GameObject guidItem)
        {
            Tr = guidItem.transform;
            OldParent = Tr.parent;
            SiblingIndex = Tr.GetSiblingIndex();
        }
        public void Restore()
        {
            if (Tr != null)
            {
                Tr.SetParent(OldParent);
                Tr.SetSiblingIndex(SiblingIndex);
            }    
        }
    }
}
