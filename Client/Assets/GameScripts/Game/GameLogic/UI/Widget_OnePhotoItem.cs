using TK_Engine;
using UnityEngine;
using Y_Base;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OnePhotoItem : UILoopGridItemWidget, IListDataItem<PhotoItemInfo>
	{
		private PhotoItemInfo _infoData;
		private bool _isLike;
		private Item_PhotoLoad _photoLoader;
		public bool IsSucceedShow => _photoLoader != null && (!_photoLoader.IsLoading && !_photoLoader.IsFailed);

		protected override void RegisterEvent()
		{
			base.RegisterEvent();
			GameEvent.AddEventListener<int, bool>(EventType.RefreshPhotoLikeStatus, OnRefreshPhotoLikeStatus);
		}

		private void OnRefreshPhotoLikeStatus(int photoId, bool isLike)
		{
			if (_infoData != null && _infoData.Id == photoId)
			{
				_isLike = isLike;
				_infoData.isLiked = isLike;
				mLikeNode.SetActive(_isLike);
			}
		}

		private void EnsurePhotoLoader()
		{
			if (_photoLoader != null) return;
			_photoLoader = gameObject.AddComponent<Item_PhotoLoad>();
			_photoLoader.Init(mPhotoIcon, mLoadingNode, mloadIcon);
		}

		public void SetItemData(PhotoItemInfo data)
		{
			EnsurePhotoLoader();

			_infoData = data;
			_isLike = data.isLiked;
			UpdateItem(data.Index);

			mPhotoIcon.gameObject.Hide();
			_photoLoader.OnLoadSuccess = OnPhotoLoadSuccess;
			_photoLoader.OnLoadFailed = () => { mPhotoIcon.gameObject.Show(); };
			_photoLoader.LoadPhotoByUrl(data.Imgurl);

			bool isLook = UserController.Instance.GetUserPhotoData(data.Id).IsLook;
			mRedNot.SetActive(!isLook);
			mLike.gameObject.SetActive(isLook);
		}

		private void OnPhotoLoadSuccess()
		{
			mPhotoIcon.gameObject.Show();
			mLikeNode.SetActive(_infoData.isLiked);
		}

		public override void UpdateItem(int index)
		{
			base.UpdateItem(index);
		}

		private void SetPhotoLike()
		{
			_isLike = !_isLike;
			UserController.Instance.SetUserPhotoLike(_infoData.Id, _isLike);
			_infoData.isLiked = _isLike;
			mLikeNode.SetActive(_isLike);
			if (_isLike)
			{
				mLike.PlayEffect();
			}
		}

		public void SetRedNot(bool isShow)
		{
			mRedNot.SetActive(isShow);
			mLike.gameObject.SetActive(!isShow);
			UserController.Instance.SetUserPhotoLook(_infoData.Id, !isShow);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GameEvent.RemoveEventListener<int, bool>(EventType.RefreshPhotoLikeStatus, OnRefreshPhotoLikeStatus);
		}
	}

	public class PhotoItemInfo
	{
		public int Index;
		public int Id;
		public int ABType;
		public string Imgurl;
		public string VideoUrl;
		public bool isLiked;
		public bool IsVideo;

		public PhotoItemInfo(int index, int id, int abtype, string imgurl, string videourl, bool isliked, bool isvideo)
		{
			Index = index;
			Id = id;
			ABType = abtype;
			Imgurl = imgurl;
			VideoUrl = videourl;
			isLiked = isliked;
			IsVideo = isvideo;
		}
	}

	public enum PhotoItemType
	{
		All = 0,
		Liked = 1,
	}
}
