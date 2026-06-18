using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using Y_Base;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AudioType = TK_Engine.AudioType;
using Object = UnityEngine.Object;
using TK_Engine;


public class UIButton2 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,IPointerClickHandler
{
    private Vector3 _scale;
    private Action<GameObject> _listener;
    private Action<GameObject> _2listener;
    
    //不可交互
    private bool _disEnable;

    public bool ButtondisEnabled
    {
        get => _disEnable;
        set
        {
            _disEnable = value;
            SetLock();
        }
    }
    private void Awake()
    {
        _scale = transform.localScale;
        /*if (IsOpenTab)
        {
            _tabGroup = transform.GetComponentInParent<TabGroup>();
        }
        if (_tabGroup != null)
        {
            _tabGroup.Subscribe(this);
        }
        */

        if (SelectNode != null)
        {
            SelectNode.Hide();
        }
        
    }
    /// <summary>
    /// 添加一个监听
    /// </summary>
    public void AddListener(Action<GameObject> msgListener)
    {
        _listener = msgListener;
    }

    public void AddSecondListener(Action<GameObject> msgListener)
    {
        _2listener = msgListener;
    }
    private float startTime = 0.25f;
    private float intervalTime = 0.15f;
    
    private float startTimer;
    private float intervalTimer = 0.15f;
    private bool _isDown;
    private bool _isUpdateDown;
    private string _defaultName = "sound_btnclick";
    public bool IsOpenTab;
    [FieldName("选中的节点")]public GameObject SelectNode;
    [FieldName("锁住的节点")]public GameObject LockNode;
    [FieldName("按钮icon")] public RectTransform icon;
    public void OnPointerDown(PointerEventData eventData)
    {
        /*_isDown = true;
        GameModule.Audio.Play(AudioType.UISound, _defaultName);*/
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        /*if (_isDown)
        {
            if (!_isUpdateDown)
            {
                _listener?.Invoke(gameObject);
                PlayAnim();
            }

            Rest();
        }*/
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if(_disEnable) return;
        GameModule.Audio.Play(AudioType.UISound, _defaultName);
        PlayAnim();
        Invoke("PlayEvent",0.1f);
    }

    public void PlayEvent()
    {
        //_tabGroup.OnTabSelected(this);
    }
    public void Select()
    {
        _listener?.Invoke(gameObject);
        //设置选中状态
        if (SelectNode != null)
        {
            SelectNode.Show();
        }
    }

    public void SelectUI()
    {
        
    }
    public void Deselect()
    {
        _2listener?.Invoke(gameObject);
        //取消掉选中状态
        if (SelectNode != null)
        {
            SelectNode.Hide();
            if (icon != null)
            {
                icon.DOAnchorPos(Vector2.zero, 0.2f).SetEase(Ease.InOutElastic);
            }
            
        }
    }

    public void DeselectUI()
    {
        
    }
    public void Rest()
    {
        _isDown = false;
        _isUpdateDown = false;
        startTimer = 0;
        intervalTimer = intervalTime;
    }

    private void SetLock()
    {
        if (_disEnable)
        {
            LockNode.Show();
        }
        else
        {
            LockNode.Hide();
        }
    }
    private void Start()
    {
        
    }
    

    private void Update()
    {
        /*if (_isDown)
        {
            startTimer += Time.deltaTime;
            if (startTimer >= startTime)
            {
                _isUpdateDown = true;
                intervalTimer += Time.deltaTime;
                if (intervalTimer >= intervalTime)
                {
                    PlayAnim();
                    _listener?.Invoke(gameObject);
                    intervalTimer = 0;
                }
            }
        }*/
    }

    private Sequence _sequence;
    private void PlayAnim()
    {
        if (_sequence == null)
        {
            _sequence = DOTween.Sequence();
            _sequence.SetAutoKill(false);
            if (icon != null)
            {
                _sequence.Append(icon.DOAnchorPos(new Vector2(0, 30f), 0.2f).SetEase(Ease.InOutElastic));
            }
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
    }


}
