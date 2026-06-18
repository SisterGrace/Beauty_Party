using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;
using Config.common;
using Y_Base;

namespace GameLogic
{
    public class Item_OnePropBomb : MonoBehaviour
    {
        public RectTransform rect;
        public GameObject PropNode;
        public Image icon;
        public GameObject Frame;
        public ParticleSystem explosion;

        private Transform _endTr;
        private Action<Item_OnePropBomb> _onCompleted;
        public void InitOnePropBomb(Vector2 startPos,OneRewardData data, Transform endTr, Action<Item_OnePropBomb> onCompleted)
        {
            rect.anchoredPosition = startPos;
            _onCompleted = null;
            _endTr = endTr;
            _onCompleted = onCompleted;
            var cfg = ConfigLoader.Instance.Tables.TbCfgGamePropInfo.GetById(data.Id);
            icon.SetSprite("icon"+cfg.Icon,true);
            ShowOnePropBomb();
        }

        private void ShowOnePropBomb()
        {
            PropNode.transform.localScale = Vector3.zero;
            explosion.Play();
            PropNode.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce).OnComplete(() =>
            {
                MoveToTarget();
            });
        }

        private void MoveToTarget()
        {
            transform.DOMove(_endTr.position, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                _onCompleted?.Invoke(this);
            });
        }
    }
}
