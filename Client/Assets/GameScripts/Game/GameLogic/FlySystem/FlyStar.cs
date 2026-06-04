using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using GameBase;
using DG.Tweening;
using Sirenix.OdinInspector;
using TK_Engine;
using TMPro;

namespace GameLogic
{
    public class  FlyStar : MonoBehaviour
    {
        public bool IsNormalFly = false;
        [ShowIf("IsNormalFly")]
        public float SpeedC = 0.7f;
        [ShowIf("IsNormalFly")]
        public float SpeedAddDeltaC = 0.1f;
        [ShowIf("IsNormalFly")]
        public bool IsMove = false;
        private float _percent = 0;//贝塞尔曲线插值参数
        private float _speedAdd = 0;//速度加成
        private float _speedAddDelta;//最大速度加成
        private float _speed;
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private Vector3 _midPos;//贝塞尔曲线中间点

       
        public RectTransform MyRectTransform;
        [LabelText("是否开启粒子特效")]
        public bool IsOpenParticle = false;
        [ShowIf("IsOpenParticle")]
        public ParticleSystem par;
        private ParticleSystem.MainModule mainModule;
        private int _timerId;
        public bool IsOpenTxt = false;
        [ShowIf("IsOpenTxt")]
        public TextMeshProUGUI flyText;
        private Transform _targetTr;
        private List<Transform> _targetList;
        public Vector2 TargetPos
        {
            get=> _targetPos;
            set=> _targetPos = value;
        }
        public Action<Transform> OnMoveEnd;
        public Action<List<Transform>> OnMoveEnd2;
        public bool IsDGFly = false;
        [ShowIf("IsDGFly")]
        public float BezierTime = 0.5f;
        [ShowIf("IsDGFly")]
        public bool BezierToScale = true;
        [ShowIf("IsDGFly")]
        public bool IsEndBombPar = false;
        private void Start()
        {
           
        }
        #region Update移动

        public void Fly(Vector3 startPos,Transform target,Vector3 midPos, Action<Transform> callback)
        {
            if (!IsMove)
            {
                _targetTr = target;
                _speedAdd = 0;
                OnMoveEnd = callback;
                _speed = SpeedC;
                _speedAddDelta = SpeedAddDeltaC;
            }
            
            _targetPos = target.position;
            _speed += _speedAdd;
            _percent = 0;
            _speedAdd = 0;
            _startPos = startPos;
            _midPos = midPos;
            IsMove = true;
        }

        private void Update()
        {
            if (IsMove)
            {
                _speedAdd += Time.unscaledDeltaTime * _speedAddDelta;
                _percent += _speed * Time.deltaTime;
                Vector3 pos = UnityExtensions.Bezier(_percent + _speedAdd, _startPos, _midPos, _targetPos);
                //pos.z = 0;
                MyRectTransform.transform.position = pos;
                if (Vector2.Distance(transform.position, _targetPos) <= 0.5f)
                {
                    IsMove = false;
                    OnMoveEnd?.Invoke(_targetTr);
                    FlySystem.Instance.HideObj(gameObject);
                }
            }
        }

        #endregion
        #region DG移动
        private ParticleSystem[] allpar;
        public void FlyDG(Vector3 startPos,Vector3 midPos,Transform targetPos,Action<Transform> callback, bool IsBezier =false,float delay =0,int count = 0)
        {
            _targetTr = targetPos;
            _targetPos = targetPos.position;
            OnMoveEnd = callback;
            if (allpar == null)
            {
                allpar = transform.GetComponentsInChildren<ParticleSystem>();
            }
            
            foreach (var par in allpar)
            {
                par.Play();
            }
            //是否延迟执行
            if (delay > 0)
            {
                _timerId = GameModule.Timer.AddTimer(timer =>
                {
                    if (!IsBezier)
                    {
                        MovePlay(_targetTr.position);
                    }
                    else
                    {
                        BezierMovePlay(startPos, midPos, _targetTr.position);
                    }
                }, delay);
            }
            else
            {
                if (!IsBezier)
                {
                    MovePlay(_targetTr.position);
                }
                else
                {
                    BezierMovePlay(startPos, midPos, _targetTr.position);
                }
            }
        }

        private void MovePlay(Vector3 endTr)
        {
            var moveAnim = DOTween.Sequence();
            moveAnim.Append(MyRectTransform.transform.DOScale(1.2f, 0.15f));
            moveAnim.Append(MyRectTransform.transform.DOMove(_targetPos, 0.5f));
            moveAnim.Append(MyRectTransform.transform.DOScale(0.2f, 0.15f));
            moveAnim.OnComplete(() =>
            {
                if (IsEndBombPar)
                {
                    GameObject endEff =  FlySystem.Instance.ShowEff("prefab_button_loading_01", transform.parent, _targetTr,
                        Vector3.one*40f);
                    ShowPropVfx endVfx = endEff.GetComponent<ShowPropVfx>();
                    endVfx.PlayPropVfx(true);
                }
                OnMoveEnd?.Invoke(_targetTr);
                OnMoveEnd = null;
                if (_timerId != 0)
                {
                    GameModule.Timer.RemoveTimer(_timerId);
                    _timerId = 0;
                }
                FlySystem.Instance.HideObj(gameObject);
                SDKManager.GameShake(1);
            });
        }

        private void BezierMovePlay(Vector3 startPos, Vector3 midPos, Vector3 endPos)
        {
            Vector3[] path = new Vector3[4];
            path[0] = startPos;
            path[1] = midPos;
            path[2] = endPos;
            path[3] = endPos;
            var moveAnim = DOTween.Sequence();
            moveAnim.Append(EXDOBezier.DOBezier(MyRectTransform.transform, path, BezierTime, (Tr) =>
            {
                if (IsEndBombPar)
                {
                    GameObject endEff =  FlySystem.Instance.ShowEff("prefab_button_loading_01", transform.parent, _targetTr,
                        Vector3.one*35f);
                    ShowPropVfx endVfx = endEff.GetComponent<ShowPropVfx>();
                    endVfx.PlayPropVfx(true);
                }
                OnMoveEnd?.Invoke(_targetTr);
                OnMoveEnd = null;
                if (_timerId != 0)
                {
                    GameModule.Timer.RemoveTimer(_timerId);
                    _timerId = 0;
                }
                FlySystem.Instance.HideObj(gameObject);
                SDKManager.GameShake(1);
            }));
            if (BezierToScale)
            {
                moveAnim.Join(MyRectTransform.transform.DOScale(0.8f, 0.3f));
            }
           
        }
        #endregion
        #region 粒子特效
        /// <summary>
        /// 播放粒子特效
        /// </summary>
        public void PlayParticle(Action<List<Transform>> callback,List<Transform> targetList=null)
        {
            if (IsOpenParticle)
            {
                mainModule = par.main;
                mainModule.stopAction = ParticleSystemStopAction.Callback;
                OnMoveEnd2 = callback;
                _targetList = targetList;
                par.Play();
            }
        }
        void OnParticleSystemStopped()
        {
            OnMoveEnd2?.Invoke(_targetList);
            OnMoveEnd = null;
            if (_timerId != 0)
            {
                GameModule.Timer.RemoveTimer(_timerId);
                _timerId = 0;
            }
            FlySystem.Instance.HideObj(gameObject);
        }
        #endregion

        #region 文字动画
        public void flyTxtAnim(bool isShow, int count)
        {
            flyText.gameObject.SetActive(isShow);
            if (isShow)
            {
                flyText.text = $"+{count}";
                flyText.transform.localScale = Vector3.zero;
                flyText.DOFade(0, 0f);
                Sequence seq = DOTween.Sequence();
                seq.Append(flyText.transform.DOScale(2f, 0.3f).SetEase(Ease.OutBack));
                seq.Join(flyText.DOFade(1, 0.3f));
                seq.Append(flyText.rectTransform.DOAnchorPos3DY(100f, 0.3f));
                seq.Append(flyText.DOFade(0, 0.2f));
            }
        }
        

        #endregion
    }
}
