using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Widget_GachaAnimation
	{

		protected MediaPlayer mAvMediaPlayer;
		protected UIButton mSkip;

		protected override void ScriptGenerator()
		{
			mAvMediaPlayer = FindChildComponent<MediaPlayer>("AV_AvMediaPlayer");
			mSkip = FindChildComponent<UIButton>("Btn_Skip");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			/* _normalPhotoEffect = mchouka01.GetComponent<UIParticle>();
			_pityPhotoEffect = mchouka02.GetComponent<UIParticle>(); */
			mSkip.AddListener(ClickSkip);
			mAvMediaPlayer.Events.AddListener(OnMediaPlayerEvent);
		}
		private void ClickSkip(GameObject obj)
		{
			Hide();
		}
		#endregion
	}
}
