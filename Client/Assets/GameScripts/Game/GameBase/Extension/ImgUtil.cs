using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace GameBase
{
    /// <summary>
    /// 图片工具类 - 提供图片加载、缓存和保存到相册功能
    /// </summary>
    public static class ImgUtil
    {
        #region ==================== 枚举定义 ====================

        /// <summary>
        /// 图片格式
        /// </summary>
        public enum ImageFormat
        {
            PNG,
            JPG
        }

        /// <summary>
        /// 图片保存状态
        /// </summary>
        public enum ImageSaveStatus
        {
            Success,
            Failed,
            Cancelled,
            PermissionDenied
        }

        #endregion

        #region ==================== 常量配置 ====================

        /// <summary>内存缓存最大数量（超出后 LRU 淘汰）</summary>
        private const int MAX_MEMORY_CACHE_COUNT = 50;

        /// <summary>URL 缓存最大数量</summary>
        private const int MAX_URL_CACHE_COUNT = 200;

        /// <summary>相册文件夹名称</summary>
        private const string GALLERY_FOLDER_NAME = "FantasyTile_Gallery";

        #endregion

        #region ==================== 私有字段 ====================

        // ---- 缓存相关 ----
        /// <summary>内存缓存（线程安全）</summary>
        private static readonly ConcurrentDictionary<string, Texture2D> _memoryCache
            = new ConcurrentDictionary<string, Texture2D>();

        /// <summary>URL 到 CacheKey 的映射缓存（线程安全）</summary>
        private static readonly ConcurrentDictionary<string, string> _urlToCacheKey
            = new ConcurrentDictionary<string, string>();

        /// <summary>正在进行的下载任务（防止重复下载）</summary>
        private static readonly ConcurrentDictionary<string, UniTaskCompletionSource<Texture2D>> _ongoingTasks
            = new ConcurrentDictionary<string, UniTaskCompletionSource<Texture2D>>();

        // ---- LRU 相关 ----
        /// <summary>LRU 链表（最近使用的在末尾）</summary>
        private static readonly LinkedList<string> _lruList = new LinkedList<string>();

        /// <summary>LRU 操作锁</summary>
        private static readonly object _lruLock = new object();

        // ---- 纹理引用计数 ----
        /// <summary>已被 LRU 淘汰但仍有外部引用的纹理</summary>
        private static readonly HashSet<Texture2D> _evictedTextures = new HashSet<Texture2D>();

        /// <summary>纹理外部引用计数（Sprite 持有时 +1，释放时 -1）</summary>
        private static readonly Dictionary<Texture2D, int> _textureRefCount = new Dictionary<Texture2D, int>();

        // ---- 其他 ----
        /// <summary>磁盘缓存目录</summary>
        private static string _cacheDirectory;

        /// <summary>MD5 实例（线程本地存储，避免重复创建）</summary>
        [ThreadStatic]
        private static MD5 _md5Instance;

        #endregion

        #region ==================== 初始化 ====================

        static ImgUtil()
        {
            _cacheDirectory = Path.Combine(Application.persistentDataPath, "TextureCache");

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }

            Debug.Log($"[ImgUtil] 初始化完成，缓存目录: {_cacheDirectory}");
        }

        #endregion

        #region ==================== 公共API - 获取纹理 ====================

        /// <summary>
        /// 异步获取图片（推荐使用）
        /// </summary>
        /// <param name="url">图片 URL</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>加载的 Texture2D，失败返回 null</returns>
        public static async UniTask<Texture2D> GetTextureAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!ValidateCacheDirectory()) return null;

            string cacheKey = GetCacheKey(url);
            string filePath = GetCacheFilePath(cacheKey);

            // 1. 检查内存缓存
            var cachedTexture = TryGetFromMemoryCache(cacheKey);
            if (cachedTexture != null)
            {
                return cachedTexture;
            }

            // 2. 检查磁盘缓存
            var diskTexture = await TryLoadFromDiskCacheAsync(cacheKey, filePath, cancellationToken);
            if (diskTexture != null)
            {
                return diskTexture;
            }

            // 3. 从网络下载（使用原子操作防止重复下载）
            return await DownloadWithDeduplicationAsync(url, cacheKey, filePath, cancellationToken);
        }

        /// <summary>
        /// 批量异步获取多个图片
        /// </summary>
        /// <param name="urls">图片 URL 数组</param>
        /// <returns>Texture2D 数组</returns>
        public static async UniTask<Texture2D[]> GetTexturesAsync(params string[] urls)
        {
            var tasks = new UniTask<Texture2D>[urls.Length];

            for (int i = 0; i < urls.Length; i++)
            {
                tasks[i] = GetTextureAsync(urls[i]);
            }

            return await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 协程方式获取图片（兼容旧代码）
        /// </summary>
        /// <param name="url">图片 URL</param>
        /// <param name="callback">完成回调</param>
        public static IEnumerator GetTexture(string url, Action<Texture2D> callback)
        {
            if (!ValidateCacheDirectory())
            {
                callback?.Invoke(null);
                yield break;
            }

            string cacheKey = GetCacheKey(url);
            string filePath = GetCacheFilePath(cacheKey);

            // 1. 检查内存缓存
            if (_memoryCache.TryGetValue(cacheKey, out var cachedTexture) && cachedTexture != null)
            {
                callback?.Invoke(cachedTexture);
                yield break;
            }

            // 2. 检查磁盘缓存
            if (File.Exists(filePath))
            {
                var diskTexture = TryLoadFromDiskCacheSync(cacheKey, filePath);
                if (diskTexture != null)
                {
                    callback?.Invoke(diskTexture);
                    yield break;
                }
            }

            // 3. 从网络下载
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(request);
                    SaveToCacheSync(tex, cacheKey, filePath);
                    callback?.Invoke(tex);
                }
                else
                {
                    Debug.LogError($"[ImgUtil] 图片下载失败: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        #endregion

        #region ==================== 公共API - 纹理引用管理 ====================

        /// <summary>
        /// 增加纹理的外部引用计数（当 Sprite.Create 使用了该纹理时调用）
        /// </summary>
        public static void RetainTexture(Texture2D tex)
        {
            if (tex == null) return;
            lock (_lruLock)
            {
                _textureRefCount.TryGetValue(tex, out int count);
                _textureRefCount[tex] = count + 1;
            }
        }

        /// <summary>
        /// 减少纹理的外部引用计数（当 Sprite 被销毁时调用）。
        /// 若引用归零且纹理已被 LRU 淘汰，则立即销毁释放内存。
        /// </summary>
        public static void ReleaseTexture(Texture2D tex)
        {
            if (tex == null) return;
            lock (_lruLock)
            {
                if (!_textureRefCount.TryGetValue(tex, out int count)) return;

                count--;
                if (count <= 0)
                {
                    _textureRefCount.Remove(tex);
                    if (_evictedTextures.Remove(tex))
                    {
                        // #region agent log
                        Debug.Log($"[DBG-9f6800] TEX_RELEASE_DESTROY | texID={tex.GetInstanceID()} (refCount=0, was evicted)");
                        // #endregion
                        UnityEngine.Object.Destroy(tex);
                    }
                }
                else
                {
                    _textureRefCount[tex] = count;
                }
            }
        }

        #endregion

        #region ==================== 公共API - 缓存管理 ====================

        /// <summary>
        /// 清理内存缓存
        /// </summary>
        public static void ClearMemoryCache()
        {
            lock (_lruLock)
            {
                foreach (var texture in _memoryCache.Values)
                {
                    if (texture != null && !_textureRefCount.ContainsKey(texture))
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                    else if (texture != null)
                    {
                        _evictedTextures.Add(texture);
                    }
                }

                _memoryCache.Clear();
                _lruList.Clear();
            }

            Debug.Log("[ImgUtil] 内存缓存已清理");
        }

        /// <summary>
        /// 清理磁盘缓存
        /// </summary>
        public static void ClearDiskCache()
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, true);
                Directory.CreateDirectory(_cacheDirectory);
                Debug.Log("[ImgUtil] 磁盘缓存已清理");
            }
        }

        /// <summary>
        /// 清理所有缓存（内存 + 磁盘）
        /// </summary>
        public static void ClearAllCache()
        {
            ClearMemoryCache();
            ClearDiskCache();
        }

        /// <summary>
        /// 获取缓存信息
        /// </summary>
        /// <param name="memoryCount">内存缓存数量</param>
        /// <param name="diskSize">磁盘缓存大小（字节）</param>
        public static void GetCacheInfo(out int memoryCount, out long diskSize)
        {
            memoryCount = _memoryCache.Count;
            diskSize = 0;

            if (Directory.Exists(_cacheDirectory))
            {
                foreach (var file in Directory.GetFiles(_cacheDirectory, "*.png"))
                {
                    diskSize += new FileInfo(file).Length;
                }
            }
        }

        #endregion

        #region ==================== 公共API - 保存到相册 ====================

        /// <summary>
        /// 保存 Texture2D 到相册
        /// </summary>
        /// <param name="texture">要保存的纹理</param>
        /// <param name="fileName">文件名（不需要扩展名）</param>
        /// <param name="imageFormat">图片格式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否保存成功</returns>
        public static async UniTask<bool> SaveToGallery(
            Texture2D texture,
            string fileName = null,
            ImageFormat imageFormat = ImageFormat.PNG,
            CancellationToken cancellationToken = default)
        {
            if (texture == null)
            {
                Debug.LogError("[ImgUtil] 无法保存空纹理到相册");
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return await SaveToAndroidGalleryInternal(texture, fileName, imageFormat, cancellationToken);
#elif UNITY_IOS && !UNITY_EDITOR
            return await SaveToiOSGalleryInternal(texture, fileName, imageFormat, cancellationToken);
#else
            return await SaveToLocalDirectory(texture, fileName, imageFormat, cancellationToken);
#endif
        }

        /// <summary>
        /// 保存 Texture2D 到相册（无取消令牌的重载）
        /// </summary>
        public static async UniTask<bool> SaveToGallery(
            Texture2D texture,
            string fileName = null,
            ImageFormat imageFormat = ImageFormat.PNG)
        {
            return await SaveToGallery(texture, fileName, imageFormat, CancellationToken.None);
        }

        /// <summary>
        /// 从 URL 下载图片并保存到相册
        /// </summary>
        /// <param name="url">图片 URL</param>
        /// <param name="fileName">文件名</param>
        /// <param name="imageFormat">图片格式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否保存成功</returns>
        public static async UniTask<bool> SaveUrlToGallery(
            string url,
            string fileName = null,
            ImageFormat imageFormat = ImageFormat.PNG,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var texture = await GetTextureAsync(url, cancellationToken);
                if (texture == null)
                {
                    Debug.LogError($"[ImgUtil] 无法下载图片: {url}");
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                string finalFileName = fileName ?? Path.GetFileNameWithoutExtension(url);
                return await SaveToGallery(texture, finalFileName, imageFormat, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存URL图片到相册操作已取消");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存URL图片到相册失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从 URL 下载图片并保存到相册（无取消令牌的重载）
        /// </summary>
        public static async UniTask<bool> SaveUrlToGallery(
            string url,
            string fileName = null,
            ImageFormat imageFormat = ImageFormat.PNG)
        {
            return await SaveUrlToGallery(url, fileName, imageFormat, CancellationToken.None);
        }

        /// <summary>
        /// 提前请求相册写入权限（建议在合适时机提前调用，如进入图片详情页时）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否获得权限</returns>
        public static async UniTask<bool> RequestGalleryPermissionAsync(CancellationToken cancellationToken = default)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#elif UNITY_IOS && !UNITY_EDITOR
            var permission = await RequestiOSWritePermissionAsync(cancellationToken);
            return permission == NativeGallery.Permission.Granted;
#else
            // 编辑器或其他平台不需要权限
            return true;
#endif
        }

        /// <summary>
        /// 检查是否已有相册写入权限（同步检查，不会弹出请求）
        /// </summary>
        /// <returns>是否已有权限</returns>
        public static bool HasGalleryPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#elif UNITY_IOS && !UNITY_EDITOR
            return NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image);
#else
            return true;
#endif
        }

        /// <summary>
        /// 保存 Texture2D 到相册（返回详细状态）
        /// </summary>
        public static async UniTask<ImageSaveStatus> SaveToGalleryWithStatus(
            Texture2D texture,
            string fileName = null,
            ImageFormat imageFormat = ImageFormat.PNG,
            CancellationToken cancellationToken = default)
        {
            if (texture == null)
            {
                Debug.LogError("[ImgUtil] 无法保存空纹理到相册");
                return ImageSaveStatus.Failed;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return await SaveToAndroidGalleryWithStatusInternal(texture, fileName, imageFormat, cancellationToken);
#elif UNITY_IOS && !UNITY_EDITOR
            return await SaveToiOSGalleryWithStatusInternal(texture, fileName, imageFormat, cancellationToken);
#else
            bool result = await SaveToLocalDirectory(texture, fileName, imageFormat, cancellationToken);
            return result ? ImageSaveStatus.Success : ImageSaveStatus.Failed;
#endif
        }

        /// <summary>
        /// 从 URL 下载图片并保存到相册（返回详细状态）
        /// </summary>
        public static async UniTask<ImageSaveStatus> SaveUrlToGalleryWithStatus(
            string url,
            string fileName = null,
            ImageFormat imageFormat = ImageFormat.PNG,
            CancellationToken cancellationToken = default)
        {
            try
            {
                //提前请求相册写入权限
                //但是此处的权限判断仅限于处理苹果的权限请求，因为NativeGallery 已经内部处理了安卓的权限权限
                var permission = await RequestGalleryPermissionAsync(cancellationToken);
                if (!permission)
                {
                    return ImageSaveStatus.PermissionDenied;
                }


                cancellationToken.ThrowIfCancellationRequested();

                var texture = await GetTextureAsync(url, cancellationToken);
                if (texture == null)
                {
                    Debug.LogError($"[ImgUtil] 无法下载图片: {url}");
                    return ImageSaveStatus.Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

                string finalFileName = fileName ?? Path.GetFileNameWithoutExtension(url);
                return await SaveToGalleryWithStatus(texture, finalFileName, imageFormat, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存URL图片到相册操作已取消");
                return ImageSaveStatus.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存URL图片到相册失败: {e.Message}");
                return ImageSaveStatus.Failed;
            }
        }

        #endregion

        #region ==================== 私有方法 - 内存缓存 ====================

        /// <summary>
        /// 尝试从内存缓存获取（同时验证 Texture 是否有效）
        /// </summary>
        private static Texture2D TryGetFromMemoryCache(string cacheKey)
        {
            if (_memoryCache.TryGetValue(cacheKey, out var cachedTexture))
            {
                if (cachedTexture != null)
                {
                    UpdateLruAccess(cacheKey);
                    return cachedTexture;
                }

                // Texture 已被外部销毁，从缓存移除
                _memoryCache.TryRemove(cacheKey, out _);
            }

            return null;
        }

        /// <summary>
        /// 添加到内存缓存（带 LRU 淘汰）
        /// </summary>
        private static void AddToMemoryCache(string cacheKey, Texture2D texture)
        {
            lock (_lruLock)
            {
                // 如果已存在，先移除旧的 LRU 节点
                if (_memoryCache.ContainsKey(cacheKey))
                {
                    _lruList.Remove(cacheKey);
                }

                // 检查是否需要淘汰
                while (_lruList.Count >= MAX_MEMORY_CACHE_COUNT && _lruList.Count > 0)
                {
                    var oldest = _lruList.First.Value;
                    _lruList.RemoveFirst();

                    if (_memoryCache.TryRemove(oldest, out var oldTexture) && oldTexture != null)
                    {
                        if (_textureRefCount.ContainsKey(oldTexture))
                        {
                            _evictedTextures.Add(oldTexture);
                            // #region agent log
                            Debug.Log($"[DBG-9f6800] LRU_EVICT_DEFERRED | key={oldest} texID={oldTexture.GetInstanceID()} size={oldTexture.width}x{oldTexture.height} refCount={_textureRefCount[oldTexture]}");
                            // #endregion
                        }
                        else
                        {
                            // #region agent log
                            Debug.Log($"[DBG-9f6800] LRU_EVICT_DESTROY | key={oldest} texID={oldTexture.GetInstanceID()} size={oldTexture.width}x{oldTexture.height} (no refs)");
                            // #endregion
                            UnityEngine.Object.Destroy(oldTexture);
                        }
                    }
                }

                // 添加新的
                _memoryCache[cacheKey] = texture;
                _lruList.AddLast(cacheKey);
            }
        }

        /// <summary>
        /// 更新 LRU 访问顺序
        /// </summary>
        private static void UpdateLruAccess(string cacheKey)
        {
            lock (_lruLock)
            {
                _lruList.Remove(cacheKey);
                _lruList.AddLast(cacheKey);
            }
        }

        #endregion

        #region ==================== 私有方法 - 磁盘缓存 ====================

        /// <summary>
        /// 异步从磁盘缓存加载
        /// </summary>
        private static async UniTask<Texture2D> TryLoadFromDiskCacheAsync(
            string cacheKey,
            string filePath,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                // 后台线程读取文件
                byte[] fileData = await ReadFileAsync(filePath, cancellationToken);

                // 原地解密（对称加密）
                FileEncrypter.EncryptInPlace(fileData);

                // 主线程创建纹理
                var texture = await CreateTextureFromBytesAsync(fileData);

                if (texture != null)
                {
                    AddToMemoryCache(cacheKey, texture);
                    return texture;
                }

                Debug.LogWarning($"[ImgUtil] 缓存文件损坏，已删除: {filePath}");
                await DeleteFileAsync(filePath);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 加载磁盘缓存失败: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// 同步从磁盘缓存加载（协程版本使用）
        /// </summary>
        private static Texture2D TryLoadFromDiskCacheSync(string cacheKey, string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                byte[] decryptedData = FileEncrypter.Decrypt(fileData);

                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(decryptedData))
                {
                    _memoryCache[cacheKey] = texture;
                    return texture;
                }

                Debug.LogWarning($"[ImgUtil] 缓存文件损坏，已删除: {filePath}");
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 加载磁盘缓存失败: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// 同步保存到缓存（协程版本使用）
        /// </summary>
        private static void SaveToCacheSync(Texture2D texture, string cacheKey, string filePath)
        {
            try
            {
                _memoryCache[cacheKey] = texture;

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] bytes = texture.EncodeToPNG();
                byte[] encrypted = FileEncrypter.Encrypt(bytes);
                File.WriteAllBytes(filePath, encrypted);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存缓存失败: {e.Message}");
            }
        }

        /// <summary>
        /// 异步保存纹理到磁盘缓存
        /// </summary>
        private static async UniTask SaveTextureToDiskCacheAsync(Texture2D texture, byte[] rawData, string filePath)
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                await UniTask.RunOnThreadPool(() => Directory.CreateDirectory(directory));
            }

            // 准备数据
            byte[] bytesToSave;
            if (rawData != null && rawData.Length > 0)
            {
                // 使用原始数据，避免 EncodeToPNG 的开销
                bytesToSave = new byte[rawData.Length];
                Array.Copy(rawData, bytesToSave, rawData.Length);
            }
            else
            {
                // 回退方案：主线程编码
                await UniTask.SwitchToMainThread();
                bytesToSave = texture.EncodeToPNG();
            }

            // 后台线程加密并保存
            await UniTask.RunOnThreadPool(() =>
            {
                FileEncrypter.EncryptInPlace(bytesToSave);
                File.WriteAllBytes(filePath, bytesToSave);
            });
        }

        #endregion

        #region ==================== 私有方法 - 网络下载 ====================

        /// <summary>
        /// 带去重的下载（防止同一 URL 重复下载）
        /// </summary>
        private static async UniTask<Texture2D> DownloadWithDeduplicationAsync(
            string url,
            string cacheKey,
            string filePath,
            CancellationToken cancellationToken)
        {
            // 原子操作：检查/添加下载任务
            var newTcs = new UniTaskCompletionSource<Texture2D>();
            var existingTcs = _ongoingTasks.GetOrAdd(cacheKey, newTcs);

            // 如果已存在相同下载任务，等待它完成
            if (existingTcs != newTcs)
            {
                return await existingTcs.Task.AttachExternalCancellation(cancellationToken);
            }

            // 我们是第一个请求者，执行下载
            try
            {
                var result = await DownloadTextureAsync(url, cacheKey, filePath, cancellationToken);
                newTcs.TrySetResult(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                newTcs.TrySetCanceled(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                newTcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _ongoingTasks.TryRemove(cacheKey, out _);
            }
        }

        /// <summary>
        /// 下载纹理
        /// </summary>
        private static async UniTask<Texture2D> DownloadTextureAsync(
            string url,
            string cacheKey,
            string filePath,
            CancellationToken cancellationToken)
        {
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ImgUtil] 图片下载失败: {request.error}");
                    return null;
                }

                var texture = DownloadHandlerTexture.GetContent(request);
                byte[] rawData = request.downloadHandler.data;

                try
                {
                    await SaveTextureToDiskCacheAsync(texture, rawData, filePath);
                    AddToMemoryCache(cacheKey, texture);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ImgUtil] 保存缓存失败: {e.Message}");
                    // 磁盘缓存失败，仍加入内存缓存
                    AddToMemoryCache(cacheKey, texture);
                }

                return texture;
            }
        }

        #endregion

        #region ==================== 私有方法 - 文件IO ====================

        /// <summary>
        /// 异步读取文件
        /// </summary>
        private static async UniTask<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();
            return await File.ReadAllBytesAsync(filePath, cancellationToken);
        }

        /// <summary>
        /// 异步删除文件
        /// </summary>
        private static async UniTask DeleteFileAsync(string filePath)
        {
            await UniTask.RunOnThreadPool(() =>
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }

        /// <summary>
        /// 从字节数组创建纹理（主线程）
        /// </summary>
        private static async UniTask<Texture2D> CreateTextureFromBytesAsync(byte[] data)
        {
            await UniTask.SwitchToMainThread();
            var texture = new Texture2D(2, 2);
            return texture.LoadImage(data) ? texture : null;
        }

        #endregion

        #region ==================== 私有方法 - 工具函数 ====================

        /// <summary>
        /// 验证缓存目录是否初始化
        /// </summary>
        private static bool ValidateCacheDirectory()
        {
            if (string.IsNullOrEmpty(_cacheDirectory))
            {
                Debug.LogError("[ImgUtil] 缓存目录未初始化!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取缓存文件路径
        /// </summary>
        private static string GetCacheFilePath(string cacheKey)
        {
            return Path.Combine(_cacheDirectory, cacheKey + ".png");
        }

        /// <summary>
        /// 根据 URL 生成缓存 Key（MD5 哈希）
        /// </summary>
        private static string GetCacheKey(string url)
        {
            if (_urlToCacheKey.TryGetValue(url, out string cachedKey))
            {
                return cachedKey;
            }

            // 复用 MD5 实例
            if (_md5Instance == null)
            {
                _md5Instance = MD5.Create();
            }

            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(url);
            byte[] hashBytes = _md5Instance.ComputeHash(inputBytes);
            string result = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // 限制缓存大小
            if (_urlToCacheKey.Count >= MAX_URL_CACHE_COUNT)
            {
                TrimUrlCache();
            }

            _urlToCacheKey[url] = result;
            return result;
        }

        /// <summary>
        /// 清理 URL 缓存（清空一半）
        /// </summary>
        private static void TrimUrlCache()
        {
            var keysToRemove = new List<string>();
            int removeCount = MAX_URL_CACHE_COUNT / 2;

            foreach (var key in _urlToCacheKey.Keys)
            {
                if (removeCount-- <= 0) break;
                keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                _urlToCacheKey.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// 编码纹理为指定格式
        /// </summary>
        private static byte[] EncodeTexture(Texture2D texture, ImageFormat format)
        {
            return format switch
            {
                ImageFormat.PNG => texture.EncodeToPNG(),
                ImageFormat.JPG => texture.EncodeToJPG(),
                _ => texture.EncodeToPNG()
            };
        }

        /// <summary>
        /// 生成相册文件名
        /// </summary>
        private static string GetGalleryFileName(string fileName, ImageFormat format)
        {
            string extension = format == ImageFormat.PNG ? ".png" : ".jpg";

            if (string.IsNullOrEmpty(fileName))
            {
                return $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            }

            return fileName.EndsWith(extension) ? fileName : fileName + extension;
        }

        #endregion

        #region ==================== 私有方法 - 保存到相册（通用） ====================

        /// <summary>
        /// 保存到本地目录（编辑器或其他平台）
        /// </summary>
        private static async UniTask<bool> SaveToLocalDirectory(
            Texture2D texture,
            string fileName,
            ImageFormat imageFormat,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string finalFileName = GetGalleryFileName(fileName, imageFormat);
                string savePath = Path.Combine(Application.persistentDataPath, "SavedImages", finalFileName);

                // 确保目录存在
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 主线程编码
                await UniTask.SwitchToMainThread();
                byte[] imageData = EncodeTexture(texture, imageFormat);

                // 后台线程保存
                await UniTask.RunOnThreadPool(() => File.WriteAllBytes(savePath, imageData), cancellationToken: cancellationToken);

                Debug.Log($"[ImgUtil] 图片已保存到本地: {savePath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存图片到本地操作已取消");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存图片到本地失败: {e.Message}");
                return false;
            }
        }

        #endregion

        #region ==================== 私有方法 - 保存到相册（Android） ====================

#if UNITY_ANDROID && !UNITY_EDITOR

        /// <summary>
        /// 保存到 Android 相册
        /// </summary>
        private static async UniTask<bool> SaveToAndroidGalleryInternal(
            Texture2D texture,
            string fileName,
            ImageFormat imageFormat,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (NativeGallery.IsMediaPickerBusy())
                {
                    Debug.LogWarning("[ImgUtil] 媒体选择器正忙");
                    return false;
                }

                string imgName = GetGalleryFileName(fileName, imageFormat);

                await UniTask.SwitchToMainThread(cancellationToken);
                byte[] imageData = EncodeTexture(texture, imageFormat);

                cancellationToken.ThrowIfCancellationRequested();

                var completionSource = new UniTaskCompletionSource<bool>();

                NativeGallery.SaveImageToGallery(imageData, GALLERY_FOLDER_NAME, imgName, (success, path) =>
                {
                    if (success)
                    {
                        Debug.Log($"[ImgUtil] 图片已保存到相册: {path}");
                        completionSource.TrySetResult(true);
                    }
                    else
                    {
                        Debug.LogError("[ImgUtil] 图片保存失败");
                        completionSource.TrySetResult(false);
                    }
                });

                return await completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存图片到安卓相册操作已取消");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存图片到安卓相册失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存到 Android 相册（返回详细状态）
        /// </summary>
        private static async UniTask<ImageSaveStatus> SaveToAndroidGalleryWithStatusInternal(
            Texture2D texture,
            string fileName,
            ImageFormat imageFormat,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (NativeGallery.IsMediaPickerBusy())
                {
                    return ImageSaveStatus.Failed;
                }

                string imgName = GetGalleryFileName(fileName, imageFormat);

                await UniTask.SwitchToMainThread(cancellationToken);
                byte[] imageData = EncodeTexture(texture, imageFormat);

                cancellationToken.ThrowIfCancellationRequested();

                var completionSource = new UniTaskCompletionSource<ImageSaveStatus>();

                NativeGallery.SaveImageToGallery(imageData, GALLERY_FOLDER_NAME, imgName, (success, path) =>
                {
                    if (success)
                    {
                        Debug.Log($"[ImgUtil] 图片已保存到相册: {path}");
                        completionSource.TrySetResult(ImageSaveStatus.Success);
                    }
                    else
                    {
                        Debug.LogError("[ImgUtil] 图片保存失败");
                        completionSource.TrySetResult(ImageSaveStatus.Failed);
                    }
                });

                return await completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存图片到安卓相册操作已取消");
                return ImageSaveStatus.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存图片到安卓相册失败: {e.Message}");
                return ImageSaveStatus.Failed;
            }
        }

        /// <summary>
        /// 获取 Android 相册路径
        /// </summary>
        private static string GetAndroidGalleryPath()
        {
            using (var environment = new AndroidJavaClass("android.os.Environment"))
            using (var picturesDir = environment.CallStatic<AndroidJavaObject>(
                "getExternalStoragePublicDirectory",
                environment.GetStatic<string>("DIRECTORY_PICTURES")))
            {
                string galleryPath = picturesDir.Call<string>("getAbsolutePath");
                return Path.Combine(galleryPath, Application.productName);
            }
        }

        /// <summary>
        /// 刷新 Android 相册
        /// </summary>
        private static void RefreshAndroidGallery(string filePath)
        {
            try
            {
                using (var mediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection"))
                using (var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = context.GetStatic<AndroidJavaObject>("currentActivity");
                    mediaScannerConnection.CallStatic("scanFile", currentActivity, new[] { filePath }, null, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ImgUtil] 媒体扫描器刷新失败: {e.Message}");
            }
        }

#endif

        #endregion

        #region ==================== 私有方法 - 保存到相册（iOS） ====================

#if UNITY_IOS && !UNITY_EDITOR

        /// <summary>
        /// 保存到 iOS 相册
        /// </summary>
        private static async UniTask<bool> SaveToiOSGalleryInternal(
            Texture2D texture,
            string fileName,
            ImageFormat imageFormat,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (NativeGallery.IsMediaPickerBusy())
                {
                    Debug.LogWarning("[ImgUtil] 媒体选择器正忙，请稍后再试");
                    return false;
                }

                // 检查权限
                var permission = await RequestiOSWritePermissionAsync(cancellationToken);
                if (permission != NativeGallery.Permission.Granted)
                {
                    Debug.LogError("[ImgUtil] 没有相册写入权限");
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                string imgName = GetGalleryFileName(fileName, imageFormat);

                await UniTask.SwitchToMainThread(cancellationToken);
                byte[] imageData = EncodeTexture(texture, imageFormat);

                cancellationToken.ThrowIfCancellationRequested();

                var completionSource = new UniTaskCompletionSource<bool>();

                NativeGallery.SaveImageToGallery(imageData, GALLERY_FOLDER_NAME, imgName, (success, path) =>
                {
                    if (success)
                    {
                        Debug.Log($"[ImgUtil] 图片已保存到iOS相册: {path}");
                        completionSource.TrySetResult(true);
                    }
                    else
                    {
                        Debug.LogError("[ImgUtil] iOS图片保存失败");
                        completionSource.TrySetResult(false);
                    }
                });

                return await completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存图片到iOS相册操作已取消");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存图片到iOS相册失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存到 iOS 相册（返回详细状态）
        /// </summary>
        private static async UniTask<ImageSaveStatus> SaveToiOSGalleryWithStatusInternal(
            Texture2D texture,
            string fileName,
            ImageFormat imageFormat,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (NativeGallery.IsMediaPickerBusy())
                {
                    Debug.LogWarning("[ImgUtil] 媒体选择器正忙，请稍后再试");
                    return ImageSaveStatus.Failed;
                }

                var permission = await RequestiOSWritePermissionAsync(cancellationToken);
                if (permission != NativeGallery.Permission.Granted)
                {
                    Debug.LogError("[ImgUtil] 没有相册写入权限");
                    return ImageSaveStatus.PermissionDenied;
                }

                cancellationToken.ThrowIfCancellationRequested();

                string imgName = GetGalleryFileName(fileName, imageFormat);

                await UniTask.SwitchToMainThread(cancellationToken);
                byte[] imageData = EncodeTexture(texture, imageFormat);

                cancellationToken.ThrowIfCancellationRequested();

                var completionSource = new UniTaskCompletionSource<ImageSaveStatus>();

                NativeGallery.SaveImageToGallery(imageData, GALLERY_FOLDER_NAME, imgName, (success, path) =>
                {
                    if (success)
                    {
                        Debug.Log($"[ImgUtil] 图片已保存到iOS相册: {path}");
                        completionSource.TrySetResult(ImageSaveStatus.Success);
                    }
                    else
                    {
                        Debug.LogError("[ImgUtil] iOS图片保存失败");
                        completionSource.TrySetResult(ImageSaveStatus.Failed);
                    }
                });

                return await completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImgUtil] 保存图片到iOS相册操作已取消");
                return ImageSaveStatus.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImgUtil] 保存图片到iOS相册失败: {e.Message}");
                return ImageSaveStatus.Failed;
            }
        }

        /// <summary>
        /// 请求 iOS 写入权限
        /// </summary>
        private static async UniTask<NativeGallery.Permission> RequestiOSWritePermissionAsync(CancellationToken cancellationToken)
        {
            // 检查当前权限
            if (NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image))
            {
                return NativeGallery.Permission.Granted;
            }

            var completionSource = new UniTaskCompletionSource<NativeGallery.Permission>();

            NativeGallery.RequestPermissionAsync(
                permission => completionSource.TrySetResult(permission),
                NativeGallery.PermissionType.Write,
                NativeGallery.MediaType.Image);

            // 带超时的等待
            try
            {
                var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

                return await completionSource.Task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                Debug.LogWarning("[ImgUtil] iOS权限请求超时");
                return NativeGallery.Permission.Denied;
            }
        }

#endif

        #endregion
    }
}
