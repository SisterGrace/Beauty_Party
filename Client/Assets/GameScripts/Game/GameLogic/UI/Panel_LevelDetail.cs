using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_LevelDetail : UIWindow
	{
		private bool _isCompleteAnim = false;
		private bool _isLevelDetailShow = false;
		private Sequence _seq;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_isLevelDetailShow = (bool)userDatas[0];
			}
			mTreasureTitle.SetActive(_isLevelDetailShow);
			mPassTitle.SetActive(!_isLevelDetailShow);
			mLevelKeys.SetActive(_isLevelDetailShow);
			mPassKeys.SetActive(!_isLevelDetailShow);
			mLevelGetRew.SetActive(_isLevelDetailShow);
			mPassGetRew.SetActive(!_isLevelDetailShow);
			mDreamPass.gameObject.SetActive(!_isLevelDetailShow);
			_isCompleteAnim = false;
			_seq = DOTween.Sequence();
			mTitle.localScale = Vector3.zero;
			mPlayGame.localScale = Vector3.zero;
			mjiantou1.localScale = Vector3.zero;
			mKeys.localScale = Vector3.zero;
			mjiantou2.localScale = Vector3.zero;
			mGetRew.localScale = Vector3.zero;
			mDreamPass.localScale = Vector3.zero;
			mtips.localScale = Vector3.zero;
			_seq.Append(mTitle.DOScale(1, 0.2f).SetEase(Ease.OutBounce));
			_seq.Append(mPlayGame.DOScale(1, 0.2f).SetEase(Ease.OutBounce));
			_seq.Append(mjiantou1.DOScale(1, 0.1f).SetEase(Ease.OutBounce));
			_seq.Append(mKeys.DOScale(1, 0.2f).SetEase(Ease.OutBounce));
			_seq.Append(mjiantou2.DOScale(1, 0.1f).SetEase(Ease.OutBounce));
			_seq.Append(mGetRew.DOScale(1, 0.2f).SetEase(Ease.OutBounce));
			if (!_isLevelDetailShow)
			{
				_seq.Append(mDreamPass.DOScale(1, 0.2f).SetEase(Ease.OutBounce));
			}
			_seq.Append(mtips.DOScale(1, 0.2f).SetEase(Ease.OutBounce));
			_seq.OnComplete(() =>
			{
				_isCompleteAnim = true;
			});
		}

		protected override void RegisterEvent()
		{
		}

		protected override void Close()
		{
			base.Close();
			if (_seq != null)
			{
				_seq.Kill();
				_seq = null;
			}
		}
	}
}
