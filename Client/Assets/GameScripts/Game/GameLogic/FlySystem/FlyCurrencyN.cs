using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Y_Base;
using TK_Engine;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

namespace GameLogic
{
    //货币
    public class FlyCurrencyN : MonoBehaviour
    {
        public Transform AllCoins;
        private List<GameObject> pars = new ();
        public TextMeshProUGUI flyText;
        public ParticleSystem BombPar;
        private Vector3 _wPos;
        private Transform _target;
        
        private Action<Transform> _closeCallback;
        public float JinaGeTime = 0.1f;
        public float MoveDuration = 2f; // 完成整条曲线所需时间
        [Header("金币延迟出现时间")]
        public float delayTime = 0.3f; // 延迟时间
        [Header("文本缩放时间")]
        public float txtScaleTime = 0.2f;//文本缩放时间
        [Header("文本移动时间")]
        public float txtMoveTime = 0.2f;//文本移动时间
        public Transform[] MyPath;
        private Vector3 [] PathPos = new Vector3[4];
        int tiemrId;
        //Currencytype：0 金币 1 网赚
        private int _currencytype;
        public void Init(Transform endPos, Action<Transform> cloaseCallback,long count=10,bool showTxt =false,int Currencytype=0,float cashAmount=0)
        {
            _target = endPos;
            _wPos = endPos.position;
            _closeCallback = cloaseCallback;
            _currencytype = Currencytype;
            flyTxtAnim(showTxt,count,cashAmount);
            PathPos[3] = _wPos;
            BombPar.transform.position = endPos.position;
            //如果是钻石，得关闭按钮上的Animator组件
            if (Currencytype == 1)
            {
                  var animator=_target.GetComponent<Animator>();
                  if (animator != null)
                  {
                      animator.enabled = false;
                  }
            }
            tiemrId= GameModule.Timer.AddTimer(timer =>
            {
                Utility.Unity.StartCoroutine(StartMove2(10,Currencytype));
            }, delayTime);
        }
        private IEnumerator StartMove2(int count,int Currencytype)
        {
            int arrivedCount = 0;
            bool allArrived = false;
            var points = GeneratePoints();
            for (int i = 0; i < count; i++)
            {
                int index = i;
                GameObject obj = CreatCoinsOrDiamond(AllCoins,Currencytype);
                obj.transform.position = points[index];
                if (Currencytype == 0)
                {
                    obj.GetComponent<ParticleSystem>().Play();
                }
                bool isBomb = index % 2 == 0;
                EXDOBezier.DOBezier(obj.transform, GetCoinsPath(obj.transform.position), MoveDuration, (Tr) =>
                {
                    HideCoinOrDiamond(Tr.gameObject);;
                    //Tr.transform.position = _startCoinsPos[index];
                    if (isBomb)
                    {
                        BombPar.Play();
                        Sequence seq = DOTween.Sequence();
                        seq.Append( _target?.DOScale(1.05f, 0.07f));
                        seq.Append( _target?.DOScale(0.95f, 0.07f));
                        seq.Append( _target?.DOScale(1f, 0.07f));
                        SDKManager.GameShake(1);
                    }
                    arrivedCount++;
                    if (arrivedCount>=count) allArrived = true;
                }).SetEase(Ease.InSine);
                yield return new WaitForSeconds(JinaGeTime);
            }
            yield return new WaitUntil(() => allArrived);
            Close();
        }
        private Vector3[] GetCoinsPath(Vector3 startPos)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    PathPos[0] = startPos;
                }
                else
                {
                    PathPos[i] = MyPath[i].position+startPos;
                }
            }

            return PathPos;
        }
        private void flyTxtAnim(bool isShow, long count,float cashAmount=0,Action callback=null)
        {
            flyText.gameObject.SetActive(isShow);
            if (isShow)
            {
                if (_currencytype == 0)
                {
                    flyText.text = $"+{count}";
                }else if (_currencytype == 1)
                {
                    flyText.text =count>0?$"+R${Math.Round(count * 0.01f, 2)}":$"+R${cashAmount}";
                }
                flyText.transform.localScale = Vector3.zero;
                flyText.rectTransform.anchoredPosition = Vector2.zero;
                flyText.DOFade(0, 0f);
                Sequence seq = DOTween.Sequence();
                seq.Append(flyText.transform.DOScale(2f, txtScaleTime).SetEase(Ease.InBack));
                seq.Join(flyText.DOFade(1, txtScaleTime));
                seq.Append(flyText.rectTransform.DOAnchorPos3DY(120f, txtMoveTime).SetEase(Ease.InBack));
                seq.Append(flyText.DOFade(0, txtMoveTime));
            }
        }
        private void Close()
        {
            transform.localPosition = Vector3.one * 999;
            
            _closeCallback?.Invoke(_target);
            _closeCallback = null;
            FlySystem.Instance.HideObj(gameObject);
            GameModule.Timer.RemoveTimer(tiemrId);
        }

        private void OnDestroy()
        {
            if (_closeCallback != null)
            {
                _closeCallback?.Invoke(_target);
                _closeCallback = null;
            }
        }

        #region 生成随机金币或者钻石
        private GameObject CreatCoinsOrDiamond(Transform parent,int Currencytype)
        {
            GameObject newObj = null;
            if (pars.Count > 0)
            {
                newObj = pars[0].gameObject;
                pars.RemoveAt(0);
                newObj.transform.SetParent(parent);
                newObj.Show();
            }
            
            if (newObj == null)
            {
                string Currencyname = Currencytype == 0 ? "efc_main_goldcoin_02" : "efc_main_diamond";
                newObj = GameModule.Resource.LoadGameObject(Currencyname,parent);
            }
            return newObj;
        }

        private void HideCoinOrDiamond(GameObject obj)
        {
            obj.Hide();
            pars.Add(obj);
        }
        [Header("生成设置")]
        public Vector2 areaSize = new Vector2(1f, 1f); // 生成区域尺寸（X轴和Y轴）
        public int numberOfPoints = 10;                  // 需要生成的坐标点数量
        public float minDistance = 0.1f;                   // 点之间的最小间距
        public int maxAttempts = 100; // 每个点的最大尝试次数
        private List<Vector3> _generatedPoints = new List<Vector3>();
        private Vector3 GenerateRandomPoint()
        {
           Vector3 point= new Vector3(Random.Range(-areaSize.x / 2, areaSize.x / 2),
                Random.Range(-areaSize.y / 2, areaSize.y / 2), 0);
           return AllCoins.position + point;
        }

        private bool IsPointValid(Vector3 position)
        {
            float minSqrDistance = minDistance * minDistance;
            foreach (var point in _generatedPoints)
            {
                if ((position - point).sqrMagnitude < minSqrDistance)
                {
                    return true;
                }
            }
            return true;
        }

        public List<Vector3> GeneratePoints()
        {
            _generatedPoints.Clear();
            for (int i = 0; i < numberOfPoints; i++)
            {
                Vector3 newPoint;
                bool isValid = false;
                int attempts = 0;
                do
                {
                    newPoint = GenerateRandomPoint();
                    isValid = IsPointValid(newPoint);
                    attempts++;
                } while (!isValid && attempts < maxAttempts);
                if (isValid)
                {
                    _generatedPoints.Add(newPoint);
                }
            }
            return _generatedPoints;
            
        }
        #endregion
    }
}
