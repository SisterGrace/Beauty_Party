using TK_Engine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config.common;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_GetReward_1 : UIWindow
	{
		private List<TBRewardData> _rewardList = new ();
		private List<int> _rewardIds;
		private List<int> _allTimesIds = new List<int>();
		//private int _timeId;
		protected override void OnRefresh()
		{
			if (userDatas.Length > 0)
			{
				_rewardIds = (List<int>)userDatas[0];
				foreach (var id in _rewardIds)
				{
					_rewardList.AddRange(ConfigLoader.Instance.Tables.TbCfgReward.Get(id).Reward);
				}
				PropsController.Instance.AddRewardData(_rewardList,OneRewardData.NoneData,false);
			}
			PlaySingleReward();
			
		}

		protected override void RegisterEvent()
		{
		}
		private void PlaySingleReward()
		{
			var spineGic = mlihe.GetComponent<Spine.Unity.SkeletonGraphic>();
			var par = meff.GetComponent<ParticleSystem>();
			AudioManager.Play(AudioType.Sound,AudioDef.UI_chestopen);
			SpineController.PlayAnim(spineGic, 0,"display",false);
			par.Play();
			var _timeId1 = GameModule.Timer.AddTimer(timer =>
			{
				PropsController.Instance.FlyReward(meff.position);
			}, 1.2f);
			_allTimesIds.Add(_timeId1);
			var _timeId2 = GameModule.Timer.AddTimer(timer =>
			{
				Close();
			}, 1.7f);
			_allTimesIds.Add(_timeId2);
			
		}
		protected override void Close()
		{
			base.Close();
			if (_allTimesIds.Count > 0)
			{
				foreach (var id in _allTimesIds)
				{
					GameModule.Timer.RemoveTimer(id);
				}
			}
			GameModule.UI.RemoveOrderPanel<Panel_GetReward_1>(false);
		}
	}
}
