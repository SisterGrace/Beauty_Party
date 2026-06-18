using System.Collections;
using System.Collections.Generic;
using Config.common;
using DG.Tweening;
using Y_Base;
using Sirenix.OdinInspector;
using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using TMPro;
using System;
using AudioType = TK_Engine.AudioType;


namespace GameLogic
{
    public class ShowCardVfx : MonoBehaviour
    {
        [LabelText("是否播放常规特效")]
        public bool IsOpenNornalVfx = false;//是否播放常规特效
        [ShowIf("IsOpenNornalVfx")]
        public ParticleSystem StarPar;//星星特效
        [ShowIf("IsOpenNornalVfx")]
        public ParticleSystem FlashPar;//白色闪烁特效
        [ShowIf("IsOpenNornalVfx")]
        public ParticleSystem ClickPar;//点击圆环特效
        [ShowIf("IsOpenNornalVfx")]
        public ParticleSystem CrusherPar;//碎片爆炸特效
        
        [LabelText("是否播放特殊特效")]
        public bool IsOpenSpecailVfx = false;//是否播放特殊特效
        [ShowIf("IsOpenSpecailVfx")]
        public Image iconNode;
        private ParticleSystem[] particleSystems;
        private ParticleSystem _particleRoot;
        [LabelText("是否播放Spine特效")]
        public bool IsOpenSpine = false;//是否播放Spine特效
        [ShowIf("IsOpenSpine")]
        public SkeletonGraphic SpineGic;
        [ShowIf("IsOpenSpine")]
        public bool IsChangeSpineData = false;//是否改变Spine数据
        [ShowIf("IsOpenSpine")]
        public string SpineDataName;
        
        [LabelText("文字特殊处理")]
        public bool IsOpenTxtVfx = false;//是否播放炸弹特效
        [ShowIf("IsOpenTxtVfx")]
        public TextMeshProUGUI CountText;
        
        [LabelText("贝壳特殊处理")]
        public bool IsOpenShellVfx = false;//是否播放贝壳特效
        [ShowIf("IsOpenShellVfx")]
        public Animator anim;

        public List<GameObject> HideObjects;//隐藏的对象
        private CardCell Maincard;
        #region 回调事件
        public Action OnShowCardVfxEnd;
        
        #endregion
        /// <summary>
        /// 初始化显示特效
        /// </summary>
        public void InitShowVfx(CardCell card,bool isInitFunctinVfx)
        {
            Maincard = card;
            if (!isInitFunctinVfx)return;
            SpecialData data = card.MySpecialCardLogic.Data;
            CoordData coordData = card.MyCoordData;
            if (data.FunctionCardTypeList != null && data.FunctionCardTypeList.Count > 0)
            {
                foreach (var functionCardType in data.FunctionCardTypeList)
                {
                    if (functionCardType == FunctionCardType.Bombshell)
                    {
                        CountText.text = data.BombCount.ToString();
                    }
                    else if (functionCardType == FunctionCardType.Shell)
                    {
                        card.Bg.Hide();
                        CountText.text = coordData.Count.ToString();
                        if (IsOpenShellVfx)
                        {
                            card.Icon.rectTransform.SetParent(iconNode.rectTransform);
                            card.Icon.rectTransform.anchoredPosition = Vector2.zero;
                            card.Icon.color = new Color(1, 1, 1, 0.5f);
                        }
                    }
                    else if (functionCardType == FunctionCardType.Reversible)
                    {
                        card.Icon.rectTransform.SetParent(iconNode.rectTransform);
                        card.Icon.rectTransform.anchoredPosition = new Vector2(0, 23f);
                    }
                }
            }
        }
        //播放碎片爆炸特效
        public void ShowCrusherVfx()
        {
            StarPar?.Play();
            FlashPar?.Play();
            ClickPar?.Play();
            //CrusherPar?.Play();
        }
        
        #region 播放特殊效果的特效

        /// <summary>
        /// 播放特殊效果的特效
        /// </summary>
        /// <param name="name">对应特效的节点名称</param>
        /// <param name="isDestroy">是否销毁特殊特效</param>
        public void ShowSpecialVfx(string name,bool isDestroy = false,string iconName="")
        {
            if (IsOpenSpecailVfx)
            {
                if (isDestroy)
                {
                    iconNode?.gameObject.Hide();
                    if (HideObjects != null)
                    {
                        foreach (var obj in HideObjects)
                        {
                            obj.Hide();
                        }
                    }
                }
                Transform par = UnityExtension.FindChild(transform, name);
                float maxDuration = 0;
                if (par != null)
                {
                    _particleRoot = par.GetComponent<ParticleSystem>();
                    _particleRoot.Play();
                    if (!string.IsNullOrEmpty(iconName) && !isDestroy)
                    {
                        if (IsOpenSpine)
                        {
                            ShowSpecialSpine(iconName,isChangeSkeData:IsChangeSpineData,dataName:SpineDataName);
                        }
                        else
                        {
                            var timerId1= GameModule.Timer.AddTimer(timer =>
                            {
                                iconNode.sprite = GameModule.Resource.LoadAsset<Sprite>(iconName);
                            }, 0.2f);
                            Maincard.AllTimerIds.Add(timerId1);
                        }
                    }

                    if (OnShowCardVfxEnd != null)
                    {
                        var timerId2 = GameModule.Timer.AddTimer(timer =>
                        {
                            OnShowCardVfxEnd?.Invoke();
                        }, maxDuration);
                        Maincard.AllTimerIds.Add(timerId2);
                    }
                }
                else
                {
                    Log.Warning($"没有找到特效节点{name}");
                }
            }
            else
            {
               Log.Warning("没有打开特殊特效");
            }
        }

        public void ShowSpecialVfx2(string Nodename,string spineName="",bool isDestroy = false)
        {
            if (IsOpenSpecailVfx)
            {
                if (isDestroy&&HideObjects!=null)
                {
                    foreach (var obj in HideObjects)
                    {
                        obj.Hide();
                    }
                }
                Transform par = UnityExtension.FindChild(transform, Nodename);
                if (par != null)
                {
                    _particleRoot = par.GetComponent<ParticleSystem>();
                    _particleRoot.Play();
                }

                if (!string.IsNullOrEmpty(spineName) && !isDestroy)
                {
                    ShowSpecialSpine(spineName);
                }
            }
        }
        
        public void ShowSpecialSpine(string name,bool isDestroy = false,bool isChangeSkeData =false,string dataName="",bool isLoop = false,bool spineDestroy = false)
        {
            if (isDestroy)
            {
                iconNode?.gameObject.Hide();
                this.DestroySafe(gameObject,0);
                return;
            }

            if (isChangeSkeData)
            {
                SpineController.SetSpineData(SpineGic, dataName);
            }
            
            SpineController.PlayAnim(SpineGic, 0,name,isLoop,"", spineDestroy? () =>
            {
                this.DestroySafe(gameObject,0);
            }:null);
        }
        #region 贝壳动画
        //贝壳点击
        public void ClickAnim(CardCell card)
        {
            Sequence seq =DOTween.Sequence();
            seq.Append(card.CardRect.DOScale(Vector3.one * 0.98f, 0.02f).SetEase(Ease.Linear));
            seq.Append(card.CardRect.DOScale(Vector3.one * 0.92f, 0.03f).SetEase(Ease.Linear));
            seq.Append(card.CardRect.DOScale(Vector3.one * 1.04f, 0.06f).SetEase(Ease.Linear));
            seq.Append(card.CardRect.DOScale(Vector3.one * 0.96f, 0.07f).SetEase(Ease.Linear));
            seq.Append(card.CardRect.DOScale(Vector3.one , 0.07f).SetEase(Ease.Linear));
        }
        
        #endregion
       
        public void HideSpineNode(int index)
        {
            SpineGic.gameObject.Hide();
        }

        public void ShowHideNode(bool isShow)
        {
            if (HideObjects != null)
            {
                foreach (var obj in HideObjects)
                {
                    obj.SetActive(isShow);
                }
            }
        }
        //更新数量
        public void UpdateCountTxt(CardCell card,int count,string stopAnimName="",bool isAtOnceDes = false)
        {
            SpecialData data = card.MySpecialCardLogic.Data;
            if (data.FunctionCardTypeList != null && data.FunctionCardTypeList.Count > 0)
            {
                if (data.FunctionCardTypeList.Contains(FunctionCardType.Bombshell))
                {
                    //预警提示
                    if (count == 1)
                    {
                        SpineGic.transform.localScale = Vector3.one * 0.8f;
                        ShowSpecialSpine("fast",isLoop:true);
                    }else if (count >= 2)
                    {
                        AudioManager.Play(AudioType.Sound,AudioDef.BombTime,false,false);
                        SpineGic.transform.localScale = Vector3.one;
                        ShowSpecialSpine("slow");
                    }else if (count == 0&&isAtOnceDes)
                    {
                        AudioManager.Play(AudioType.Sound,AudioDef.BombDie,false,false);
                        SpineGic.transform.localScale = Vector3.one * 0.8f;
                        Transform par = UnityExtension.FindChild(transform, stopAnimName);
                        if (par != null)
                        {
                            _particleRoot = par.GetComponent<ParticleSystem>();
                            _particleRoot.Play();
                        }
                        SpineGic.transform.DOScale(0, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                        {
                            gameObject.Hide();
                        });
                    }
                }
                CountText.text = count.ToString();
            }
            
            
        }
        #endregion
        
       
    }
}
