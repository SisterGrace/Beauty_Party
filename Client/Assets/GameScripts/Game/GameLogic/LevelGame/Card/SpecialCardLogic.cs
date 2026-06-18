using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using TK_Engine;
using Config;
using Config.common;
using Config.config;
using Y_Base;
using UnityEngine.UI;
using AudioType = TK_Engine.AudioType;
using Random = UnityEngine.Random;

namespace GameLogic
{
    //TODO: 卡牌特殊类型拆分：无特殊效果、被锁住、能直接点击且有交互的、拉链、生成器卡牌（3的倍数）
    public class SpecialCardLogic : MonoBehaviour
    {
        #region 通用属性
        private SpecialData _data;
        private CfgLockCardInfo _lockCardInfo;
        private Vector2 _startAnchoredPos;
        private RectTransform _cardRect;
        private CardCell _cardcell;
        //锁卡牌特效
        private ShowCardVfx _showLockVfx;
        //功能卡牌特效
        private Dictionary<FunctionCardType,ShowCardVfx> _showFunctionVfx = new Dictionary<FunctionCardType, ShowCardVfx>();
        public RectTransform SpecialNode;
        public Transform CurMask;
        public RectTransform CardRect
        {
            get
            {
                if (_cardRect == null)
                {
                    _cardRect = GetComponent<RectTransform>();
                }

                return _cardRect = GetComponent<RectTransform>();
            }
        }

        public CardCell MyCardcell
        {
            get
            {
                if (_cardcell == null)
                {
                    _cardcell = GetComponent<CardCell>();
                }

                return _cardcell;
            }
        }
        public SpecialData Data => _data;
        #endregion

        #region 卡牌特殊属性
        //解锁次数
        private int _unLockTimes;
        public bool IsLock => _unLockTimes>0||IsShell;
        
        public bool IsLockCard => _unLockTimes > 0;
        //卡牌锁类型
        public LockCardType GetLockCardType => _data._lockCardType;
        //卡牌功能类型
        public List<FunctionCardType> GetFunctionCardTypes => _data.FunctionCardTypeList;
        public Dictionary<FunctionCardType,ShowCardVfx> ShowFunctionVfx => _showFunctionVfx;
        //卡牌分组id
        public int GetGroupId => _data.CombinedId;
        //金块消除次数
        private int _goldTimes = 0;
        //炸弹引爆次数
        public int BombTimes
        {
            get => _data.BombCount;
            set
            {
                _data.BombCount = value;
            }
        } 
        //是否是贝壳
        public bool IsShell=> _data.FunctionCardTypeList!=null&&_data.FunctionCardTypeList.Contains(FunctionCardType.Shell);
        //是否销毁贝壳
        private bool _isDestroyShell = false;
        //是否藤蔓或者拉链
        public bool IsZipperOrVine => _data._lockCardType == LockCardType.Vines ||
                                      (_data.FunctionCardTypeList != null &&
                                       _data.FunctionCardTypeList.Contains(FunctionCardType.Zipper));
        //是否分组卡牌的根节点
        public bool IsRoot => _data.IsRoot;
        /// <summary>
        /// 卡牌是否是对应的功能卡牌
        /// </summary>
        /// <param name="fType"></param>
        /// <returns></returns>
        public bool IsFunctionCard(FunctionCardType fType)
        {
            if (_data.FunctionCardTypeList == null) return false;
            if (_data.FunctionCardTypeList.Count == 0) return false;
            bool isFunction = _data.FunctionCardTypeList.Contains(fType);
            return isFunction;
        }
        //是否需要剔除的卡牌
        public bool IsReject()
        {
            return GetLockCardType == LockCardType.Honey ||
                   GetLockCardType == LockCardType.Rock ||
                   GetLockCardType == LockCardType.Grassy ||
                   GetLockCardType == LockCardType.Door ||
                   IsFunctionCard(FunctionCardType.Reversible) ||
                   IsFunctionCard(FunctionCardType.Shell) ||
                   IsFunctionCard(FunctionCardType.GoldCube);
        }
        #endregion

        public void InitSpecialCard(SpecialData data)
        {
            _data = data;
            if (data._lockCardType != LockCardType.None)
            {
                _lockCardInfo = ConfigLoader.Instance.Tables.TbCfgLockCardInfo.GetByLockType(data._lockCardType);
                _unLockTimes =  _lockCardInfo.UnLocktimes;
            }
            //初始金块的消除次数
            if (data.FunctionCardTypeList != null &&data.FunctionCardTypeList.Contains(FunctionCardType.GoldCube))
            {
                _goldTimes = ConfigLoader.Constant.GoldCardEliminate;
            }
            //根据特殊类型设置卡牌的特殊效果
            SetFunctionStatus();
            SetLockStatus();
        }

        public void SetStartAnchoredPos(Vector2 startAnchoredPos)
        {
            _startAnchoredPos = startAnchoredPos;
            _holdUp = false;
        }
        //设置卡牌锁样式
        private void SetLockStatus()
        {
            //生成锁效果,藤蔓组只生成最右边那一个，其它两个直接跳过
            if(_data._lockCardType == LockCardType.None)return;
            bool isCreatLock = true;
            bool isSetVineOrder = false;
            if (_data._lockCardType == LockCardType.Vines)
            {
                isCreatLock = _data.IsRoot;
            }
            if (!isCreatLock) return;
            string lockName =  _lockCardInfo.TypeFace;
            GameObject lockObj = GameModule.Resource.LoadGameObject(lockName, SpecialNode);
            _showLockVfx = lockObj.GetComponent<ShowCardVfx>();
            _showLockVfx.InitShowVfx(MyCardcell,false);
        }
        //设置卡牌功能样式
        private void SetFunctionStatus()
        {
            //非锁卡牌，功能卡牌直接返回
            if(_data.FunctionCardTypeList==null||_data.FunctionCardTypeList.Count==0)return;
            //对于分组的卡牌，只需要设置他为根节点的样式即可
            //生成功能效果，拉链只能生成最右边哪一个
            foreach (var FcItem in _data.FunctionCardTypeList)
            {
                if (FcItem == FunctionCardType.None) continue;
                if (!_showFunctionVfx.ContainsKey(FcItem))
                {
                    bool isCreatFunction = true;
                    if (FcItem == FunctionCardType.Zipper)
                    {
                        isCreatFunction = _data.IsRoot;;
                    }
                    MyCardcell.SetMaskImg(FcItem,isCreatFunction);
                    if (FcItem == FunctionCardType.Reversible)
                    {
                       SetReversible();
                    }
                    if(!isCreatFunction) continue;
                    string fcName = ConfigLoader.Instance.Tables.TbCfgFunctionCardInfo.GetByFunctionType(FcItem).TypeFace;
                    GameObject fcObj = GameModule.Resource.LoadGameObject(fcName, SpecialNode);
                    ShowCardVfx fcVfx = fcObj.GetComponent<ShowCardVfx>();
                    fcVfx.InitShowVfx(MyCardcell,true);
                    _showFunctionVfx.Add(FcItem, fcVfx);
                }
            }
           
           
        }
        
        public void SpecialAnim(Action<bool> PlayingAnim = null,bool isAdd=false)
        {
            //是否有锁住的
            bool isNotLocked = (_data._lockCardType == LockCardType.None || !IsLock)&&!IsShell;
            //是否有锁住的拉链
            bool hasNoLockedZipper = !CheckLockZipper(_data.CombinedId);
            //是否是拉链组
            bool isZipperGroup = IsZipperGroup(_data.CombinedId, out var zipperGroup);
            if (isNotLocked&&hasNoLockedZipper)
            {
                //如果是拉链卡牌，得一组同时播放动画
                if (isZipperGroup &&zipperGroup!=null)
                {
                    foreach (var card in zipperGroup)
                    {
                        if (card.MySpecialCardLogic.IsRoot)
                        {
                            card.MySpecialCardLogic.CardHoldUpAnim(PlayingAnim);
                        }
                        
                    }
                }else
                {
                    CardHoldUpAnim(PlayingAnim);
                }

                if (isAdd)
                {
                    CardsController.Instance.AddTempCardCell(true,MyCardcell);
                }
            }
            else
            {
                if (isZipperGroup &&zipperGroup!=null)
                {
                    foreach (var card in zipperGroup)
                    {
                        card.MySpecialCardLogic.CardLockAnim();
                    }
                }
                else
                {
                    CardLockAnim();
                }
            }
        }
        /// <summary>
        /// 当卡牌被覆盖时，一些特殊效果不用展示
        /// </summary>
        /// <param name="isShow"></param>
        public void ShowSpecialVfx(bool isShow)
        {
            if (IsFunctionCard(FunctionCardType.Bombshell))
            {
                _showFunctionVfx[FunctionCardType.Bombshell].ShowHideNode(isShow);
            }
        }
        #region 无特殊效果

        //卡牌是否抬起
        private bool _holdUp = false;

        public void CardHoldUpAnim(Action<bool> PlayingAnim=null)
        {
           
            _holdUp = !_holdUp;
            Tween tw = _holdUp
                ? CardRect.DOAnchorPosY(_startAnchoredPos.y + 23, 0.1f).SetEase(Ease.OutSine)
                : CardRect.DOAnchorPosY(_startAnchoredPos.y, 0.1f).SetEase(Ease.OutSine);
            tw.OnComplete(() =>
            {
                PlayingAnim?.Invoke(false);
            });
            if (_holdUp)
            {
                AudioManager.Play(AudioType.Sound,AudioDef.FruitClick);
            }
        }
        #endregion

        #region 被锁住的卡牌
        //卡牌被锁住动画
        public void CardLockAnim()
        {
            if(_data._lockCardType == LockCardType.None&&!IsLock) return;
            switch (_data._lockCardType)
            {
                case LockCardType.None:
                    //这里播放贝壳动画
                    if (IsShell)
                    {
                        AudioManager.Play(AudioType.Sound,AudioDef.ShellClick,false,false);
                        _showFunctionVfx[FunctionCardType.Shell].ClickAnim(MyCardcell);
                    }
                    break;
                case LockCardType.OneShackle:
                    AudioManager.Play(AudioType.Sound,AudioDef.ChainClick,false,false);
                    int animIndex = _unLockTimes - 1;
                    string animName = _lockCardInfo.ClickAnims[animIndex];
                    _showLockVfx.ShowSpecialSpine(animName);
                    break;
                case LockCardType.TwoShackles:
                    AudioManager.Play(AudioType.Sound,AudioDef.ChainClick,false,false);
                    int animIndex1 = _unLockTimes - 1;
                    string animName1 = _lockCardInfo.ClickAnims[animIndex1];
                    _showLockVfx.ShowSpecialSpine(animName1);
                    break;
                case LockCardType.IceCube:
                    AudioManager.Play(AudioType.Sound,AudioDef.IceClick,false,false);
                    string animName2 = _lockCardInfo.ClickAnims[0];
                    _showLockVfx.ShowSpecialVfx(animName2);
                    _showLockVfx.ClickAnim(MyCardcell);
                    break;
                case LockCardType.Honey:
                    AudioManager.Play(AudioType.Sound,AudioDef.HoneyClick,false,false);
                    _showLockVfx.ClickAnim(MyCardcell);
                    break;
                case LockCardType.Vines:
                    AudioManager.Play(AudioType.Sound,AudioDef.vineClick,false,false);
                    var rootCard = CardsController.Instance.GetGoupRootCard(_data.CombinedId, 2);
                    if (MyCardcell !=  rootCard)
                    {
                        rootCard.MySpecialCardLogic.VinesAnim(false);
                    }
                    else
                    {
                        VinesAnim(false);
                    }
                    break;
                case LockCardType.Butterfly:
                    if (_lockCardInfo == null)
                    {
                        _lockCardInfo =
                            ConfigLoader.Instance.Tables.TbCfgLockCardInfo.GetByLockType(LockCardType.Butterfly);
                    }
                    string animName4 = _lockCardInfo.ClickAnims[0];
                    _showLockVfx.ShowSpecialVfx(animName4);
                    break;
                case LockCardType.Door:
                    _showLockVfx.ClickAnim(MyCardcell);
                    break;
                case LockCardType.Rock:
                    AudioManager.Play(AudioType.Sound,AudioDef.StoneClick,false,false);
                    _showLockVfx.ClickAnim(MyCardcell);
                    break;
                case LockCardType.Grassy :
                    int animIndex5 = _unLockTimes - 1;
                    string animName5 = _lockCardInfo.ClickAnims[animIndex5];
                    _showLockVfx.ShowSpecialSpine(animName5);
                    break;
            }
        }
        #region 各类特殊点击动画
        //播放藤蔓动画
        public void VinesAnim(bool isUnlcok,int times=0)
        {
            if (isUnlcok)
            {
                int FaceIndex = Mathf.Min( _lockCardInfo.UnLocktimes - times-1,  _lockCardInfo.AnimNames.Count-1);
                string VfxName =  _lockCardInfo.AnimNames[FaceIndex];
                if (FaceIndex >= 2)//播放销毁动画
                {
                    AudioManager.Play(AudioType.Sound,AudioDef.vineDie2,false,false);
                    _showLockVfx.ShowSpecialVfx(VfxName,true);
                    //隐藏藤蔓spine动画
                    _showLockVfx.HideSpineNode(0);
                }
                else//播放藤蔓移除spine
                {
                    AudioManager.Play(AudioType.Sound,AudioDef.vineDie1,false,false);
                    _showLockVfx.ShowSpecialSpine(VfxName);
                }
            }
            else//播放藤蔓摇晃动画
            {
                string animName = _unLockTimes.ToString();
                _showLockVfx.ShowSpecialSpine(animName);
            }
        }
       
        #endregion
        /// <summary>
        /// 解锁一次当前卡牌
        /// </summary>
        public void UnlockCurrentCard(CardCell Nextcard = null,bool FullyUnlock=false,bool isSetDoor = false)
        {
            //当没有锁，直接返回
            if (_data._lockCardType == LockCardType.None) return;
            //解锁次数减1，是否全解锁
            if (!FullyUnlock)
            {
                _unLockTimes--;
            }
            else
            {
                _unLockTimes = 0;
            }
           
            switch (_data._lockCardType)
            {
                //如果是蝴蝶，直接解锁
                case LockCardType.Butterfly:
                    _data._lockCardType = LockCardType.None;
                    SetOtherButterfly(Nextcard?.CardRect, Nextcard?.MySpecialCardLogic, FullyUnlock);
                    return;
                //如果是藤蔓，特殊处理
                case LockCardType.Vines:
                    UnLockVines(FullyUnlock);
                    return;
                //门卡牌。特殊处理
                case LockCardType.Door:
                    SetDoorLock(FullyUnlock,isSetDoor);
                    return;
            }
            //当解锁次数为0时，直接隐藏锁样式
            UnlockAnimal(_unLockTimes);
        }
        //播放解锁动画
        private void UnlockAnimal(int times)
        {
            bool isDestroy = times <= 0;
            UnLockAudio(GetLockCardType, isDestroy);
            //锁链解锁触发撤回引导
            if (GetLockCardType == LockCardType.OneShackle && UserController.Instance.Level == 2)
            {
                GuideController.Instance.CheckTriggerGuide(105);
            }
            int FaceIndex = Mathf.Min(_lockCardInfo.UnLocktimes - times-1, _lockCardInfo.AnimNames.Count-1);
            string iconName = "";
            if (!isDestroy)
            {
                iconName = _lockCardInfo.ChangeFaces[FaceIndex+1];
            }
            else
            {
                _data._lockCardType = LockCardType.None;
            }
            string lockAnim = _lockCardInfo.AnimNames[FaceIndex];
            _showLockVfx.ShowSpecialVfx(lockAnim, isDestroy, iconName);
        }
       
        #region 藤蔓相关
        private void UnLockVines(bool FullyUnlock =false)
        {
            if(_data._lockCardType != LockCardType.Vines) return;
            
            //如果是藤蔓，特殊处理
            if (_unLockTimes>=0)
            {
                var vinesGroup = CardsController.Instance.GetGroupCards(_data.CombinedId,2);
                if (!FullyUnlock&&MyCardcell ==CardsController.Instance.GetGoupRootCard(_data.CombinedId,2))
                {
                    //移除最外围的藤蔓
                    CardCell temp = vinesGroup[_unLockTimes];
                    vinesGroup.Remove(temp);
                    temp.MySpecialCardLogic.RemoveOneVines();
                    VinesAnim(true,_unLockTimes);
                }
                //直接消除藤蔓
                else if(FullyUnlock)
                {
                    foreach (var card in vinesGroup)
                    {
                        card.MySpecialCardLogic.RemoveOneVines();
                        card.MySpecialCardLogic._data.CombinedId = -1;
                    }
                    vinesGroup[0].MySpecialCardLogic._showLockVfx.gameObject.Hide();
                    CardsController.Instance.RemoveOneGroup(_data.CombinedId,2);
                }
                
            }
           
        }
        //移除一根藤蔓
        private void RemoveOneVines()
        {
            _data._lockCardType = LockCardType.None;
            _unLockTimes = 0;
            SetGroupParent(false);
            //检测当前是否有遮罩
            if (CardsController.Instance.CheckIsTop(MyCardcell))
            {
                MyCardcell.ExposeCell();
            }
        }
        
        public void SetGroupParent(bool Enter,Transform root=null)
        {
            if (Enter && root!= null)
            {
                CardRect.SetParent(root);
                CardRect.SetSiblingIndex(1);
                CurMask.SetParent(root);
            }
            else
            {
                CardRect.SetParent(MyCardcell.LayerParent);
                CardRect.SetSiblingIndex(0);
                CurMask.SetParent(transform);
            }
        }
        #endregion

        #region 门相关
        private void SetDoorLock(bool FullyUnlock=false,bool isSetDoor = false)
        {
            if (isSetDoor)
            {
                _unLockTimes = 0;
                return;
            }
            _unLockTimes = Mathf.Abs(_unLockTimes);
            string changeFace = _lockCardInfo.ChangeFaces[_unLockTimes];
            string animName = _lockCardInfo.AnimNames[_unLockTimes];
            if (FullyUnlock)
            {
                _unLockTimes = 0;
                _data._lockCardType = LockCardType.None;
                _showLockVfx.transform.SetParent(MyCardcell.LayerParent);
                //门碎掉动画
                animName = _lockCardInfo.AnimNames[2];
                _showLockVfx.ShowSpecialVfx(animName, true);
                return;
            }
            _showLockVfx.ShowSpecialSpine(animName);
        }
        /// <summary>
        /// 设置门的状态
        /// </summary>
        /// <param name="isOpen">0为开，1为关</param>
        public void SetDoorState(int isOpen)
        {
            if(isOpen == _unLockTimes) return;
            _unLockTimes = isOpen;
            string animName = _lockCardInfo.AnimNames[_unLockTimes];
            _showLockVfx.ShowSpecialSpine(animName);
        }
        #endregion

        #region 蝴蝶相关
        //设置卡牌的蝴蝶锁定
        public void SetOtherButterfly(Transform target,SpecialCardLogic special,bool FullyUnlock =false)
        {
            if (FullyUnlock)
            {
                _showLockVfx.gameObject.Hide();
                return;
            }
            if (target==null||special==null)
            {
                //蝴蝶飞走
                AudioManager.Play(AudioType.Sound,AudioDef.butterflyFly,false,false);
                _showLockVfx.ShowSpecialSpine("fly",isLoop:true);
                ButterflyFlyOff();
                return;
            }
            _showLockVfx.transform.SetParent(CardsController.Instance.CardMoving);
            special._showLockVfx = _showLockVfx;
            //播放蝴蝶飞行动画和特效
            AudioManager.Play(AudioType.Sound,AudioDef.butterflyFly,false,false);
            special._showLockVfx.ShowSpecialSpine("fly",isLoop:true);
            /*var lockCardInfo = ConfigLoader.Instance.Tables.TbCfgLockCardInfo.GetByLockType(LockCardType.Butterfly);
            string animName4 = lockCardInfo.ClickAnims[0];
            special._showLockVfx.ShowSpecialVfx(animName4);*/
            //蝴蝶飞向下一个卡牌，贝塞尔移动
            SetButterflyPos(transform, target, () =>
            {
                //到达目标后蝴蝶播放idle动画和特效
                AudioManager.Play(AudioType.Sound,AudioDef.butterflyDown,false,false);
                special._showLockVfx.ShowSpecialSpine("idle",isLoop:true);
                var lockCardInfo = ConfigLoader.Instance.Tables.TbCfgLockCardInfo.GetByLockType(LockCardType.Butterfly);
                string animName4 = lockCardInfo.ClickAnims[0];
                special._showLockVfx.ShowSpecialVfx(animName4);
            });
        }
        
        public void SetButtterFlyInfo()
        {
            _unLockTimes = 1;
            _data._lockCardType = LockCardType.Butterfly;
        }
        //蝴蝶飞走
        private void ButterflyFlyOff()
        {
            //先判断往左飞还是右飞
            bool isLeft = MyCardcell.CardRect.anchoredPosition.x < 0;
            Vector3 targetPos = isLeft?_showLockVfx.HideObjects[1].transform.position: _showLockVfx.HideObjects[0].transform.position;
            Vector3[] path = new Vector3[4];
            path[0] = transform.position;
            path[1] =GetMiddlePos(transform.position, targetPos);
            path[2] = targetPos;
            path[3] = targetPos;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(EXDOBezier.DOBezier(_showLockVfx.transform, path, 0.6f, (Tr) =>
            {
                //消除当前的蝴蝶锁定
                _showLockVfx.gameObject.Hide();
            }).SetEase(Ease.Linear));
           
        }
        //计算bezier曲线的中点
        private Vector2 GetMiddlePos(Vector2 start, Vector2 target)
        {
            Vector2 m = Vector2.Lerp(start, target, 0.1f);
            Vector2 dir = (target - start).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            //随机贝塞尔中间值
            float rd = Random.Range(1f, 1.5f);
            float curveRatio = 0.3f;
            return m + (start - target).magnitude * curveRatio * rd * normal;
        }

        private void SetButterflyPos(Transform start, Transform target,Action callback=null)
        {
            Vector3[] path = new Vector3[4];
            path[0] = start.position;
            Vector2 middle = GetMiddlePos(start.position, target.position);
            path[1] = new Vector3(middle.x, middle.y, start.position.z);
            path[2] = target.position;
            path[3] = target.position;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(EXDOBezier.DOBezier(_showLockVfx.transform, path, 0.6f, (Tr) =>
            {
                CardCell tCard = target.GetComponent<CardCell>();
                _showLockVfx.transform.SetParent(tCard.MySpecialCardLogic.SpecialNode);
                callback?.Invoke();
            }).SetEase(Ease.Linear));
        }
        //销毁掉当前蝴蝶
        public void DestoryButterfly()
        {
            _data._lockCardType = LockCardType.None;
            _unLockTimes = 0;
            this.DestroySafe(_showLockVfx.gameObject,1.3f);
        }
        #endregion
        #endregion

        private void UnLockAudio(LockCardType lockType,bool isDestory)
        {
            switch (lockType)
            {
                case LockCardType.OneShackle:
                case LockCardType.TwoShackles:
                    AudioManager.Play(AudioType.Sound,AudioDef.ChainDie);
                    break;
                case LockCardType.IceCube:
                    AudioManager.Play(AudioType.Sound,AudioDef.IceDie);
                    break;
                case LockCardType.Honey:
                    AudioManager.Play(AudioType.Sound,AudioDef.HoneyDie,false,false);
                    break;
                case LockCardType.Rock:
                    AudioManager.Play(AudioType.Sound,AudioDef.StoneDie,false,false);
                    break;
            }
        }
        #region 功能性卡牌

        /// <summary>
        /// 执行一次功能
        /// </summary>
        public void ExecuteFunction(bool isClick,Action<bool> Iszipper=null,bool ExecuteShell=false,bool isDestroy=false,bool isShellPush =false)
        {
            //当前卡牌没有功能或者卡牌被锁住，不执行功能
            if(_data.FunctionCardTypeList==null||_data.FunctionCardTypeList.Count==0||IsLockCard) return;
            bool IsRemove = false;
            List<FunctionCardType> tempRemove = new List<FunctionCardType>();
            foreach (var function in _data.FunctionCardTypeList)
            {
                switch (function)
                {
                    case FunctionCardType.GoldCube:
                        CollectGold(isClick,isDestroy, (remove,type) =>
                        {
                            IsRemove = remove;
                            tempRemove.Add(type);
                        });
                        break;
                    case FunctionCardType.Reversible:
                        FlipReversible(isClick,isDestroy,(remove,type) =>
                        {
                            IsRemove = remove;
                            tempRemove.Add(type);
                        });
                        break;
                    case FunctionCardType.Bombshell:
                        CheckBomb(isClick,isDestroy,(remove,type) =>
                        {
                            IsRemove = remove;
                            tempRemove.Add(type);
                        });
                        break;
                }
                //如果是拉链卡牌，需要判断是否是点击
                if (isClick && function == FunctionCardType.Zipper)
                {
                    bool iszipper = IsZipperGroup(_data.CombinedId, out var zipperGroup);
                    if (iszipper)
                    {
                        if (!isDestroy)
                        {
                            Iszipper?.Invoke(true);
                        }
                        for (int i = 0; i < zipperGroup.Count; i++)
                        {
                            zipperGroup[i].MySpecialCardLogic.SetGroupParent(false);
                            zipperGroup[i].MySpecialCardLogic.ZipperAnim(i,isDestroy, (remove, types) =>
                            {
                                IsRemove = remove;
                                tempRemove.Add(types);
                            });
                        }
                    }
                    
                }
                if (ExecuteShell)
                {
                    ShellCreatCard(isShellPush);
                }
            }

            if (IsRemove)
            {
                RemoveFunction(_data,tempRemove,isDestroy);
            }
        }

        private void RemoveFunction(SpecialData data,List<FunctionCardType> removeList,bool isDestroy=false)
        {
            foreach (var FcItem in removeList)
            {
                //拉链分组的特殊删除
                if (FcItem == FunctionCardType.Zipper)
                {
                    if (IsZipperGroup(data.CombinedId, out var zipperGroup))
                    {
                        for (int i = 0; i < zipperGroup.Count; i++)
                        {
                            zipperGroup[i].MySpecialCardLogic.GetFunctionCardTypes.Remove(FcItem);
                            zipperGroup[i].MySpecialCardLogic.ShowFunctionVfx.RemoveSafe(FcItem);
                        }
                    }

                    if (isDestroy)
                    {
                        CardsController.Instance.RemoveOneGroup(GetGroupId,1);
                    }
                }
                else
                {
                    GetFunctionCardTypes.Remove(FcItem);
                    _showFunctionVfx.RemoveSafe(FcItem);
                }
            }
        }
        //金块收集
        private void CollectGold(bool isClick,bool isDestroy=false,Action<bool,FunctionCardType> CollectCallback=null)
        {
            if (isDestroy && !isClick) return;
            if (isDestroy&&isClick)
            {
                //金块被破碎，移除卡牌
                _showFunctionVfx[FunctionCardType.GoldCube].gameObject.Hide();
                CollectCallback?.Invoke(true,FunctionCardType.GoldCube);
                return;
            }
            
            //金块收集
            if (isClick&&_goldTimes>0)
            {
                //播放金币收集动画
                _showFunctionVfx[FunctionCardType.GoldCube].gameObject.Hide();
                CollectCallback?.Invoke(true,FunctionCardType.GoldCube);
                PropsController.CollectGold(CardRect.transform.position, () =>
                {
                    //CardsController.Instance.CollectNugget();
                    AudioManager.Play(AudioType.Sound,AudioDef.GoldGet,false,false);
                });
              
            }
            //金块破碎
            else if(_goldTimes>0)
            {
                _goldTimes--;
                bool isDestroyGold = _goldTimes <= 0;
                var cfg = ConfigLoader.Instance.Tables.TbCfgFunctionCardInfo.GetByFunctionType(FunctionCardType.GoldCube);
                int maxEliminate = ConfigLoader.Constant.GoldCardEliminate;
                int FaceIndex = Mathf.Min(maxEliminate - _goldTimes - 1, cfg.AnimNames.Count - 1);
                string iconName = !isDestroyGold?cfg.ChangeFaces[FaceIndex+1]:"";
                string animName = cfg.AnimNames[FaceIndex];
                AudioManager.Play(AudioType.Sound,AudioDef.GoldBreak,false,false);
                _showFunctionVfx[FunctionCardType.GoldCube].ShowSpecialVfx2(animName,iconName,isDestroyGold);
                if (isDestroyGold)
                {
                    CollectCallback?.Invoke(true, FunctionCardType.GoldCube);
                }
            }
        }
        public void SetReversible()
        {
            MyCardcell.Icon.transform.localScale = Vector3.one * 0.9f;
            //MyCardcell.SetMaskImg(FunctionCardType.Reversible);
        }
        //双面牌
        private void FlipReversible(bool isClick,bool isDestroy=false,Action<bool,FunctionCardType> FlipCallback=null)
        {
            if (isDestroy&&!isClick)return;
            var cfg = ConfigLoader.Instance.Tables.TbCfgFunctionCardInfo.GetByFunctionType(FunctionCardType.Reversible);
            if (isClick||isDestroy)
            {
                //双面牌破碎，然后在生成一个
                MyCardcell.Icon.transform.SetParent(MyCardcell.Bg.transform);
                MyCardcell.Icon.rectTransform.anchoredPosition = new Vector2(0, 1.8f);
                MyCardcell.Bg.GetComponent<Image>().enabled = true;
                _showFunctionVfx[FunctionCardType.Reversible].transform.SetParent(MyCardcell.LayerParent);
                _showFunctionVfx[FunctionCardType.Reversible].ShowSpecialVfx(cfg.AnimNames[0], true);
                FlipCallback?.Invoke(true,FunctionCardType.Reversible);
                int NextType=RemoveOneCardType(1);
                CoordData newCoord = new CoordData(MyCardcell.MyCoordData.X, MyCardcell.MyCoordData.Y,1,new SpecialData(LockCardType.None,new List<FunctionCardType>()));
                CardsController.Instance.CreatOneCard(newCoord,MyCardcell.LayerParent,NextType);
            }else
            {
                GetNexttype();
                //双面牌翻转动画
                _showFunctionVfx[FunctionCardType.Reversible].ShowSpecialSpine(cfg.AnimNames[1], false);
                MyCardcell.FlipCardIcon(() =>
                {
                    MyCardcell.SetType();
                });
            }
        }
        //双面卡牌翻转获取类型
        public int GetNexttype()
        {
            MyCardcell.AllCardTypes.Swap(0, 1);
            return MyCardcell.AllCardTypes[0];
        }
        //移除一个卡牌类型
        public int RemoveOneCardType(int index=0)
        {
            int delType = MyCardcell.AllCardTypes[index];
            MyCardcell.AllCardTypes.RemoveAt(index);
            return delType;
        }
        //拉链破碎动画
        public void ZipperAnim(int index,bool isDestroy=false,Action<bool,FunctionCardType> ZipperCallback=null)
        {
            if (index == 0)
            {
                //播放拉链动画
                AudioManager.Play(AudioType.Sound,AudioDef.ZipperDie);
                _showFunctionVfx[FunctionCardType.Zipper].transform.SetParent(MyCardcell.LayerParent);
                var cfg = ConfigLoader.Instance.Tables.TbCfgFunctionCardInfo.GetByFunctionType(FunctionCardType.Zipper);
                _showFunctionVfx[FunctionCardType.Zipper].ShowSpecialSpine(cfg.AnimNames[0],isDestroy,spineDestroy:true);
                //移除拉链卡牌
                ZipperCallback?.Invoke(true,FunctionCardType.Zipper);
            }
            //如果是拉链直接消除，不需要后续动画
            if (isDestroy) return;
            MyCardcell.SetMaskImg(FunctionCardType.None);
            float move = index==0?-30:30;
            float rotate = index==0?15:-15;
            Sequence seq = DOTween.Sequence();
            seq.SetDelay(0.25f);
            seq.Insert(0,CardRect.DOAnchorPosX(_startAnchoredPos.x+move, 0.3f).SetEase(Ease.OutBack));
            seq.Insert(0, CardRect.DOLocalRotate(new Vector3(0, 0, rotate), 0.3f).SetEase(Ease.OutBack));
            seq.OnComplete(() =>
            {
                if (index == 0)
                {
                    CardsController.Instance.MoveToEliminateCard(MyCardcell,true);
                }
            });
        }
        //贝壳卡牌生成器
        private void ShellCreatCard(bool isShellPush =false)
        {
            MyCardcell.MyCoordData.Count--;
            var cfg = ConfigLoader.Instance.Tables.TbCfgFunctionCardInfo.GetByFunctionType(FunctionCardType.Shell);
            _showFunctionVfx[FunctionCardType.Shell].UpdateCountTxt(MyCardcell,MyCardcell.MyCoordData.Count);
            _isDestroyShell = MyCardcell.MyCoordData.Count <= 0;
            CoordData newCoord = new CoordData(MyCardcell.MyCoordData.X, MyCardcell.MyCoordData.Y + 2,1,new SpecialData(LockCardType.None,new List<FunctionCardType>()));
            CardCell newCard =CardsController.Instance.CreatOneCard(newCoord, !isShellPush?LevelController.Instance.GetMaxLayerTrans():MyCardcell.LayerParent, MyCardcell.CardType,IsSetPos:false,isShell:isShellPush);
            //播放贝壳动画
            AudioManager.Play(AudioType.Sound,AudioDef.ShellTu,false,false);
            _showFunctionVfx[FunctionCardType.Shell].ShowSpecialVfx2(cfg.AnimNames[0],cfg.AnimNames[5]);
            newCard.CardRect.localScale = Vector3.zero;
            newCard.CardRect.anchoredPosition = MyCardcell.CardRect.anchoredPosition;
            Sequence seq = DOTween.Sequence();
            seq.Insert(0,newCard.CardRect.DOAnchorPos(newCard.MySpecialCardLogic._startAnchoredPos, 0.3f)
                .SetEase(Ease.InBounce));
            seq.Insert(0,newCard.CardRect.DOScale(Vector3.one, 0.25f).SetEase(Ease.InBounce));
            seq.OnComplete(() =>
            {
                newCard.RecordLocation();
                if (_isDestroyShell)
                {
                    //移除贝壳卡牌
                    CardsController.Instance.PopUnitCoverStack(MyCardcell,out var downCards);
                    CardsController.Instance.CheckShellNeedCreateCard(MyCardcell);
                    _showFunctionVfx[FunctionCardType.Shell].ShowSpecialVfx2(cfg.AnimNames[1],"",true);
                    AudioManager.Play(AudioType.Sound,AudioDef.ShellDie,false,false);
                    MyCardcell.Eliminate();
                }
            });
            if(_isDestroyShell) return;
            RemoveOneCardType();
            //更新贝壳下一个卡牌的类型
            MyCardcell.SetType();
        }
        //检测炸弹是否爆炸
        public void CheckBomb(bool isClick,bool isDestroy=false,Action<bool,FunctionCardType> BombCallback=null)
        {
            if(isDestroy&&!isClick) return;
            var cfg = ConfigLoader.Instance.Tables.TbCfgFunctionCardInfo.GetByFunctionType(FunctionCardType.Bombshell);
            if (isDestroy || isClick)
            {
                BombTimes = 0;
                //同时把这个炸弹卡牌的特殊效果移除
                _showFunctionVfx[FunctionCardType.Bombshell].transform.SetParent(MyCardcell.LayerParent);
                _showFunctionVfx[FunctionCardType.Bombshell].UpdateCountTxt(MyCardcell,BombTimes,cfg.AnimNames[0],true);
                BombCallback?.Invoke(true, FunctionCardType.Bombshell);
            }
            else
            {
                if (_data.FunctionCardTypeList != null && _data.FunctionCardTypeList.Contains(FunctionCardType.Bombshell))
                {
                    BombTimes--;
                    if (BombTimes <= 0)
                    {
                        AudioManager.Play(AudioType.Sound,AudioDef.BombTime0,false,false);
                        _showFunctionVfx[FunctionCardType.Bombshell].UpdateCountTxt(MyCardcell,BombTimes,cfg.AnimNames[0]);
                        GameModule.Timer.AddTimer(tiemr =>
                        {
                            //游戏结束
                            CardsController.Instance.Lose(1);
                            //炸弹暂时先不用移除，等待玩家触发炸弹销毁效果
                            CardsController.Instance.CurFailBombCards.AddWithoutRepetition(MyCardcell);
                        },0.3f);
                        return;
                    }
                    //更新数字显示
                    _showFunctionVfx[FunctionCardType.Bombshell].UpdateCountTxt(MyCardcell,BombTimes,cfg.AnimNames[0]);
                }
            }
            
        }
        //炸弹卡牌数据销毁
        public void DestroyBomb(FunctionCardType fType)
        {
            GetFunctionCardTypes.Remove(fType);
            _showFunctionVfx.RemoveSafe(fType);
        }
        //检测拉链组是否有被覆盖遮挡的卡牌
        public bool CheckCoveredZipper(int groupId)
        {
            if (groupId <= 0) return false;
            if(IsZipperGroup(groupId,out var zipperGroup))
            {
                foreach (var card in zipperGroup)
                {
                    if (card.IsCovered)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //检测拉链组是否有锁住的卡牌
        public bool CheckLockZipper(int groupId)
        {
            if (groupId <= 0) return false;
            if (IsZipperGroup(groupId ,out var zipperGroup))
            {
                foreach (var card in zipperGroup)
                {
                    if (card.MySpecialCardLogic.IsLock)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //检测卡牌是否是拉链组，并输出拉链组
        private bool IsZipperGroup(int groupId, out List<CardCell> zipperGroup)
        {
            if ((_data.FunctionCardTypeList != null &&
                 _data.FunctionCardTypeList.Contains(FunctionCardType.Zipper)) ||
                CardsController.Instance.GetGroupCards(_data.CombinedId, 1) != null)
            {
                zipperGroup = CardsController.Instance.GetGroupCards(groupId, 1);
                return true;
            }
            zipperGroup = null;
            return false;
        }
        #endregion
    }
    
}