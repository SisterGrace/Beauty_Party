using TK_Engine;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using DG.Tweening;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_OneSelectPhoto : UILoopItemWidget,IListDataItem<OneSelectPhotoInfo>
	{
		public bool _isLoad => _photoLoader != null && _photoLoader.IsLoading;
		private OneSelectPhotoInfo _infoData;
		private Item_PhotoLoad _photoLoader;
		
		#region 选中动画相关参数
		private RectTransform _contentRoot;     // 内容容器（SelectBtn），用于动画移动
		private Vector3 _originalPos;           // 原始位置
		private bool _hasRecordedPos = false;   // 是否已记录原始位置
		private const float SelectMoveDistance = 40f;  // 选中时向上移动的距离
		private const float AnimDuration = 0.25f;      // 动画持续时间
		private Tweener _selectTweener;         // 当前动画Tweener
		#endregion

		private void EnsurePhotoLoader()
		{
			if (_photoLoader != null) return;
			_photoLoader = gameObject.AddComponent<Item_PhotoLoad>();
			_photoLoader.Init(mLevelIcon, mloading, mloadIcon);
		}

		public void SetItemData(OneSelectPhotoInfo data)
		{
			EnsurePhotoLoader();
			UpdateItem(data.Index);
			//boos关卡不需要刷新按钮
			mRefresh.gameObject.SetActive(!data.IsBoosPhoto);
			_infoData = data;
			mFrame.Hide();
			_photoLoader.OnLoadSuccess = () => { mFrame.Show(); };
			_photoLoader.OnLoadFailed = () => { mFrame.Show(); };
			_photoLoader.LoadPhotoByUrl(data.Imgurl);
			SetItem(data.Index);
			SetRefreshButtonState(data.RefreshState);
		}
		public override void UpdateSelect()
		{
			base.UpdateSelect();
			//如果是Boss关卡不需要执行选中
			if (_infoData.IsBoosPhoto) return;
			
			// 初始化内容容器和记录原始位置
			InitContentRoot();
			
			// 停止之前的动画，避免冲突
			_selectTweener?.Kill();
			
			//根据是否选中设置Item的选中动画
			if (IsSelected())
			{
				Vector3 targetPos = _originalPos + new Vector3(0, SelectMoveDistance, 0);
				mPitchOn.Show();
				// 如果已经在目标位置，不播放动画
				if (Vector3.Distance(_contentRoot.anchoredPosition3D, targetPos) < 0.1f)
				{
					return;
				}
				_selectTweener = _contentRoot.DOAnchorPos3D(targetPos, AnimDuration)
					.SetEase(Ease.OutBack);
			}
			else
			{
				mPitchOn.Hide();
				// 如果已经在原始位置，不播放动画
				if (Vector3.Distance(_contentRoot.anchoredPosition3D, _originalPos) < 0.1f)
				{
					return;
				}
				_selectTweener = _contentRoot.DOAnchorPos3D(_originalPos, AnimDuration)
					.SetEase(Ease.OutQuad);
			}
		}
		
		/// <summary>
		/// 初始化内容容器
		/// </summary>
		private void InitContentRoot()
		{
			if (!_hasRecordedPos)
			{
				_contentRoot = transform.GetChild(0) as RectTransform;
				_originalPos = _contentRoot.anchoredPosition3D;
				_hasRecordedPos = true;
			}
		}
		
		/// <summary>
		/// 初始化默认选中位置（不播放动画，用于面板首次打开）
		/// </summary>
		public void InitSelectPosition()
		{
			if (_infoData.IsBoosPhoto) return;
			
			InitContentRoot();
			
			Vector3 targetPos = _originalPos + new Vector3(0, SelectMoveDistance, 0);
			_contentRoot.anchoredPosition3D = targetPos;
		}

		private void SetItem(int index)
		{
			mBM.SetActive(index != 0);
			mAdNode.SetActive(index == 1 || index == 2);
			mVipNode.Hide();
			string bgIcon = "bg_gqzp_xiao";
			if (_infoData.BoosLevel)
			{
				rectTransform.sizeDelta = new Vector2(756, 1284);
				bgIcon = "bg_gqzp_da";
			}
			else
			{
				rectTransform.sizeDelta = new Vector2(378, 621);
			}
			Image bg = transform.GetChild(0).GetComponent<Image>();
			bg.sprite = GameModule.Resource.LoadAsset<Sprite>(bgIcon);
		}
		public void SetRefreshButtonState(int state)
		{
			if (_infoData.IsBoosPhoto) return;
			switch (state)
			{
				case 0:
					mRefresh.gameObject.Show();
					mAdRefresh.Hide();
					break;
				case 1:
					mRefresh.gameObject.Show();
					mAdRefresh.Show();
					SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_Refresh.ToString());
					break;
				case 2:
					mRefresh.gameObject.Hide();
					break;
			}
		}
		public override void UpdateItem(int index)
		{
			base.UpdateItem(index);
		}
		
		protected override void OnDestroy()
		{
			base.OnDestroy();
			_selectTweener?.Kill();
			_selectTweener = null;
		}
	}

	public struct OneSelectPhotoInfo
	{
		public int Index;
		public int Id;
		public int ABType;
		public string Imgurl;
		public string VideoUrl;
		public bool BoosLevel;
		public int Picture_Type;
		public bool IsVideo;
		public bool IsBoosPhoto;
		public int RefreshState;
		public OneSelectPhotoInfo(int index, int id, int abType, string imgurl, string videoUrl, bool boosLevel, int picture_Type, bool isVideo, bool isBoosPhoto, int refreshState)
		{
			Index = index;
			Id = id;
			ABType = abType;
			Imgurl = imgurl;
			VideoUrl = videoUrl;
			BoosLevel = boosLevel;
			Picture_Type = picture_Type;
			IsVideo = isVideo;
			IsBoosPhoto = isBoosPhoto;
			RefreshState = refreshState;
		}
	}
}
