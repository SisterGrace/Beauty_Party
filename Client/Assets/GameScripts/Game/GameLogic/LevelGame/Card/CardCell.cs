using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Config.common;
using DG.Tweening;
using GameBase;
using Sirenix.OdinInspector;
using TK_Engine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AudioType = TK_Engine.AudioType;
using Random = UnityEngine.Random;

namespace GameLogic
{
    public class CardCell : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
    {
        public CoordData _coordData;
        //卡牌类型
        public List<int> _cardTypes;
        //是否在槽位中
        private bool _isInSolt;
        //是否可见
        private bool _isVisible;
        //是否临时存储
        private bool _isStored;
        //是否被覆盖
        private bool _isCovered;
        //是否设置遮罩层级
        private bool _isSetMaskOrder;
        private Transform _layerParent;
        private RectTransform _cardRect;
        private Transform _cardLocation;
        private Vector3 _locationWorldPos;
        private int _locationIndex;
        public List<int> _coverIds;
        private int _stackIndex = -1;
        private SpecialCardLogic _specialCardLogic;

        private bool _cardDestroy;
        //时间延迟事件id
        public List<int> AllTimerIds = new List<int>();
        public Transform LayerParent=>_layerParent;
        public RectTransform CardRect
        {
            get
            {
                if (_cardRect == null)
                {
                    _cardRect = GetComponent<RectTransform>();
                }
                return _cardRect;
            }
        }
        public CoordData MyCoordData => _coordData;
        public bool IsVisible => _isVisible;
        public List<int> CoverIds=>_coverIds;
        public int CardType =>  _cardTypes[0];
        public List<int> AllCardTypes => _cardTypes;
        public bool IsCovered => _isCovered;
        public Vector3 LocationWorldPos=> _locationWorldPos;
        public int StackIndex
        {
            get=>_stackIndex;
            set=>_stackIndex = value;
        }
        //是否临时存储
        public bool IsStored
        {
            get => _isStored;
            set => _isStored = value;
        }
        //是否在槽位
        public bool IsInSolt
        {
            get => _isInSolt;
            set => _isInSolt = value;
        }

        public SpecialCardLogic MySpecialCardLogic
        {
            get
            {
                if (_specialCardLogic == null)
                {
                    _specialCardLogic = GetComponent<SpecialCardLogic>();
                }
                return _specialCardLogic;
            }
        }
        #region 各类事件
        public Action<CardCell,bool> DelClick;//卡牌点击事件
        public Action<CardCell> DelEliminate;//卡牌合成消除事件
        public Action<CardCell> DelMoveBack;//卡牌移动回原位事件
        #endregion

        public GameObject Bg;
        public Image Icon;
        public Image Mask;
        public void InitCard(List<int> types, CoordData data)
        {
            _coordData = data;
            _isInSolt = false;
            _isVisible = false;
            _isStored = false;
            _cardDestroy = false;
            SetType(types);
            MySpecialCardLogic.InitSpecialCard(_coordData.CursSpecial);
            GameModule.UI.GetWindow(typeof(Panel_Game).FullName);
        }

        #region 卡牌各种设置
        [Button("设置卡牌类型")]
        //设置卡牌类型
        public void SetType(List<int> types=null)
        {
            //_cardType = type;
            if (types != null)
            {
                _cardTypes = types;
            }
            if ( _cardTypes != null)
            {
                if (_cardTypes[0] > 100)
                {
                    /*Bg.GetComponent<Image>().sprite = GameModule.Resource.LoadAsset<Sprite>(GetCardIconName( _cardTypes[0]));
                    Bg.GetComponent<Image>().SetNativeSize();*/
                    Bg.GetComponent<Image>().SetSprite(GetCardIconName( _cardTypes[0]),true);
                    Icon.enabled = false;
                }
                else
                {
                    Icon.enabled = true;
                    Icon.SetSprite(GetCardIconName( _cardTypes[0]),true);
                    /*Bg.GetComponent<Image>().sprite = GameModule.Resource.LoadAsset<Sprite>("icon_cardBg");
                    Bg.GetComponent<Image>().SetNativeSize();*/
                    Bg.GetComponent<Image>().SetSprite("icon_cardBg",true);
                }
                
            }
        }
        //设置卡牌可见性
        public void SetVisible(bool isVisible ,bool isCheckZipperMask =false)
        {
            _isVisible = isVisible;
            gameObject.SetActive(isVisible);
            if (MySpecialCardLogic.GetGroupId > 0)
            {
                if(!isVisible)Mask.gameObject.SetActive(isVisible);
                if (isCheckZipperMask)
                {
                    bool isTop = CardsController.Instance.CheckIsTop(this);
                    Mask.gameObject.SetActive(!isTop);
                }
            }
           
        }

        public void SetParent(Transform parent, bool isLayer = false,bool isMove =false,Transform Newparent =null)
        {
            CardRect.parent = parent;
            if (isLayer)
            {
                _layerParent = parent;
            }

            if (isMove)
            {
                MoveTo(parent.position, t =>
                {
                    if (Newparent != null)
                    {
                        t.transform.SetParent(Newparent);
                    }
                });
            }
        }

        public void SetAnchoredPos(Vector2 pos,bool IssetPos =true)
        {
            _specialCardLogic.SetStartAnchoredPos(pos);
            if (IssetPos)
            {
                CardRect.anchoredPosition = pos;
            }
            _cardRect.localRotation = Quaternion.identity;
        }
        public void SetCoverIds(List<int> coverIds)
        {
            _coverIds = coverIds;
            _stackIndex = -1;
        }
        public void CoverCell(bool isSetOrder =false)
        {
            if (!_isCovered)
            {
                _isCovered = true;
                Mask.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
                Mask.gameObject.Show();
                MySpecialCardLogic.ShowSpecialVfx(false);
            }

            if (MySpecialCardLogic.GetLockCardType == LockCardType.Vines)
            {
                var goupCards = CardsController.Instance.GetGroupCards(MySpecialCardLogic.GetGroupId, 2);
                foreach (var item in goupCards)
                {
                    if(item == this)continue;
                    if (!item._isCovered)
                    {
                        item._isCovered = true;
                        item.Mask.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
                        item.Mask.gameObject.Show();
                    }
                }
            }
        }

        public void RecordLocation()
        {
            _cardLocation = CardRect.parent;
            _locationWorldPos = CardRect.position;
            _locationIndex = CardRect.GetSiblingIndex();
        }

        public void ExposeCell(bool isDestory =false)
        {
            if (_isCovered)
            {
                _isCovered = false;
                Mask.gameObject.Hide();
                MySpecialCardLogic.ShowSpecialVfx(true);
            }

            if (MySpecialCardLogic.GetLockCardType == LockCardType.Vines&&!isDestory)
            {
                var goupCards = CardsController.Instance.GetGroupCards(MySpecialCardLogic.GetGroupId, 2);
                foreach (var item in goupCards)
                {
                    if (item == this) continue;
                    if (item._isCovered)
                    {
                        item._isCovered = false;
                        item.Mask.gameObject.Hide();
                    }
                }
            }
        }
        //设置遮罩图
        public void SetMaskImg(FunctionCardType type,bool isRoot=false)
        {
            string maskName = "icon_cardBg";
            switch (type)
            {
                case FunctionCardType.None:
                    maskName="icon_cardBg";
                    Mask.rectTransform.anchoredPosition = Vector2.zero;
                    break;
                case FunctionCardType.Shell:
                    maskName="icon_gaming_shell";
                    Mask.rectTransform.anchoredPosition = new Vector2(0, -10);
                    break;
                case FunctionCardType.Zipper:
                    maskName = isRoot ? "zipperLeft_mask" : "zipperRight_mask";
                    Mask.rectTransform.anchoredPosition = isRoot ?new Vector2(1,0f):new Vector2(-1, 0f);
                    break;
                case FunctionCardType.Reversible :
                    maskName = "icon_gaming_flip";
                    Mask.rectTransform.localScale = Vector3.one*1.17f;
                    break;
            }
            Mask.SetSprite(maskName);
        }
        //隐藏和显示卡牌的背景
        public void ShowBg(bool isShow)
        {
            Bg.GetComponent<Image>().enabled = isShow;
            Icon.DOFade(isShow ? 1 : 0.7f, 0.1f);
        }
        #endregion

        #region 卡牌交互
        private Rect _pressedScreenRect;
        private bool _isAnimationPlaying = false;
        public void OnPointerDown(PointerEventData eventData)
        { 
            if(IsInSolt||_isCovered||
               MySpecialCardLogic.CheckCoveredZipper(MySpecialCardLogic.GetGroupId)||
               CardsController.Instance.EliminateSlot.IsFull||
               CardsController.Instance.CurPlayCardType == playCardType.useprop||
               _cardDestroy)return;
            _isAnimationPlaying = true;
            if (!MySpecialCardLogic.IsLock)
            {
                GameEvent.Send(EventType.HideGuide);
            }
            _specialCardLogic.SpecialAnim(isCom =>
            {
                _isAnimationPlaying = isCom;
            },true);
            GetCardRect();
            SDKManager.GameShake(1);
        }
    
        public void OnPointerUp(PointerEventData eventData)
        {
            if(IsInSolt||_isCovered||MySpecialCardLogic.IsLock||
               MySpecialCardLogic.CheckCoveredZipper(MySpecialCardLogic.GetGroupId)||
               MySpecialCardLogic.CheckLockZipper(MySpecialCardLogic.GetGroupId)||
                CardsController.Instance.CurPlayCardType == playCardType.useprop||
               _cardDestroy)return;
            StartCoroutine(ProcessPointerUpAfterAnimation(eventData.position));
        }

        private IEnumerator ProcessPointerUpAfterAnimation(Vector2 position)
        {
            while (_isAnimationPlaying)
            {
                yield return null;
            }
            if(CardsController.Instance.CurPlayCardType== playCardType.useprop) yield break;
            //判断是否点击在卡牌上
            if (_pressedScreenRect.Contains(position)&& CardsController.Instance.CurPlayCardType != playCardType.useprop)
            {
                DelClick?.Invoke(this,false); 
            }
            else
            {
                _specialCardLogic.SpecialAnim();
                CardsController.Instance.AddTempCardCell(false,this);
            }
            
        }
        //卡牌移动
        public void MoveTo(Vector3 pos, Action<CardCell> moveDone = null, float time = 0.2f)
        {
            Tween tw = _cardRect.DOMove(pos, time).SetEase(Ease.OutSine);
            if (moveDone != null)
            {
                tw.OnComplete(() =>
                {
                    moveDone?.Invoke(this);
                    //更新当前位置
                    SetAnchoredPos(_cardRect.anchoredPosition);
                });
            }
        }

        public void MoveTo2(Vector3 pos, Action<CardCell> moveDone = null, float time = 0.2f)
        {
            bool isLeft = pos.x> _cardRect.position.x;
            Vector3[] posArr = new Vector3[4];
            posArr[0] = _cardRect.position;
            posArr[1] = GetMiddlePosition(_cardRect.position,pos,isLeft);
            posArr[2] = pos;
            posArr[3] = pos;
            Sequence seq = DOTween.Sequence();
            seq.Append(EXDOBezier.DOBezier(_cardRect, posArr, time, (Tr) =>
            {
                moveDone?.Invoke(this);
                //更新当前位置
                SetAnchoredPos(_cardRect.anchoredPosition);
            }));
        }
        private Vector3 GetMiddlePosition(Vector3 a, Vector3 b,bool isLeft = true)
        {
            Vector3 m = Vector3.Lerp(a, b, 0.05f);
            //ab的垂线
            Vector3 nomal = Vector2.Perpendicular(a - b).normalized;
            float rd = isLeft?-0.8f:0.8f;
            float curRatio = 0.2f;
            return m + (a - b).magnitude * curRatio * rd * nomal;
        }
        public void MoveBack()
        {
            MoveTo(_locationWorldPos, LocateBack);
        }

        private void LocateBack(CardCell tCard)
        {
            tCard.SetParent(tCard._cardLocation);
            tCard._cardRect.SetSiblingIndex(tCard._locationIndex);
            DelMoveBack?.Invoke(tCard);
        }
        //卡牌消除
        public void Eliminate(float dutation = 0.3f)
        {
            _cardDestroy = true;
            DelEliminate?.Invoke(this);
            DestrouCard(dutation);
            foreach (var timeid in AllTimerIds)
            {
                GameModule.Timer.RemoveTimer(timeid);
            }
        }

        public void DestrouCard(float dutation = 0.3f)
        {
            if (!MySpecialCardLogic.IsFunctionCard(FunctionCardType.Shell))
            {
                _cardRect.DOScale(Vector3.one * 1.2f, 0.13f).SetEase(Ease.InOutBack).OnComplete(() =>
                {
                    //卡牌消除特效,根据此时不同的类型做不同的特效
                    AudioManager.Play(AudioType.Sound,AudioDef.FruitDie);
                    GameObject Vfx = GameModule.Resource.LoadGameObject(GetDestoryVfxName(CardType), transform);
                    ShowCardVfx showVfx = Vfx.GetComponent<ShowCardVfx>();
                    showVfx.ShowCrusherVfx();
                    //播放音效
                    Bg.transform.DOScale(0, dutation);
                    this.DestroySafe(gameObject,2f);
                });
            }
            else
            {
                this.DestroySafe(gameObject,2f);
            }
            
        }
        //卡牌提示消除
        public void TipsCard()
        {
            //卡牌提示动画
            Sequence seq = DOTween.Sequence();
            seq.Append(_cardRect.DOScale(Vector3.one * 1.2f, 0.2f).SetEase(Ease.OutBounce));
            seq.Append(_cardRect.DOPunchRotation(new Vector3(0,0,15f), 0.5f,7,0.5f));
            seq.Append(_cardRect.DOScale(Vector3.one , 0.2f).SetEase(Ease.OutBounce));
            seq.OnComplete(() =>
            {
               
            });
        }
        //得到卡牌矩形区域,用于PointerUp判断
        private void GetCardRect()
        {
            //对于拉链棋子的矩形区域是两个卡牌的矩形区域
            if (MySpecialCardLogic.IsFunctionCard(FunctionCardType.Zipper))
            {
                var goupCard = CardsController.Instance.GetGroupCards(MySpecialCardLogic.GetGroupId, 1);
                CardCell cardCell1 = goupCard[0];
                CardCell cardCell2 = goupCard[1];
                Vector3[] corners1 = new Vector3[4];
                Vector3[] corners2 = new Vector3[4];
                cardCell1._cardRect.GetWorldCorners(corners1);
                cardCell2._cardRect.GetWorldCorners(corners2);
                List<Vector3> AllCorners = new List<Vector3>(8);
                AllCorners.AddRange(corners1);
                AllCorners.AddRange(corners2);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                foreach (var item in AllCorners)
                {
                    Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(GameModule.UI.UICamera, item);
                    min.x = Mathf.Min(min.x, screenCorner.x);
                    min.y = Mathf.Min(min.y, screenCorner.y);
                    max.x = Mathf.Max(max.x, screenCorner.x);
                    max.y = Mathf.Max(max.y, screenCorner.y);
                }
                _pressedScreenRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            }
            else
            {
                Vector3[] corners = new Vector3[4];
                _cardRect.GetWorldCorners(corners);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                foreach (var item in corners)
                {
                    Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(GameModule.UI.UICamera, item);
                    min.x = Mathf.Min(min.x, screenCorner.x);
                    min.y = Mathf.Min(min.y, screenCorner.y);
                    max.x = Mathf.Max(max.x, screenCorner.x);
                    max.y = Mathf.Max(max.y, screenCorner.y);
                }
                _pressedScreenRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            }
        }
        
        //卡牌翻转
        public void FlipCardIcon(Action callback1 = null,Action callback2 = null)
        {
            Sequence seq = DOTween.Sequence();
            Bg.GetComponent<Image>().enabled = false;
            seq.Append(Icon.transform.DOScaleY(0, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
            {
                callback1?.Invoke();
            }));
            seq.Append(Icon.transform.DOScaleY(1.7f, 0.1f).SetEase(Ease.InOutBack));
            seq.OnComplete(() =>
            {
                callback2.Invoke();
                //Destroy(c1);
            });
        }
        #endregion
        
        //获取卡牌icon名称
        public string GetCardIconName(int type)
        {
            //return string.Format("icon_card_{0:D2}", type);
            return ConfigLoader.Instance.Tables.TbCfgNormalCardInfo.Get(type).Icon;
        }
        //获取卡牌销毁特效名称
        public string GetDestoryVfxName(int type)
        {
            return ConfigLoader.Instance.Tables.TbCfgNormalCardInfo.Get(type).DesVfx;
        }
       
    }
}
