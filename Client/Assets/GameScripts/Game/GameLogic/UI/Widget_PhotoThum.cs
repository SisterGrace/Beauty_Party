using TK_Engine;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Y_Base;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_PhotoThum : UIWidget
	{
		private UILoopListWidget<Widget_OneShrinkPic, OneShrinkPicInfo> _shrinkPicList;
		private List<OneShrinkPicInfo> _shrinkPicInfos = new List<OneShrinkPicInfo>();

		public List<int> _photoIds;
		Sequence TipsTween;
		public void Open(List<int> photoIds, Transform tr)
		{
			gameObject.Show();
			_photoIds = photoIds;
			CreateShrinkPicList();
			//设置缩略图的位置
			RectTransform rec= tr as RectTransform;
			Vector3 newPos = tr.position;
			if (rec.pivot != Vector2.one * 0.5f)
			{
				Vector3[] corners = new Vector3[4];
				rec.GetWorldCorners(corners);
				newPos = (corners[0] + corners[2]) * 0.5f;
			}

			mnode1.position = new Vector3(newPos.x, newPos.y + 0.5f, newPos.z);
			mnode1.localScale = Vector3.zero;
			ShowAnim(mnode1);
		}
		private void CreateShrinkPicList()
		{
			if (_shrinkPicList == null)
			{
				_shrinkPicList = CreateWidget<UILoopListWidget<Widget_OneShrinkPic, OneShrinkPicInfo>>(mAllPackPics);
				_shrinkPicList.itemBase = mOneShrinkPic;
			}
			_shrinkPicInfos.Clear();
			//展示当前卡包所有的图
			for (int i = 0; i < _photoIds.Count; i++)
			{
				var photoId = _photoIds[i];
				_shrinkPicInfos.Add(new OneShrinkPicInfo(photoId));
			}
			_shrinkPicList.SetDatas(_shrinkPicInfos);
		}
		public void Hide()
		{
			gameObject.Hide();
			if (TipsTween != null)
			{
				TipsTween.Kill();
				TipsTween = null;
			}
		}
		private void ShowAnim(Transform tr)
		{
			if (TipsTween != null)
			{
				TipsTween.Kill();
				TipsTween = null;
			}
			TipsTween = DOTween.Sequence();
			TipsTween.Append(tr.DOScale(1, 0.1f));
			TipsTween.Append(tr.DOScale(1.2f, 0.1f));
			TipsTween.Append(tr.DOScale(1f, 0.1f));
			TipsTween.Append(tr.DOScale(1f, 1.5f));
		}
	}
}
