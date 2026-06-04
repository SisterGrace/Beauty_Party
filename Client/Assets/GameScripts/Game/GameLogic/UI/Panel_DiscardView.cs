using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_Discard
	{

		protected UIButton mmask;
		protected UIButton mUseRedemption;
		protected UIButton mJoin;
		protected UIButton mClose;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected Transform mloadIcon;
		protected TMP_InputField mRedemptionCode;

		protected override void ScriptGenerator()
		{
			mmask = FindChildComponent<UIButton>("Btn_mask");
			mUseRedemption = FindChildComponent<UIButton>("AnimNode/Bg/CenterNode/Btn_UseRedemption");
			mJoin = FindChildComponent<UIButton>("AnimNode/Bg/Btn_Join");
			mClose = FindChildComponent<UIButton>("AnimNode/Btn_Close");
			mPhotoNode = FindChild("AnimNode/Bg/CenterNode/Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("AnimNode/Bg/CenterNode/Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mloadIcon = FindChild("AnimNode/Bg/CenterNode/Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mRedemptionCode = FindChildComponent<TMP_InputField>("AnimNode/Bg/CenterNode/Input_RedemptionCode");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			_itemPhotoLoad = mPhotoNode.GetComponent<Item_PhotoLoad>();
			mClose.AddListener(ClickClose);
			mUseRedemption.AddListener(ClickUseRedemption);
			mJoin.AddListener(ClickJoin);
		}
		private void ClickClose(GameObject obj)
		{
			base.Close();
		}
		private void ClickUseRedemption(GameObject obj)
		{
			UseRedemption();
		}
		private void ClickJoin(GameObject obj)
		{
			JoinDiscord();
		}
		#endregion
	}
}
