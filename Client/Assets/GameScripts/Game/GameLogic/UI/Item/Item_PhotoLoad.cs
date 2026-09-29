using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Y_Base;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TK_Engine;

namespace GameLogic
{
    public class Item_PhotoLoad : MonoBehaviour
    {
        public Image PhotoImg;
        public GameObject MyLoadingNode;
        public Transform LoadIcon;
        public TextMeshProUGUI PhotoCount;
        public AspectRatioFitter AspectRatioFitter;

        /// <summary>
        /// 失败时显示的占位 Sprite（可通过 Inspector 或代码赋值，未赋值则使用默认灰色占位）
        /// </summary>
        public Sprite FallbackSprite;

        public bool IsLoading => _isLoading;
        public bool IsFailed => _isFailed;
        public Action OnLoadSuccess;
        public Action OnLoadFailed;

        private bool _isLoading;
        private bool _isFailed;
        private float _rotateSpeed = -360f;
        private string _currentLoadingUrl;
        private CancellationTokenSource _cts;
        private Sprite _createdSprite;
        private Texture2D _retainedTexture;
        // #region agent log
        private float _dbgCheckTimer;
        // #endregion

        private const int MaxRetryCount = 2;
        private static Texture2D _defaultFallbackTexture;

        /// <summary>
        /// 通过代码初始化引用（用于非序列化场景）
        /// </summary>
        public void Init(Image photoImg, GameObject loadingNode, Transform loadIcon,
                         TextMeshProUGUI photoCount = null, AspectRatioFitter aspectRatioFitter = null)
        {
            PhotoImg = photoImg;
            MyLoadingNode = loadingNode;
            LoadIcon = loadIcon;
            PhotoCount = photoCount;
            AspectRatioFitter = aspectRatioFitter;
        }

        public void LoadPhoto(int id, int count)
        {
            string url = PropsController.GetImgUrl(id);
            StartLoad(url, showLoading: true);
            if (PhotoCount != null)
            {
                if (count <= 1)
                {
                    PhotoCount.gameObject.SetActive(false);
                }
                else
                {
                    PhotoCount.gameObject.SetActive(true);
                    PhotoCount.text = $"X{count}";
                }
            }
        }

        public void LoadUIImg(string url)
        {
            StartLoad(url, showLoading: true);
            if (PhotoCount != null)
            {
                PhotoCount.gameObject.SetActive(false);
            }
        }

        public void LoadUIImg(int id)
        {
            string url = PropsController.GetImgUrl(id);
            
            // ✅ 自动检测所属页面
            string pageSource = DetectPageSource();
            
            StartLoad(url, showLoading: true, pageSource);
            
            if (PhotoCount != null)
            {
                PhotoCount.gameObject.SetActive(false);
            }
        }
        public void RequestLoad(int photoId)
        {
            PhotoLoadThrottler.RequestLoad(photoId, this);
        }
        /// <summary>
        /// 自动检测所属页面
        /// </summary>
        private string DetectPageSource()
        {
            // 向上查找父节点，直到找到 Panel_ 开头的节点
            Transform parent = transform;
            while (parent != null)
            {
                if (parent.name.StartsWith("Panel_"))
                {
                    return parent.name;
                }
                parent = parent.parent;
            }
            
            return null; // 未找到页面标识
        }

        /// <summary>
        /// 通过 URL 加载图片并显示加载动画
        /// </summary>
        public void LoadPhotoByUrl(string url)
        {
            StartLoad(url, showLoading: true);
            if (PhotoCount != null)
            {
                PhotoCount.gameObject.SetActive(false);
            }
        }

        public void CancelLoad()
        {
            CancelCurrentTask();
            _isLoading = false;
            _currentLoadingUrl = null;
        }

        private void StartLoad(string url, bool showLoading)
        {
            StartLoad(url, showLoading, null);
        }

        private void StartLoad(string url, bool showLoading, string pageSource)
        {
            CancelCurrentTask();
            DestroyCreatedSprite();

            _currentLoadingUrl = url;
            _isLoading = true;
            _isFailed = false;

            if (MyLoadingNode != null)
            {
                if (showLoading) MyLoadingNode.Show();
                else MyLoadingNode.Hide();
            }
            if (PhotoImg != null) PhotoImg.sprite = null;

            _cts = new CancellationTokenSource();
            LoadImageAsync(url, pageSource, _cts.Token).Forget();
        }

        private void CancelCurrentTask()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTask LoadImageAsync(string url, string pageSource, CancellationToken token)
        {
            for (int attempt = 0; attempt <= MaxRetryCount; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        int delayMs = 1000 * (1 << (attempt - 1));
                        await UniTask.Delay(delayMs, cancellationToken: token);
                        if (gameObject == null || _currentLoadingUrl != url) return;
                    }

                    // ✅ 使用带优先级的加载方法
                    Texture2D texture = await ImgUtil.GetTextureAsync(
                        url,
                        ImgUtil.DownloadPriority.Normal,
                        pageSource,
                        token
                    );
                    
                    if (gameObject == null || _currentLoadingUrl != url) return;

                    if (texture != null)
                    {
                        _isLoading = false;
                        if (MyLoadingNode != null) MyLoadingNode.Hide();
                        SetSprite(texture);
                        OnLoadSuccess?.Invoke();
                        return;
                    }

                    if (attempt < MaxRetryCount)
                    {
                        Log.Warning($"图片加载返回空，第{attempt + 1}次重试, url:{url}");
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    if (attempt >= MaxRetryCount)
                    {
                        Log.Warning($"图片加载失败，已达最大重试次数:{e.Message}, url:{url}");
                        break;
                    }
                    Log.Warning($"图片加载失败，第{attempt + 1}次重试:{e.Message}, url:{url}");
                }
            }

            if (gameObject == null || _currentLoadingUrl != url) return;
            _isLoading = false;
            _isFailed = true;
            if (MyLoadingNode != null) MyLoadingNode.Hide();
            SetFallbackSprite();
            OnLoadFailed?.Invoke();
        }

        private void SetSprite(Texture2D tex)
        {
            DestroyCreatedSprite();

            ImgUtil.RetainTexture(tex);
            _retainedTexture = tex;

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sprite.name = _currentLoadingUrl ?? string.Empty;
            _createdSprite = sprite;

            if (AspectRatioFitter != null)
            {
                AspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                AspectRatioFitter.aspectRatio = (float)tex.width / (float)tex.height;
                //Debug.LogError(AspectRatioFitter.aspectRatio+" width:"+ tex.width+" height:"+ tex.height);
            }
            if (PhotoImg != null) PhotoImg.sprite = sprite;
        }

        private void DestroyCreatedSprite()
        {
            if (_createdSprite != null)
            {
                if (PhotoImg != null && PhotoImg.sprite == _createdSprite)
                    PhotoImg.sprite = null;
                Destroy(_createdSprite);
                _createdSprite = null;
            }

            if (_retainedTexture != null)
            {
                ImgUtil.ReleaseTexture(_retainedTexture);
                _retainedTexture = null;
            }
        }

        private void SetFallbackSprite()
        {
            if (PhotoImg == null) return;

            if (FallbackSprite != null)
            {
                PhotoImg.sprite = FallbackSprite;
                return;
            }

            if (_defaultFallbackTexture == null)
            {
                _defaultFallbackTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var colors = _defaultFallbackTexture.GetPixels();
                var fillColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                for (int i = 0; i < colors.Length; i++) colors[i] = fillColor;
                _defaultFallbackTexture.SetPixels(colors);
                _defaultFallbackTexture.Apply();
            }

            PhotoImg.sprite = Sprite.Create(
                _defaultFallbackTexture,
                new Rect(0, 0, _defaultFallbackTexture.width, _defaultFallbackTexture.height),
                new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            if (_isLoading && LoadIcon != null)
            {
                LoadIcon.Rotate(0, 0, _rotateSpeed * Time.unscaledDeltaTime);
            }

            // #region agent log
            _dbgCheckTimer += Time.unscaledDeltaTime;
            if (_dbgCheckTimer >= 2f)
            {
                _dbgCheckTimer = 0f;
                if (PhotoImg != null && PhotoImg.sprite != null && PhotoImg.sprite.texture == null)
                {
                    Debug.Log($"[DBG-9f6800] TEX_DESTROYED | obj={gameObject.name} spriteName={PhotoImg.sprite.name} url={_currentLoadingUrl}");
                }
            }
            // #endregion
        }

        private void OnDestroy()
        {
            CancelCurrentTask();
            DestroyCreatedSprite();
            OnLoadSuccess = null;
            OnLoadFailed = null;
        }
    }
}
