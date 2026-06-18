using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Y_Base;
using TK_Engine;
namespace GameLogic
{
	public partial class Panel_GetReward
	{

		protected GameObject mNormal;
		protected GameObject mStoryRew;
		protected UIButton mMask;
		protected UIButton mGet;
		protected UIButton mAdGet;
		protected Transform mAllRewardNode;
		protected Transform mChestNode;
		protected Transform meff;
		protected Transform mlihe;
		protected TextMeshProUGUI mAdMultiple1;
		protected TextMeshProUGUI mAdMultiple2;
		protected RectTransform mnative;

		protected override void ScriptGenerator()
		{
			mNormal = FindChild("Obj_Normal").gameObject;
			mStoryRew = FindChild("Obj_StoryRew").gameObject;
			mMask = FindChildComponent<UIButton>("Obj_Normal/Btn_Mask");
			mGet = FindChildComponent<UIButton>("Obj_Normal/Btn_Get");
			mAdGet = FindChildComponent<UIButton>("Obj_Normal/Btn_AdGet");
			mAllRewardNode = FindChild("Obj_Normal/Trans_AllRewardNode");
			mChestNode = FindChild("Obj_Normal/Trans_ChestNode");
			meff = FindChild("Obj_StoryRew/efc_main_gift_01/Trans_eff");
			mlihe = FindChild("Obj_StoryRew/efc_main_gift_01/Trans_lihe");
			mAdMultiple1 = FindChildComponent<TextMeshProUGUI>("Obj_Normal/Btn_AdGet/TMP_AdMultiple1");
			mAdMultiple2 = FindChildComponent<TextMeshProUGUI>("Obj_Normal/Btn_AdGet/TMP_AdMultiple2");
			mnative = FindChildComponent<RectTransform>("Rect_native");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			mGet.AddListener(ClickGet);
			mAdGet.AddListener(ClickAdGet);
		}

		private void ClickGet(GameObject obj)
		{
			PropsController.Instance.AddRewardData(_rewardList,false,isRefresh:true);
			//PropsController.Instance.FlyReward(transform.position);
			Close();
		}

		private void ClickAdGet(GameObject obj)
		{
			//SDKManager.Instance.Button_ShowNative(mnative);
			SDKManager.Instance.Button_ShowRV(HCRVPositionName.RV_Chest, (success) =>
			{
				if (success)
				{
					PropsController.Instance.AddRewardData(_rewardList,true,0,isRefresh:true);
					//PropsController.Instance.FlyReward(transform.position);
					Close();
				}
			});
			
		}
		#endregion
	}
}
