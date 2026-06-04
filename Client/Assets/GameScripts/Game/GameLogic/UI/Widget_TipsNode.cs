using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using DG.Tweening;
using GameBase;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_TipsNode : UIWidget
	{
		public bool isCanUse;
		Sequence TipsTween;
		public void OnOpen(string tips,Transform tr,int openNode,params string[] userDatas)
		{
			gameObject.Show();
			mnode1.gameObject.SetActive(openNode == 1);
			mnode2.gameObject.SetActive(openNode == 2);
			mnode3.gameObject.SetActive(openNode == 3);
			switch (openNode)
			{
				case 1:
					ShowTips1(tips, tr, userDatas);
					break;
				case 2:
					ShowTips2(tips, tr, userDatas);
					break;
				case 3:
					ShowTips3(tips, userDatas);
					break;
			}
		}

		private void ShowTips1(string tips,Transform tr,params string[] userDatas)
		{
			isCanUse = true;
			//对于一些UI的锚点Picot发生了改变，这是要得到基于锚点（0.5，0.5）的位置
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
			mTipsTxt.SetLanguage(tips,userDatas);
		}

		private void ShowTips2(string tips, Transform tr, params string[] userDatas)
		{
			isCanUse = true;
			//对于一些UI的锚点Picot发生了改变，这是要得到基于锚点（0.5，0.5）的位置
			RectTransform rec= tr as RectTransform;
			Vector3 newPos = tr.position;
			if (rec.pivot != Vector2.one * 0.5f)
			{
				Vector3[] corners = new Vector3[4];
				rec.GetWorldCorners(corners);
				newPos = (corners[0] + corners[2]) * 0.5f;
			}
			mnode2.position = new Vector3(newPos.x, newPos.y + 0.5f, newPos.z);
			//计算气泡框的偏移
			Vector3[] tipCorners = new Vector3[4];
			mtipsBg2.GetWorldCorners(tipCorners);
			// 转换为屏幕坐标
			Vector2 min = GameModule.UI.UICamera.WorldToScreenPoint(tipCorners[0]);
			Vector2 max = GameModule.UI.UICamera.WorldToScreenPoint(tipCorners[2]);
			// 计算偏移量
			Vector2 offset = Vector2.zero;
			float margin = 10f; // 安全边距
			// 水平方向检测
			if (max.x > Screen.width) offset.x = Screen.width - max.x - margin;
			else if (min.x < 0) offset.x = -min.x + margin;

			if (offset != Vector2.zero)
			{
				Vector3 worldOffest =
					GameModule.UI.UICamera.ScreenToWorldPoint(
						GameModule.UI.UICamera.WorldToScreenPoint(mtipsBg2.position) + (Vector3)offset) -
					mtipsBg2.position;
				mtipsBg2.position += worldOffest;
			}
			
			mnode2.localScale = Vector3.zero;
			ShowAnim(mnode2);
			mTipsTxt2.SetLanguage(tips,userDatas);
		}
		private void ShowTips3(string tips, params string[] userDatas)
		{
			
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
		public void Hide()
		{
			gameObject.Hide();
			if (TipsTween != null)
			{
				TipsTween.Kill();
				TipsTween = null;
			}
		}
	}
}
