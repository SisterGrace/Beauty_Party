using Coffee.UIExtensions;
using UnityEngine;

namespace GameLogic
{
    public class Item_ShineEffect : MonoBehaviour
    {
        [Header("扫光速度(值越大越快)")]
        public float Speed = 1f;

        [Header("每次扫光结束后的等待时间")]
        public float Interval = 1f;

        [Header("起始值")]
        public float FromValue = 0f;

        [Header("结束值")]
        public float ToValue = 1f;

        private ShinyEffectForUGUI _shinyEffect;
        private float _timer;
        private float _waitTimer;
        private bool _isWaiting;

        private void OnEnable()
        {
            if (_shinyEffect == null)
                _shinyEffect = GetComponent<ShinyEffectForUGUI>();

            ResetState();
        }
        public void SetShineEffect()
        {
            Speed = 0.7f;
            Interval = 0.4f;
        }
        private void ResetState()
        {
            _timer = 0f;
            _waitTimer = 0f;
            _isWaiting = false;
            if (_shinyEffect != null)
                _shinyEffect.location = FromValue;
        }

        private void Update()
        {
            if (_shinyEffect == null) return;

            if (_isWaiting)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= Interval)
                {
                    _isWaiting = false;
                    _timer = 0f;
                }
                return;
            }

            float duration = Mathf.Abs(ToValue - FromValue) / Mathf.Max(Speed, 0.001f);
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / duration);
            float value = Mathf.Lerp(FromValue, ToValue, t);
            _shinyEffect.location = value;

            if (t >= 1f)
            {
                _isWaiting = true;
                _waitTimer = 0f;
            }
        }
    }
}
