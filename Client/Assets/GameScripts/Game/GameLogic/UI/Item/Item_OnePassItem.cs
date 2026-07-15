using System.Collections;
using System.Collections.Generic;
using Config.common;
using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Y_Base;


namespace GameLogic
{
    public class Item_OnePassItem : MonoBehaviour
    {
        private static readonly Vector2 PropIconSize = new Vector2(118, 122);

        public CanvasGroup CurObj;

        #region 普通奖励
        public Image norRewardImg;
        public TextMeshProUGUI norRewardTxt;
        public GameObject norGetObj;
        public UIButton NotBtn;
        public Image norBgImg;
        public Item_PhotoLoad norPhotoNode;
        #endregion
        #region 通行证奖励
        public Image speRewardImg;
        public TextMeshProUGUI speRewardTxt;
        public GameObject speLockObj;
        public GameObject speGetObj;
        public UIButton SpeBtn;
        public Image speBgImg;
        public Item_PhotoLoad spePhotoNode;
        public GameObject videoIcon; // 高级视频图标，在Inspector中拖拽赋值
        public GameObject norVideoIcon; // 普通视频图标，在Inspector中拖拽赋值
        public IReadOnlyList<Transform> GetBtnTransforms() => btns;
        #endregion

        public Image getBarObj;
        public TextMeshProUGUI itemLeveltxt;
        public UIButton GetBtnObj;

        public Image LevelBg;
        public GameObject LevelBgMask;
        public OnePassItemInfo Mdata;
        List<Transform> btns = new List<Transform>();
        private int abType;
        public void Start()
        {
            NotBtn.AddListener(ClickNorRew);
            SpeBtn.AddListener(ClickSpeRew);
            GetBtnObj.AddListener(ClickGetRewBtn);
            btns.Add(norRewardImg.transform);
            btns.Add(speRewardImg.transform);
        }
        public void InitPassItem(OnePassItemInfo data)
        {
            abType = UserController.Instance.GetCurUserType;
            Mdata = data;

            var dreamPassData = UserController.Instance.GetOneActivityData(ActivityType.DreamPass);
            bool isWaitGet = data.NorType == PassItemType.WaitGet;
            bool isCurr = data.NorType == PassItemType.Curr;

            // 进度条
            int currLevel = dreamPassData.CurrDreamPassLevel;
            getBarObj.fillAmount = data.Id < currLevel ? 1 : data.Id == currLevel ? 0.5f : 0;
            CurObj.alpha = isCurr ? 1 : 0;

            // 等级背景
            LevelBg.sprite = GameModule.Resource.LoadAsset<Sprite>(isCurr ? "icon_txz_dj3" : "icon_txz_dj2");

            // 普通奖励
            bool isNorGotten = UserController.Instance.CheckGetDreamPassReward(data.Id, true);
            norBgImg.sprite = GameModule.Resource.LoadAsset<Sprite>(isWaitGet && !isNorGotten ? "bg_txz_05" : "bg_txz_02");
            norGetObj.SetActive(isNorGotten);
            LevelBgMask.SetActive(isNorGotten);
            SetupReward(data.RewardList[0], norRewardImg, norRewardTxt, norPhotoNode);

            // 特殊奖励（高级）
            bool isSpeLocked = !dreamPassData.IsOpenDreamPass;
            bool isSpeGotten = UserController.Instance.CheckGetDreamPassReward(data.Id, false); // 只定义一次
            speBgImg.sprite = GameModule.Resource.LoadAsset<Sprite>(isWaitGet && !isSpeGotten && !isSpeLocked ? "bg_txz_03" : "bg_txz_06");
            speLockObj.gameObject.SetActive(isSpeLocked);
            speGetObj.SetActive(isSpeGotten);
            SetupReward(data.RewardList[1], speRewardImg, speRewardTxt, spePhotoNode);

            itemLeveltxt.text = $"{data.UnlockLevel}";

            // 高级视频图标：高级是图片、未领、可领、通行证已开
            bool isSpePhoto = data.RewardList[1].Type == ERewardType.Photo;
            bool showSpeVideo = isSpePhoto && !isSpeGotten && isWaitGet && dreamPassData.IsOpenDreamPass;
            videoIcon.SetActive(showSpeVideo);

            // 普通视频图标：普通是图片、未领、可领
            bool isNorPhoto = data.RewardList[0].Type == ERewardType.Photo;
            bool showNorVideo = isNorPhoto && !isNorGotten && isWaitGet;
            if (norVideoIcon != null) norVideoIcon.SetActive(showNorVideo);
        }

        private void SetupReward(TBRewardData reward, Image rewardImg, TextMeshProUGUI rewardTxt, Item_PhotoLoad photoNode)
        {
            bool isPhoto = reward.Type == ERewardType.Photo;
            rewardImg.gameObject.SetActive(!isPhoto);
            photoNode.gameObject.SetActive(isPhoto);

            if (!isPhoto)
            {
                rewardImg.sprite = GameModule.Resource.LoadAsset<Sprite>(PropsController.GetRewardIcon(reward));
                rewardTxt.gameObject.SetActive(true);
                rewardTxt.text = PropsController.GetRewardCount(reward);
                if (reward.Type == ERewardType.Prop)
                    rewardImg.rectTransform.sizeDelta = PropIconSize;
                else
                    rewardImg.SetNativeSize();
            }
            else
            {
                int rewId = abType == 1 ? reward.OtherID : reward.ID;
                photoNode.LoadPhoto(rewId, (int)reward.Count);
            }
        }

        public void SetOnSelect(bool isOn)
        {
           /*  CurObj.DOFade(isOn ? 1 : 0, 0.3f).SetEase(Ease.Linear);
            if (!isOn && Mdata.NorType == PassItemType.WaitGet)
            {
                getBarObj.DOFillAmount(1, 0.3f).SetEase(Ease.Linear);
                GetBtnObj.gameObject.Show();
            } */
        }

        public void UnlockAnim()
        {
            speLockObj.Hide();
        }
        private void ClickNorRew(GameObject obj)
        {
            if (Mdata.NorType == PassItemType.Getted)
            {
                GameEvent.Send(EventType.ShowTips, "uitips_dream_get", true);
                return;
            }
            if (Mdata.NorType == PassItemType.Curr || Mdata.NorType == PassItemType.Lock)
            {
                GameEvent.Send(EventType.ShowTips, "uitips_dream_free", true);
                return;
            }
            if (Mdata.NorType == PassItemType.WaitGet)
            {
                // 普通已领则提示
                bool isNorGotten = UserController.Instance.CheckGetDreamPassReward(Mdata.Id, true);
                if (isNorGotten)
                {
                    GameEvent.Send(EventType.ShowTips, "uitips_dream_get", true);
                    return;
                }

                bool isNorPhoto = Mdata.RewardList[0].Type == ERewardType.Photo;
                if (isNorPhoto)
                {
                    // 普通是图片 → 看视频后领取普通
                    SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_PhotoNormal, (success) =>
                    {
                        if (success)
                        {
                            GameEvent.Send(EventType.GetNormalRewardPhoto, this);
                        }
                        else
                        {
                            GameEvent.Send(EventType.ShowTips, "uitips_video_fail", true);
                        }
                    });
                }
                else
                {
                    // 普通非图片
                    bool isOpen = UserController.Instance.GetOneActivityData(ActivityType.DreamPass).IsOpenDreamPass;
                    bool isSpePhoto = Mdata.RewardList[1].Type == ERewardType.Photo;
                    bool isSpeGotten = UserController.Instance.CheckGetDreamPassReward(Mdata.Id, false);

                    if (isOpen && !isSpePhoto && !isSpeGotten)
                    {
                        // 通行证已开 + 高级非图片 + 高级未领 → 一键全领
                        GameEvent.Send(EventType.GetDreamReward, btns, this);
                    }
                    else
                    {
                        // 其他情况只领普通
                        GameEvent.Send(EventType.GetDreamPassNormalOnly, this);
                    }
                }
            }
        }

        private void ClickSpeRew(GameObject obj)
        {
            bool isOpen = UserController.Instance.GetOneActivityData(ActivityType.DreamPass).IsOpenDreamPass;
            if (!isOpen)
            {
                GameEvent.Send(EventType.ShowTips, "uitips_dream_active", true);
                return;
            }

            if (Mdata.NorType == PassItemType.Getted)
            {
                GameEvent.Send(EventType.ShowTips, "uitips_dream_get", true);
                return;
            }
            if (Mdata.NorType == PassItemType.Curr || Mdata.NorType == PassItemType.Lock)
            {
                GameEvent.Send(EventType.ShowTips, "uitips_dream_free", true);
                return;
            }
            if (Mdata.NorType == PassItemType.WaitGet)
            {
                // 高级已领则提示
                bool isSpeGotten = UserController.Instance.CheckGetDreamPassReward(Mdata.Id, false);
                if (isSpeGotten)
                {
                    GameEvent.Send(EventType.ShowTips, "uitips_dream_get", true);
                    return;
                }
                bool isPhoto = Mdata.RewardList[1].Type == ERewardType.Photo;
                if (isPhoto)
                {
                    SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_PhotoSpecial, (success) =>
                    {
                        if (success)
                        {
                            GameEvent.Send(EventType.GetSpecialRewardPhoto, this);
                        }
                        else
                        {
                            GameEvent.Send(EventType.ShowTips, "uitips_video_fail", true);
                        }
                    });
                }
                else
                {
                    // 非图片直接领取高级
                    GameEvent.Send(EventType.GetSpecialRewardPhoto, this);
                }
            }
        }

        private void ClickGetRewBtn(GameObject obj)
        {
            GameEvent.Send(EventType.GetDreamReward, btns, this);
            obj.Hide();
        }
    }

    public class OnePassItem : UILoopItemWidget, IListDataItem<OnePassItemInfo>
    {
        private Item_OnePassItem itemObj;
        public void SetItemData(OnePassItemInfo data)
        {
            itemObj = gameObject.GetComponent<Item_OnePassItem>();
            itemObj.InitPassItem(data);
        }

        public override void UpdateSelect()
        {
            base.UpdateSelect();
            itemObj?.SetOnSelect(IsSelected());
        }

        public void UnlockAnim()
        {
            itemObj?.UnlockAnim();
        }

        public List<Transform> GetBtns()
        {
            List<Transform> btns = new List<Transform>();
            btns.Add(itemObj.norRewardImg.transform);
            btns.Add(itemObj.speRewardImg.transform);
            return btns;
        }

        public OnePassItemInfo GetDataInfo()
        {
            return itemObj.Mdata;
        }
    }

    public struct OnePassItemInfo
    {
        public int Id;
        public int UnlockLevel;
        public PassItemType NorType;
        public List<TBRewardData> RewardList;
        //public bool IsOpenDreamPass;
        public OnePassItemInfo(int id, int unlockLevel, PassItemType type1, List<TBRewardData> rewardList)
        {
            Id = id;
            UnlockLevel = unlockLevel;
            NorType = type1;
            RewardList = rewardList;
            //IsOpenDreamPass = isOpenDreamPass;
        }

    }

    public enum PassItemType
    {
        None,
        Getted,//已领取
        WaitGet,//待领取
        Curr,//当前
        Lock,//锁定
    }
}
