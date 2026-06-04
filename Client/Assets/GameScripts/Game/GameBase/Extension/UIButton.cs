using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AudioType = TK_Engine.AudioType;
using Object = UnityEngine.Object;
using TK_Engine;
using Coffee.UIExtensions;


public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool isPlaySound = true;
    public bool isPlayAnim = true;
    public string soundName;
    public UIParticle effectTarget;
    [HideInInspector]
    public Image thisImager;
    private Vector3 _scale;
    private Sequence _sequence;
    private bool _thisEnabled = true;
    private Action<GameObject> _listener;
    private Action<GameObject, object> _listener2;
    //按钮禁用提示
    private Action _listener3;
    //按下事件监听
    private Action<GameObject> _pointerDownListener;
    //抬起事件监听
    private Action<GameObject> _pointerUpListener;
    private object _data;
    public CanvasGroup CurentCanvasGroup;
    private RectTransform _rectTr;
    private bool _isProcessingClick = false;


    public RectTransform RectTr
    {
        get
        {
            if (_rectTr == null)
            {
                _rectTr = GetComponent<RectTransform>();
            }
            return _rectTr;
        }
    }
    public bool ButtonEnabled
    {
        set
        {
            if (_thisEnabled == value) return;
            _thisEnabled = value;
            SetGray();
        }
    }

    private void Awake()
    {
        _scale = transform.localScale;
        thisImager = gameObject.GetComponent<Image>();
    }

    /// <summary>
    /// 添加一个监听
    /// </summary>
    /// <param name="msgListener">点击回调</param>
    /// <param name="pointerDownListener">可选的按下回调，用于在按下瞬间触发（比如设置道具状态阻止其他交互）</param>
    public void AddListener(Action<GameObject> msgListener, Action<GameObject> pointerDownListener = null)
    {
        _listener = msgListener;
        if (pointerDownListener != null)
        {
            _pointerDownListener = pointerDownListener;
        }
    }
    /// <summary>
    /// 添加一个监听
    /// </summary>
    public void AddListener(object param, Action<GameObject, object> msgListener)
    {
        _data = param;
        _listener2 = msgListener;
    }

    public void AddListener(Action listener)
    {
        _listener3 = listener;
    }

    /// <summary>
    /// 添加按下事件监听
    /// </summary>
    public void AddPointerDownListener(Action<GameObject> msgListener)
    {
        _pointerDownListener = msgListener;
    }

    /// <summary>
    /// 添加抬起事件监听
    /// </summary>
    public void AddPointerUpListener(Action<GameObject> msgListener)
    {
        _pointerUpListener = msgListener;
    }

    /// <summary>
    /// 清空所有监听
    /// </summary>
    public void RemoveListener()
    {
        _listener = null;
        _listener2 = null;
        _listener3 = null;
        _pointerDownListener = null;
        _pointerUpListener = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isProcessingClick) return;
        if (GameModule.UI.IsButtonLock) return;
        GameModule.UI.IsButtonLock = true;
        _isProcessingClick = true;
        if (!_thisEnabled)
        {
            _listener3?.Invoke();
            return;
        }
        PlaySound();
        PlayAnim();
        if (isPlayAnim)
        {
            Invoke("PlayEvent", 0.1f);
        }
        else
        {
            PlayEvent();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_thisEnabled) return;
        _pointerDownListener?.Invoke(this.gameObject);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_thisEnabled) return;
        _pointerUpListener?.Invoke(this.gameObject);
    }
    public void PlayEvent()
    {
        if (gameObject != null)
        {
            try
            {
                if (_listener != null)
                {
                    _listener.Invoke(this.gameObject);
                }
                else if (_listener2 != null)
                {
                    _listener2.Invoke(this.gameObject, _data);
                }

            }
            finally
            {
                ResetClickState();
            }
        }
    }

    private void ResetClickState()
    {
        _isProcessingClick = false;
        if (GameModule.GameModuleRoot != null && GameModule.UI != null)
        {
            GameModule.UI.IsButtonLock = false;
        }
    }
    private void PlayAnim()
    {
        if (!isPlayAnim)
        {
            return;
        }
        if (_sequence == null)
        {
            _sequence = DOTween.Sequence();
            _sequence.SetAutoKill(false);
            _sequence.Append(transform.DOScale(_scale * 1.1f, 0.15f));
            _sequence.Append(transform.DOScale(_scale * 0.9f, 0.08f));
            _sequence.Append(transform.DOScale(_scale * 1f, 0.08f));
        }
        else
        {
            _sequence.Restart();
        }

    }

    private void OnDestroy()
    {
        _sequence.Kill();
        ResetClickState();
    }

    private string _defaultName = "sound_btnclick";
    private void PlaySound()
    {
        if (isPlaySound)
        {
            if (!string.IsNullOrEmpty(soundName))
                _defaultName = soundName;
            GameModule.Audio.Play(AudioType.Sound, _defaultName);
        }
    }

    private Material _defaultMaterial;
    private void SetGray()
    {
        if (CurentCanvasGroup == null) return;
        if (!_thisEnabled)
        {
            //_defaultMaterial = thisImager.material;
            //thisImager.material = AssetManager.Instance.GrayMaterial;
            CurentCanvasGroup.alpha = 0.5f;
        }
        else
        {
            //thisImager.material = _defaultMaterial;
            CurentCanvasGroup.alpha = 1f;
        }
    }
    //播放按钮特效
    public void PlayEffect()
    {
        if (effectTarget != null)
        {
            effectTarget.Play();
        }
    }
}
