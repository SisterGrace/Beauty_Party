using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Sirenix.OdinInspector;
using TK_Engine;

namespace GameLogic
{
    public class ShowPropVfx : MonoBehaviour
    {
        public ParticleSystem EffectParent;
        private ParticleSystem.MainModule mainModule;
        [LabelText("是否播放Spine动画")]
        public bool IsShowSpine;
        [ShowIf("IsShowSpine")]
        public SkeletonGraphic SpineGic;
        [ShowIf("IsShowSpine")]
        public string SpineName;
        
        private bool _isHide = false;
        private Action _callback;
        
        public void PlayPropVfx(bool isHide, Action callback=null)
        {
            _isHide = isHide;
            if (EffectParent != null)
            {
                mainModule = EffectParent.main;   //获取MainModule
                mainModule.stopAction = ParticleSystemStopAction.Callback; 
                EffectParent.Play();
            }
            _callback = callback;
            if (IsShowSpine)
            {
                SpineGic.Initialize(true);
                SpineController.PlayAnim(SpineGic, 0,SpineName,false,"", () =>
                {
                    _callback?.Invoke();
                });
            }
        }

        private void OnParticleSystemStopped()
        {
            Log.Info("特效回调");
            if (_isHide)
            {
                FlySystem.Instance.HideObj(gameObject);
            }
            
        }
    }
}
