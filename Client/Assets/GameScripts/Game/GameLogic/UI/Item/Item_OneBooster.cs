using System;
using System.Collections;
using System.Collections.Generic;
using TK_Engine;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GameLogic
{
    public class Item_OneBooster : MonoBehaviour
    {
        public Image Bg;
        public Image icon;
        public TextMeshProUGUI countTxt;
        public TextMeshProUGUI CountDownTxt;
        public GameObject addObj;
        public GameObject CountDownObj;
        public GameObject SelectObj;
        public GameObject mLock;
        public GameObject CountObj;
        public GameObject FreeObj;
        private PropInfo _propInfo;
        private float timer = 1;
        
        private bool _isSelect;
        public void InitOneBooster(PropInfo data)
        {
            _propInfo = data;
            _isSelect = false;
            UserController.Instance.CheckBoosterTime(data);
            var Cfgprop = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetByType(data.mType);
            //icon.SetSprite("icon"+Cfgprop.Icon,true);
            string iconName = "icon" + Cfgprop.Icon;
            icon.sprite = GameModule.Resource.LoadAsset<Sprite>(iconName);
            icon.SetNativeSize();
            mLock.SetActive(!data.IsUnlock);
            countTxt.transform.parent.gameObject.SetActive(data.IsUnlock);
            string bgIcon = _isSelect? "ui_selectprop" : "btn_popup_tool";
            Bg.SetSprite(bgIcon,true);
            if (!data.IsUnlock) return;
            CountDownObj.SetActive(data.EndTime > 0&&!data.IsFree);
            SelectObj.SetActive(data.EndTime > 0&&!data.IsFree);
            CountObj.SetActive(!(data.EndTime>0)&&!data.IsFree);
            countTxt.gameObject.SetActive(data.Num>0);
            countTxt.text = $"{data.Num}";
            addObj.SetActive(!(data.Num>0));
            _isSelect = false;
            FreeObj.SetActive(data.IsFree);
            if (data.EndTime <= 0&&PropsController.IsGetOneBooster(data.mType))
            {
                SelectItem();
            }
        }

        private void Start()
        {
            Utility.Unity.AddUpdateListener(UpdataTime);
        }
        /// <summary>
        /// 更新时间
        /// </summary>
        private void UpdataTime()
        {
            if (_propInfo is { EndTime: > 0 })
            {
                timer += Time.deltaTime;
                if (timer > 1)
                {
                    timer = 0;
                    var t = _propInfo.EndTime - TimeUtil.GetTimeStamp();
                    if (t <= 0)
                    {
                        UserController.Instance.CheckBoosterTime(_propInfo);
                        CountDownObj.SetActive(false);
                        SelectObj.SetActive(false);
                        CountObj.SetActive(true);
                        countTxt.gameObject.SetActive(_propInfo.Num>0);
                        countTxt.text = $"{_propInfo.Num}";
                        addObj.SetActive(!(_propInfo.Num>0));
                        return;
                    }
                    CountDownTxt.text = TimeUtil.GetHourOrMinute(t);
                }
            }
        }

        public bool SelectItem()
        {
            _isSelect = !_isSelect;
            SelectObj.SetActive(_isSelect);
            CountObj.SetActive(!_isSelect);
            string bgIcon = _isSelect? "ui_selectprop" : "btn_popup_tool";
            //Bg.sprite = GameModule.Resource.LoadAsset<Sprite>(bgIcon);
            Bg.SetSprite(bgIcon,true);
           //Bg.SetNativeSize();
            return _isSelect;
        }
        private void OnDestroy()
        {
            Utility.Unity.RemoveUpdateListener(UpdataTime);
        }
    }

    public class OneBooster : UILoopGridItemWidget, IListDataItem<PropInfo>
    {
        private Item_OneBooster _item;
        public void SetItemData(PropInfo data)
        {
            _item = gameObject.GetComponent<Item_OneBooster>();
            _item.InitOneBooster(data);
        }

        public bool OnItemSelected()
        {
             return _item.SelectItem();
        }
    }
}
