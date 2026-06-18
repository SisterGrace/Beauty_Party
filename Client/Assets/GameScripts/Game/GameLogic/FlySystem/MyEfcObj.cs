using System;
using System.Collections;
using System.Collections.Generic;
using Y_Base;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using TMPro;

namespace GameLogic
{
    public class MyEfcObj : MonoBehaviour
    {
        public ParticleSystem MainPar;
        private ParticleSystem.MainModule main;
        private ParticleSystemRenderer[] allpar;
        [LabelText("是否播放文字动画")] public bool ShowTextAni;
        [ShowIf("ShowTextAni")]
        public TextMeshProUGUI TextAni;
        [ShowIf("ShowTextAni")]
        public string TxtAtlasName;
        [ShowIf("ShowTextAni")] [LabelText("文本缩放时间")]
        public float txtScaleTime = 0.2f;//文本缩放时间
        [ShowIf("ShowTextAni")] [LabelText("文本移动时间")]
        public float txtMoveTime = 0.2f;//文本移动时间
        [ShowIf("ShowTextAni")] [LabelText("文本起始坐标")]
        public Vector2 TxtStartPos;
        [ShowIf("ShowTextAni")] [LabelText("文本移动的目标Y坐标")]
        public float TxtEndPosY = 120f;
        
        private Action _endCallback;
        private void Awake()
        {
            main = MainPar.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
        public void PlayEfc(Vector3 startPos,float count=0,Action callback=null)
        {
            transform.position = startPos;
            MainPar.Play();
            ShowTxtAnim(ShowTextAni, count, callback);
            _endCallback = callback;
            /*if (allpar == null)
            {
                 allpar=transform.GetComponentsInChildren<ParticleSystemRenderer>();
            }
            foreach (var par in allpar)
            {
                par.sortingOrder = 
            }*/
        }
        /// <summary>
        /// 显示文字动画
        /// </summary>
        /// <param name="isShow">是否播放动画</param>
        /// <param name="count">文字信息</param>
        /// <param name="callback"></param>
        private void ShowTxtAnim(bool isShow, float count,Action callback=null)
        {
            if (TextAni == null) return;
            TextAni.gameObject.SetActive(isShow);
            if (isShow)
            {
                TextAni.text = string.IsNullOrEmpty(TxtAtlasName) ? count.ToString() : $"{TxtAtlasName}+{count}";
                TextAni.transform.localScale = Vector3.zero;
                TextAni.rectTransform.anchoredPosition = TxtStartPos;
                TextAni.DOFade(0, 0f);
                Sequence seq = DOTween.Sequence();
                seq.Append(TextAni.transform.DOScale(2f, txtScaleTime).SetEase(Ease.InBack));
                seq.Join(TextAni.DOFade(1, txtScaleTime));
                seq.Append(TextAni.rectTransform.DOAnchorPos3DY(TxtEndPosY, txtMoveTime).SetEase(Ease.InBack));
                seq.Append(TextAni.DOFade(0, txtMoveTime));
            }
        }
        private void OnParticleSystemStopped()
        {
            gameObject.Hide();
            FlySystem.Instance.HideObj(gameObject);
            _endCallback?.Invoke();
            _endCallback = null;
        }
    }
}
