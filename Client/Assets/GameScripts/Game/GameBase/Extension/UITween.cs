using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
//using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace GameBase
{
    public class UITween : MonoBehaviour
    {
        private Tween _tween;
        public TweenType tweenType;
        public TargetType targetType;
        [LabelText("动画曲线")] public Ease ease;
        [LabelText("循环模式")] public LoopType loopType = LoopType.Yoyo;
        [LabelText("持续时间")]public float duration;
        
        [ShowIf(_mShowIf2)]
        [LabelText("结束颜色")]public Color endColor;
        
        [ShowIf(_mShowIf1)]
        [LabelText("结束位置")]public Vector3 endPos;

#if UNITY_EDITOR
       
        [Button("播放一下", ButtonSizes.Large)]
        public void EditorPlay()
        {
            _time = 0;
            _tween = PlayTween();
            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.EditorApplication.update += EditorUpdate;
        }

        private float _time;
        public void EditorUpdate()
        {
            DG.DOTweenEditor.DOTweenEditorPreview.PrepareTweenForPreview(_tween);
            DG.DOTweenEditor.DOTweenEditorPreview.Start();
            _time += Time.deltaTime;
            if (_time >= duration * 5)
            {
                DG.DOTweenEditor.DOTweenEditorPreview.Stop(true);
                UnityEditor.EditorApplication.update -= EditorUpdate;
                _time = 0;
            }
        }
#endif
        private void Start()
        {
            _tween = PlayTween();
        }

        private Tween PlayTween()
        {
            switch (targetType)
            {
                case TargetType.Image:
                    if (tweenType == TweenType.DoColor)
                    {
                        Image image = GetComponent<Image>();
                        return DoColor(image);
                    }
                    break;
                case TargetType.Text:
                    if (tweenType == TweenType.DoColor)
                    {
                        Text text = GetComponent<Text>();
                        return DoColor(text);
                    }
                    break;
                case TargetType.Transform:
                    switch (tweenType)
                    {
                        case TweenType.DOScale:
                            return DOScale(transform);
                        case TweenType.DOLocalMove:
                            return DOLocalMove(transform);
                        case TweenType.DOMove:
                            return DOMove(transform);
                    }
                    break;
                case TargetType.RectTransform:
                    switch (tweenType)
                    {
                        case TweenType.DOAnchorPos:
                            return DOAnchorPos(transform);
                    }
                    break;
            }

            return null;
        }
        
        private Tween DoColor(Image image)
        {
            return image.DOColor(endColor, duration).SetEase(ease).SetLoops(-1, loopType);
        }
        
        private Tween DoColor(Text text)
        {
            return text.DOColor(endColor, duration).SetEase(ease).SetLoops(-1, loopType);
        }
        
        private Tween DOScale(Transform trans)
        {
            return trans.DOScale(endPos, duration).SetEase(ease).SetLoops(-1, loopType);
        } 
        
        private Tween DOLocalMove(Transform trans)
        {
            return trans.DOLocalMove(endPos, duration).SetEase(ease).SetLoops(-1, loopType);
        } 
        
        private Tween DOMove(Transform trans)
        {
            return trans.DOMove(endPos, duration).SetEase(ease).SetLoops(-1, loopType);
        } 
        private Tween DOAnchorPos(Transform trans)
        {
            var rect = trans.GetComponent<RectTransform>();
            return rect.DOAnchorPos(endPos, duration).SetEase(ease).SetLoops(-1, loopType);
        }

        private void OnDestroy()
        {
            _tween.Kill();
        }


        private const string _mShowIf1 = "@(this.tweenType == TweenType.DOMove || " +
                                         "this.tweenType == TweenType.DOLocalMove ||" +
                                         "this.tweenType == TweenType.DOScale) && " +
                                         "this.targetType == TargetType.Transform || " +
                                         "this.targetType == TargetType.RectTransform";
        private const string _mShowIf2 = "@this.tweenType == TweenType.DoColor  && " +
                                "(this.targetType == TargetType.Image || this.targetType == TargetType.Text)";
    }

    public enum TweenType
    {
        DoColor,
        DOScale,
        DOLocalMove,
        DOMove,
        DOAnchorPos
    }
    
    public enum TargetType
    {
        Image,
        Text,
        Transform,
        RectTransform,
    }
}
