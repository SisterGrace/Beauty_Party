using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameBase;
using TK_Engine;
using Config.common;
using DG.Tweening;
using AudioType = TK_Engine.AudioType;
using Random = UnityEngine.Random;

namespace GameLogic
{
    public class FlySystem : SingletonBehaviour<FlySystem>
    {
        #region 道具奖励

        public void GetFlyProp(Transform parent, List<OneRewardData> props, Vector3 startPos,Transform endTr,
            Action<Transform> callback)
        {
            Utility.Unity.StartCoroutine(GetFlyProps(parent, props, startPos,endTr, callback));
        }

        public IEnumerator GetFlyProps(Transform parent, List<OneRewardData> props, Vector3 startPos,Transform endTr,
            Action<Transform> callback)
        {
            int arrivedCount = 0;
            bool allArrived = false;
            if (props != null && props.Count > 1)
            {
                float radius = 100;
                if (props.Count >= 4 && props.Count < 6)
                {
                    radius = 150;
                }else if (props.Count >= 6 && props.Count <= 8)
                {
                    radius = 180;
                }
                if(endTr==null) endTr = GetPos(FlyReType.prop);
                for (int i = 0; i < props.Count; i++)
                {
                    AudioManager.Play(AudioType.Sound,AudioDef.UI_gainprops);
                    var item = AddObj("prefab_FlyProp", parent);
                    var cs = item.GetComponent<FlyProp>();
                    float angle = i * Mathf.PI * 2 / props.Count + Mathf.PI;
                    Vector2 startUIPos = new Vector2(Mathf.Cos(angle),
                        Mathf.Sin(angle));
                    cs.Init(true,startPos,startUIPos,radius,props[i],endTr, (Tr) =>
                    {
                        arrivedCount++;
                        if(arrivedCount>=props.Count) allArrived = true;
                        AudioManager.Play(AudioType.Sound,AudioDef.UI_propfly);
                        SDKManager.GameShake(1,1);
                    });
                    yield return new WaitForSeconds(0.2f);
                }
                yield return new WaitUntil(() => allArrived);
                callback?.Invoke(endTr);
            }
            else
            {
                GetFlyProp(parent, startPos, props[0], endTr, callback);
            }
        }

        public void GetFlyProp(Transform parent, Vector3 startPos,OneRewardData prop, Transform endTr, Action<Transform> callback)
        {
            if(endTr==null) endTr = GetPos(FlyReType.prop);
            if (prop.flyType != itemFlyType.EachFly)
            {
                AudioManager.Play(AudioType.Sound,AudioDef.UI_gainprops);
                var item = AddObj("prefab_FlyProp", parent);
                var cs = item.GetComponent<FlyProp>();
                cs.Init(false,startPos,Vector2.zero,0, prop, endTr, (Tr) =>
                {
                    callback?.Invoke(Tr);
                    AudioManager.Play(AudioType.Sound,AudioDef.UI_propfly);
                    SDKManager.GameShake(1,1);
                });
            }
            else
            {
                Utility.Unity.StartCoroutine(GetEachFlyProp(parent, startPos, prop, endTr, callback));
            }
        }
        private IEnumerator GetEachFlyProp(Transform parent, Vector3 startPos, OneRewardData prop, Transform endTr,
            Action<Transform> callback)
        {
            int arrivedCount = 0;
            bool allArrived = false;
            for (int i = 0; i < prop.Count; i++)
            {
                AudioManager.Play(AudioType.Sound,AudioDef.UI_gainprops);
                var item = AddObj("prefab_FlyProp", parent);
                var cs = item.GetComponent<FlyProp>();
                float angle = i * Mathf.PI * 2 / prop.Count + Mathf.PI;
                Vector2 startUIPos = new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle));
                cs.Init(true,startPos,startUIPos,prop.Count>1?120f:0,prop,endTr, (Tr) =>
                {
                    arrivedCount++;
                    if (arrivedCount >= prop.Count) allArrived = true;
                    AudioManager.Play(AudioType.Sound,AudioDef.UI_propfly);
                    //关卡内道具购买震动
                    if (prop.Type == ERewardType.Prop)
                    {
                        SDKManager.GameShake(1,1);
                    }
                });
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitUntil(() => allArrived);
            callback?.Invoke(endTr);
        }
        //获取道具，不用飞向目标位置(此时道具数据已经写入用户)
        public void GetFlyPropNoTarget(Transform parent, List<OneRewardData> props, List<Vector3> startPos,Action callback)
        {
            if (props != null && startPos.Count == props.Count)
            {
                for (int i = 0; i < props.Count; i++)
                {
                    int index = i;
                    var item = AddObj("prefab_FlyProp", parent);
                    var cs = item.GetComponent<FlyProp>();
                    cs.Init(false,startPos[index],Vector2.zero,0, props[index],null,index==0? (Tr) =>
                    {
                        callback?.Invoke();
                    }:null );
                }
            }
        }
        //（局内）获取道具，飞向不同的目标位置
        public void GameGetFlyProps(List<OneRewardData> props, Vector3 startPos, Action<Transform,OneRewardData> oneCallback,
            Action endCallback)
        {
            Utility.Unity.StartCoroutine(GetFlyProps(props, startPos, oneCallback, endCallback));
        }
        private IEnumerator GetFlyProps(List<OneRewardData> props,Vector3 startPos,Action<Transform,OneRewardData> oneCallback,Action endCallback)
        {
            Transform parent = GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName).transform;
            int arrivedCount = 0;
            bool allArrived = false;
            for (int i = 0; i < props.Count; i++)
            {
                AudioManager.Play(AudioType.Sound,AudioDef.UI_gainprops);
                Transform endTr = GetPos((FlyReType)(props[i].Id + 4));
                var item = AddObj("prefab_FlyProp", parent);
                var cs = item.GetComponent<FlyProp>();
                float angle = i * Mathf.PI * 2 / props.Count + Mathf.PI;
                Vector2 startUIPos = new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle));
                cs.Init(true,startPos,startUIPos,120f,props[i],endTr, null,(Tr,pt) =>
                {
                    arrivedCount++;
                    if (arrivedCount >= props.Count) allArrived = true;
                    AudioManager.Play(AudioType.Sound,AudioDef.UI_propfly);
                    oneCallback?.Invoke(Tr,pt);
                });
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitUntil(() => allArrived);
            endCallback?.Invoke();
        }
        #endregion
        #region 货币奖励
        private Dictionary<FlyReType, Transform> _posDic = new Dictionary<FlyReType, Transform>();

        //设置飞行物品的位置
        public void RegisterPos(FlyReType type, Transform pos)
        {
            _posDic[type] = pos;
        }
        /// <summary>
        /// 注销位置
        /// </summary>
        /// <param name="type"></param>
        public void UnRegisterPos(FlyReType type)
        {
            _posDic.Remove(type);
        }
        public Transform GetPos(FlyReType type)
        {
            return _posDic[type];
        }

        public void GetFlyCurrencyN(ERewardType type, int id, itemFlyType flyType,Vector3 start, long count, Action<Transform> callback)
        {
            var endTrans = GetEndTrans(type, id, out var iconName);
            UIWindow parent = GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName);
            if(start==Vector3.zero) start = parent.transform.position;
            if (flyType!=itemFlyType.AddTimeFly&&id==0)//加金币
            {
                AudioManager.Play(AudioType.Sound,AudioDef.UI_gaincoins);
                var item = AddObj("prefab_FlyCurrency", parent.transform);
                var cs = item.GetComponent<FlyCurrencyN>();
                item.transform.position = start;
                cs.Init(endTrans,callback,count,id==0,0);
            }
            else if (id==1)//给无限生命加时间
            {
                OneRewardData data = new OneRewardData(type, id, count, flyType);
                GetFlyProp(parent.transform, start,data,endTrans,callback);
            }
        }

        public Transform GetEndTrans(ERewardType type, int id, out string iconName)
        {
            if (type == ERewardType.Currency)
            {
                switch (id)
                {
                    case 0://金币
                        iconName = "Eff_gold";
                        //如果有商店的金币位置，就使用商店的金币位置，否则使用默认的金币位置
                        if (_posDic.ContainsKey(FlyReType.ShopGold))
                        {
                            return _posDic[FlyReType.ShopGold];
                        }

                        if (_posDic.ContainsKey(FlyReType.FailGold))
                        {
                            return _posDic[FlyReType.FailGold];
                        }
                        return _posDic[FlyReType.Gold];
                    case 1://生命
                        iconName = "Eff_life";;
                        return _posDic[FlyReType.Life];
                }
            }
            iconName = "Eff_gold";
            return _posDic[FlyReType.Gold];
        }
        #endregion

        #region 改造特效
        /// <summary>
        /// 改造飞行星星
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="count">改造星星消耗数量</param>
        /// <param name="startPos">生成星星位置</param>
        /// <param name="targetPos">星星移动目标位置</param>
        /// <param name="callback1">更新星星消耗数量的回调</param>
        /// <param name="callback2">更新改造按钮样式的回调</param>
        /// <param name="callback3">开始改造的回调</param>
        /// <returns></returns>
        public IEnumerator GetFlyStar(Transform parent,int count,Vector3 startPos,Transform targetPos,Action callback1,Action<Transform> callback2,Action<Transform> callback3)
        {
            Vector3 midPos = GetMiddlePosition(startPos, targetPos.position);
            for (int i = 0; i < count; i++)
            {
                var stat = AddObj("prefab_TaskStart", parent,startPos);
                var flyStar = stat.GetComponent<FlyStar>();
                //flyStar.transform.position= startPos;
                if (i < count - 1)
                {
                    flyStar.Fly(startPos,targetPos,midPos,callback2);
                }
                else//最后一个星星改造完成传入callback3回调
                {
                    flyStar.Fly(startPos,targetPos,midPos,callback3);
                }
                
                callback1?.Invoke();
                yield return new WaitForSeconds(0.3f);
            }
        }
        //计算中间点
        private Vector3 GetMiddlePosition(Vector3 a, Vector3 b,bool isLeft = true)
        {
            Vector3 m = Vector3.Lerp(a, b, 0.1f);
            //ab的垂线
            Vector3 nomal = Vector2.Perpendicular(a - b).normalized;
            float rd = isLeft?Random.Range(-1.5f, -1f):Random.Range(1f, 1.5f);
            float curRatio = 0.3f;
            return m + (a - b).magnitude * curRatio * rd * nomal;
        }

        public void GetFlyOneStep(Transform parent, Vector3 startPos, Transform targetPos, Action<Transform> callback)
        {
            var oneStep = AddObj("prefab_FlyOneStep", parent,startPos);
            var flyCs = oneStep.GetComponent<FlyStar>();
            //flyCs.transform.position = startPos;
            Vector3 midPos = GetMiddlePosition(startPos, targetPos.position);
            flyCs.Fly(startPos,targetPos,midPos,callback);
        }
        #endregion

        #region 普通飞行物品
        /// <summary>
        /// 普通飞行物品
        /// </summary>
        /// <param name="objName">飞行特效名称</param>
        /// <param name="parent">父对象</param>
        /// <param name="startPos">初始位置</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="callback">到达目标位置回调</param>
        /// <param name="IsBezier">是否贝塞尔曲线飞行</param>
        /// <param name="isLeft">贝塞尔曲线弯曲方向</param>
        /// <param name="delay">延迟时间移动</param>
        public GameObject GetNormalFly(string objName,Transform parent, Vector3 startPos, Transform targetPos, Action<Transform> callback,bool IsBezier =false,bool isLeft = true,float delay = -1)
        {
            var norFly = AddObj(objName, parent);
            var flyCs = norFly.GetComponent<FlyStar>();
            norFly.transform.position = startPos;
            Vector3 midPos = IsBezier ? GetMiddlePosition(startPos, targetPos.position,isLeft) : Vector3.zero;
            flyCs.FlyDG(startPos,midPos,targetPos,callback,IsBezier,delay);
            return norFly;
        }
        /// <summary>
        /// 金币卡牌收集
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="startPos"></param>
        /// <param name="callback"></param>
        public void GetGoldFly1(Transform parent, Vector3 startPos, Action callback,bool IsgameGoldCard,int count=0)
        {
            string goldName = IsgameGoldCard ? "prefab_FlyGold" : "prefab_FlyUIGold";
            var goldFly = AddObj(goldName, parent);
            var flyCs = goldFly.GetComponent<FlyStar>();
            Transform targetPos = GetPos(IsgameGoldCard?FlyReType.GameGoldCard:FlyReType.UIGoleCube);
            goldFly.transform.position = startPos;
            flyCs.MyRectTransform.transform.position = startPos;
            Vector3 midPos = GetMiddlePosition(startPos, targetPos.position);
            var clickEff = AddObj("efc_icon_click2", parent);
            clickEff.GetComponent<MyEfcObj>().PlayEfc(startPos);
            if (!IsgameGoldCard&&count>0)
            {
                flyCs.flyTxtAnim(true,count);
            }
            goldFly.transform.DOMoveY(startPos.y + 0.3f, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                flyCs.FlyDG(new Vector3(startPos.x, startPos.y + 0.3f, startPos.z),midPos,targetPos, (Tr)=>
                {
                    var clickEff = AddObj("efc_icon_click1", parent);
                    clickEff.GetComponent<MyEfcObj>().PlayEfc(targetPos.position);
                    callback?.Invoke();
                },true,count:count);
            });
        }
        //网赚撒钱特效
        public void ShowCashOutEff(float count, Action<Transform> callback)
        {
            /*parent = parent == null? GameModule.UI.GetWindow(typeof(Panel_Game).FullName).transform : parent;
            var eff = AddObj("efc_cashboom_01", parent);
            var EffCs = eff.GetComponent<MyEfcObj>();
            EffCs.PlayEfc(parent.position,count,callback);*/
            
            var endTrans = GetEndTrans(ERewardType.Currency, 2, out var iconName);
            UIWindow parent = GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName);
            Vector3 start = parent.transform.position;
            var item = AddObj("prefab_FlyCashOut", parent.transform);
            var cs = item.GetComponent<FlyCurrencyN>();
            item.transform.position = start;
            cs.Init(endTrans,callback,0,true,1,cashAmount:count);
        }
        #endregion

        #region 普通特效

        public GameObject ShowEff(string name, Transform parent, Transform startPos,Vector3 scale)
        {
            parent = parent == null? GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName).transform : parent;
            var eff = AddObj(name, parent);
            if (startPos != null)
            {
                eff.transform.position = startPos.position;
            }
            eff.transform.localScale = scale;
            return eff;
        }

        public GameObject ShowEff2(string name, Transform parent, List<Transform> startPos, Vector3 scale)
        {
            parent = parent == null? GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName).transform : parent;
            var eff = AddObj(name, parent);
            eff.transform.position = parent.position;
            if (eff.transform.childCount >= startPos.Count)
            {
                for (int i = 0; i < eff.transform.childCount; i++)
                {
                    Transform child = eff.transform.GetChild(i);
                    if (i < startPos.Count)
                    {
                        child.gameObject.SetActive(true);
                        child.position = startPos[i].position;
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
            eff.transform.localScale = scale;
            return eff;
        }
        //钥匙特效
        public void ShowKeyEff(List<Transform> startPos,Transform parent, Action endCallback=null)
        {
            parent = parent == null? GameModule.UI.GetWindow(typeof(Panel_FloatTips).FullName).transform : parent;
            var eff = AddObj("prefab_FlyLevelKey2", parent);
            eff.transform.localScale = Vector3.one;
            var cs = eff.GetComponent<FlyKey>();
            eff.transform.position = parent.position;
            cs.InitFlyKey(startPos);
        }
        #endregion
        
        #region 对象池
        private Dictionary<string, List<GameObject>> _gamePool = new Dictionary<string, List<GameObject>>();

        public GameObject AddObj(string objName, Transform parent,float delay = -1)
        {
            GameObject newObj = null;
            if (_gamePool.TryGetValue(objName, out var list))
            {
                if (list.Count > 0)
                {
                    newObj = list[0];
                    list.RemoveAt(0);
                    newObj.transform.SetParent(parent);
                    newObj.transform.localScale = Vector3.one;
                    newObj.Show();
                }
            }

            if (newObj == null)
            {
                newObj = GameModule.Resource.LoadGameObject(objName, parent);
                newObj.name = objName;
            }

            if (delay > 0)
            {
                GameModule.Timer.AddTimer(timer =>
                {
                    HideObj(newObj);
                }, delay);
            }
            return newObj;
        }

        public GameObject AddObj(string objName, Transform parent, Vector3 startPos)
        {
            GameObject newObj = null;
            if (_gamePool.TryGetValue(objName, out var list))
            {
                if (list.Count > 0)
                {
                    newObj = list[0];
                    list.RemoveAt(0);
                    newObj.transform.SetParent(parent);
                    newObj.transform.position = startPos;
                    newObj.transform.localScale = Vector3.one;
                    newObj.Show();
                }
            }
            if (newObj == null)
            {
                newObj = GameModule.Resource.LoadGameObject(objName, parent);
                newObj.transform.position = startPos;
                newObj.name = objName;
            }
            return newObj;
        }
        public void HideObj(GameObject obj)
        {
            //obj.transform.localScale = Vector3.one;
            if (obj == null)
            {
                Log.Error("FlySystem HideObj obj is null");
                return;
            }
            obj.Hide();
            obj.transform.SetParent(transform);
            if (_gamePool.ContainsKey(obj.name))
            {
                _gamePool[obj.name].Add(obj);
            }
            else
            {
                _gamePool.Add(obj.name,new List<GameObject>() { obj });
            }
        }
        

        #endregion
        
    }

    public enum FlyReType
    {
        None=0,
        Life=1,
        Gold=2,
        Stamina=3,
        prop=4,
        Revoke=5,//撤回
        MessUp=6,//打乱
        DestoryGruop=7,//销毁一组
        MoveOut=8,//移出
        Prompt=9,//提示
        DrawCard=10,//抽卡
        AddSolt=11,//增加卡槽
        GameGoldCard =12,//游戏内金卡牌
        FreezeTime=13,//冻结时间
        UIGoleCube=14,//主界面金砖
        LevelKey=15,//等级钥匙
        ShopGold=16,//商店金币
        DreamPassKey=17,//梦想通行证钥匙
        FailGold=18,//失败金币
        CashOut=19,//网赚入口
        StoryCash=20,//故事模式网赚
        LevelCash = 21,//关卡网赚
    }
}
