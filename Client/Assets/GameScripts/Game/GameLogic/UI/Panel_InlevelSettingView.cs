using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_InlevelSetting
	{

		protected UIButton mMask;
		protected UIButton mExitGame;
		protected GameObject mS2;
		protected RectTransform mSoundEffS2;
		protected RectTransform mAudioS2;
		protected RectTransform mShakeS2;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<UIButton>("Btn_Mask");
			mExitGame = FindChildComponent<UIButton>("Obj_S2/AllSettingBtns/Btn_ExitGame");
			mS2 = FindChild("Obj_S2").gameObject;
			mSoundEffS2 = FindChildComponent<RectTransform>("Obj_S2/AllSettingBtns/Rect_SoundEffS2");
			mAudioS2 = FindChildComponent<RectTransform>("Obj_S2/AllSettingBtns/Rect_AudioS2");
			mShakeS2 = FindChildComponent<RectTransform>("Obj_S2/AllSettingBtns/Rect_ShakeS2");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mMask.AddListener(ClickMask);
			mExitGame.AddListener(ClickExitGame);
		}
		private void ClickMask(GameObject obj)
		{
			Close();
		}
		private void ClickExitGame(GameObject obj)
		{
			/* Close();
			GameModule.UI.ShowUI<Panel_ExitLevel>(); */
			ExitGame();
		}
		#endregion
	}
}
