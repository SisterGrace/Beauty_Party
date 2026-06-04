using Coffee.UIExtensions;
using GameBase;
using TK_Engine;
using UnityEngine;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_Collectphotos : UIWindow
	{
		private Item_PhotoLoad _photoLoad;
		private int _photoId;
		private int _photoCount;
		//普通照片特效
		private UIParticle _normalPhotoEffect;
		//保底照片特效
		private UIParticle _pityPhotoEffect;
		private GachaResult _result;

		protected override void OnRefresh()
		{
			if (userDatas.Length > 1)
			{
				_photoId = (int)userDatas[0];
				_photoCount = (int)userDatas[1];

			}
			else if (userDatas.Length == 1)
			{
				_result = (GachaResult)userDatas[0];
				_photoId = _result.PhotoId;
				_photoCount = 1;
			}
			_photoLoad.LoadUIImg(_photoId);
			SetPhotoEffect();
		}

		protected override void RegisterEvent()
		{
		}
		private void SetPhotoEffect()
		{
			_pityPhotoEffect.gameObject.Hide();
			_normalPhotoEffect.gameObject.Hide();
			if (_result.PhotoId != 0)
			{
				UIParticle effect = _result.IsHighestPool ? _pityPhotoEffect : _normalPhotoEffect;
				effect.gameObject.Show();
				effect.Play();
			}
		}
		protected override void Close()
		{
			base.Close();
			// 从 OrderPanel 队列中移除，并触发下一个面板显示
			GameModule.UI.RemoveOrderPanel<Panel_Collectphotos>();
		}
	}
}
