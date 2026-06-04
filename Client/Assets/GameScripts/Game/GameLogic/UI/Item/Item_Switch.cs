using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK_Engine;

namespace GameLogic
{
    public class Item_Switch : MonoBehaviour
    {
        public int switchID;
        public UIButton close;
        public UIButton open;
        public bool isOpen;

        private void Start()
        {
            close.AddListener(ClickClose);
            open.AddListener(ClickOpen);
            Init();
        }

        private void Init()
        {
            isOpen = GetSwitch();
            close.gameObject.SetActive(isOpen);
            open.gameObject.SetActive(!isOpen);
        }

        private void ClickClose(GameObject obj)
        {
            isOpen = false;
            close.gameObject.SetActive(isOpen);
            open.gameObject.SetActive(!isOpen);
            SetSwitch();
        }

        private void ClickOpen(GameObject obj)
        {
            isOpen = true;
            close.gameObject.SetActive(isOpen);
            open.gameObject.SetActive(!isOpen);
            SetSwitch();
        }

        private void SetSwitch()
        {
            if (switchID == 1)//音效开关
            {
                //PlayerPrefs.SetInt(Constant.Setting.SoundMuted,isOpen?1:0);
                GameModule.Setting.SetBool(Constant.Setting.SoundMuted, isOpen);
                GameModule.Audio.SoundEnable = isOpen;
            }
            else if (switchID == 2)//音乐开关
            {
                //PlayerPrefs.SetInt(Constant.Setting.MusicMuted, isOpen? 1 : 0);
                GameModule.Setting.SetBool(Constant.Setting.MusicMuted, isOpen);
                GameModule.Audio.MusicEnable = isOpen;
            }
            else if (switchID == 3)//震动开关
            {
                //PlayerPrefs.SetInt(Constant.Setting.Vibrate, isOpen ? 1 : 0);
                GameModule.Setting.SetBool(Constant.Setting.Vibrate, isOpen);
            }
            PlayerPrefs.Save();
        }

        private bool GetSwitch()
        {
            if (switchID == 1)//音效开关
            {
                return GameModule.Setting.GetBool(Constant.Setting.SoundMuted, true);
            }
            else if (switchID == 2)//音乐开关
            {
                return GameModule.Setting.GetBool(Constant.Setting.MusicMuted, true);
            }
            else if (switchID == 3)//震动开关
            {
                return GameModule.Setting.GetBool(Constant.Setting.Vibrate, true);
            }
            return true;
        }
    }
}
