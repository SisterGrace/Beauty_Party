using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
using RenderHeads.Media.AVProVideo;
namespace GameLogic
{
	public partial class Panel_CollectDiamond
	{

		protected Image mMask;
		protected GameObject mDiamondShow;
		protected RectTransform mDiaTitle;
		protected RectTransform mDisIcon;
		protected RectTransform mDiaBtns;
		protected UIButton mCurDiaNode;
		protected UIButton mContinue;
		protected UIButton mAdGet;
		protected TextMeshProUGUI mCurDiaCount;
		protected TextMeshProUGUI mnorDiaCount;
		protected TextMeshProUGUI mAdMultiple;

		protected override void ScriptGenerator()
		{
			mMask = FindChildComponent<Image>("Img_Mask");
			mDiamondShow = FindChild("AnimNode/Obj_DiamondShow").gameObject;
			mDiaTitle = FindChildComponent<RectTransform>("AnimNode/Obj_DiamondShow/Rect_DiaTitle");
			mDisIcon = FindChildComponent<RectTransform>("AnimNode/Obj_DiamondShow/Rect_DisIcon");
			mDiaBtns = FindChildComponent<RectTransform>("AnimNode/Obj_DiamondShow/Rect_DiaBtns");
			mCurDiaNode = FindChildComponent<UIButton>("AnimNode/Obj_DiamondShow/Btn_CurDiaNode");
			mContinue = FindChildComponent<UIButton>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_Continue");
			mAdGet = FindChildComponent<UIButton>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_AdGet");
			mCurDiaCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_DiamondShow/Btn_CurDiaNode/TMP_CurDiaCount");
			mnorDiaCount = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_Continue/TMP_norDiaCount");
			mAdMultiple = FindChildComponent<TextMeshProUGUI>("AnimNode/Obj_DiamondShow/Rect_DiaBtns/Btn_AdGet/TMP_AdMultiple");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mContinue.AddListener(ClickContinue);
			mAdGet.AddListener(ClickAdGet);
		}
		private void ClickContinue(GameObject obj)
		{
			// 普通领取，直接关闭面板
			// 数据已在打开面板前写入，UI刷新在 Close() 中统一处理
			Close();
		}
		private void ClickAdGet(GameObject obj)
		{
			SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_Daily, (success) =>
			{
				if (success)
				{
					// 发送广告倍数奖励（额外奖励）
					SendAdReward();
				}
				// 无论成功与否都关闭面板，UI刷新在 Close() 中统一处理
				Close();
			});
		}
		#endregion
	}
}
