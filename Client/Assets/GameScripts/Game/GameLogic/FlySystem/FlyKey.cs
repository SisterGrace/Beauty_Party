using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GameBase;

namespace GameLogic
{
    public class FlyKey : MonoBehaviour
    {
        public List<RectTransform>  Rects;
        public List<CanvasGroup> PropCanvasGroups;
        private List<Transform> m_PropTransforms;

        public void InitFlyKey(List<Transform> propTransforms)
        {
            m_PropTransforms = propTransforms;
            for (int j = 0; j < Rects.Count; j++)
            {
                int index = j;
                Rects[index].gameObject.Hide();
                if (index < propTransforms.Count)
                {
                    Rects[index].gameObject.Show();
                    RectTransform rect = Rects[index];
                    rect.anchoredPosition = Vector2.zero;
                    PropCanvasGroups[index].alpha = 1;
                    Vector3 pos = propTransforms[index].position;
                    Vector3 newPos = new Vector3(index == 0 ? pos.x + 0.5f : pos.x - 0.5f, pos.y, pos.z);
                    rect.transform.position = newPos;
                }
            }
           
            MoveToUp();
        }
        private void MoveToUp()
        {
            for (int i = 0; i < m_PropTransforms.Count; i++)
            {
                int index = i;
                RectTransform rect = Rects[index];
                CanvasGroup PropCanvasGroup = PropCanvasGroups[index];
                float moveY = rect.anchoredPosition.y + 100f;
                
                PropCanvasGroup.DOFade(0, 0.7f).SetEase(Ease.InBack);
                rect.DOAnchorPosY(moveY, 0.7f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (index == 0)
                    {
                        Close();
                    }
                });
            }
            
        }
        private void Close()
        {
            transform.localPosition = Vector3.one * 999;
            FlySystem.Instance.HideObj(gameObject);
        }
    }
}
