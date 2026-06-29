using UnityEngine;
using UnityEngine.UI;
using Coffee.UIExtensions;
using System.Collections;
namespace GameLogic
{
    /// <summary>
    /// 广告按钮扫光控制器
    /// 自动根据AD图标的显示/隐藏状态控制扫光效果的启用/禁用
    /// </summary>
    public class AdButtonShineController : MonoBehaviour
    {
        [Header("AD图标对象")]
        [Tooltip("用于判断是否显示广告的图标GameObject")]
        public GameObject adIcon;

        [Header("扫光效果组件")]
        [Tooltip("如果不设置，会自动查找按钮上的Item_ShineEffect组件")]
        public Item_ShineEffect shineEffect;

        private bool _lastAdIconState;

        public ShinyEffectForUGUI shinyEffectForUGUI;

        private void Awake()
    {
            // 自动查找扫光组件
            if (shineEffect == null)
            {
                shineEffect = GetComponent<Item_ShineEffect>();
            }
            // 如果GameObject上没有Item_ShineEffect组件，自动添加
            if (shineEffect == null)
            {
                shineEffect = gameObject.AddComponent<Item_ShineEffect>();
                Debug.LogWarning($"[AdButtonShineController] {gameObject.name}: Auto-created Item_ShineEffect");
            }
            shineEffect.SetShineEffect();
            shineEffect.enabled = false;
            // 确保ShinyEffectForUGUI组件存在
            shinyEffectForUGUI = GetComponent<ShinyEffectForUGUI>();
            if (shinyEffectForUGUI == null)
            {
                shinyEffectForUGUI = gameObject.AddComponent<ShinyEffectForUGUI>();
                Debug.LogWarning($"[AdButtonShineController] {gameObject.name}: Auto-created ShinyEffectForUGUI");
            }
            shinyEffectForUGUI.SetShinyEffect();
            shinyEffectForUGUI.enabled = false;
            StartCoroutine(DeactivateAfterDelay());
        }
        IEnumerator DeactivateAfterDelay()
        {
            // yield return new WaitForSeconds(2f); 这行就是暂停点
            // 程序运行到这里会暂停，等待2秒（游戏时间）后，再继续执行后面的代码
            yield return new WaitForSeconds(0.01f);

            // 初始化 _lastAdIconState
            if (adIcon != null)
            {
                _lastAdIconState = adIcon.activeInHierarchy;
            }
            else
            {
                Debug.LogError($"[AdButtonShineController] {gameObject.name}: adIcon is not assigned!");
            }

            // 初始状态
            UpdateShineState();
            update = true;
        }
        bool update = false;
        private void Update()
        {
            if (!update) { return; }
            // 检测AD图标状态变化
            if (adIcon != null)
            {
                bool currentState = adIcon.activeInHierarchy;
                if (currentState != _lastAdIconState)
                {
                    _lastAdIconState = currentState;
                    UpdateShineState();
                }
            }
        }

        private void UpdateShineState()
        {
            if (shineEffect != null && adIcon != null)
            {
                // 只有当AD图标显示时才启用扫光
                bool shouldEnable = adIcon.activeInHierarchy;
                shineEffect.enabled = shouldEnable;
                shinyEffectForUGUI.enabled= shouldEnable;
                Debug.Log($"[AdButtonShineController] {gameObject.name}: adIcon={adIcon.name}, active={shouldEnable}, shineEffect.enabled={shineEffect.enabled}");
            }
            else
            {
                Debug.LogWarning($"[AdButtonShineController] {gameObject.name}: shineEffect={shineEffect != null}, adIcon={adIcon != null}");
            }
        }

        /// <summary>
        /// 手动设置扫光状态（用于特殊情况）
        /// </summary>
        public void SetShineEnabled(bool enabled)
        {
            if (shineEffect != null)
            {
                shineEffect.enabled = enabled;
            }
        }
    }
}
