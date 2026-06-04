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
    public class UITweenPro : MonoBehaviour
    {
        [LabelText("动画列表")]public List<TweenData> tweenList;
        [LabelText("是否自动播放")]public bool isAutoPlay = true;
        [LabelText("循环类型")]public LoopType loopType = LoopType.Restart;
        [LabelText("循环次数")]public int loops = 1;
        private Sequence _sequence;
        private float _duration;

        private void Start()
        {
            PlayTween();
        }

        private void OnEnable()
        {
            _sequence.Play();
        }
        
        private void OnDisable()
        {
            _sequence.Pause();
        }
        
        private void OnDestroy()
        {
            _sequence.Kill();
        }
        
        public void PlayTween()
        {
            _sequence = DOTween.Sequence();
            foreach (var data in tweenList)
            {
                var obj = data.GetTween(gameObject);
                if (obj != null)
                {
                    if (data.isEaseCurve)
                    {
                        obj.SetEase(data.easeCurve);
                    }
                    else
                    {
                        obj.SetEase(data.ease);
                    }
                    
                    if (data.playType == PlayType.Append)
                    {
                        _duration += data.delay + data.duration;
                        _sequence.Append(obj);
                    }
                    else
                    {
                        if (_duration < data.delay + data.duration)
                        {
                            _duration = data.delay + data.duration;
                        }
                        _sequence.Join(obj);
                    }
                   
                }
            }

            _sequence.SetLoops(loops, loopType);
        }

#if UNITY_EDITOR
       
        [Button("播放一下", ButtonSizes.Large)]
        public void EditorPlay()
        {
            _duration = 0;
            PlayTween();
            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.EditorApplication.update += EditorUpdate;
        }
        [Button("停止", ButtonSizes.Large)]
        public void EditorStop()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            DG.DOTweenEditor.DOTweenEditorPreview.Stop(true);
        }
        
        public void EditorUpdate()
        {
            DG.DOTweenEditor.DOTweenEditorPreview.PrepareTweenForPreview(_sequence);
            DG.DOTweenEditor.DOTweenEditorPreview.Start();
        }
#endif

        
         public const string ShowIf3 = "@animationType == AnimationType.Fade ||" +  
                                         "animationType == AnimationType.PunchPosition ||" +
                                         "animationType == AnimationType.PunchRotation ||" +
                                         "animationType == AnimationType.PunchScale ||" +
                                         "animationType == AnimationType.ShakePosition ||" +
                                         "animationType == AnimationType.ShakeRotation ||" +
                                         "animationType == AnimationType.ShakeScale";
        
         public const string ShowIf4 =  "@animationType == AnimationType.PunchPosition ||" +
                                          "animationType == AnimationType.PunchRotation ||" +
                                          "animationType == AnimationType.PunchScale ||" +
                                          "animationType == AnimationType.ShakePosition ||" +
                                          "animationType == AnimationType.ShakeRotation ||" +
                                          "animationType == AnimationType.ShakeScale";
        
         public const string ShowIf1 = "@animationType == AnimationType.Color";

         public const string ShowIf2 = "@animationType == AnimationType.Move ||" +
                                         "animationType == AnimationType.LocalMove ||" +
                                         "animationType == AnimationType.LocalMoveX ||" +
                                         "animationType == AnimationType.Rotate ||" +
                                         "animationType == AnimationType.LocalRotate ||" +
                                         "animationType == AnimationType.Scale ||" +
                                         "animationType == AnimationType.PunchPosition ||" +
                                         "animationType == AnimationType.PunchRotation ||" +
                                         "animationType == AnimationType.PunchScale ||" +
                                         "animationType == AnimationType.ShakePosition ||" +
                                         "animationType == AnimationType.ShakeRotation ||" +
                                         "animationType == AnimationType.ShakeScale ||" +
                                         "animationType == AnimationType.AnchorPos";
        
         public const string ShowIf5 = "@isEaseCurve";
         public const string ShowIf6 = "@!isEaseCurve";
         public const string ShowIf7 = "@animationType == AnimationType.ToSizeDelta";
    }
    
    [Serializable]
    public class TweenData
    {
       // [LabelText("动画物体")]public GameObject gameObject;
        [LabelText("序列类型")]public PlayType playType;
        [LabelText("动画类型")]public AnimationType animationType;
        [LabelText("动画目标")]public ComponentType targetType;
        [LabelText("动画延迟")]public float delay;
        [LabelText("动画时长")]public float duration = 1;
        [LabelText("是否使用曲线")]public bool isEaseCurve = true;
        [ShowIf(UITweenPro.ShowIf5)]
        [LabelText("动画曲线")]public AnimationCurve easeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [ShowIf(UITweenPro.ShowIf6)]
        [LabelText("内置曲线")]public Ease ease;
        [ShowIf(UITweenPro.ShowIf1)]
        [LabelText("结束颜色")]public Color endColor;
        [ShowIf(UITweenPro.ShowIf2)]
        [LabelText("结束位置")]public Vector3 endPos;
        [ShowIf(UITweenPro.ShowIf3)]
        [LabelText("结束数值1")]public float endValue1;
        [ShowIf(UITweenPro.ShowIf4)]
        [LabelText("结束数值2")]public float endValue2;

        [ShowIf(UITweenPro.ShowIf7)] 
        [LabelText("结束值3")] public Vector2 endValue3;
        private Component _target;

        public Tween GetTween(GameObject obj)
        {
            GetComponent(obj);
            if (_target == null) return null;

            try
            {
                switch (animationType)
                {
                    case AnimationType.Move:
                        if (targetType == ComponentType.Transform)
                            return (_target as Transform).DOMove(endPos, duration).SetDelay(delay);
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DOMove(endPos, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.LocalMoveX:
                        if (targetType == ComponentType.Transform)
                            return (_target as Transform).DOLocalMoveX(endPos.x, duration) .SetDelay(delay);
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DOLocalMoveX(endPos.x, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.LocalMove:
                        if (targetType == ComponentType.Transform)
                            return (_target as Transform).DOLocalMove(endPos, duration) .SetDelay(delay);
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DOLocalMove(endPos, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.Rotate:
                        if (targetType == ComponentType.Transform)
                            return (_target as Transform).DORotate(endPos, duration) .SetDelay(delay);
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DORotate(endPos, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.LocalRotate:
                        if (targetType == ComponentType.Transform)
                            return (_target as Transform).DOLocalRotate(endPos, duration) .SetDelay(delay);
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DOLocalRotate(endPos, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.AnchorPos:
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DOAnchorPos(endPos, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.Scale:
                        if (targetType == ComponentType.Transform)
                            return (_target as Transform).DOScale(endPos, duration) .SetDelay(delay);
                        if (targetType == ComponentType.RectTransform)
                            return (_target as RectTransform).DOScale(endPos, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.Color:
                        if (targetType == ComponentType.SpriteRenderer)
                            return (_target as SpriteRenderer).DOColor(endColor, duration) .SetDelay(delay);
                        if (targetType == ComponentType.Image)
                            return (_target as Image).DOColor(endColor, duration) .SetDelay(delay);
                        if (targetType == ComponentType.TextMeshProUGUI)
                            return (_target as Text).DOColor(endColor, duration) .SetDelay(delay);
                        return null;
                    case AnimationType.Fade:
                        if (targetType == ComponentType.SpriteRenderer)
                            return (_target as SpriteRenderer).DOFade(endValue1, duration) .SetDelay(delay);
                        if (targetType == ComponentType.Image)
                            return (_target as Image).DOFade(endValue1, duration) .SetDelay(delay);
                        if (targetType == ComponentType.TextMeshProUGUI)
                            return (_target as Text).DOFade(endValue1, duration)  .SetDelay(delay);
                        return null;
                    case AnimationType.PunchPosition:
                        return (_target as RectTransform).DOPunchPosition(endPos, duration, (int)endValue1, endValue2)
                             .SetDelay(delay);
                    case AnimationType.PunchRotation:
                        return (_target as RectTransform).DOPunchRotation(endPos, duration, (int)endValue1, endValue2)
                             .SetDelay(delay);
                    case AnimationType.PunchScale:
                        return (_target as RectTransform).DOPunchScale(endPos, duration, (int)endValue1, endValue2)
                             .SetDelay(delay);
                    case AnimationType.ShakePosition:
                        return (_target as RectTransform).DOShakePosition(duration, endPos, (int)endValue1, endValue2)
                             .SetDelay(delay);
                    case AnimationType.ShakeRotation:
                        return (_target as RectTransform).DOShakeRotation(duration, endPos, (int)endValue1, endValue2)
                             .SetDelay(delay);
                    case AnimationType.ShakeScale:
                        return (_target as RectTransform).DOShakeScale(duration, endPos, (int)endValue1, endValue2)
                             .SetDelay(delay);
                    case AnimationType.ToSizeDelta:
                        RectTransform rect = _target as RectTransform;
                        return DOTween.To(() => rect.sizeDelta, sizeDelta => rect.sizeDelta = sizeDelta,endValue3 , duration);

                }
            }
            catch (Exception e)
            {
                Debug.LogError(animationType + "播放失败,请检查组件是否存在");
            }
            return null;
        }
        public void GetComponent(GameObject obj)
        {
            switch (targetType)
            {
                case ComponentType.Transform:
                    _target = obj.GetComponent<Transform>();
                    break;
                case ComponentType.CanvasGroup:
                    _target = obj.GetComponent<CanvasGroup>();
                    break;
                case ComponentType.Image:
                    _target = obj.GetComponent<Image>();
                    break;
                case ComponentType.RectTransform:
                    _target = obj.GetComponent<RectTransform>();
                    break;
                case ComponentType.SpriteRenderer:
                    _target = obj.GetComponent<SpriteRenderer>();
                    break;
                case ComponentType.TextMeshProUGUI:
                    _target = obj.GetComponent<Text>();
                    break;
            }
        }

    }
    
    public enum PlayType
    {
        Append,
        Join,
    }
    
    public enum AnimationType
    {
        None,
        Move, 
        LocalMove, 
        Rotate, 
        LocalRotate,
        Scale,
        Color, 
        Fade,
        PunchPosition, PunchRotation, PunchScale,
        ShakePosition, ShakeRotation, ShakeScale,
        LocalMoveX,
        AnchorPos,
        ToSizeDelta,
    }

    public enum ComponentType
    {
        Transform,
        CanvasGroup,
        Image,
        RectTransform,
        SpriteRenderer,
        TextMeshProUGUI
    }
}
