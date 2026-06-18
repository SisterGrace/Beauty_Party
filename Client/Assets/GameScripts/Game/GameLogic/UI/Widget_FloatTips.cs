using TK_Engine;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TK_Engine.Localization;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_FloatTips : UIWidget
	{
		private Vector2 pos;
		public bool isCanUse;
		private Sequence sequence;
		private Localize _translate;
		public void Open(string txt,bool isTranslate = false,params string[] userDatas)
		{
			if (!isTranslate)
			{
				mContent.text = txt;
			}
			else
			{
				if (_translate == null)
				{
					_translate = mContent.gameObject.GetOrAddComponent<Localize>();
				}

				if (userDatas!= null && userDatas.Length > 0)
				{
					_translate.MySetTerm(txt, userDatas);
				}
				else
				{
					_translate.SetTerm(txt);
				}
				
			}
			pos = rectTransform.anchoredPosition;
			gameObject.Show();
			rectTransform.anchoredPosition = new Vector2(pos.x, pos.y - 243f);
			/*mBg.DOFade(0, 0);
			mContent.DOFade(0, 0);*/
			PlayAnim();
		}

		public void Hide()
		{
		}
		private void PlayAnim()
		{
			isCanUse = false;
			if (sequence == null)
			{
				sequence = DOTween.Sequence();
				sequence.Append(rectTransform.DOAnchorPosY(pos.y, 0.5f));
				sequence.Join(mBg.DOFade(0.5f, 0.5f));
				sequence.Join(mContent.DOFade(1f, 0.5f));
				sequence.AppendInterval(1.5f);
				sequence.Append(rectTransform.DOAnchorPosY(243f, 0.5f));
				sequence.Join(mBg.DOFade(0, 0.5f));
				sequence.Join(mContent.DOFade(0, 0.5f));
				sequence.OnComplete(() =>
				{
					mBg.DOFade(0, 0.1f);
					mContent.DOFade(0, 0.1f);
					rectTransform.anchoredPosition = pos;
					isCanUse = true;
					gameObject.SetActive(false);
				});
				sequence.SetAutoKill(false);
			}
			else
			{
				sequence.Restart();
			}
			sequence.Play();
		}
	}
}
