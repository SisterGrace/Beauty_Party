using System.Collections;
using System.Collections.Generic;
using Config.common;
using DG.Tweening;
using Spine.Unity;
using TK_Engine;
using UnityEngine;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
    public class PropYaoba : MonoBehaviour
    {
        public SkeletonGraphic Spine;
        public ParticleSystem Par;
        public List<RectTransform> InitPos;
        public Transform TempCardParent;
        private int _cardType;
        private Transform _cardParent;
        private List<CardCell> tempCards = new List<CardCell>();
        private ParticleSystem.MainModule mainModule;
        public void InitYaoba(int type,Transform parent)
        {
            tempCards.Clear();
            _cardType = type;
            _cardParent = parent;
            Utility.Unity.StartCoroutine(CreateCard());
        }

        private IEnumerator CreateCard()
        {
            GameEvent.Send(EventType.DoPropAnim,PropType.DrawCard,true,0f);
            AudioManager.Play(AudioType.Sound,AudioDef.UseLaba);
            SpineController.PlayAnim(Spine,0,"display",false,"", () =>
            {
                GameEvent.Send(EventType.DoPropAnim,PropType.DrawCard,false,0f);
                FlySystem.Instance.HideObj(gameObject);
            });
            Par.Play();
            yield return new WaitForSeconds(0.9f);
            AudioManager.Play(AudioType.Sound,AudioDef.Labafruit);
            for (int i = 0; i < 3; i++)
            {
                CardCell curCard = CardsController.Instance.CreatOneCard(new CoordData(0,0,1,new SpecialData(LockCardType.None)),_cardParent,_cardType,false);
                tempCards.Add(curCard);
                curCard.ShowBg(false);
                //这里把IsInSolt设置为true，是为了让卡片在出牌时，不被点击处理，当进入到暂存区会自动设置为false
                curCard.IsInSolt = true;
                Vector3 pos = InitPos[i].position;
                Vector2 UIPos = InitPos[i].anchoredPosition;
                curCard.transform.position = new Vector3(pos.x, pos.y+0.1f, pos.z);
                Sequence seq = DOTween.Sequence();
                seq.Append(curCard.CardRect.DOAnchorPosY(UIPos.y - 30f, 0.1f));
                seq.Append(curCard.CardRect.DOAnchorPosY(UIPos.y +15f, 0.1f));
                seq.Append(curCard.CardRect.DOAnchorPosY(UIPos.y, 0.1f));
                yield return new WaitForSeconds(0.4f);
            }
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < tempCards.Count; i++)
            {
                tempCards[i].ShowBg(true);
                CardsController.Instance.StoreSlot.StoreCard(tempCards[i], i);
                yield return new WaitForSeconds(0.1f);
            }
        }
        
    }
}
