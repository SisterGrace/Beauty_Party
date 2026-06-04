using System;
using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameBase;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_AllPhotoItems : ChildPageBase
	{
		//循环网格布局
		private UILoopGridWidget<Widget_OnePhotoItem, PhotoItemInfo> _photoGrid;
		private List<PhotoItemInfo> _dataList = new List<PhotoItemInfo>();
		private PhotoItemType _curTabType;
		private List<UserPhotoData> _curLevelALLdata;
		private List<UserPhotoData> _curLikedALLdata;
		protected override void OnRefresh()
		{
			base.OnRefresh();
			//CreatItems();
			CreatCycleItems();
		}
		protected override void RegisterEvent()
		{
			base.RegisterEvent();
			GameEvent.AddEventListener(EventType.RefreshPhotoTab, RefreshPhotoTab);
			GameEvent.AddEventListener<PhotoItemInfo>(EventType.SetOnePhotoRedPoint, RefreshPhotoRedPoint);
		}
		public override void OnPageShowed(int oldShowType, int newShowType)
		{
			base.OnPageShowed(oldShowType, newShowType);
			if (m_shareObjData != null)
			{
				SetClassificationsData((PhotoItemType)newShowType);
			}
		}
		//创建循环列表
		private void CreatCycleItems()
		{
			_photoGrid = CreateWidget<UILoopGridWidget<Widget_OnePhotoItem, PhotoItemInfo>>(mallINode);
			_photoGrid.itemBase = mOnePhotoItem;
			_photoGrid.funcOnItemClick += index =>
			{
				var item = _photoGrid.GetItem(index);
				if (item.IsSucceedShow)
				{
					//打开图片详情界面
					var info = _photoGrid.GetData(index);
					GameModule.UI.ShowUI<Panel_PhotoDetails>(info, _photoGrid.datas);
					//刷新红点
					RefreshPhotoRedPoint(info);
				}
				else
				{
					GameEvent.Send(EventType.ShowTips, "uitps_loading_tips2", true);
				}

			};
		}
		private void SetAllItemData(List<UserPhotoData> tDatas)
		{
			_dataList.Clear();
			//更新当前页数据
			var curPageData = tDatas;
			if (curPageData == null) return;
			for (int i = curPageData.Count - 1; i >= 0; i--)
			{
				var item = curPageData[i];
				var cfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(item.Id);
				string imgPath = PropsController.GetImgUrl(cfg.ImageID, cfg.ImageType, item.ABType);
				string videoPath = PropsController.GetVideoUrl(cfg.ImageID, item.ABType, cfg.IsVideo);
				PhotoItemInfo info = new PhotoItemInfo(_dataList.Count, item.Id, item.ABType, imgPath, videoPath, item.IsLike, cfg.IsVideo);
				_dataList.Add(info);
			}
			_photoGrid.SetDatas(_dataList);
		}
		/// <summary>
		/// 设置当前页签数据
		/// </summary>
		/// <param name="type"></param>
		private void SetClassificationsData(PhotoItemType type)
		{
			_dataList.Clear();
			_curTabType = type;
			switch (type)
			{
				case PhotoItemType.All:
					_curLevelALLdata = null;
					if (_curLevelALLdata == null)
					{
						_curLevelALLdata = UserController.Instance.GetUserPhotoDataList(0, isGetAll: true);
					}
					SetAllItemData(_curLevelALLdata);
					break;

				case PhotoItemType.Liked:
					_curLikedALLdata = null;
					if (_curLikedALLdata == null)
					{
						_curLikedALLdata = UserController.Instance.GetUserPhotoDataList(0, true);
					}
					SetAllItemData(_curLikedALLdata);
					break;

			}
		}
		private void RefreshPhotoTab()
		{
			List<UserPhotoData> tDatas = null;
			switch (_curTabType)
			{
				case PhotoItemType.All:
					tDatas = _curLevelALLdata;
					break;
				case PhotoItemType.Liked:
					tDatas = _curLikedALLdata;
					break;
			}
			SetAllItemData(tDatas);
		}

		private void RefreshPhotoRedPoint(PhotoItemInfo info)
		{
			var item = _photoGrid.GetItem(info.Index);
			item.SetRedNot(false);
			//这里还得刷新页签的红点
			GameEvent.Send(EventType.SwitchRedPoint, (int)_curTabType);
		}

		public void Hide()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener(EventType.RefreshPhotoTab, RefreshPhotoTab);
			GameEvent.RemoveEventListener<PhotoItemInfo>(EventType.SetOnePhotoRedPoint, RefreshPhotoRedPoint);
		}

	}
}
