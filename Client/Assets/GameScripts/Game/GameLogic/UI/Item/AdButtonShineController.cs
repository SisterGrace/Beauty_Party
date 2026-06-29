using UnityEngine;
using UnityEngine.UI;
using Coffee.UIExtensions;
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

        private void Awake()
        {
            // 自动查找扫光组件
            if (shineEffect == null)
            {
                shineEffect = GetComponent<Item_ShineEffect>();
            }

            // 如果按钮上没有扫光组件，自动添加
            if (shineEffect == null)
            {
                shineEffect = gameObject.AddComponent<Item_ShineEffect>();
                shineEffect.SetShineEffect();
            }

            // 确保ShinyEffectForUGUI组件存在
            var shinyEffect = GetComponent<ShinyEffectForUGUI>();
            if (shinyEffect == null)
            {
                shinyEffect = gameObject.AddComponent<ShinyEffectForUGUI>();
                // 设置与主界面"进入关卡"按钮相同的参数
                shinyEffect.SetShinyEffect();
                
            }

            // 初始状态
            UpdateShineState();
        }

        private void Update()
        {
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
                shineEffect.enabled = adIcon.activeInHierarchy;
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
