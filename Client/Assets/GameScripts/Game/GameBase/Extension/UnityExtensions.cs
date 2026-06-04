using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using TK_Engine;
using System.Threading;
namespace GameBase
{
    public static class UnityExtensions
    {
        public static Vector2 ConvertWorldToUI(Camera CurCamera,Camera uiCamera,RectTransform uiElement,Vector3 worldPosition)
        {
            // 获取摄像机
            Camera GameCamera = CurCamera; // 游戏场景的相机
            Camera UICamera = uiCamera; // UI相机
            // 将世界坐标转换为屏幕坐标
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GameCamera, worldPosition);

            // 将屏幕坐标转换为 UI 坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElement, screenPoint, UICamera, out localPoint);
            return localPoint;
        }
        /// <summary>
        /// 贝塞尔曲线（使用于2d平面）
        /// </summary>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Vector2 Bezier(float t, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = Vector2.Lerp(a, b, t);
            var bc = Vector2.Lerp(b, c, t);
            return Vector2.Lerp(ab, bc, t);
        }
        /// <summary>
        /// 贝塞尔曲线（使用于3d空间）
        /// </summary>
        /// <param name="t"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Vector3 Bezier(float t, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = Vector3.Lerp(a, b, t);
            var bc = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(ab, bc, t);
        }
        /// <summary>
        /// 设置插入的层级关系
        /// </summary>
        /// <param name="parent">插入的父对象</param>
        /// <param name="x">插入对象的横坐标（世界坐标下）</param>
        /// <returns>返回在父对象下的层级索引</returns>
        public static int SiblingIndex(Transform parent, float x)
        {
            int insertIndex = 0;
            // 获取所有子物体
            List<Transform> children = new List<Transform>();
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                children.Add(child);
            }
            foreach (Transform child in children)
            {
                if (child == parent) continue;  // 忽略父物体自身

                if (child.position.x <= x)
                {
                    insertIndex++;
                }
            }
            return insertIndex;
        }
        /// <summary>
        /// 修改原先颜色的透明度为目标值
        /// </summary>
        /// <param name="color"></param>
        /// <param name="targetAlpha"></param>
        /// <returns></returns>
        public static Color SetAlpha(this Color color, float targetAlpha)
        {
            return new Color(color.r, color.g, color.b, targetAlpha);
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="targetAlpha">目标透明度</param>
        public static void SetAlpha(this SpriteRenderer sr, float targetAlpha)
        {
            sr.color = sr.color.SetAlpha(targetAlpha);
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="graphic"></param>
        /// <param name="targetAlpha">目标透明度</param>
        /// <returns></returns>
        public static T SetAlpha<T>(this T graphic, float targetAlpha) where T : Graphic
        {
            graphic.color = graphic.color.SetAlpha(targetAlpha);
            return graphic;
        }

        /// <summary>
        /// 从范围里随机获取一个数字
        /// </summary>
        /// <param name="p"></param>
        /// <param name="total">分母</param>
        /// <returns></returns>
        public static float GetRandom(this float end, float start = 0)
        {
            return UnityEngine.Random.Range(Mathf.Min(start, end), Mathf.Max(start, end));
        }

        /// <summary>
        /// 从范围里随机获取一个数字
        /// </summary>
        /// <param name="p"></param>
        /// <param name="total">分母</param>
        /// <returns></returns>
        public static int GetRandom(this int end, int start = 0)
        {
            return UnityEngine.Random.Range(Mathf.Min(start, end), Mathf.Max(start, end));
        }

        /// <summary>
        /// 以当前值作为事件发生的概率，返回一个随机的结果，参数为分母
        /// </summary>
        /// <param name="p"></param>
        /// <param name="total">分母</param>
        /// <returns></returns>
        public static bool Random(this float p, float total = 1)
        {
            return UnityEngine.Random.Range(0, total) < p;
        }

        /// <summary>
        /// 以当前值作为事件发生的概率，返回一个随机的结果，参数为分母
        /// </summary>
        /// <param name="p"></param>
        /// <param name="total">分母</param>
        /// <returns></returns>
        public static bool Random(this double p, float total = 1)
        {
            return UnityEngine.Random.Range(0, total) < p;
        }

        /// <summary>
        /// 水平翻转物体
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="scaleMode">翻转scale还是翻转eulerAngles</param>
        public static T FlipHorizontal<T>(this T component, bool scaleMode = false) where T : Component
        {
            if (scaleMode)
            {
                Vector3 scale = component.transform.localScale;
                component.transform.SetScale(-scale.x);
            }
            else
            {
                Vector3 eulerAngles = component.transform.eulerAngles;
                component.transform.SetEulerAnglesY(eulerAngles.y == 180 ? 0 : 180);
            }

            return component;
        }

        /// <summary>
        /// 更加安全的销毁方式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mono"></param>
        /// <param name="t"></param>
        /// <param name="delayTime">销毁延时</param>
        public static void DestroySafe<T>(this MonoBehaviour mono, T t, float delayTime) where T : Component
        {
            if (t != null)
            {
                if (delayTime != 0)
                {
                    Object.Destroy(t, delayTime);
                }
                else
                {
                    Object.Destroy(t);
                }
            }
        }

        /// <summary>
        /// 更加安全的销毁方式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mono"></param>
        /// <param name="t"></param>
        /// <param name="delayTime">销毁延时</param>
        public static void DestroySafe(this MonoBehaviour mono, GameObject go, float delayTime)
        {
            if (go != null)
            {
                if (delayTime != 0)
                {
                    Object.Destroy(go, delayTime);
                }
                else
                {
                    Object.Destroy(go);
                }
            }
        }

        /// <summary>
        /// 更加安全地关闭某个协程函数
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="coroutine"></param>
        public static void StopCoroutineSafe(this MonoBehaviour mono, Coroutine coroutine)
        {
            if (coroutine != null)
            {
                mono.StopCoroutine(coroutine);
            }
        }

        /// <summary>
        /// 显示某个名字的子物体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="name">子物体名字</param>
        /// <returns></returns>
        public static T ShowChildByName<T>(this T component, string name) where T : Component
        {
            Transform child = component.transform.Find(name);
            if (child != null)
            {
                child.gameObject.Show();
                return component;
            }

            return null;
        }

        /// <summary>
        /// 隐藏某个名字的子物体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="name">子物体名字</param>
        /// <returns></returns>
        public static T HideChildByName<T>(this T component, string name) where T : Component
        {
            Transform child = component.transform.Find(name);
            if (child != null)
            {
                child.gameObject.Hide();
                return component;
            }

            return null;
        }
    }

    public static class UIComponentExtension
    {
        /// <summary>
        /// 便捷设置图片填充值
        /// </summary>
        /// <param name="image"></param>
        /// <param name="fillAmount"></param>
        /// <returns></returns>
        public static Image FillAmount(this Image image, float fillAmount)
        {
            image.fillAmount = fillAmount;
            return image;
        }

    }

    public static class RectTransExtension
    {
        /// <summary>
        /// 设置锚点坐标X
        /// </summary>
        /// <param name="rectTrans"></param>
        /// <param name="anchorPosX"></param>
        /// <returns></returns>
        public static RectTransform AnchorPosX(this RectTransform rectTrans, float anchorPosX)
        {
            Vector2 pos = rectTrans.anchoredPosition;
            pos.x = anchorPosX;
            rectTrans.anchoredPosition = pos;
            return rectTrans;
        }

        /// <summary>
        /// 设置锚点坐标Y
        /// </summary>
        /// <param name="rectTrans"></param>
        /// <param name="anchorPosY"></param>
        /// <returns></returns>
        public static RectTransform AnchorPosY(this RectTransform rectTrans, float anchorPosY)
        {
            Vector2 pos = rectTrans.anchoredPosition;
            pos.y = anchorPosY;
            rectTrans.anchoredPosition = pos;
            return rectTrans;
        }

        /// <summary>
        /// 设置尺寸宽度
        /// </summary>
        /// <param name="rectTrans"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static RectTransform SizeWidth(this RectTransform rectTrans, float width)
        {
            Vector2 size = rectTrans.sizeDelta;
            size.x = width;
            rectTrans.sizeDelta = size;
            return rectTrans;
        }

        /// <summary>
        /// 设置尺寸高度
        /// </summary>
        /// <param name="rectTrans"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static RectTransform SizeHeight(this RectTransform rectTrans, float height)
        {
            Vector2 size = rectTrans.sizeDelta;
            size.y = height;
            rectTrans.sizeDelta = size;
            return rectTrans;
        }
    }

    public static class TransformExtension
    {
        #region 属性归一化

        /// <summary>
        /// 属性归一化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T Identity<T>(this T component) where T : Component
        {
            component.transform.position = Vector3.zero;
            component.transform.eulerAngles = Vector3.zero;
            component.transform.localScale = Vector3.one;
            return component;
        }

        /// <summary>
        /// 局部属性归一化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T LocalIdentity<T>(this T component) where T : Component
        {
            component.transform.localPosition = Vector3.zero;
            component.transform.localEulerAngles = Vector3.zero;
            component.transform.localScale = Vector3.one;
            return component;
        }

        #endregion

        #region 坐标

        /// <summary>
        /// 获取组件坐标
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Vector3 GetPostion<T>(this T component) where T : Component
        {
            return component.transform.position;
        }

        /// <summary>
        /// 获取组件局部坐标
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Vector3 GetLocalPostion<T>(this T component) where T : Component
        {
            return component.transform.localPosition;
        }

        /// <summary>
        /// 归一化坐标
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T PosIdentity<T>(this T component) where T : Component
        {
            component.transform.position = Vector3.zero;
            return component;
        }

        /// <summary>
        /// 归一化局部坐标
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T LocalPosIdentity<T>(this T component) where T : Component
        {
            component.transform.localPosition = Vector3.zero;
            return component;
        }

        /// <summary>
        /// 设置坐标某些轴向的值(此方法不支持将XYZ的值设置成PI，填PI会被视作不修改此轴向的值)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static T SetPosition<T>(this T component, float x = MathF.PI, float y = MathF.PI, float z = MathF.PI)
            where T : Component
        {
            component.transform.position = new Vector3
            (
                x == MathF.PI ? component.transform.position.x : x,
                y == Mathf.PI ? component.transform.position.y : y,
                z == MathF.PI ? component.transform.position.z : z
            );
            return component;
        }

        /// <summary>
        /// 设置局部坐标某些轴向的值(此方法不支持将XYZ的值设置成PI，填PI会被视作不修改此轴向的值)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static T SetLocalPosition<T>(this T component, float x = MathF.PI, float y = MathF.PI,
            float z = MathF.PI) where T : Component
        {
            component.transform.localPosition = new Vector3
            (
                x == MathF.PI ? component.transform.localPosition.x : x,
                y == Mathf.PI ? component.transform.localPosition.y : y,
                z == MathF.PI ? component.transform.localPosition.z : z
            );
            return component;
        }

        #endregion

        #region 旋转

        /// <summary>
        /// 获取组件旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Quaternion GetRotation<T>(this T component) where T : Component
        {
            return component.transform.rotation;
        }

        /// <summary>
        /// 获取组件局部旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Quaternion GetLocalRotation<T>(this T component) where T : Component
        {
            return component.transform.localRotation;
        }

        /// <summary>
        /// 获取组件旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Vector3 GetEulerAngles<T>(this T component) where T : Component
        {
            return component.transform.eulerAngles;
        }

        /// <summary>
        /// 获取组件局部旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Vector3 GetLocalEulerAngles<T>(this T component) where T : Component
        {
            return component.transform.localEulerAngles;
        }

        /// <summary>
        /// 归一化旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T RotationIdentity<T>(this T component) where T : Component
        {
            component.transform.rotation = Quaternion.identity;
            return component;
        }

        /// <summary>
        /// 归一化局部旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T LocalRotationIdentity<T>(this T component) where T : Component
        {
            component.transform.localRotation = Quaternion.identity;
            return component;
        }

        /// <summary>
        /// 归一化旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T EulerAnglesIdentity<T>(this T component) where T : Component
        {
            component.transform.eulerAngles = Vector3.zero;
            return component;
        }

        /// <summary>
        /// 归一化局部旋转
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T LocalEulerAnglesIdentity<T>(this T component) where T : Component
        {
            component.transform.localEulerAngles = Vector3.zero;
            return component;
        }

        /// <summary>
        /// 设置旋转(此方法不支持将XYZ的值设置成PI，填PI会被视作不修改此轴向的值)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static T SetRotate<T>(this T component, float x = MathF.PI, float y = MathF.PI, float z = MathF.PI)
            where T : Component
        {
            component.transform.eulerAngles = new Vector3
            (
                x == MathF.PI ? component.transform.eulerAngles.x : x,
                y == Mathf.PI ? component.transform.eulerAngles.y : y,
                z == MathF.PI ? component.transform.eulerAngles.z : z
            );
            return component;
        }

        /// <summary>
        /// 设置局部旋转(此方法不支持将XYZ的值设置成PI，填PI会被视作不修改此轴向的值)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static T SetLocalRotate<T>(this T component, float x = MathF.PI, float y = MathF.PI, float z = MathF.PI)
            where T : Component
        {
            component.transform.localEulerAngles = new Vector3
            (
                x == MathF.PI ? component.transform.localEulerAngles.x : x,
                y == Mathf.PI ? component.transform.localEulerAngles.y : y,
                z == MathF.PI ? component.transform.localEulerAngles.z : z
            );
            return component;
        }

        #endregion

        #region 缩放

        /// <summary>
        /// 获取缩放
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Vector3 GetScale<T>(this T component) where T : Component
        {
            return component.transform.localScale;
        }

        /// <summary>
        /// 归一化缩放
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T ScaleIdentity<T>(this T component) where T : Component
        {
            component.transform.localScale = Vector3.one;
            return component;
        }

        /// <summary>
        /// 设置缩放(此方法不支持将XYZ的值设置成PI，填PI会被视作不修改此轴向的值)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static T SetScale<T>(this T component, float x = MathF.PI, float y = MathF.PI, float z = MathF.PI)
            where T : Component
        {
            component.transform.localScale = new Vector3
            (
                x == MathF.PI ? component.transform.localScale.x : x,
                y == Mathf.PI ? component.transform.localScale.y : y,
                z == MathF.PI ? component.transform.localScale.z : z
            );
            return component;
        }

        #endregion

        #region 销毁子物体

        /// <summary>
        /// 销毁全部子物体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T DestroyAllChildren<T>(this T component) where T : Component
        {
            foreach (Transform childTrans in component.transform)
            {
                Object.Destroy(childTrans.gameObject);
            }

            return component;
        }

        /// <summary>
        /// 销毁全部子物体
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static GameObject DestroyAllChildren(this GameObject go)
        {
            foreach (Transform childTrans in go.transform)
            {
                Object.Destroy(childTrans.gameObject);
            }

            return go;
        }

        #endregion

        #region 层级设置

        /// <summary>
        /// 设置到最上层
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T AsFirstSibling<T>(this T component) where T : Component
        {
            component.transform.SetAsFirstSibling();
            return component;
        }

        /// <summary>
        /// 设置到最下层
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T AsLastSibling<T>(this T component) where T : Component
        {
            component.transform.SetAsLastSibling();
            return component;
        }

        /// <summary>
        /// 设置到目标层
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T SibilingIndex<T>(this T component, int index) where T : Component
        {
            component.transform.SetSiblingIndex(index);
            return component;
        }

        #endregion

        #region 仅修改或者增加   坐标、旋转、缩放的   某一个轴向的值

        /// <summary>
        /// 修改坐标X
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">x 坐标值。</param>
        public static void SetPositionX(this Transform transform, float newValue)
        {
            Vector3 v = transform.position;
            v.x = newValue;
            transform.position = v;
        }

        /// <summary>
        /// 修改坐标Y
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">y 坐标值。</param>
        public static void SetPositionY(this Transform transform, float newValue)
        {
            Vector3 v = transform.position;
            v.y = newValue;
            transform.position = v;
        }

        /// <summary>
        /// 修改坐标Z
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">z 坐标值。</param>
        public static void SetPositionZ(this Transform transform, float newValue)
        {
            Vector3 v = transform.position;
            v.z = newValue;
            transform.position = v;
        }

        /// <summary>
        /// 增加X坐标的值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">x 坐标值增量。</param>
        public static void AddPositionX(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.position;
            v.x += deltaValue;
            transform.position = v;
        }

        /// <summary>
        /// 增加Y坐标的值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">y 坐标值增量。</param>
        public static void AddPositionY(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.position;
            v.y += deltaValue;
            transform.position = v;
        }

        /// <summary>
        /// 增加Z坐标的值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">z 坐标值增量。</param>
        public static void AddPositionZ(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.position;
            v.z += deltaValue;
            transform.position = v;
        }

        /// <summary>
        /// 修改局部坐标X
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">x 局部坐标值</param>
        public static void SetLocalPositionX(this Transform transform, float newValue)
        {
            Vector3 v = transform.localPosition;
            v.x = newValue;
            transform.localPosition = v;
        }

        /// <summary>
        /// 修改局部坐标Y
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">y 局部坐标值</param>
        public static void SetLocalPositionY(this Transform transform, float newValue)
        {
            Vector3 v = transform.localPosition;
            v.y = newValue;
            transform.localPosition = v;
        }

        /// <summary>
        /// 修改局部坐标Z
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">z 局部坐标值</param>
        public static void SetLocalPositionZ(this Transform transform, float newValue)
        {
            Vector3 v = transform.localPosition;
            v.z = newValue;
            transform.localPosition = v;
        }

        /// <summary>
        /// 局部坐标X增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">x 局部坐标值增量</param>
        public static void AddLocalPositionX(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.localPosition;
            v.x += deltaValue;
            transform.localPosition = v;
        }

        /// <summary>
        /// 局部坐标Y增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">y 局部坐标值增量</param>
        public static void AddLocalPositionY(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.localPosition;
            v.y += deltaValue;
            transform.localPosition = v;
        }

        /// <summary>
        /// 局部坐标Z增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">z 局部坐标值增量</param>
        public static void AddLocalPositionZ(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.localPosition;
            v.z += deltaValue;
            transform.localPosition = v;
        }

        /// <summary>
        /// 修改旋转X
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">值</param>
        public static void SetEulerAnglesX(this Transform transform, float newValue)
        {
            Vector3 v = transform.eulerAngles;
            v.x = newValue;
            transform.eulerAngles = v;
        }

        /// <summary>
        /// 修改旋转Y
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">值</param>
        public static void SetEulerAnglesY(this Transform transform, float newValue)
        {
            Vector3 v = transform.eulerAngles;
            v.y = newValue;
            transform.eulerAngles = v;
        }

        /// <summary>
        /// 修改旋转Z
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">值</param>
        public static void SetEulerAnglesZ(this Transform transform, float newValue)
        {
            Vector3 v = transform.eulerAngles;
            v.z = newValue;
            transform.eulerAngles = v;
        }

        /// <summary>
        /// X轴角度增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">增量</param>
        public static void AddEulerAnglesX(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.eulerAngles;
            v.x += deltaValue;
            transform.eulerAngles = v;
        }

        /// <summary>
        /// Y轴角度增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">增量</param>
        public static void AddEulerAnglesY(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.eulerAngles;
            v.y += deltaValue;
            transform.eulerAngles = v;
        }

        /// <summary>
        /// Z轴角度增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="deltaValue">增量</param>
        public static void AddEulerAnglesZ(this Transform transform, float deltaValue)
        {
            Vector3 v = transform.eulerAngles;
            v.z += deltaValue;
            transform.eulerAngles = v;
        }

        /// <summary>
        /// 修改局部旋转X
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">新的X轴旋转</param>
        public static void SetLocalEulerAnglesX(this Transform transform, float newValue)
        {
            Vector3 v = transform.localEulerAngles;
            v.x = newValue;
            transform.localEulerAngles = v;
        }

        /// <summary>
        /// 修改局部旋转Y
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">新的Y轴旋转</param>
        public static void SetLocalEulerAnglesY(this Transform transform, float newValue)
        {
            Vector3 v = transform.localEulerAngles;
            v.y = newValue;
            transform.localEulerAngles = v;
        }

        /// <summary>
        /// 修改局部旋转Z
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="newValue">新的Z轴旋转</param>
        public static void SetLocalEulerAnglesZ(this Transform transform, float newValue)
        {
            Vector3 v = transform.localEulerAngles;
            v.z = newValue;
            transform.localEulerAngles = v;
        }

        /// <summary>
        /// 局部旋转X增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="add">增量X</param>
        public static void AddLocalEulerAnglesX(this Transform transform, float add)
        {
            Vector3 v = transform.localEulerAngles;
            v.x += add;
            transform.localEulerAngles = v;
        }

        /// <summary>
        /// 局部旋转Y增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="add">增量Y</param>
        public static void AddLocalEulerAnglesY(this Transform transform, float add)
        {
            Vector3 v = transform.localEulerAngles;
            v.y += add;
            transform.localEulerAngles = v;
        }

        /// <summary>
        /// 局部旋转Z增加给定值
        /// </summary>
        /// <param name="transform"><see cref="Transform" /> 对象。</param>
        /// <param name="add">增量Z</param>
        public static void AddLocalEulerAnglesZ(this Transform transform, float add)
        {
            Vector3 v = transform.localEulerAngles;
            v.z += add;
            transform.localEulerAngles = v;
        }

        #endregion

        #region 获取组件 或者指定子物体

        /// <summary>
        /// 从子物体里递归获取对应名字游戏物体的Transform组件
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="name">目标子物体名字</param>
        /// <returns></returns>
        public static Transform FindTransInChildren(this Transform trans, string name)
        {
            if (trans.name == name) return trans;

            //递归获取
            Transform target = null;
            foreach (Transform item in trans)
            {
                target = item.FindTransInChildren(name);
                if (target != null)
                {
                    break;
                }
            }

            return target;
        }

        /// <summary>
        /// 从子物体里递归获取对应Tag对象的Transform组件
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="name">目标子物体标签</param>
        /// <returns></returns>
        public static Transform FindTransWithTagInChildren(this Transform trans, string tag)
        {
            if (trans.tag == tag) return trans;

            Transform target = null;
            foreach (Transform item in trans)
            {
                target = item.FindTransWithTagInChildren(tag);
                if (target != null)
                {
                    break;
                }
            }

            return target;
        }

        /// <summary>
        /// 递归获取子物体里目标标签的全部子物体Transform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static List<Transform> FindTransAllWithTagInChildren(this Transform trans, string tag)
        {
            List<Transform> allTrans = new List<Transform>();
            if (trans.tag == tag)
            {
                allTrans.Add(trans);
            }

            foreach (Transform item in trans)
            {
                allTrans.AddRange(item.FindTransAllWithTagInChildren(tag));
            }

            return allTrans;
        }

        /// <summary>
        /// 从子物体里获取某个组件(包括隐藏着的子物体)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <returns></returns>
        public static T GetTInChildrenIncludeHide<T>(this Transform trans) where T : Object
        {
            //先从自身获取
            T t = trans.GetComponent<T>();
            if (t != default)
            {
                return t;
            }

            //递归获取
            foreach (Transform item in trans)
            {
                t = item.GetTInChildrenIncludeHide<T>();
                if (t != default)
                {
                    break;
                }
            }

            return t;

        }

        /// <summary>
        /// 从子物体里获取某些组件(包括隐藏着的子物体)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <param name="list"></param>
        public static List<T> GetTsInChildrenIncludeHide<T>(this Transform trans)
        {
            List<T> ts = trans.GetComponents<T>().ToList();

            foreach (Transform item in trans)
            {
                ts.AddRange(item.GetTsInChildrenIncludeHide<T>());
            }

            return ts;
        }

        private static readonly List<Transform> m_CachedTransforms = new List<Transform>();

        /// <summary>
        /// 递归设置游戏对象的层次。
        /// </summary>
        /// <param name="gameObject"><see cref="GameObject" /> 对象</param>
        /// <param name="layer">目标层级。</param>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.GetComponentsInChildren(true, m_CachedTransforms);
            for (int i = 0; i < m_CachedTransforms.Count; i++)
            {
                m_CachedTransforms[i].gameObject.layer = layer;
            }

            m_CachedTransforms.Clear();

        }

        #endregion

        /// <summary>
        /// 复制Transform属性给另一个Transform
        /// </summary>
        /// <param name="self"></param>
        /// <param name="target"></param>
        public static void CopyTransfrom(this Transform self, Transform target)
        {
            self.position = target.position;
            self.eulerAngles = target.eulerAngles;
            self.localScale = target.localScale;
        }

    }

    public static class BehaviourExtension
    {
        /// <summary>
        /// 启用组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static T Enable<T>(this T behaviour) where T : Behaviour
        {
            behaviour.enabled = true;
            return behaviour;
        }

        /// <summary>
        /// 禁用组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static T Disable<T>(this T behaviour) where T : Behaviour
        {
            behaviour.enabled = false;
            return behaviour;
        }
    }

    public static class GameObjectExtension
    {
        /// <summary>
        /// 游戏对象淡入淡出(PS:淡出结束会隐藏或销毁物体)
        /// </summary>
        /// <param name="go"></param>
        /// <param name="fadeIn">是否是淡入</param>
        /// <param name="time">淡入淡出用时</param>
        /// <param name="autoDes">淡出结束自动销毁</param>
        public static void GameObjectFadeInOrOut(this GameObject go, bool fadeIn, float time, bool autoDes = true)
        {
            if (go == null) return;

            //先获取这个物体上的所有材质球
            List<Material> materials = go.GetMaterialsRecursion();
            if (fadeIn)
            {
                go.Show();
                foreach (var item in materials)
                {
                    //记录原先的颜色
                    Color oldColor = item.GetColor("_Color");
                    //将初始透明度设置为0
                    item.SetColor("_Color", oldColor.SetAlpha(0));
                    float a = 0;
                    //在给定时间内将透明度渐变到原先记录的透明度
                    DOTween.To(() => a, b =>
                    {
                        a = b;
                        Color color = oldColor.SetAlpha(a);
                        item.SetColor("_Color", color);
                    }, oldColor.a, time);
                }
            }
            else
            {
                foreach (var item in materials)
                {
                    //记录原先的颜色
                    Color oldColor = item.GetColor("_Color");
                    //在给定时间内将透明度渐变到0
                    float a = oldColor.a;
                    DOTween.To(() => a, b =>
                    {
                        a = b;
                        Color color = oldColor.SetAlpha(a);
                        item.SetColor("_Color", color);

                    }, 0, time).onComplete += () =>
                    {
                        //淡入结束后 如果需要销毁对象就销毁 如果不需要销毁对象则隐藏对象以及将颜色还原回去
                        if (go != null && autoDes)
                        {
                            Object.Destroy(go);
                        }
                        else if (go != null)
                        {
                            go.Hide();
                        }

                        if (item != null)
                        {
                            item.SetColor("_Color", oldColor);
                        }
                    };
                }
            }
        }

        /// <summary>
        /// 递归获取这个游戏对象的全部材质球
        /// </summary>
        /// <param name="go"></param>
        /// <param name="materials"></param>
        public static List<Material> GetMaterialsRecursion(this GameObject go)
        {
            List<Material> materials = new List<Material>();
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                materials.AddRange(renderer.materials);
            }

            foreach (Transform trans in go.transform)
            {
                materials.AddRange(GetMaterialsRecursion(trans.gameObject));
            }

            return materials;
        }

        /// <summary>
        /// 递归获取这个游戏对象的全部材质球
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="materials"></param>
        public static List<Material> GetMaterialsRecursion(this Transform transform)
        {
            return transform.gameObject.GetMaterialsRecursion();
        }

        /// <summary>
        /// 修改层级
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="layer">目标层级</param>
        /// <param name="recursion">递归</param>
        public static void SetLayer(this GameObject obj, int layer, bool recursion = true)
        {
            obj.layer = layer;
            if (recursion)
            {
                foreach (Transform item in obj.transform)
                {
                    item.gameObject.layer = layer;
                    SetLayer(item.gameObject, layer, recursion);
                }
            }

        }

        /// <summary>
        /// 显示物体
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static GameObject Show(this GameObject go)
        {
            if (!go.activeSelf)
            {
                go.SetActive(true);
            }

            return go;
        }

        /// <summary>
        /// 隐藏物体
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static GameObject Hide(this GameObject go)
        {
            if (go.activeSelf)
            {
                go.SetActive(false);
            }

            return go;
        }

        /// <summary>
        /// 获取某个组件，如果不存在就添加一个
        /// </summary>
        /// <param name="go"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T MyGetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T t = go.GetComponent<T>();
            return t ? t : go.AddComponent<T>();
        }

        /// <summary>
        /// 获取某个组件，如果不存在就添加一个
        /// </summary>
        /// <param name="go"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T GetOrAddComponent<T>(this Transform trans) where T : Component
        {
            T t = trans.gameObject.GetComponent<T>();
            return t ? t : trans.gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 设置游戏物体层级
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static T Layer<T>(this T component, int layer) where T : Component
        {
            component.gameObject.layer = layer;
            return component;
        }

        /// <summary>
        /// 设置游戏物体层级
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static T Layer<T>(this T component, string layer) where T : Component
        {
            component.gameObject.layer = LayerMask.NameToLayer(layer);
            return component;
        }

        /// <summary>
        /// 设置游戏物体层级
        /// </summary>
        /// <param name="go"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static GameObject Layer(this GameObject go, int layer)
        {
            go.layer = layer;
            return go;
        }

        /// <summary>
        /// 设置游戏物体层级
        /// </summary>
        /// <param name="go"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static GameObject Layer(this GameObject go, string layer)
        {
            go.layer = LayerMask.NameToLayer(layer);
            return go;
        }

        /// <summary>
        /// 销毁游戏物体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="delay">销毁延时</param>
        /// <returns></returns>
        public static T DestroyGameObjectSafe<T>(this T component, float delay = 0) where T : Component
        {
            if (component && component.gameObject)
            {
                Object.Destroy(component.gameObject, delay);
            }

            return component;
        }

        /// <summary>
        /// 销毁游戏物体
        /// </summary>
        /// <param name="go"></param>
        /// <param name="delay">销毁延时</param>
        public static void DestroyGameObjectSafe(this GameObject go, float delay = 0)
        {
            if (go != null)
            {
                Object.Destroy(go, delay);
            }
        }

    }

    public static class CameraExtension
    {
        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="rect">范围</param>
        /// <returns></returns>
        public static Texture2D ScreenShot(this Camera camera, Rect rect)
        {
            var renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            var screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(rect, 0, 0);
            screenShot.Apply();

            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(renderTexture);
            return screenShot;
        }
    }

    public static class ParticleExtension
    {
        /// <summary>
        /// 播放一个游戏物体上的全部特效组件
        /// </summary>
        /// <param name="ps"></param>
        public static void RecursionPlayParticle(this GameObject ps)
        {
            ParticleSystem[] list = ps.GetComponentsInChildren<ParticleSystem>();

            for (int i = 0; i < list.Length; i++)
            {
                list[i].Play();
            }
        }

        /// <summary>
        /// 停止一个游戏物体上的全部特效组件
        /// </summary>
        /// <param name="ps"></param>
        public static void RecursionStopParticle(this GameObject ps)
        {
            ParticleSystem[] list = ps.GetComponentsInChildren<ParticleSystem>();

            for (int i = 0; i < list.Length; i++)
            {
                list[i].Stop();
            }
        }

        /// <summary>
        /// 暂停一个游戏物体上的全部特效组件
        /// </summary>
        /// <param name="ps"></param>
        public static void RecursionPauseParticle(this GameObject ps)
        {
            ParticleSystem[] list = ps.GetComponentsInChildren<ParticleSystem>();

            for (int i = 0; i < list.Length; i++)
            {
                list[i].Pause();
            }
        }

       
    }

    public static class PlayGameExtensions
    {
        #region 向量处理
         /// <summary>
        /// 向量旋转
        /// </summary>
        /// <param name="v"></param>
        /// <param name="rotateAngle">旋转角度，单位度</param>
        /// <returns></returns>
        public static Vector2 Vector2Rotate(this Vector2 v, float rotateAngle)
        {
            //先将角度换算成弧度
            rotateAngle = rotateAngle * Mathf.PI / 180;
            float cosTheta = Mathf.Cos(rotateAngle);
            float sinTheta = Mathf.Sin(rotateAngle);
            return new Vector2((v.x * cosTheta) - (v.y * sinTheta), (v.x * sinTheta) + (v.y * cosTheta));
        }
        /// <summary>
        /// 根据角度获取一个单位向量
        /// </summary>
        /// <param name="angle">向量与X轴正方向的夹角</param>
        /// <returns></returns>
        public static Vector2 GetUnitVector2ByAngle(this float angle)
        {
            return new Vector2(Mathf.Cos(angle * Mathf.PI / 180), Mathf.Sin(angle * Mathf.PI / 180));
        }
        /// <summary>
        /// 根据角度获取一个单位向量
        /// </summary>
        /// <param name="angle">向量与X轴正方向的夹角</param>
        /// <returns></returns>
        public static Vector2 GetUnitVector2ByAngle(this int angle)
        {
            return new Vector2(Mathf.Cos(angle * Mathf.PI / 180), Mathf.Sin(angle * Mathf.PI / 180));
        }
        /// <summary>
        /// 从上下左右四维向量里面随机得到一个二维向量(在某个范围内随机得到一点)
        /// </summary>
        /// <param name="v4">四个分量为 上下左右</param>
        /// <returns></returns>
        public static Vector2 GetRandomV2FromV4(this Vector4 v4)
        {                                              //左     右                            //下    上
            return new Vector2(UnityEngine.Random.Range(v4.z, v4.w), UnityEngine.Random.Range(v4.y, v4.x));
        }
        /// <summary>
        /// 在一个范围内UI的显示范围限制
        /// </summary>
        /// <param name="v2">UI自己的anchoredPosition</param>
        /// <param name="width">范围宽度</param>
        /// <param name="height">范围高度</param>
        /// <param name="sizeDelta">UI自己的高度和宽度</param>
     
        /// <returns></returns>
        public static Vector2 GetRestrictedRange(this Vector2 pos,float width,float height,Vector2 sizeDelta)
        {
            Vector2 Newpos = Vector2.zero;
            float minX = (width / 2 - sizeDelta.x / 2)*-1;
            float maxX = width / 2 - sizeDelta.x / 2;
            float minY = (height / 2 - sizeDelta.y / 2)*-1;
            float maxY = height / 2 - sizeDelta.y / 2;
            float NewX = Mathf.Clamp(pos.x, minX, maxX);
            float NewY = Mathf.Clamp(pos.y, minY, maxY);
            Newpos = new Vector2(NewX, NewY);
            return Newpos;
        }
        /// <summary>
        /// 在9*6的区域内根据格子索引返回一个二维坐标，都以左下角为起点
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector2Int IndexToVector2Int(this int pos)
        {
            int x = pos % 9;
            int y = 5 - (5 - (int)pos / 9);
            return new Vector2Int(x, y);
        }
        /// <summary>
        /// 在7*5的区域内根据格子索引返回一个二维坐标，都以左下角为起点
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector2Int IntToV2 (this int pos,bool OpenGuide =false)
        {
            int x = 0;
            int y = 0;
            if (!OpenGuide)
            {
                x = pos % 5;
                y = 6 - (6 - (int)pos / 5);
            }
            else
            {
                x = pos % 3;
                y = 2 - (2 - (int)pos / 3);
            }
           
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// 得到一个二维数组中最大的x和y
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        public static Vector2Int GetMaxV2( this List<Vector2Int> temp)
        {
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (var item in temp)
            {
                if (item.x > maxX)
                    maxX = item.x;
                if (item.y > maxY)
                    maxY = item.y;
            }
            return new Vector2Int((int)maxX, (int)maxY);
        }
        /// <summary>
        /// 得到一个二维数组中最小的x和y
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        public static Vector2Int GetMinV2(this List<Vector2Int> temp)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            foreach (var item in temp)
            {
                if (item.x < minX)
                    minX = item.x;
                if (item.y < minY)
                    minY = item.y;
            }
            return new Vector2Int((int)minX, (int)minY);
        }
        public static List<Vector2Int> FilterCoordinates(this List<Vector2Int> coordinates, Vector2Int maxV2, Vector2Int minV2)
        {
            return coordinates
                .Where(v => v.x <= maxV2.x && v.y <= maxV2.y && v.x >= minV2.x && v.y >= minV2.y)
                .ToList();
        }
        

        #endregion

        #region 角度处理
        /// <summary>
        /// 将角度映射到0-360
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float RemapAngle360(this float angle)
        {
            return ((angle % 360) + 360) % 360;
        }
        /// <summary>
        /// 将角度映射到0-360
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float RemapAngle360(this int angle)
        {
            return ((angle % 360) + 360) % 360;
        }
        

        #endregion

        #region 数组索引处理
        /// <summary>
        /// 将当前数字映射到范围内
        /// </summary>
        /// <param name="index">当前索引</param>
        /// <param name="count">数组长度</param>
        /// <returns></returns>
        public static int MapIndex(this int index, int count)
        {
            return ((index % count) + count) % count;
        }

        /// <summary>
        /// 在数组的索引范围内随机获取一个索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetRandomIndex<T>(this List<T> list)
        {
            return UnityEngine.Random.Range(0, list.Count);
        }
        

        #endregion

        #region X，Y之间随机数处理
        /// <summary>
        /// 随机获取一个位于X,Y之间的值
        /// </summary>
        /// <param name="range">随机范围</param>
        /// <returns></returns>
        public static float GetRandomFormV2(this Vector2 range)
        {
            if (range.y > range.x)
            {
                return UnityEngine.Random.Range(range.x, range.y);
            }
            else
            {
                return UnityEngine.Random.Range(range.y, range.x);
            }

        }

        /// <summary>
        /// 随机获取一个位于X,Y之间的值
        /// </summary>
        /// <param name="range">随机范围</param>
        /// <returns></returns>
        public static int GetRandomIntFormV2(this Vector2 range)
        {
            if (range.y > range.x)
            {
                return UnityEngine.Random.Range((int)range.x, (int)range.y);
            }
            else
            {
                return UnityEngine.Random.Range((int)range.y, (int)range.x);
            }

        }
        

        #endregion
        
        #region 数组、集合、字典类型处理
        /// <summary>
        /// 从数组里随机获取一个跟给定的元素不同的元素(如果没有给定元素则无要求)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="remove">是否将随机到的元素从原数组里移除</param>
        /// <param name="conflict">希望屏蔽掉的元素</param>
        /// <returns></returns>
        public static T RandomFromList<T>(this List<T> list, bool remove = true, T conflict = null) where T : UnityEngine.Object
        {
            //随机获取一个元素
            int index = UnityEngine.Random.Range(0, list.Count);
            T temp = list[index];
            //如果元素与想要屏蔽掉的元素相同 就重新随机一个
            if (temp == conflict)
            {
                return RandomFromList(list, conflict);
            }
            //如果需要将元素从原数组里移除 就移除一下
            if (remove)
            {
                list.RemoveAt(index);
            }
            return temp;
        }
       
        public static T RandomFromAllList<T>(this List<T> list, bool remove = true, T conflict = default(T))
        {
            //随机获取一个元素
            int index = UnityEngine.Random.Range(0, list.Count);
            T temp = list[index];
            //如果元素与想要屏蔽掉的元素相同 就重新随机一个
            if (temp.Equals(conflict))
            {
                return RandomFromAllList(list,remove, conflict);
            }
            //如果需要将元素从原数组里移除 就移除一下
            if (remove)
            {
                list.RemoveAt(index);
            }
            return temp;
        }

        /// <summary>
        /// 从列表中随机取一个元素（交换移除，O(1)，无递归）
        /// </summary>
        public static T RandomSwapFromList<T>(this List<T> list, bool remove = true)
        {
            if (list == null || list.Count == 0) return default;

            int index = UnityEngine.Random.Range(0, list.Count);
            T temp = list[index];

            if (remove)
            {
                int last = list.Count - 1;
                list[index] = list[last];
                list.RemoveAt(last);
            }

            return temp;
        }

        public static V RandomFromDic<K, V>(this Dictionary<K, V> dic, bool remove = false, V conflict = default(V))
        {
            int index = UnityEngine.Random.Range(0, dic.Count);
            K key = dic.Keys.ElementAt(index);
            V value = dic[key];
            if (value.Equals(conflict))
            {
                return RandomFromDic(dic, remove, conflict);
            }

            if (remove)
            {
                dic.Remove(key);
            }
            return value;
        }
        /// <summary>
        /// 从数组里随机获取一组元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="count">获取元素的数量</param>
        /// <param name="remove">是否将随机到的元素从数组里移除</param>
        /// <returns></returns>
        public static List<T> RandomFromList<T>(this List<T> list, int count, bool remove = false)
        {
            //如果数组里的元素比需要的元素数量少 或者需要的元素数量为0的话 就返回个空数组
            if (list.Count < count || count <= 0)
            {
                return new List<T>();
            }

            //创建一个存放结果的数组
            List<T> result = new List<T>(count);
            //如果不移除 那么就拷贝一份数组，否则就用原数组 
            List<T> temp = !remove ? new List<T>(list) : list;
            for (int i = 0; i < count; i++)
            {
                T t = temp[UnityEngine.Random.Range(0, temp.Count)];
                temp.Remove(t);
                result.Add(t);
            }
            return result;
        }

        /// <summary>
        /// 从数组里随机获取一个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static T RandomFromArray<T>(this T[] values)
        {
            return values[UnityEngine.Random.Range(0, values.Length)];
        }
        /// <summary>
        /// 返回一个打乱后的数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T> Shuffle<T>(this List<T> list)
        {
            List<T> temp = new List<T>();
            List<int> indexList = list.Count.GetRandomIndexList();
            for (int i = 0; i < indexList.Count; i++)
            {
                temp.Add(list[indexList[i]]);
            }
            return temp;
        }
        /// <summary>
        /// 返回一组打乱的(0至数组count-1)的索引
        /// </summary>
        /// <returns></returns>
        public static List<int> GetRandomIndexList<T>(this List<T> selfList)
        {
            return selfList.Count.GetRandomIndexList();
        }
        /// <summary>
        /// 返回一组打乱的(0至count-1)的索引
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<int> GetRandomIndexList(this int count)
        {
            //构建一个存放索引的数组 和一个存放结果的数组
            List<int> list = new List<int>(count);
            List<int> resule = new List<int>();
            for (int i = 0; i < count; i++)
            {
                list.Add(i);
            }
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, list.Count);
                resule.Add(list[index]);
                list.RemoveAt(index);
            }

            return resule;
        }
        /// <summary>
        /// 如果数组里不包含此元素就将此元素加入数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="t">要添加的新元素</param>
        /// <returns></returns>
        public static List<T> AddWithoutRepetition<T>(this List<T> list, T t)
        {
            if (!list.Contains(t))
            {
                list.Add(t);
            }
            return list;
        }
        /// <summary>
        /// 交换数组中两个值的位置
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index1">当前需要换位置的元素的索引</param>
        /// <param name="index2">需要将这个元素换到哪个索引</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> Swap<T>(this List<T> list, int index1, int index2)
        {
            (list[index1], list[index2]) = (list[index2], list[index1]);
            return list;
        }
        /// <summary>
        /// 判断两个集合是否相同
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool IsEqual<T>(this List<T> list1, List<T> list2)
        {
            bool equal = true;     
            //数组1里有数组2里没有的元素 或者数组2里有数组1里没有的元素 就说明两个数组不相同
            list1.ForEach(a =>
            {
                if (!list2.Contains(a))
                {
                    equal = false;
                }
            });
            list2.ForEach(a =>
            {
                if (!list1.Contains(a))
                {
                    equal = false;
                }
            });
            return equal;
        }
        /// <summary>
        /// 判断小列表所有元素是否都存在于大列表中
        /// </summary>
        /// <param name="smallList">小列表</param>
        /// <param name="bigList">大列表</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool IsSubset<T>(List<T> smallList, List<T> bigList)
        {
            return smallList.All(item => bigList.Contains(item));
        }
        /// <summary>
        /// 对数组元素求和
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int Sum(this List<int> list)
        {
            int total = 0;
            list.ForEach(a => total += a);
            return total;
        }
        /// <summary>
        /// 对数组元素求和
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static float Sum(this List<float> list)
        {
            float total = 0;
            list.ForEach(a => total += a);
            return total;
        }
        /// <summary>
        /// 对数组元素求和
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static double Sum(this List<double> list)
        {
            double total = 0;
            list.ForEach(a => total += a);
            return total;
        }
        /// <summary>
        /// 获取数组里某个索引的值，如果索引超出上限就返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selfList"></param>
        /// <param name="index">获取哪个索引的元素</param>
        /// <returns></returns>
        public static T TryGetValue<T>(this List<T> selfList, int index)
        {
            return selfList.Count > index ? selfList[index] : default;
        }
        /// <summary>
        /// 获取字典里某个键的值，如果不存在这个键就返回默认值
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key">获取哪个键的元素</param>
        /// <returns></returns>
        public static T MyTryGetValue<K, T>(this Dictionary<K, T> dic, K key)
        {
            if (dic.ContainsKey(key))
            {
                return dic[key];
            }
            return default;
        }
        /// <summary>
        /// 获取字典里某个值的键，如果不存在这个键就返回默认值
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="value"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public static TKey GetKeyFromValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value)
        {
            foreach (var pair in dictionary)
            {
                if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                {
                    return pair.Key; // 找到匹配的值，返回对应的键
                }
            }
            return default; // 如果没有找到，返回默认值
        }
        /// <summary>
        /// 移除数组里的某个元素，如果他存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="target">要移除的元素</param>
        public static void RemoveSafe<T>(this List<T> list, T target)
        {
            if (list.Contains(target))
            {
                list.Remove(target);
            }
        }
        /// <summary>
        /// 移除字典里的某个键，如果他存在
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dic"></param>
        /// <param name="targetKey">要移除的键</param>
        public static void RemoveSafe<K, V>(this Dictionary<K, V> dic, K targetKey)
        {
            if (dic.ContainsKey(targetKey))
            {
                dic.Remove(targetKey);
            }
        }
        /// <summary>
        /// 写法更加便捷的Foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selfArray"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEnumerable<T> mForEach<T>(this IEnumerable<T> selfArray, Action<T> action)
        {
            if (action == null) return selfArray;
            foreach (var item in selfArray)
            {
                action(item);
            }
            return selfArray;
        }
        /// <summary>
        /// 写法更加便捷的倒序Foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selfList"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static List<T> ForEachReverse<T>(this List<T> selfList, Action<T> action)
        {
            if (action == null) return selfList;

            for (var i = selfList.Count - 1; i >= 0; i--)
            {
                action(selfList[i]);
            }
            return selfList;
        }
        /// <summary>
        /// 写法更加便捷的倒序Foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selfList"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static List<T> ForEachReverse<T>(this List<T> selfList, Action<T, int> action)
        {
            if (action == null) return selfList;

            for (var i = selfList.Count - 1; i >= 0; i--)
            {
                action(selfList[i], i);
            }
            return selfList;
        }
        /// <summary>
        /// 如果字典里取不到值，就返回我们给定的默认值
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public static TValue GetValueOrDefaultDic<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        /// <summary>
        /// 如果字典里取不到值，就通过我们给的方法创建一个值返回
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key">键</param>
        /// <param name="createFun">创建默认值的方法</param>
        /// <returns></returns>
        public static TValue GetValueOrDefaultDic<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createFun)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : createFun();

        }
        /// <summary>
        /// 如果字典里不包含这个键，就将这对键值添加进去
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        public static void AddSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value = default)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }

        }
        /// <summary>
        /// 合并两个字典
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dic1"></param>
        /// <param name="dic2"></param>
        /// <returns></returns>
        public static Dictionary<K, V> AddRange<K, V>(this Dictionary<K, V> dic1, Dictionary<K, V> dic2)
        {
            foreach (var item in dic2)
            {
                if (!dic1.ContainsKey(item.Key))
                {
                    dic1.Add(item.Key, item.Value);
                }
            }
            return dic1;
        }
        /// <summary>
        /// 合并两个数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        public static T[] AddRange<T>(this T[] array1, T[] array2)
        {
            T[] temp = new T[array1.Length + array2.Length];
            int index = 0;
            foreach (var item in array1)
            {
                temp[index] = item;
                index++;
            }
            foreach (var item in array2)
            {
                temp[index] = item;
                index++;
            }
            return temp;
        }
        /// <summary>
        /// 根据给定键名从一组参数里面找到想要的参数  键名:值 
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="key">键名</param>
        /// <returns></returns>
        public static string GetValueByKey(this object[] objs, string key)
        {
            string value = "";
            if (objs == null || objs.Length == 0) return value;

            foreach (var item in objs)
            {
                string str = item.ToString();
                if (!str.Contains(key)) continue;
                value = str.Split(str.Contains("：") ? '：' : ':')[1];
                break;
            }
            return value;
        }
        /// <summary>
        /// 根据给定键名从一组参数里面找到想要的参数  键名:值 
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="key">键名</param>
        /// <returns></returns>
        public static string GetValueByKey(this List<object> objs, string key)
        {
            string value = "";
            if (objs == null || objs.Count == 0) return value;

            foreach (var item in objs)
            {
                string str = item.ToString();
                if (!str.Contains(key)) continue;
                value = str.Split(str.Contains("：") ? '：' : ':')[1];
                break;
            }
            return value;
        }
        /// <summary>
        /// 将数组里找到的第一个类型匹配上的元素返回
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static T GetTargetTypeValue<T>(this object[] arg)
        {
            T t = default;
            if (arg == null || arg.Length == 0) return t;

            //遍历数组 找到目标类型的数据返回        
            foreach (var item in arg)
            {
                if (item is T tt)
                {
                    t = tt;
                    break;
                }
            }
            return t;
        }
        /// <summary>
        /// 将数组里找到的第一个类型匹配上的元素返回
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static T GetTargetTypeValue<T>(this List<object> arg)
        {
            T t = default;
            if (arg == null || arg.Count == 0) return t;

            //遍历数组 找到目标类型的数据返回        
            foreach (var item in arg)
            {
                if (item is T tt)
                {
                    t = tt;
                    break;
                }
            }
            return t;
        }
        /// <summary>
        ///  从一个堆栈移除一个元素，不只是从栈顶移除一个元素
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveItem<T>(this Stack<T> stack, T item)
        {
            if (stack == null) throw new ArgumentNullException("stack");
    
            var tempStack = new Stack<T>();
            bool isRemoved = false;

            // 遍历原堆栈，跳过首个匹配项
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!isRemoved && EqualityComparer<T>.Default.Equals(current, item))
                {
                    isRemoved = true;  // 标记已移除，后续相同值不处理
                    continue;
                }
                tempStack.Push(current); // 非目标元素压入临时堆栈
            }

            // 将临时堆栈元素还原到原堆栈
            while (tempStack.Count > 0)
            {
                stack.Push(tempStack.Pop());
            }
        }


        #endregion

        #region Object对象拓展
         /// <summary>
        /// object转整形
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <param name="defaulValue">默认值</param>
        /// <returns></returns>
        public static int ToInt(this object obj, int defaulValue = 0)
        {
            return obj.ToString().ToInt(defaulValue);
        }
        
        /// <summary>
        /// object转小数，兼容不同区域小数表示不一样的情况
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <param name="defaulValue">默认值</param>
        /// <returns></returns>
        public static float ToFloat(this object obj, float defaulValue = 0)
        {
            return obj.ToString().ToFloat(defaulValue);
        }
        /// <summary>
        /// object转布尔
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <returns></returns>
        public static bool ToBool(this object obj, bool defaulValue = false)
        {
            return obj.ToString().ToBool(defaulValue);
        }
        /// <summary>
        /// object转二维向量
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="safe">安全模式，安全模式会校验一下字符串，去除掉括号等用不到的符号</param>
        /// <returns></returns>
        public static Vector2 ToVector2(this object obj, Vector2 defaultValue = default, bool safe = true)
        {
            return obj.ToString().ToVector2(defaultValue, safe);
        }
        /// <summary>
        /// object转三维向量
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="safe">安全模式，安全模式会校验一下字符串，去除掉括号等用不到的符号</param>
        /// <returns></returns>
        public static Vector3 ToVector3(this object obj, Vector3 defaultValue = default, bool safe = true)
        {
            return obj.ToString().ToVector3(defaultValue, safe);
        }

        /// <summary>
        /// object转四维向量
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="safe">安全模式，安全模式会校验一下字符串，去除掉括号等用不到的符号</param>
        /// <returns></returns>
        public static Vector4 ToVector4(this object obj, Vector4 defaultValue = default, bool safe = true)
        {
            return obj.ToString().ToVector4(defaultValue, safe);
        }
        /// <summary>
        /// object转颜色
        /// </summary>
        /// <param name="obj">要强转的对象</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="safe">安全模式，安全模式会校验一下字符串，去除掉括号等用不到的符号</param>
        /// <returns></returns>
        public static Color ToColor(this object obj, Color defaultValue = new Color(), bool safe = true)
        {
            return obj.ToString().ToColor(defaultValue, safe);
        }
        /// <summary>
        /// 改变数据类型
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static object ChangeType<T>(this object value)
        {
            if (typeof(T) == typeof(Vector2))
            {
                return value.ToVector2();
            }
            else if (typeof(T) == typeof(Vector3))
            {
                return value.ToVector3();
            }
            else if (typeof(T) == typeof(Vector4))
            {
                return value.ToVector4();
            }
            else if (typeof(T) == typeof(Color))
            {
                return value.ToColor();
            }
            else if (typeof(T) == typeof(int))
            {
                return value.ToInt();
            }
            else if (typeof(T) == typeof(float))
            {
                return value.ToFloat();
            }
            else if (typeof(T) == typeof(double))
            {
                return value.ToFloat();
            }
            else
            {
                return value.ToString();
            }
        }
        

        #endregion

        #region 字符串处理
        /// <summary>
        /// 获取字符串里面的数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static float GetNumbInString(this string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", "").ToFloat();
        }
        public static Color GetColor(this string hex)
        {
            // 默认黑色
            if (string.IsNullOrEmpty(hex)) return Color.black;
            // 转换颜色
            hex = hex.ToLower();
            //如果不是16进制的颜色
            if (hex.IndexOf("#", StringComparison.Ordinal) != 0 || hex.Length != 7)
            {
                switch (hex)
                {
                    case "red": return Color.red;
                    case "green": return Color.green;
                    case "blue": return Color.blue;
                    case "yellow": return Color.yellow;
                    case "black": return Color.black;
                    case "white": return Color.white;
                    case "cyan": return Color.cyan;
                    case "gray": return Color.gray;
                    case "grey": return Color.grey;
                    case "magenta": return Color.magenta;
                    default: return Color.black;
                }
            }
            //16进制颜色
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        /// <summary>
        /// 分割字符串[[[]]]，[[[]]]，[[]]，[]，[[]]
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string[] SplitEventStr(this string content)
        {
            //如果不包含括号的话 说明不需要分割 直接返回
            if (!content.Contains("["))
            {
                return new string[] { content };
            }

            List<string> list = new List<string>();
            int symbolCount = 0;
            int oldIndex = 0;
            for (int i = 0; i < content.Length; i++)
            {
                //左括号+1 右括号-1 一左一右抵消后就为零
                if (content[i] == '[')
                {
                    symbolCount++;
                }
                if (content[i] == ']')
                {
                    symbolCount--;
                }

                //如果括号数量为零了 那这之间的字符串就是一块内容
                if (symbolCount == 0)
                {
                    string str = content.Substring(oldIndex + 1, i - oldIndex - 1);
                    list.Add(str);
                    //因为[],[]  []，[] 间有一个分割用的字符 因此这里需要额外再让i+1 让他直接从下一个 [ 开始
                    i += 1;
                    // ]，[ 右括号是当前当前i的位置是逗号 让他再+1 之后 位置就来到了左括号这里 也就是下一块内容的起点
                    oldIndex = i + 1;
                }
            }
            return list.ToArray();
        }
        /// <summary>
        /// 字符串转整型
        /// </summary>
        /// <param name="selfStr">字符串</param>
        /// <param name="defaulValue">默认值</param>
        /// <returns></returns>
        public static int ToInt(this string selfStr, int defaulValue = 0)
        {
            var retValue = defaulValue;
            //转换成功返回retValue，转换不成功直接返回默认值
            return int.TryParse(selfStr, out retValue) ? retValue : defaulValue;
        }

        public static float ToFloat(this string selfStr, float defaulValue = 0)
        {
            //如果字符串为空 则返回默认值
            if (string.IsNullOrEmpty(selfStr)) return defaulValue;
            //获取当前线程的文化信息，这是为了处理不同语言环境下浮点数的表示方式（小数点或者逗号作为小数分隔符）
            CultureInfo culture = Thread.CurrentThread.CurrentCulture;
            if (selfStr.Contains("."))
            {
                return float.Parse(selfStr.Replace(".", culture.NumberFormat.NumberDecimalSeparator));
            }
            else
            {
                return float.Parse(selfStr);
            }
        }
        /// <summary>
        /// 字符串转布尔
        /// </summary>
        /// <param name="selfStr"></param>
        /// <returns></returns>
        public static bool ToBool(this string selfStr)
        {
            selfStr = selfStr.Trim();
            return selfStr == "1" || selfStr == "是" || selfStr == "能" || selfStr == "可以" || selfStr == "行" ||
                   selfStr == "yes" || selfStr == "Yes" || selfStr == "YES" || selfStr == "true" || selfStr == "True" || selfStr == "TRUE";
        }

        /// <summary>
        /// 字符串转二维向量
        /// </summary>
        /// <param name="selfStr">字符串</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="safe">安全模式，安全模式会校验一下字符串，去除掉括号等用不到的符号</param>
        /// <returns></returns>
        public static Vector2 ToVector2(this string selfStr, Vector2 defaultValue = default, bool safe = true)
        {
            if (string.IsNullOrEmpty(selfStr)) return defaultValue;
            //去除掉一些用不到的符号
            if (safe)
            {
                selfStr = selfStr.Trim().Replace("(", "").Replace("（", "").Replace(")", "").Replace("）", "").Replace("[", "").Replace("]", "");
            }
            //分割字符串 然后转换成向量
            char c = selfStr.Contains("，") ? '，' : selfStr.Contains(",") ? ',' : '|';
            string[] strs = selfStr.Split(c);
            return new Vector2(strs[0].ToFloat(), strs[1].ToFloat());
        }
        /// <summary>
        /// 安全获取所有参数（自动编码特殊字符）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Encode(string input)
        {
            return  UnityEngine.Networking.UnityWebRequest.EscapeURL(input);
        }
        #endregion

        #region 延迟帧处理

        /// <summary>
        /// 延迟帧处理回调方法
        /// </summary>
        /// <param name="delay">延迟的帧数</param>
        /// <param name="fun">回调方法</param>
        /// <returns></returns>
        public static IEnumerator DelayIEFrame(int delay, Action fun)
        {
            for (int i = 0; i < delay; i++)
            {
                yield return null;
            }
            fun();
        }

        #endregion

        #region 数值处理
        /// <summary>
        /// 数字取小数点后几位（非四舍五入）
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static decimal FormatPercentage(double value, int decimalPlaces)
        {
            decimal tur = 0;
            decimal factor = (decimal)Math.Pow(10, decimalPlaces);
            decimal percentage = (decimal)value;
            decimal truncated = Math.Truncate(percentage * factor) / factor;
            tur = truncated;
            return tur;
        }

        #endregion
    }

    public static class EXDOBezier
    {
        public static Tweener DOBezier( Transform transform, Vector3[] path, float timer,Action<Transform> call)
        {
            BezierPath bezierPath = new BezierPath();
            List<Vector3> c = new List<Vector3>();
            for (int o = 0; o < path.Length; o++)
            {
                if (path[o] != null)
                {
                    Vector3 p = path[o];
                    c.Add(p);
                }
            }
            bezierPath.DeletePath();
            bezierPath.CreateCurve(c);
            return transform.DOPath(bezierPath.pathPoints.ToArray(), timer)
                .OnComplete(()=> {
                    call?.Invoke(transform);
                });
        }
        public class BezierPath
        {
            public List<Vector3> pathPoints;
            private int segments;
            public int pointCount;

            public BezierPath()
            {
                pathPoints = new List<Vector3>();
                pointCount = 50;
            }

            public void DeletePath()
            {
                pathPoints.Clear();
            }

            Vector3 BezierPathCalculation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                float tt = t * t;
                float ttt = t * tt;
                float u = 1.0f - t;
                float uu = u * u;
                float uuu = u * uu;

                Vector3 B = new Vector3();
                B = uuu * p0;
                B += 3.0f * uu * t * p1;
                B += 3.0f * u * tt * p2;
                B += ttt * p3;

                return B;
            }
            public void CreateCurve(List<Vector3> controlPoints)
            {
                segments = controlPoints.Count / 3;

                for (int s = 0; s < controlPoints.Count - 3; s += 3)
                {
                    Vector3 p0 = controlPoints[s];
                    Vector3 p1 = controlPoints[s + 1];
                    Vector3 p2 = controlPoints[s + 2];
                    Vector3 p3 = controlPoints[s + 3];

                    if (s == 0)
                    {
                        pathPoints.Add(BezierPathCalculation(p0, p1, p2, p3, 0.0f));
                    }

                    for (int p = 0; p < (pointCount / segments); p++)
                    {
                        float t = (1.0f / (pointCount / segments)) * p;
                        Vector3 point = new Vector3();
                        point = BezierPathCalculation(p0, p1, p2, p3, t);
                        pathPoints.Add(point);
                    }
                }
            }
        }
    }
}