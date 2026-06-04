using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using TK_Engine;
namespace GameLogic
{
	public partial class Widget_ShowRews
	{

		protected GameObject mGroup1;
		protected GameObject mGroup2;
		protected GameObject mLife;
		protected GameObject mNoAd;
		protected Image mProp1;
		protected Image mProp2;
		protected Image mProp3;
		protected Image mProp4;
		protected TextMeshProUGUI mCount1;
		protected TextMeshProUGUI mCount2;
		protected TextMeshProUGUI mCount3;
		protected TextMeshProUGUI mCount4;
		protected TextMeshProUGUI mLifeCount;
		protected TextMeshProUGUI mNoAdCount;

		protected override void ScriptGenerator()
		{
			mGroup1 = FindChild("Obj_Group1").gameObject;
			mGroup2 = FindChild("Obj_Group2").gameObject;
			mLife = FindChild("Obj_Life").gameObject;
			mNoAd = FindChild("Obj_NoAd").gameObject;
			mProp1 = FindChildComponent<Image>("Obj_Group1/Img_Prop1");
			mProp2 = FindChildComponent<Image>("Obj_Group1/Img_Prop2");
			mProp3 = FindChildComponent<Image>("Obj_Group2/Img_Prop3");
			mProp4 = FindChildComponent<Image>("Obj_Group2/Img_Prop4");
			mCount1 = FindChildComponent<TextMeshProUGUI>("Obj_Group1/Img_Prop1/TMP_Count1");
			mCount2 = FindChildComponent<TextMeshProUGUI>("Obj_Group1/Img_Prop2/TMP_Count2");
			mCount3 = FindChildComponent<TextMeshProUGUI>("Obj_Group2/Img_Prop3/TMP_Count3");
			mCount4 = FindChildComponent<TextMeshProUGUI>("Obj_Group2/Img_Prop4/TMP_Count4");
			mLifeCount = FindChildComponent<TextMeshProUGUI>("Obj_Life/lifeIcon/TMP_LifeCount");
			mNoAdCount = FindChildComponent<TextMeshProUGUI>("Obj_NoAd/NoAdIcon/TMP_NoAdCount");
			InitCodeBlock();
		}

		#region 可编辑代码块
		private void InitCodeBlock()
		{
			OneRewardItem item1 = new OneRewardItem(mProp1,mCount1);
			_rewardItems.Add(item1);
			OneRewardItem item2 = new OneRewardItem(mProp2,mCount2);
			_rewardItems.Add(item2);
			OneRewardItem item3 = new OneRewardItem(mProp3,mCount3);
			_rewardItems.Add(item3);
			OneRewardItem item4 = new OneRewardItem(mProp4,mCount4);
			_rewardItems.Add(item4);
		}
		#endregion
	}
}
