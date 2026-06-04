using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RenderHeads.Media.AVProVideo;

namespace GameLogic
{
	[Window(UILayer.UI,PlayAnim = true)]
	public partial class Panel_DownloadTip : UIWindow
	{
		private int _userPhotoId;

		//下载类型，0：AVPro缓存，1：保存视频到本地相册，2：保存图片到本地相册
		private int _downloadType;
		private Item_PhotoLoad _photoLoad;
		//是否能免费下载
		private bool _isFreeDownload;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_userPhotoId = (int)userDatas[0];
			}
			_downloadType = 0;
			_photoLoad.LoadUIImg(_userPhotoId);
			UpdateDownloadBtnStatus();
		}
		private void UpdateDownloadBtnStatus()
		{
			int curCount = UserController.Instance.VIPFreeDownloadCount();
			_isFreeDownload = curCount > 0 || curCount == -1;
			mAdicn.SetActive(!_isFreeDownload);
			mFreeDowCount.gameObject.SetActive(_isFreeDownload);
			if (!_isFreeDownload && mAdicn.activeSelf)
			{
				//RV按钮上报
				SDKManager.Instance.LogEvent("RV_Button_Show", "Position", HCRVPositionName.RV_DownloadPicture.ToString());
			}
			if (_isFreeDownload)
			{
				string freeCount = curCount > 0 ? curCount.ToString() : "∞";
				mFreeDowCount.text = freeCount;
			}
		}
		protected override void RegisterEvent()
		{
		}

		private void Download()
		{
			//执行广告下载
			Close();
			GameEvent.Send(EventType.DownloadPicture);
		}

		protected override void OnUpdate()
		{
			
		}
		protected override void Close()
		{
			base.Close();
		}
	}
}
