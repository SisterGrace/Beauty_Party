using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
using Spine.Unity;
namespace GameLogic
{
	public partial class Widget_EndlessItem
	{

		protected GameObject mLeft;
		protected GameObject mUp;
		protected GameObject mRight;
		protected GameObject mAdicon;
		protected GameObject mlock;
		protected GameObject mOneRew1;
		protected GameObject mOneRew2;
		protected GameObject mOneRew3;
		protected GameObject mPhotoNode;
		protected GameObject mLoadingNode;
		protected GameObject mTickNode;
		protected GameObject meff;
		protected UIButton mGet;
		protected TextMeshProUGUI mGetPrice;
		protected TextMeshProUGUI mOneRewCount1;
		protected TextMeshProUGUI mOneRewCount2;
		protected TextMeshProUGUI mOneRewCount3;
		protected Transform mAllRewards;
		protected Transform mloadIcon;
		protected Transform mTick;

		protected override void ScriptGenerator()
		{
			mLeft = FindChild("AllDir/Obj_Left").gameObject;
			mUp = FindChild("AllDir/Obj_Up").gameObject;
			mRight = FindChild("AllDir/Obj_Right").gameObject;
			mAdicon = FindChild("Btn_Get/Obj_Adicon").gameObject;
			mlock = FindChild("Obj_lock").gameObject;
			mOneRew1 = FindChild("Trans_AllRewards/Obj_OneRew1").gameObject;
			mOneRew2 = FindChild("Trans_AllRewards/Obj_OneRew2").gameObject;
			mOneRew3 = FindChild("Trans_AllRewards/Obj_OneRew3").gameObject;
			mPhotoNode = FindChild("Obj_PhotoNode").gameObject;
			mLoadingNode = FindChild("Obj_PhotoNode/Obj_LoadingNode").gameObject;
			mTickNode = FindChild("Obj_TickNode").gameObject;
			meff = FindChild("Obj_TickNode/Obj_eff").gameObject;
			mGet = FindChildComponent<UIButton>("Btn_Get");
			mGetPrice = FindChildComponent<TextMeshProUGUI>("Btn_Get/TMP_GetPrice");
			mOneRewCount1 = FindChildComponent<TextMeshProUGUI>("Trans_AllRewards/Obj_OneRew1/TMP_OneRewCount1");
			mOneRewCount2 = FindChildComponent<TextMeshProUGUI>("Trans_AllRewards/Obj_OneRew2/TMP_OneRewCount2");
			mOneRewCount3 = FindChildComponent<TextMeshProUGUI>("Trans_AllRewards/Obj_OneRew3/TMP_OneRewCount3");
			mAllRewards = FindChild("Trans_AllRewards");
			mloadIcon = FindChild("Obj_PhotoNode/Obj_LoadingNode/Trans_loadIcon");
			mTick = FindChild("Obj_TickNode/Trans_Tick");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private CanvasGroup mCanvasGroup;
		//private SkeletonGraphic _mLockAnim;
		private HorizontalLayoutGroup _mLayoutGroup;
		private void InitCodeBlock()
		{
			OneEndlessItem item1 = new OneEndlessItem(mOneRew1, mOneRewCount1);
			OneEndlessItem item2 = new OneEndlessItem(mOneRew2, mOneRewCount2);
			OneEndlessItem item3 = new OneEndlessItem(mOneRew3, mOneRewCount3);
			_items.Add(item1);
			_items.Add(item2);
			_items.Add(item3);
			mGet.AddListener(ClickGet);
			mCanvasGroup = transform.GetComponent<CanvasGroup>();
			_mLockAnim = mlock.GetComponent<SkeletonGraphic>();
			_mLayoutGroup = mAllRewards.GetComponent<HorizontalLayoutGroup>();
		}
		private void ClickGet(GameObject obj)
		{
			if(_isLock) return;
			RechargeController.Instance.HandleBuyOrAd(_rechargeItem, transform.position,
				() => { UpdateAdUnLockInfo(); },
				(productID) =>
				{
					BuyOneItem();
				});
		}
		#endregion
	}
}
