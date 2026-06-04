using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Config.common;

namespace GameLogic
{
    public class EditUnit : MonoBehaviour
    {
        public Image ColorImage;
        public Image ShadowImage;
        public TextMeshProUGUI InfoTxt;
        public GameObject TestObj;
        private bool _isTaken;
        private bool _isShadow;
        //设置单元格占用状态
        public bool IsTaken
        {
            get=> _isTaken;
            set
            {
                _isTaken = value;
                if (_isTaken)
                {
                    IsShadow = false;
                    ColorImage.gameObject.SetActive(true);
                }
                //当取消单元格占用状态时，如果是上一层的占用状态，则设置阴影状态
                else if(!_isTaken && LevelEdirtorController.Instance.IsContainLastUnits(this))
                {
                    _isShadow = true;
                    ColorImage.gameObject.SetActive(false);
                    ShadowImage.gameObject.SetActive(_isShadow);
                } else if (!_isTaken)
                {
                    InfoTxt.gameObject.SetActive(false);
                };
                ColorImage.color = _isTaken?Color.blue:Color.white;
            }
        }
        //设置阴影的时候，如果当前层的某单元格和上一层重叠，则不用关闭设置状态
        public bool IsShadow
        {
            get => _isShadow;
            set
            {
                _isShadow = value;
                ShadowImage.gameObject.SetActive(_isShadow);
                if (_isShadow && !LevelEdirtorController.Instance.IsContainCurrentUnits(this))
                {
                    ColorImage.gameObject.SetActive(!_isShadow);
                }

                ShadowImage.color = _isShadow?GetLayerColor(LevelEdirtorController.Instance.CurrentLayerIndex-1):Color.white;
            }
        }

        public void SetInfo(LockCardType mylock, List<FunctionCardType> functions,List<int> cardtype)
        {
            if (mylock == LockCardType.None && (functions == null || functions.Count == 0)&&(cardtype==null||cardtype.Count==0))
            {
                InfoTxt.gameObject.SetActive(false);
                return;
            }

            string lockStr = "";
           
            string functionStr = "";
            switch (mylock)
            {
                case LockCardType.OneShackle:
                    lockStr = "单锁";
                    break;
                case LockCardType.TwoShackles:
                    lockStr = "双锁";
                    break;
                case LockCardType.IceCube:
                    lockStr = "冰";
                    break;
                case LockCardType.Honey:
                    lockStr = "蜜";
                    break;
                case LockCardType.Vines:
                    lockStr = "藤";
                    break;
                case LockCardType.Butterfly:
                    lockStr = "蝶";
                    break;
                case LockCardType.Door:
                    lockStr = "门";
                    break;
                case LockCardType.Rock:
                    lockStr = "岩";
                    break;
                case LockCardType.Grassy:
                    lockStr = "草";
                    break;
            }

            if (functions != null)
            {
                foreach (var item in functions)
                {
                    switch (item)
                    {
                        case FunctionCardType.GoldCube:
                            functionStr += "金";
                            break;
                        case FunctionCardType.Reversible:
                            functionStr += "双";
                            break;

                        case FunctionCardType.Bombshell:
                            functionStr += "炸";
                            break;
                        case FunctionCardType.Zipper:
                            functionStr += "链";
                            break;
                        case FunctionCardType.Shell:
                            functionStr += "壳";
                            break;
                    }
                }
            }
            string midTxt = (cardtype!=null&&cardtype.Count>0)?$"{cardtype[0]}":"";
            InfoTxt.gameObject.SetActive(true);
            InfoTxt.text = lockStr +midTxt+ functionStr;
            InfoTxt.rectTransform.anchoredPosition = new Vector2(-35, 35);
        }

        public void SetTest()
        {
            TestObj.SetActive(true);
        }
        private Color GetLayerColor(int layer)
        {
            switch (layer)
            {
                // 基础颜色（0-4）
                case 0:  return new Color(1, 0, 0, 0.3f);        // 纯红色
                case 1:  return new Color(0, 0.5f, 0, 0.3f);     // 深绿色
                case 2:  return new Color(0.53f, 0.81f, 0.92f, 0.3f); // 天蓝色
                case 3:  return new Color(1, 0.84f, 0, 0.3f);     // 金色
                case 4:  return new Color(1, 0, 1, 0.3f);         // 品红色

                // 柔和色系（5-9）
                case 5:  return new Color(1, 0.71f, 0.76f, 0.3f); // 浅粉色
                case 6:  return new Color(0.6f, 1, 0.8f, 0.3f);   // 薄荷绿
                case 7:  return new Color(0.7f, 0.5f, 1, 0.3f);   // 薰衣草紫
                case 8:  return new Color(1, 0.5f, 0.31f, 0.3f);  // 珊瑚橙
                case 9:  return new Color(1, 0.99f, 0.82f, 0.3f); // 奶油黄

                // 深色系（10-14）
                case 10: return new Color(0.25f, 0, 0.3f, 0.3f); // 深紫色
                case 11: return new Color(0, 0, 0.5f, 0.3f);      // 海军蓝
                case 12: return new Color(0.39f, 0.26f, 0.13f, 0.3f); // 咖啡棕
                case 13: return new Color(0.2f, 0.2f, 0.2f, 0.3f); // 炭灰色
                case 14: return new Color(0, 0.2f, 0.13f, 0.3f);  // 墨绿色

                // 半透明颜色（15-19）
                case 15: return new Color(0.2f, 1, 0.2f, 0.3f);   // 青柠绿
                case 16: return new Color(0.68f, 0.85f, 0.9f, 0.3f); // 淡蓝色
                case 17: return new Color(0.5f, 0, 0, 0.3f);      // 深红色
                case 18: return new Color(0.75f, 0.75f, 0.75f, 0.3f); // 银色
                case 19: return new Color(0.8f, 0.6f, 1, 0.3f);   // 淡紫色

                default: 
                    Debug.LogWarning($"未定义层级 {layer} 的颜色，返回白色");
                    return Color.white;
            }
        }
    }
}
