using TK_Engine;
using UnityEngine;
using Config.common;
using System.Collections;
using System.Collections.Generic;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_GetReward : UIWindow
	{
		private List<int> _rewardIds;
		private List<TBRewardData> _rewardList = new List<TBRewardData>();
		private List<OneRewardData> _collateRewardList;
		private List<Vector3> _positionList;
		private int _timeId;
		private bool _isNormalReward;
        //是否存在网赚奖励
        private bool _isMMOReward;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_rewardIds = (List<int>)userDatas[0];
				for (int i = 0; i < _rewardIds.Count; i++)
				{
					_rewardList.AddRange(ConfigLoader.Instance.Tables.TbCfgReward.Get(_rewardIds[i]).Reward);
				}
				_isNormalReward = (bool)userDatas[1];
			}
			_isMMOReward = ISGetMMORew();
			mNormal.SetActive(_isNormalReward);
			mStoryRew.SetActive(!_isNormalReward);
			if (_isNormalReward)
			{
				_positionList = new List<Vector3>();
				_collateRewardList = PropsController.Instance.CollateReward(_rewardList);
				AudioManager.Play(AudioType.Sound,AudioDef.UI_chestopen);
				_timeId = GameModule.Timer.AddTimer(timer =>
				{
					CreateRewardItem();
				}, 1f);
				mAdMultiple1.gameObject.SetActive(!_isMMOReward);
				mAdMultiple2.gameObject.SetActive(_isMMOReward);
				if (_isMMOReward)
				{
					mAdMultiple2.text = $"<sprite name=\"MMOicon_cash\">X{ConfigLoader.Constant.MMOIntegralMultiple}";
				}
				else
				{
					mAdMultiple1.text = $"<sprite name=\"icon_jinbi\">X{ConfigLoader.Constant.AdMultiple}";
				}
				
			}
			SDKManager.Instance.LogEvent("RV_Button_Show","Position","RV_Chest");
		}
		public float radius = 300f;  
		protected override void RegisterEvent()
		{
		}
		
		private void CreateRewardItem()
		{
			//根据奖励数量设置奖励生成半径
			if (_collateRewardList.Count <= 5)
			{
				radius = 200f;
			}else if (_collateRewardList.Count > 5 && _collateRewardList.Count <= 7)
			{
				radius = 275f;
			}
			for (int i = 0; i < _collateRewardList.Count; i++)
			{
				// 从正左方开始（偏移+180度），按360/count等分
				float angle = i * Mathf.PI * 2 / _rewardList.Count + Mathf.PI;
				Widget_RewardItem item = CreateWidgetByType<Widget_RewardItem>(mAllRewardNode);
				item.Open(_collateRewardList[i]);
				Vector2 position = new Vector2(
					Mathf.Cos(angle) * radius,
					Mathf.Sin(angle) * radius
				);
				item.ShowAnim(mChestNode.position,position,0.3f);
			}
		}
		//单礼包奖励
		private void PlaySingleReward()
		{
			var spineGic = mlihe.GetComponent<Spine.Unity.SkeletonGraphic>();
			var par = meff.GetComponent<ParticleSystem>();
			SpineController.PlayAnim(spineGic, 0,"display",false);
			par.Play();
			_timeId = GameModule.Timer.AddTimer(timer =>
			{
				Close();
				PropsController.Instance.FlyReward(meff.position);
			}, 1.7f);
		}
		/// <summary>
		/// 是否存在网赚奖励
		/// </summary>
		/// <returns></returns>
		private bool ISGetMMORew()
		{
			foreach (var data in _rewardList)
			{
				if (data.Type == ERewardType.Currency && data.ID == 2)
				{
					
					return true;
				}
			}
			return false;
		}
		protected override void Close()
		{
			base.Close();
			if (_timeId > 0)
			{
				GameModule.Timer.RemoveTimer(_timeId);
			}
			//这里关闭不需要检测后续界面打开，当奖励发送完毕的时候，会有检测
			GameModule.UI.RemoveOrderPanel<Panel_GetReward>(true);
		}
	}
}
