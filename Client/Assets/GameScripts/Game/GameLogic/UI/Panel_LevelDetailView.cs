using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_LevelDetail
	{

		protected UIButton mmask;
		protected RectTransform mTitle;
		protected RectTransform mPlayGame;
		protected RectTransform mjiantou1;
		protected RectTransform mKeys;
		protected RectTransform mjiantou2;
		protected RectTransform mGetRew;
		protected RectTransform mDreamPass;
		protected RectTransform mtips;
		protected GameObject mPassTitle;
		protected GameObject mTreasureTitle;
		protected GameObject mLevelKeys;
		protected GameObject mPassKeys;
		protected GameObject mLevelGetRew;
		protected GameObject mPassGetRew;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mTitle = FindChildComponent<RectTransform>("Rect_Title");
			mPlayGame = FindChildComponent<RectTransform>("Rect_PlayGame");
			mjiantou1 = FindChildComponent<RectTransform>("Rect_jiantou1");
			mKeys = FindChildComponent<RectTransform>("Rect_Keys");
			mjiantou2 = FindChildComponent<RectTransform>("Rect_jiantou2");
			mGetRew = FindChildComponent<RectTransform>("Rect_GetRew");
			mDreamPass = FindChildComponent<RectTransform>("Rect_DreamPass");
			mtips = FindChildComponent<RectTransform>("Rect_tips");
			mPassTitle = FindChild("Rect_Title/Obj_PassTitle").gameObject;
			mTreasureTitle = FindChild("Rect_Title/Obj_TreasureTitle").gameObject;
			mLevelKeys = FindChild("Rect_Keys/Obj_LevelKeys").gameObject;
			mPassKeys = FindChild("Rect_Keys/Obj_PassKeys").gameObject;
			mLevelGetRew = FindChild("Rect_GetRew/Obj_LevelGetRew").gameObject;
			mPassGetRew = FindChild("Rect_GetRew/Obj_PassGetRew").gameObject;
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mmask.AddListener(ClickClose);
		}

		private void ClickClose(GameObject obj)
		{
			if (_isCompleteAnim)
			{
				Close();
			}
		}
		#endregion
	}
}
