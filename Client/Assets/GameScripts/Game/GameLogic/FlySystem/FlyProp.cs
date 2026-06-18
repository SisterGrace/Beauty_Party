using System;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Y_Base;
using TK_Engine;
using TMPro;


namespace GameLogic
{
    //道具
    public class FlyProp : MonoBehaviour
    {
        public GameObject FlyNode;
        public CanvasGroup PropCanvasGroup;
        public GameObject OneProoNode;
        public RectTransform Rect;
        public Image icon;
        public TextMeshProUGUI countTxt;
        public GameObject Frame;
        public TextMeshProUGUI timeTxt;
        public ParticleSystem bombPar;
        public float Radius = 30f;
        private itemFlyType _flyPropType;
        private Action<Transform> _closeCallback;
        private Action<Transform, OneRewardData> _closeCallback2;
        private Transform _targetTr;
        private OneRewardData _rewData;
        private List<Item_OnePropBomb> _bombList = new List<Item_OnePropBomb>();

        public float flyDuration = 0.5f;

        public Item_PhotoLoad PhotoNode;

        public void Init(bool isSetUIPos, Vector3 startWorldPos, Vector2 startUIPos, float radius, OneRewardData data, Transform endTr, Action<Transform> callback, Action<Transform, OneRewardData> callback2 = null)
        {
            PropCanvasGroup.alpha = 1;
            transform.position = startWorldPos;
            if (isSetUIPos)
            {
                Rect.anchoredPosition += startUIPos * radius;
            }
            _flyPropType = data.flyType;
            if (callback != null)
            {
                _closeCallback = callback;
            }
            else
            {
                _rewData = data;
                _closeCallback2 = callback2;
            }

            _targetTr = endTr;
            switch (data.flyType)
            {
                case itemFlyType.None:
                    break;
                case itemFlyType.AddTimeFly:
                    Frame.Show();
                    countTxt.gameObject.Hide();
                    timeTxt.text = $"∞{TimeUtil.GetSecToMin(data.Count)}m";
                    break;
                case itemFlyType.EachFly:
                    Frame.Hide();
                    countTxt.gameObject.Hide();
                    break;
                case itemFlyType.MultipleFly:
                    Frame.Hide();
                    countTxt.gameObject.Show();
                    countTxt.text = PropsController.GetRewardCount(data);
                    break;
            }
            OneProoNode.SetActive(data.Type != ERewardType.Photo);
            PhotoNode.gameObject.SetActive(data.Type == ERewardType.Photo);
            if (data.Type == ERewardType.Prop)
            {
                //var cfg = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetById(data.Id);
                icon.SetSprite(PropsController.GetFlyItemIcon(data));
                
            }
            else if (data.Type == ERewardType.Currency)
            {
                icon.SetSprite(PropsController.GetFlyItemIcon(data));
            }
            else if (data.Type == ERewardType.Photo)
            {
                PhotoNode.LoadPhoto(data.Id, (int)data.Count);
            }
            icon.SetNativeSize();
            ShowOnePropBomb();
        }
        private void ShowOnePropBomb()
        {
            FlyNode.transform.localScale = Vector3.zero;
            bombPar.Play();
            Sequence seq = DOTween.Sequence();
            seq.Append(FlyNode.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBounce));
            seq.AppendInterval(0.1f);
            seq.OnComplete(() =>
            {
                if (_targetTr != null)
                {
                    //道具飞向目标位置
                    MoveToTarget();
                }
                else
                {
                    //道具简单的向上移动
                    MoveToUp();
                }

            });
        }
        private void MoveToTarget()
        {
            transform.DOMove(_targetTr.position, flyDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                GameObject endEff = FlySystem.Instance.ShowEff("prefab_button_loading_01", transform.parent, _targetTr,
                    Vector3.one * 35f);
                ShowPropVfx endVfx = endEff.GetComponent<ShowPropVfx>();
                endVfx.PlayPropVfx(true);
                Close();
            });
        }

        private void MoveToUp()
        {
            float moveY = Rect.anchoredPosition.y + 60f;
            PropCanvasGroup.DOFade(0, 0.3f).SetEase(Ease.OutSine);
            Rect.DOAnchorPosY(moveY, 0.3f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                Close();
            });
        }
        private void Close()
        {
            transform.localPosition = Vector3.one * 999;
            _closeCallback?.Invoke(_targetTr);
            _closeCallback2?.Invoke(_targetTr, _rewData);
            _closeCallback = null;
            _closeCallback2 = null;
            FlySystem.Instance.HideObj(gameObject);
        }

    }

}
