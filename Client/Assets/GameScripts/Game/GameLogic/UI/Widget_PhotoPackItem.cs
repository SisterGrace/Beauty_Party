using TK_Engine;
using UnityEngine;
using Config.config;
using Y_Base;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;


namespace GameLogic
{
    [Window(UILayer.UI)]
    public partial class Widget_PhotoPackItem : UIWidget, IListDataItem<PhotoPackItemInfo>
    {
        int _packId;
        private PhotoPackItemInfo _data;
        private UILoopListWidget<Widget_OneShrinkPic, OneShrinkPicInfo> _shrinkPicList;
        private bool _isShowShrinkPicList = false;
        public List<int> _photoIds;
        private List<OneShrinkPicInfo> _shrinkPicInfos = new List<OneShrinkPicInfo>();
        private Item_PhotoLoad _itemPhotoLoad;

        private CancellationTokenSource _autoScrollCts;
        private int _scrollDirection = 1;
        private bool _shrinkPicDataDirty = false;
        private const float AutoScrollSpeed = 80f;
        private const float BoundaryPauseDuration = 800;
        private const float DragResumePauseDuration = 500;
        private const int StuckFrameThreshold = 3;
        private const float StuckPosDeltaThreshold = 0.5f;

        public void SetItemData(PhotoPackItemInfo data)
        {
            StopAutoScroll();
            InitData(data);
            _isShowShrinkPicList = false;
            //初始先隐藏缩略图
            mPackThumbnail.Hide();
            _photoIds = data.PhotoIDs;
            _shrinkPicDataDirty = true;
        }

        public void InitData(PhotoPackItemInfo data)
        {
            _data = data;
            _packId = data.PackId;
            _itemPhotoLoad.RequestLoad(_data.PackfirstPic);
        }

        private void EnsureShrinkPicList()
        {
            if (_shrinkPicList == null)
            {
                _shrinkPicList = CreateWidget<UILoopListWidget<Widget_OneShrinkPic, OneShrinkPicInfo>>(mAllPackPics);
                _shrinkPicList.itemBase = mOneShrinkPic;
            }
            if (!_shrinkPicDataDirty) return;
            _shrinkPicDataDirty = false;

            _shrinkPicInfos.Clear();
            for (int i = 0; i < _photoIds.Count; i++)
            {
                _shrinkPicInfos.Add(new OneShrinkPicInfo(_photoIds[i]));
            }
            _shrinkPicList.SetDatas(_shrinkPicInfos);
        }

        /// <summary>
        /// 显示缩略图循环列表（供Panel_Gacha在轮播切换时调用）
        /// </summary>
        public void ShowShrinkPicList()
        {
            if (_photoIds == null || _photoIds.Count == 0) return;

            EnsureShrinkPicList();
            _isShowShrinkPicList = true;
            mPackThumbnail.SetActive(true);
            StartAutoScroll();
        }

        /// <summary>
        /// 隐藏缩略图循环列表
        /// </summary>
        public void HideShrinkPicList()
        {
            _isShowShrinkPicList = false;
            mPackThumbnail.SetActive(false);
            StopAutoScroll();
        }

        private void StartAutoScroll()
        {
            StopAutoScroll();
            if (_shrinkPicList == null || _photoIds == null || _photoIds.Count <= 4) return;

            _scrollDirection = 1;
            _autoScrollCts = new CancellationTokenSource();
            RunAutoScroll(_autoScrollCts.Token).Forget();
        }

        private void StopAutoScroll()
        {
            if (_autoScrollCts != null)
            {
                _autoScrollCts.Cancel();
                _autoScrollCts.Dispose();
                _autoScrollCts = null;
            }
        }

        private async UniTaskVoid RunAutoScroll(CancellationToken ct)
        {
            var loopView = _shrinkPicList.LoopRectView;
            var scrollRect = loopView.ScrollRect;

            await UniTask.DelayFrame(3, cancellationToken: ct);
            loopView.MovePanelToItemIndex(0, 0);
            await UniTask.DelayFrame(1, cancellationToken: ct);

            Vector3 prevPos = scrollRect.content.localPosition;
            int stuckFrames = 0;
            bool hasMovedSinceReverse = false;
            bool wasDragging = false;

            while (!ct.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (loopView.IsDraging)
                {
                    prevPos = scrollRect.content.localPosition;
                    stuckFrames = 0;
                    hasMovedSinceReverse = false;
                    wasDragging = true;
                    continue;
                }

                if (wasDragging)
                {
                    wasDragging = false;
                    scrollRect.StopMovement();
                    await UniTask.Delay((int)DragResumePauseDuration, cancellationToken: ct);
                    prevPos = scrollRect.content.localPosition;
                    continue;
                }

                scrollRect.velocity = loopView.IsVertList
                    ? new Vector2(0, AutoScrollSpeed * _scrollDirection)
                    : new Vector2(-AutoScrollSpeed * _scrollDirection, 0);

                Vector3 curPos = scrollRect.content.localPosition;
                float posDelta = loopView.IsVertList
                    ? Mathf.Abs(curPos.y - prevPos.y)
                    : Mathf.Abs(curPos.x - prevPos.x);
                prevPos = curPos;

                if (posDelta >= StuckPosDeltaThreshold)
                {
                    hasMovedSinceReverse = true;
                    stuckFrames = 0;
                }
                else if (hasMovedSinceReverse)
                {
                    stuckFrames++;
                    if (stuckFrames >= StuckFrameThreshold)
                    {
                        scrollRect.StopMovement();
                        stuckFrames = 0;
                        hasMovedSinceReverse = false;

                        await UniTask.Delay((int)BoundaryPauseDuration, cancellationToken: ct);

                        _scrollDirection = -_scrollDirection;
                        prevPos = scrollRect.content.localPosition;
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAutoScroll();
            _shrinkPicList = null;
        }
    }

    public struct PhotoPackItemInfo
    {
        public int PackId;
        public string Name;
        public List<int> PhotoIDs;
        public int PackfirstPic;
        public PhotoPackItemInfo(int index, int packId, string name, List<int> photoIds,int packfirstPic)
        {
            PackId = packId;
            Name = name;
            PhotoIDs = photoIds;
            PackfirstPic = packfirstPic;
        }
    }
}
