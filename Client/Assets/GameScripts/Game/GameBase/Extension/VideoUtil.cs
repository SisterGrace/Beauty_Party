using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace GameBase
{
    public static class VideoUtil
    {
        // 用于跟踪正在进行的下载任务
        private static Dictionary<string, UniTask<bool>> ongoingVideoDownloads = new Dictionary<string, UniTask<bool>>();
        
        /// <summary>
        /// 视频下载和保存状态
        /// </summary>
        public enum VideoSaveStatus
        {
            Success,
            Failed,
            Cancelled,
            PermissionDenied,
            NetworkError,
            FileError
        }

        /// <summary>
        /// 下载进度回调
        /// </summary>
        public class DownloadProgress
        {
            public float Progress { get; set; }
            public long DownloadedBytes { get; set; }
            public long TotalBytes { get; set; }
            public string Status { get; set; }
        }

        /// <summary>
        /// 从URL下载视频并保存到相册
        /// </summary>
        /// <param name="url">视频URL</param>
        /// <param name="fileName">自定义文件名</param>
        /// <param name="onProgress">下载进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>保存状态</returns>
        public static async UniTask<VideoSaveStatus> SaveVideoToGallery(
            string url, 
            string fileName = null, 
            System.Action<DownloadProgress> onProgress = null,
            CancellationToken cancellationToken = default)
        {
            // 检查URL有效性
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("视频URL不能为空");
                return VideoSaveStatus.Failed;
            }

#if UNITY_IOS && !UNITY_EDITOR
            // iOS平台提前请求权限，避免下载后无权限保存
            NativeGallery.Permission permission = await RequestiOSVideoWritePermissionAsync();
            if (permission != NativeGallery.Permission.Granted)
            {
                return VideoSaveStatus.PermissionDenied;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android平台提前请求权限，避免下载后无权限保存
            bool hasPermission = NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Video);
            if (!hasPermission)
            {
                NativeGallery.Permission permission = await RequestAndroidVideoWritePermissionAsync();
                if (permission != NativeGallery.Permission.Granted)
                {
                    Debug.LogError("没有存储权限，无法保存视频到相册");
                    return VideoSaveStatus.PermissionDenied;
                }
            }
#endif

            string cacheKey = GetVideoCacheKey(url);
            
            // 检查是否已经有正在进行的下载
            if (ongoingVideoDownloads.TryGetValue(cacheKey, out var ongoingTask))
            {
                try
                {
                    bool result = await ongoingTask;
                    return result ? VideoSaveStatus.Success : VideoSaveStatus.Failed;
                }
                catch (OperationCanceledException)
                {
                    return VideoSaveStatus.Cancelled;
                }
            }
            // 创建一个 UniTaskCompletionSource 供后续重复请求等待
            var completionSource = new UniTaskCompletionSource<bool>();
            ongoingVideoDownloads[cacheKey] = completionSource.Task.Preserve();
            
            try
            {
                // 执行实际下载任务
                VideoSaveStatus status = await DownloadAndSaveVideo(url, fileName, onProgress, cancellationToken);
                
                // 通知后续等待者结果
                completionSource.TrySetResult(status == VideoSaveStatus.Success);
                
                return status;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"视频下载已取消: {url}");
                completionSource.TrySetCanceled();
                return VideoSaveStatus.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogError($"视频保存失败: {e.Message}");
                completionSource.TrySetResult(false);
                return VideoSaveStatus.Failed;
            }
            finally
            {
                ongoingVideoDownloads.Remove(cacheKey);
            }
        }

        /// <summary>
        /// 下载并保存视频
        /// </summary>
        private static async UniTask<VideoSaveStatus> DownloadAndSaveVideo(
            string url, 
            string fileName, 
            System.Action<DownloadProgress> onProgress,
            CancellationToken cancellationToken)
        {
            string tempFilePath = Path.Combine(Application.temporaryCachePath, $"temp_video_{Guid.NewGuid()}.mp4");
            
            try
            {
                // 下载视频到临时文件
                bool downloadSuccess = await DownloadVideoToFile(url, tempFilePath, onProgress, cancellationToken);
                if (!downloadSuccess)
                {
                    return VideoSaveStatus.NetworkError;
                }

                // 保存到相册（返回详细状态）
                return await SaveVideoFileToGallery(tempFilePath, fileName);
            }
            finally
            {
                // 清理临时文件
                await CleanupTempFile(tempFilePath);
            }
        }

        /// <summary>
        /// 下载视频到临时文件
        /// </summary>
        private static async UniTask<bool> DownloadVideoToFile(
            string url, 
            string filePath, 
            System.Action<DownloadProgress> onProgress,
            CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = new UnityWebRequest(url))
            {
                // 配置下载处理器
                request.downloadHandler = new DownloadHandlerFile(filePath);
                request.disposeDownloadHandlerOnDispose = true;

                // 发送请求
                var operation = request.SendWebRequest();

                // 进度监控
                while (!operation.isDone && !cancellationToken.IsCancellationRequested)
                {
                    onProgress?.Invoke(new DownloadProgress
                    {
                        Progress = operation.progress,
                        DownloadedBytes = (long)(request.downloadedBytes),
                        TotalBytes = (long)request.downloadedBytes, // 注意：UnityWebRequest不提供总大小
                        Status = "下载中..."
                    });

                    await UniTask.Delay(100, cancellationToken: cancellationToken);
                }

                // 检查取消
                cancellationToken.ThrowIfCancellationRequested();

                // 检查结果
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"视频下载失败: {request.error}");
                    return false;
                }

                onProgress?.Invoke(new DownloadProgress
                {
                    Progress = 1f,
                    DownloadedBytes = (long)request.downloadedBytes,
                    TotalBytes = (long)request.downloadedBytes,
                    Status = "下载完成"
                });

                return true;
            }
        }

        /// <summary>
        /// 保存视频文件到相册
        /// </summary>
        private static async UniTask<VideoSaveStatus> SaveVideoFileToGallery(string filePath, string fileName)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"视频文件不存在: {filePath}");
                return VideoSaveStatus.FileError;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return await SaveVideoToAndroidGallery2(filePath, fileName);
#elif UNITY_IOS && !UNITY_EDITOR
            return await SaveVideoToIOSGallery(filePath, fileName);
#else
            bool result = await SaveVideoToLocalDirectory(filePath, fileName);
            return result ? VideoSaveStatus.Success : VideoSaveStatus.FileError;
#endif
        }

        /// <summary>
        /// 保存到安卓相册
        /// </summary>
        private static async UniTask<bool> SaveVideoToAndroidGallery(string filePath, string fileName)
        {
            try
            {
                string finalFileName = GetVideoFileName(fileName);
                string galleryPath = GetAndroidVideoGalleryPath();
                string destinationPath = Path.Combine(galleryPath, finalFileName);

                // 确保目录存在
                string directory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 在后台线程复制文件
                await UniTask.RunOnThreadPool(() =>
                {
                    File.Copy(filePath, destinationPath, true);
                });

                // 通知媒体扫描器
                RefreshAndroidGallery(destinationPath);

                Debug.Log($"视频已保存到相册: {destinationPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存视频到安卓相册失败: {e.Message}");
                return false;
            }
        }

        private static async UniTask<VideoSaveStatus> SaveVideoToAndroidGallery2(string filePath, string fileName)
        {   
            if (!NativeGallery.IsMediaPickerBusy())
            {
                string finalFileName = GetVideoFileName(fileName);
                // 创建 UniTaskCompletionSource 来等待回调
                var completionSource = new UniTaskCompletionSource<VideoSaveStatus>();
                NativeGallery.SaveVideoToGallery(filePath, "FantasyTile_Gallery", finalFileName, (success, path) =>
                {
                    if (success)
                    {
                        Debug.Log($"视频已保存到相册: {path}");
                        completionSource.TrySetResult(VideoSaveStatus.Success);
                    }
                    else
                    {
                        Debug.LogError("视频保存失败");
                        completionSource.TrySetResult(VideoSaveStatus.FileError);
                    }
                });
                // 等待回调完成
                return await completionSource.Task;
            }

            return VideoSaveStatus.Failed;
        }
        
        /// <summary>
        /// 异步请求Android相册写入权限（视频）
        /// </summary>
        private static async UniTask<NativeGallery.Permission> RequestAndroidVideoWritePermissionAsync()
        {
            // 创建 UniTaskCompletionSource 来等待权限回调
            var completionSource = new UniTaskCompletionSource<NativeGallery.Permission>();
            
            // 请求权限
            NativeGallery.RequestPermissionAsync((permission) =>
            {
                completionSource.TrySetResult(permission);
            }, NativeGallery.PermissionType.Write, NativeGallery.MediaType.Video);
            
            // 等待权限结果，设置超时时间为30秒
            try
            {
                var timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
                
                return await completionSource.Task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Android视频权限请求超时");
                return NativeGallery.Permission.Denied;
            }
        }
        /// <summary>
        /// 获取安卓视频相册路径
        /// </summary>
        private static string GetAndroidVideoGalleryPath()
        {
            string galleryPath;
            
            // 使用Android API获取公共视频目录
            using (var environment = new AndroidJavaClass("android.os.Environment"))
            using (var moviesDir = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", 
                environment.GetStatic<string>("DIRECTORY_MOVIES")))
            {
                galleryPath = moviesDir.Call<string>("getAbsolutePath");
            }

            // 创建应用专属子文件夹
            string appFolder = Path.Combine(galleryPath, Application.productName);
            return appFolder;
        }

        /// <summary>
        /// 保存到iOS相册
        /// </summary>
        private static async UniTask<VideoSaveStatus> SaveVideoToIOSGallery(string filePath, string fileName)
        {
            try
            {
                // 检查是否正在进行媒体选择器操作
                if (NativeGallery.IsMediaPickerBusy())
                {
                    Debug.LogWarning("媒体选择器正忙，请稍后再试");
                    return VideoSaveStatus.Failed;
                }

                string finalFileName = GetVideoFileName(fileName);
                
                // 创建 UniTaskCompletionSource 来等待回调
                var completionSource = new UniTaskCompletionSource<VideoSaveStatus>();
                
                // 使用 NativeGallery 保存视频到 iOS 相册
                // 注意：iOS 14+ 由于权限限制，视频会保存到默认相册而不是指定的相册
                NativeGallery.SaveVideoToGallery(filePath, "FantasyTile_Gallery", finalFileName, (success, path) =>
                {
                    if (success)
                    {
                        Debug.Log($"视频已保存到iOS相册: {path}");
                        completionSource.TrySetResult(VideoSaveStatus.Success);
                    }
                    else
                    {
                        Debug.LogError("iOS视频保存失败");
                        completionSource.TrySetResult(VideoSaveStatus.FileError);
                    }
                });
                
                return await completionSource.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("保存视频到iOS相册操作已取消");
                return VideoSaveStatus.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存视频到iOS相册失败: {e.Message}");
                return VideoSaveStatus.Failed;
            }
        }

        /// <summary>
        /// 异步请求iOS相册写入权限（视频）
        /// </summary>
        /// <returns>权限状态</returns>
        private static async UniTask<NativeGallery.Permission> RequestiOSVideoWritePermissionAsync()
        {
            // 先检查当前权限状态
            bool hasPermission = NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Video);
            if (hasPermission)
            {
                return NativeGallery.Permission.Granted;
            }

            // 创建 UniTaskCompletionSource 来等待权限回调
            var completionSource = new UniTaskCompletionSource<NativeGallery.Permission>();
            
            // 请求权限
            NativeGallery.RequestPermissionAsync((permission) =>
            {
                completionSource.TrySetResult(permission);
            }, NativeGallery.PermissionType.Write, NativeGallery.MediaType.Video);
            
            // 等待权限结果，设置超时时间为30秒
            try
            {
                var timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
                
                return await completionSource.Task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("iOS视频权限请求超时");
                return NativeGallery.Permission.Denied;
            }
        }

        /// <summary>
        /// 保存到本地目录（编辑器或其他平台使用）
        /// </summary>
        private static async UniTask<bool> SaveVideoToLocalDirectory(string filePath, string fileName)
        {
            try
            {
                string finalFileName = GetVideoFileName(fileName);
                string savePath = Path.Combine(Application.persistentDataPath, "SavedVideos", finalFileName);
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 在后台线程复制文件
                await UniTask.RunOnThreadPool(() =>
                {
                    File.Copy(filePath, savePath, true);
                });

                Debug.Log($"视频已保存到本地: {savePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存视频到本地失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 通知媒体扫描器刷新相册（安卓）
        /// </summary>
        private static void RefreshAndroidGallery(string filePath)
        {
            try
            {
                using (var mediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection"))
                using (var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var currentActivity = context.GetStatic<AndroidJavaObject>("currentActivity");
                    mediaScannerConnection.CallStatic("scanFile", currentActivity, 
                        new string[] { filePath }, new string[] { "video/mp4" }, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"媒体扫描器刷新失败: {e.Message}");
            }
        }

        /// <summary>
        /// 生成视频文件名
        /// </summary>
        private static string GetVideoFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return $"VIDEO_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            }
            
            return fileName.EndsWith(".mp4") ? fileName : fileName + ".mp4";
        }
        
        /// <summary>
        /// 获取视频缓存键
        /// </summary>
        private static string GetVideoCacheKey(string url)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(url);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        private static async UniTask CleanupTempFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await UniTask.RunOnThreadPool(() =>
                    {
                        File.Delete(filePath);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"清理临时文件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 取消指定URL的下载
        /// </summary>
        public static void CancelDownload(string url)
        {
            string cacheKey = GetVideoCacheKey(url);
            // 注意：实际取消需要通过CancellationToken实现
            // 这里只是从正在进行的任务字典中移除
            ongoingVideoDownloads.Remove(cacheKey);
        }

        /// <summary>
        /// 获取文件大小（用于显示预估下载大小）
        /// </summary>
        public static async UniTask<long> GetVideoFileSize(string url)
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Head(url))
                {
                    await request.SendWebRequest().ToUniTask();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string contentLength = request.GetResponseHeader("Content-Length");
                        if (long.TryParse(contentLength, out long fileSize))
                        {
                            return fileSize;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"获取视频文件大小失败: {e.Message}");
            }

            return 0;
        }
    }
}