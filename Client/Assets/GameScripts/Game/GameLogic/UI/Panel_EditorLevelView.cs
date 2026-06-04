using Config.common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using DG.Tweening;
namespace GameLogic
{
	public partial class Panel_EditorLevel
	{

		protected GameObject mTouch;
		protected GameObject mEditBoard;
		protected GameObject mCustomNode;
		protected RectTransform mAllUnits;
		protected RectTransform mLeftUI;
		protected RectTransform mRightUI;
		protected TextMeshProUGUI mAllCardCount;
		protected TextMeshProUGUI mAllLayerCount;
		protected TextMeshProUGUI mCurrLayerCount;
		protected TextMeshProUGUI mCurrLayer;
		protected TMP_InputField mTypeTxt;
		protected TMP_InputField mGetLockType;
		protected TMP_InputField mGetFunctionType;
		protected TMP_InputField mGetShellCount;
		protected TMP_InputField mGetBombCount;
		protected TMP_InputField mCustomType;
		protected UIButton mCloseSetLock;
		protected UIButton mCloseSetFunction;
		protected UIButton mSaveInfo;
		protected UIButton mNextLayer;
		protected UIButton mLastLayer;
		protected UIButton mAddAll;
		protected UIButton mClearAll;
		protected UIButton mUp;
		protected UIButton mDown;
		protected UIButton mLeft;
		protected UIButton mRight;
		protected UIButton mCustomizable;
		protected UIButton mSetLock;
		protected UIButton mSetFunction;
		protected Toggle mIsAdd;

		protected override void ScriptGenerator()
		{
			mTouch = FindChild("Obj_Touch").gameObject;
			mEditBoard = FindChild("Obj_EditBoard").gameObject;
			mCustomNode = FindChild("BottomUI/Obj_CustomNode").gameObject;
			mAllUnits = FindChildComponent<RectTransform>("Obj_EditBoard/Rect_AllUnits");
			mLeftUI = FindChildComponent<RectTransform>("Rect_LeftUI");
			mRightUI = FindChildComponent<RectTransform>("Rect_RightUI");
			mAllCardCount = FindChildComponent<TextMeshProUGUI>("TopUI/TMP_AllCardCount");
			mAllLayerCount = FindChildComponent<TextMeshProUGUI>("TopUI/TMP_AllLayerCount");
			mCurrLayerCount = FindChildComponent<TextMeshProUGUI>("TopUI/TMP_CurrLayerCount");
			mCurrLayer = FindChildComponent<TextMeshProUGUI>("BottomUI/CurrLayer/TMP_CurrLayer");
			mTypeTxt = FindChildComponent<TMP_InputField>("TopUI/TypeTxt/Input_TypeTxt");
			mGetLockType = FindChildComponent<TMP_InputField>("Rect_LeftUI/Input_GetLockType");
			mGetFunctionType = FindChildComponent<TMP_InputField>("Rect_RightUI/Input_GetFunctionType");
			mGetShellCount = FindChildComponent<TMP_InputField>("Rect_RightUI/Input_GetShellCount");
			mGetBombCount = FindChildComponent<TMP_InputField>("Rect_RightUI/Input_GetBombCount");
			mCustomType = FindChildComponent<TMP_InputField>("BottomUI/Obj_CustomNode/Input_CustomType");
			mCloseSetLock = FindChildComponent<UIButton>("Rect_LeftUI/Btn_CloseSetLock");
			mCloseSetFunction = FindChildComponent<UIButton>("Rect_RightUI/Btn_CloseSetFunction");
			mSaveInfo = FindChildComponent<UIButton>("BottomUI/Btn_SaveInfo");
			mNextLayer = FindChildComponent<UIButton>("BottomUI/Btn_NextLayer");
			mLastLayer = FindChildComponent<UIButton>("BottomUI/Btn_LastLayer");
			mAddAll = FindChildComponent<UIButton>("BottomUI/Btn_AddAll");
			mClearAll = FindChildComponent<UIButton>("BottomUI/Btn_ClearAll");
			mUp = FindChildComponent<UIButton>("BottomUI/Btn_Up");
			mDown = FindChildComponent<UIButton>("BottomUI/Btn_Down");
			mLeft = FindChildComponent<UIButton>("BottomUI/Btn_Left");
			mRight = FindChildComponent<UIButton>("BottomUI/Btn_Right");
			mCustomizable = FindChildComponent<UIButton>("BottomUI/Btn_Customizable");
			mSetLock = FindChildComponent<UIButton>("Btn_SetLock");
			mSetFunction = FindChildComponent<UIButton>("Btn_SetFunction");
			mIsAdd = FindChildComponent<Toggle>("BottomUI/Tog_IsAdd");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mAddAll.AddListener(ClickAddAll);
			mClearAll.AddListener(ClickClearAll);
			mTypeTxt.onEndEdit.AddListener(OnInputCount);
			mUp.AddListener(ClickUp);
			mDown.AddListener(ClickDown);
			mLeft.AddListener(ClickLeft);
			mRight.AddListener(ClickRight);
			mLastLayer.AddListener(ClickLastLayer);
			mNextLayer.AddListener(ClickNextLayer);
			mSaveInfo.AddListener(ClickSaveInfo);
			mCustomizable.AddListener(ClickCustomizable);

			mSetLock.AddListener(ClickSetLock);
			mSetFunction.AddListener(ClickSetFunction);
			mCloseSetLock.AddListener(ClickCloseSetLock);
			mCloseSetFunction.AddListener(ClickCloseSetFunction);
			mGetLockType.onEndEdit.AddListener(OnGetLockType);
			mGetFunctionType.onEndEdit.AddListener(OnGetFunctionType);
			mGetShellCount.onEndEdit.AddListener(OnGetShellCount);
			mGetBombCount.onEndEdit.AddListener(OnGetBombCount);
			mCustomType.onEndEdit.AddListener(OnGetCustomTyps);
		}

		private void ClickAddAll(GameObject obj)
		{
			LevelEdirtorController.Instance.AddAllCard();
		}

		private void ClickClearAll(GameObject obj)
		{
			LevelEdirtorController.Instance.RemoveAllCard();
		}

		private void OnInputCount(string value)
		{
			int temp = 0;
			int.TryParse(value, out temp);
			temp = Mathf.Clamp(temp, 1, EditorScene.Instance.MaxCardType);
			UpdateCardTypeCount(temp);
			LevelEdirtorController.Instance.CardTypeCount = temp;
		}

		private void ClickUp(GameObject obj)
		{
			LevelEdirtorController.Instance.AddStack(1,mEditBoard.transform);
		}

		private void ClickDown(GameObject obj)
		{
			LevelEdirtorController.Instance.AddStack(2,mEditBoard.transform);
		}

		private void ClickLeft(GameObject obj)
		{
			LevelEdirtorController.Instance.AddStack(3,mEditBoard.transform);
		}

		private void ClickRight(GameObject obj)
		{
			LevelEdirtorController.Instance.AddStack(4,mEditBoard.transform);
		}

		private void ClickLastLayer(GameObject obj)
		{
			LevelEdirtorController.Instance.LastLayer();
		}

		private void ClickNextLayer(GameObject obj)
		{
			LevelEdirtorController.Instance.NextLayer();
		}

		private void ClickSaveInfo(GameObject obj)
		{
			LevelEdirtorController.Instance.Save();
		}

		private void ClickSetLock(GameObject obj)
		{
			_isSet = true;
			_curSet = 1;
			obj.Hide();
			mLeftUI.DOAnchorPos3DX(230f, 0.5f).SetEase(Ease.Linear);
			mGetLockType.text = "0";
		}

		private void ClickSetFunction(GameObject obj)
		{
			_isSet = true;
			_curSet = 2;
			obj.Hide();
			mRightUI.DOAnchorPos3DX(-230f, 0.5f).SetEase(Ease.Linear);
			mGetFunctionType.text = "";
		}

		private void ClickCloseSetLock(GameObject obj)
		{
			_isSet = false;
			mLeftUI.DOAnchorPos3DX(-260f, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
			{
				mSetLock.gameObject.Show();
			});
			_curSet = 0;
			mGetLockType.text = "0";
			LevelEdirtorController.Instance.CurrentLockCardType = LockCardType.None;
		}

		private void ClickCloseSetFunction(GameObject obj)
		{
			_isSet = false;
			mGetShellCount.gameObject.Hide();
			mGetBombCount.gameObject.Hide();
			mRightUI.DOAnchorPos3DX(260f, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
			{
				mSetFunction.gameObject.Show();
			});
			_curSet = 0;
			mGetFunctionType.text = "";
			mGetShellCount.text = "";
			LevelEdirtorController.Instance.BombCount = 0;
			LevelEdirtorController.Instance.ShellOrReversibleCount = 0;
			LevelEdirtorController.Instance.CurrentFunctionCardTypes = null;
		}
		//自定义样式
		private void ClickCustomizable(GameObject obj)
		{
			SetCustomType();
		}
		#endregion
	}
}
