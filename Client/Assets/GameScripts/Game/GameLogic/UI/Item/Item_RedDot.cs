using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace GameLogic
{
    public class Item_RedDot : MonoBehaviour
    {
        public float Looptime = 2.0f;
        public Sequence Twanim;
        private bool isPlaying = false;
        private RectTransform rectTransform;
        private Vector3 startPos;
        public bool IsUI = true;
        
        public bool IsPlayAnim = true;
        
        public bool IsNew;
        private bool _isPlay;
        private bool _isInit;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            rectTransform = GetComponent<RectTransform>();
            startPos = new Vector3(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
            _isInit = true;
        }

        private void OnEnable()
        {
            if (IsNew)
            {
                isPlaying = true;
                PlayAnim();
                /*if (_isPlay)
                {
                    StopAnim();
                }
                else
                {
                    
                }*/
            }
        }

        public void PlayAnim(bool isUI = true)
        {
            if(!isPlaying) return;
            if (Twanim == null)
            {
                if (!_isInit) Init();
                Twanim = DOTween.Sequence();
                //初始位置和缩放
                transform.localScale = isUI ? Vector3.one : Vector3.one * 0.72f;
                if (isUI)
                {
                    rectTransform.anchoredPosition = startPos;
                }
                else
                {
                    transform.localPosition = startPos;
                }
            }
            else
            {
                //防止重复播放动画
                return;
            }

            _isPlay = true;
            Twanim.Append(transform.DOScale(1.1f, 0.1f));
            Twanim.Append(transform.DOScale(0.7f, 0.3f));
            Twanim.Append(transform.DOScale(1.05f, 0.3f));
            Twanim.Append(transform.DOScale(1f, 0.1f));
            Twanim.OnStepComplete(() =>
            {
                //当红点所在的父节点隐藏时，停止动画
                if (!gameObject.activeInHierarchy)
                {
                    StopAnim(isUI);
                }
            });
            Twanim.SetDelay(Looptime);
            Twanim.SetLoops(-1, LoopType.Yoyo);
        }

        public void StopAnim(bool isUI = true)
        {
            if(!isPlaying) return;
            _isPlay = false;
            transform.localScale = isUI ? Vector3.one : Vector3.one * 0.72f;
            Twanim.Kill(true);
            Twanim = null;
        }

        private void OnDestroy()
        {
            if (Twanim != null)
            {
                Twanim.Kill(true);
                Twanim = null;
            }
        }
    }
}