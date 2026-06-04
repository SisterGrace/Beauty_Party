using System.Collections.Generic;
using System.Linq;
using Config.config;
using GameBase;
using TK_Engine;
using UnityEngine;

namespace GameLogic
{
	/// <summary>
	/// 抽卡结果数据结构
	/// </summary>
	public struct GachaResult
	{
		/// <summary>抽到的照片ID</summary>
		public int PhotoId;
		/// <summary>照片等级</summary>
		public int PhotoRating;
		/// <summary>是否重复（全部已拥有）</summary>
		public bool IsDuplicate;
		/// <summary>重复返还的钻石数</summary>
		public int RefundDiamonds;
		/// <summary>是否保底触发</summary>
		public bool IsPityGuarantee;
		/// <summary>是否来自最高档位奖池（用于UI展示不同特效）</summary>
		public bool IsHighestPool;
	}

	public class GachaController : Singleton<GachaController>
	{
		/// <summary>
		/// 记录已经拥有的照片id，用于筛选剩余数量。
		/// </summary>
		private HashSet<int> _ownedPhotos;

		/// <summary>
		/// 每个卡包中，按奖池ID分组的未拥有照片
		/// Key: packId, Value: Dictionary(poolId, List(photoId))
		/// </summary>
		private Dictionary<int, Dictionary<int, List<int>>> _photosPerPackByPool = new Dictionary<int, Dictionary<int, List<int>>>();

		/// <summary>
		/// 卡包列表信息（供UI读取）
		/// </summary>
		private List<PhotoPackItemInfo> _photoPackList = new List<PhotoPackItemInfo>();
		public List<PhotoPackItemInfo> PhotoPackList => _photoPackList;

		/// <summary>
		/// 初始化卡包和奖池数据，在UI打开时调用
		/// </summary>
		public void InitPhotoPacks()
		{
			_photoPackList.Clear();
			_ownedPhotos = UserController.Instance.GetOwnedPhotoSet();
			int AbType = UserController.Instance.GetCurUserType;
			var tbGacha = ConfigLoader.Instance.Tables.TbCfgGacha;
			var tbPhotoGroupRew = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew;
			_photosPerPackByPool.Clear();
			foreach (var gachaCfg in tbGacha.DataList)
			{
				//VIP卡包依赖内购解锁，当前关闭内购功能，仅保留普通卡包
				if (gachaCfg.VIPLock) continue;
				int packId = gachaCfg.Id;
				_photosPerPackByPool[packId] = new Dictionary<int, List<int>>();

				foreach (int poolId in gachaCfg.Categories)
				{
					var groupCfg = tbPhotoGroupRew.GetOrDefault(poolId);
					if (groupCfg == null) continue;
					var photoIdList = groupCfg.BPhotoRews;
					if (!_photosPerPackByPool[packId].ContainsKey(poolId))
					{
						_photosPerPackByPool[packId][poolId] = new List<int>();
					}
					for (int i = 0; i < photoIdList.Count; i++)
					{
						int photoId = photoIdList[i];
						if (_ownedPhotos.Contains(photoId)) continue;
						_photosPerPackByPool[packId][poolId].Add(photoId);
					}
				}

				var info = new PhotoPackItemInfo();
				info.PackId = packId;
				info.Name = gachaCfg.Name;
				info.PhotoIDs = new List<int>();
				info.PackfirstPic = AbType == 0 ? gachaCfg.APackFirstView : gachaCfg.BPackFirstView;
				foreach (var id in AbType == 0 ? gachaCfg.APackPreview : gachaCfg.BPackPreview)
				{
					info.PhotoIDs.Add(id);
				}
				_photoPackList.Add(info);
			}
		}

		/// <summary>
		/// 执行抽卡逻辑，返回抽卡结果
		/// </summary>
		public GachaResult DoGacha(int packId)
		{
			var result = new GachaResult();
			var gachaCfg = ConfigLoader.Instance.Tables.TbCfgGacha.GetOrDefault(packId);
			if (gachaCfg == null) return result;

			int pityCount = UserController.Instance.GetGachaPityCount(packId);
			bool needPityHighest = (pityCount + 1) >= gachaCfg.GachaPityCount;

			if (!_photosPerPackByPool.ContainsKey(packId))
			{
				_photosPerPackByPool[packId] = new Dictionary<int, List<int>>();
			}
			var photosDict = _photosPerPackByPool[packId];

			int highestPoolId = gachaCfg.Categories.Max();

			int targetPoolId = DetermineTargetPool(photosDict, gachaCfg.Categories, gachaCfg.CategoryWeights, needPityHighest);

			int photoId = SelectPhoto(targetPoolId, photosDict);

			bool isDuplicate = (photoId == -1);
			if (isDuplicate)
			{
				photoId = SelectRandomOwnedPhoto(packId);
				result.IsDuplicate = true;
				result.RefundDiamonds = (int)(gachaCfg.GachaCost * gachaCfg.GachaDuplicateRefund);
			}

			var photoCfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(photoId);
			result.PhotoId = photoId;
			result.PhotoRating = photoCfg?.ContentRating ?? 0;

			result.IsHighestPool = (targetPoolId == highestPoolId);
			result.IsPityGuarantee = needPityHighest && result.IsHighestPool;

			if (result.IsHighestPool)
			{
				UserController.Instance.ResetGachaPityCount(packId);
			}
			else
			{
				UserController.Instance.AddGachaPityCount(packId);
			}

			return result;
		}

		/// <summary>
		/// 应用抽卡结果（发放奖励/返钻）
		/// </summary>
		public void ApplyGachaResult(GachaResult result, int packId)
		{
			if (result.IsDuplicate)
			{
				UserController.Instance.Diamond += result.RefundDiamonds;
				GameEvent.Send(EventType.ShowTips, "uitips_gacha_duplicate", true);
			}
			else
			{
				UserController.Instance.AddUserPhotoData(result.PhotoId, 1);
				_ownedPhotos.Add(result.PhotoId);
			}

			UserController.Instance.SetLuckyDrawCount++;

			var photoCfg = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(result.PhotoId);
			SDKManager.Instance.LogCommonEvent(CommonEvent.ComLevelEvent);
		}

		/// <summary>
		/// 判断指定卡包的所有奖池是否已全部抽完
		/// </summary>
		public bool IsPackAllCollected(int packId)
		{
			if (!_photosPerPackByPool.ContainsKey(packId)) return true;
			var photosDict = _photosPerPackByPool[packId];
			foreach (var kv in photosDict)
			{
				if (kv.Value.Count > 0) return false;
			}
			return true;
		}

		#region 内部抽卡算法

		/// <summary>
		/// 确定目标抽取奖池
		/// 保底时强制ID最大的奖池，否则按权重随机选择（含已抽完的奖池）
		/// </summary>
		private int DetermineTargetPool(Dictionary<int, List<int>> photosDict, List<int> poolIds, List<int> poolWeights, bool needPityHighest)
		{
			int highestPoolId = poolIds.Max();

			if (needPityHighest)
			{
				return highestPoolId;
			}

			int totalWeight = 0;
			for (int i = 0; i < poolIds.Count; i++)
			{
				int w = (poolWeights != null && i < poolWeights.Count) ? poolWeights[i] : 1;
				totalWeight += w;
			}

			int rnd = Random.Range(0, totalWeight);
			int cumulative = 0;
			for (int i = 0; i < poolIds.Count; i++)
			{
				int w = (poolWeights != null && i < poolWeights.Count) ? poolWeights[i] : 1;
				cumulative += w;
				if (rnd < cumulative)
				{
					return poolIds[i];
				}
			}

			return poolIds[poolIds.Count - 1];
		}

		/// <summary>
		/// 从目标奖池中选取照片
		/// </summary>
		private int SelectPhoto(int targetPoolId, Dictionary<int, List<int>> photosDict)
		{
			if (targetPoolId == -1) return -1;

			if (!photosDict.ContainsKey(targetPoolId) || photosDict[targetPoolId].Count == 0)
			{
				return -1;
			}

			var photoList = photosDict[targetPoolId];
			int rndIdx = Random.Range(0, photoList.Count);
			int photoId = photoList[rndIdx];

			photoList.RemoveAt(rndIdx);

			return photoId;
		}

		/// <summary>
		/// 全部已拥有时，随机选一个已有照片（用于显示）
		/// </summary>
		private int SelectRandomOwnedPhoto(int packId)
		{
			var gachaCfg = ConfigLoader.Instance.Tables.TbCfgGacha.GetOrDefault(packId);
			var tbPhotoGroupRew = ConfigLoader.Instance.Tables.TbCfgPhotoGroupRew;
			List<int> ownedInPack = new List<int>();

			foreach (int poolId in gachaCfg.Categories)
			{
				var groupCfg = tbPhotoGroupRew.GetOrDefault(poolId);
				if (groupCfg == null) continue;
				foreach (int photoId in groupCfg.BPhotoRews)
				{
					if (_ownedPhotos.Contains(photoId) && !ownedInPack.Contains(photoId))
					{
						ownedInPack.Add(photoId);
					}
				}
			}

			if (ownedInPack.Count > 0)
			{
				return ownedInPack[Random.Range(0, ownedInPack.Count)];
			}
			return -1;
		}

		#endregion
	}
}
