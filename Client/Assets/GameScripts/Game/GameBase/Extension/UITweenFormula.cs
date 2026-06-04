using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GameBase
{
    public class UITweenFormula
    {
        /// <summary>
        /// 打开Tips
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="action"></param>
        public static void OpenTips(Transform transform, Action action = null)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(1f, 0f));
            sequence.Append(transform.DOScaleY(1.3f, 0.06f));
            sequence.Append(transform.DOScale(new Vector3(1.3f, 0.9f, 1), 0.06f));
            sequence.Append(transform.DOScale(1f, 0.06f));
            if (action != null)
            {
                sequence.OnComplete(action.Invoke);
            }
        }
        
        /// <summary>
        /// 关闭Tips
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="action"></param>
        public static void CloseTips(Transform transform, Action action= null)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(1f, 0f));
            sequence.Append(transform.DOScale(new Vector3(1.39f, 0.6f, 1), 0.1f));
            if (action != null)
            {
                sequence.OnComplete(action.Invoke);
            }
        }
        
        /// <summary>
        /// 打开Tips复用
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="action"></param>
        public static Sequence OpenTipsPool(Sequence sequence, Transform transform, Action action = null)
        {
            if (sequence != null)
            {
                sequence.Restart();
            }
            else
            {
                sequence = DOTween.Sequence();
                sequence.SetAutoKill(false);
                sequence.Append(transform.DOScale(1f, 0f));
                sequence.Append(transform.DOScaleY(1.3f, 0.06f));
                sequence.Append(transform.DOScale(new Vector3(1.3f, 0.9f, 1), 0.06f));
                sequence.Append(transform.DOScale(1f, 0.06f));
                if (action != null)
                {
                    sequence.OnComplete(action.Invoke);
                }
            }
            return sequence;
        }
        /// <summary>
        /// 关闭Tips复用
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="action"></param>
        public static Sequence CloseTipsPool(Sequence sequence, Transform transform, Action action= null)
        {
            if (sequence != null)
            {
                sequence.Restart();
            }
            else
            {
                sequence = DOTween.Sequence();
                sequence.SetAutoKill(false);
                sequence.Append(transform.DOScale(1f, 0f));
                sequence.Append(transform.DOScale(new Vector3(1.39f, 0.6f, 1), 0.1f));
                if (action != null)
                {
                    sequence.OnComplete(action.Invoke);
                }
            }
            return sequence;
        }

        public static void ShowWidgetButton(RectTransform rect, bool isLeft)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(rect.DOAnchorPosX(35, 0.2f));
            sequence.Join(rect.DOScale(Vector3.one * 1.1443f,  0.2f));
            sequence.Append(rect.DOAnchorPosX(20f,  0.1f));
            sequence.Join(rect.DOScale(Vector3.one * 1.2f,  0.1f));
            sequence.Append(rect.DOScale(Vector3.one,  0.1f));
        }
        
        public static void HideWidgetButton(RectTransform rect, bool isLeft, GameObject obj)
        {
            var x = isLeft ? -434 : 300;
            rect.DOAnchorPosX(x, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                obj.SetActive(false);
            });
        }
        
        public static void Button(Sequence sequence, Transform rect, float scale)
        {
            if (sequence == null)
            {
                sequence = DOTween.Sequence();
                sequence.SetAutoKill(false);
                sequence.Append(rect.DOScale(scale * 1.1f, 0.15f));
                sequence.Append(rect.DOScale(scale * 0.9f, 0.08f));
                sequence.Append(rect.DOScale(scale * 1f, 0.08f));
            }
            else
            {
                sequence.Restart();
            }
        }

        public static void CurrencyAnim(Sequence sequence, Transform rect)
        {
            if (sequence == null)
            {
                sequence = DOTween.Sequence();
                sequence.SetAutoKill(false);
                sequence.Append(rect.DOScale(1.1f, 0.15f));
                sequence.Append(rect.DOScale(0.9f, 0.08f));
                sequence.Append(rect.DOScale(1f, 0.08f));
            }
            else
            {
                sequence.Restart();
            }
        }
        
        public static void OpenBuildMaskAnim(Sequence sequence, Transform rect)
        {
            if (sequence == null)
            {
                sequence = DOTween.Sequence();
                sequence.SetAutoKill(false);
                rect.transform.localScale = Vector3.zero;
                sequence.Append(rect.DOScale(1.1f, 0.8f));
                sequence.Append(rect.DOScale(0.9f, 0.1f));
                sequence.Append(rect.DOScale(1f, 0.1f));
            }
            else
            {
                sequence.Restart();
            }
        }
    }
}
