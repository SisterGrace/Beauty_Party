// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using Unity.EditorCoroutines.Editor;
// using UnityEditor;
// using UnityEditor.Build;
// using UnityEditor.Build.Reporting;
// using UnityEngine;
// using UnityEngine.Networking;

// namespace CoreLite
// {
//     public class CoreDownloadConfig : IPreprocessBuildWithReport, IPostprocessBuildWithReport
//     {
//         public int callbackOrder => 0;
//         public void OnPreprocessBuild(BuildReport report)
//         {
            
//             CoreLogUtils.Debug("开始预构建处理 - 下载配置文件");
            
//             // 根据构建平台确定参数
//             string platform = report.summary.platform.ToString().ToLower();
//             string pkg = PlayerSettings.applicationIdentifier;
            
//             if (platform.Contains("android"))
//             {
//                 platform = "android";
//             }
//             else if (platform.Contains("ios") || platform.Contains("iphone"))
//             {
//                 platform = "ios";
//             }
//             else
//             {
//                 CoreLogUtils.Error($"不支持的构建平台: {platform}");
//                 return;
//             }

//             EditorCoroutineRunner.ExecuteCoroutine(DownloadConfigCoroutine(pkg, platform));
//         }

//         public void OnPostprocessBuild(BuildReport report)
//         {
//             // 构建后处理，可以清理临时文件
//         }

//         private static IEnumerator DownloadConfigCoroutine(string pkg, string platform)
//         {
//             // 确保StreamingAssets目录存在
//             string streamingPath = Application.streamingAssetsPath;
//             if (!Directory.Exists(streamingPath))
//             {
//                 Directory.CreateDirectory(streamingPath);
//             }

//             // 步骤1: 请求下载配置
//             string apiUrl = $"https://dc-api.dgtverse.cn/internal/app/download-config?pkg={Uri.EscapeUriString(pkg)}&platform={platform}";
//             CoreLogUtils.Debug($"请求配置API: {apiUrl}");

//             UnityWebRequest request = UnityWebRequest.Get(apiUrl);
//             request.SetRequestHeader("Authorization", "eyJVc2VySWQiOjEsIklzU3VwZXIiOnRydWUsImV4cCI6MTcxNzQ2ODg5OCwiaWF0IjoxNzE3MzgyNDk4fQ");

//             yield return request.SendWebRequest();

//             if (request.result != UnityWebRequest.Result.Success)
//             {
//                 CoreLogUtils.Error($"API请求失败: {request.error}");
//                 yield break;
//             }

//             string responseText = request.downloadHandler.text;
//             CoreLogUtils.Debug($"API响应: {responseText}");

//             // 解析JSON响应
//             ConfigResponse configResponse = JsonUtility.FromJson<ConfigResponse>(responseText);

//             if (configResponse.code != 0 || string.IsNullOrEmpty(configResponse.data.download_url))
//             {
//                 CoreLogUtils.Error($"获取下载URL失败: {configResponse.msg}");
//                 yield break;
//             }

//             string downloadUrl = configResponse.data.download_url;
//             CoreLogUtils.Debug($"下载URL: {downloadUrl}");
//             yield break;

//             // 步骤2: 下载ZIP文件
//             // yield return DownloadAndProcessZip(downloadUrl, streamingPath, platform);
//         }

//         private static IEnumerator DownloadAndProcessZip(string url, string streamingPath, string platform)
//         {
//             string tempZipPath = Path.Combine(Application.temporaryCachePath, "temp_config.zip");
//             string extractPath = Path.Combine(Application.temporaryCachePath, "extracted_config");
            
//             // 清理临时目录
//             if (File.Exists(tempZipPath))
//             {
//                 File.Delete(tempZipPath);
//             }
//             if (Directory.Exists(extractPath))
//             {
//                 Directory.Delete(extractPath, true);
//             }
//             Directory.CreateDirectory(extractPath);

//             // 下载ZIP文件
//             CoreLogUtils.Debug($"开始下载ZIP文件到: {tempZipPath}");
            
//             UnityWebRequest downloadRequest = UnityWebRequest.Get(url);
//             downloadRequest.downloadHandler = new DownloadHandlerFile(tempZipPath);
            
//             yield return downloadRequest.SendWebRequest();

//             if (downloadRequest.result != UnityWebRequest.Result.Success)
//             {
//                 CoreLogUtils.Error($"下载ZIP文件失败: {downloadRequest.error}");
//                 yield break;
//             }

//             CoreLogUtils.Debug($"ZIP文件下载完成，开始解压...");

//             // 解压ZIP文件
//             try
//             {
//                 System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractPath);
//                 CoreLogUtils.Debug($"解压完成到: {extractPath}");
//             }
//             catch (Exception e)
//             {
//                 CoreLogUtils.Error($"解压ZIP文件失败: {e.Message}");
//                 yield break;
//             }

//             // 处理解压后的文件
//             yield return ProcessExtractedFiles(extractPath, streamingPath, platform);

//             // 清理临时文件
//             try
//             {
//                 File.Delete(tempZipPath);
//                 Directory.Delete(extractPath, true);
//                 CoreLogUtils.Debug("临时文件清理完成");
//             }
//             catch (Exception e)
//             {
//                 CoreLogUtils.Error($"清理临时文件时出错: {e.Message}");
//             }
//         }

//         private static IEnumerator ProcessExtractedFiles(string extractPath, string streamingPath, string platform)
//         {
//             // 查找解压后的文件
//             string[] allFiles = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories);
            
//             CoreLogUtils.Debug($"找到 {allFiles.Length} 个文件");

//             // 目标文件名
//             string targetFileName = platform.ToLower() == "ios" 
//                 ? "tkg_config_mainly.plist" 
//                 : "tkg_config_mainly.properties";

//             string targetPath = Path.Combine(streamingPath, targetFileName);

//             // 清理旧的配置文件

            
//             if (File.Exists(targetPath))
//             {
//                 File.Delete(targetPath);
//                 CoreLogUtils.Debug($"已删除旧的配置文件: {targetFileName}");
//             }

//             // 查找并复制合适的文件
//             bool fileCopied = false;
//             foreach (string file in allFiles)
//             {
//                 string fileName = Path.GetFileName(file).ToLower();
                
//                 // 根据平台选择文件
//                 if ((platform.ToLower() == "ios" && (fileName.EndsWith(".plist") || fileName.Contains("ios"))) ||
//                     (platform.ToLower() != "ios" && (fileName.EndsWith(".properties") || fileName.Contains("android"))))
//                 {
//                     File.Copy(file, targetPath, true);
//                     CoreLogUtils.Debug($"已复制文件: {Path.GetFileName(file)} -> {targetFileName}");
//                     fileCopied = true;
//                     break;
//                 }
//             }

//             // 如果没有找到特定文件，复制第一个文件
//             if (!fileCopied && allFiles.Length > 0)
//             {
//                 File.Copy(allFiles[0], targetPath, true);
//                 CoreLogUtils.Debug($"已复制第一个文件: {Path.GetFileName(allFiles[0])} -> {targetFileName}");
//                 fileCopied = true;
//             }

//             if (fileCopied)
//             {
//                 CoreLogUtils.Debug($"配置文件已成功放置到: {targetPath}");
                
//                 // 刷新AssetDatabase
//                 AssetDatabase.Refresh();
                
//                 // 添加一个标记文件，表示配置已下载
//                 string markerPath = Path.Combine(streamingPath, "config_downloaded.txt");
//                 File.WriteAllText(markerPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Config downloaded for {platform}");
//             }
//             else
//             {
//                 CoreLogUtils.Error("未找到任何配置文件");
//             }

//             yield return null;
//         }

//         [System.Serializable]
//         public class ConfigResponse
//         {
//             public int code;
//             public string msg;
//             public ConfigData data;
//         }

//         [System.Serializable]
//         public class ConfigData
//         {
//             public string download_url;
//         }
//     }
//     public static class EditorCoroutineRunner
//     {
//         private class CoroutineHandler : UnityEditor.EditorWindow
//         {
//             public static void Execute(IEnumerator coroutine)
//             {
//                 var window = GetWindow<CoroutineHandler>();
//                 window.StartCoroutine(coroutine);
//             }
//         }

//         public static void ExecuteCoroutine(IEnumerator coroutine)
//         {
//             CoroutineHandler.Execute(coroutine);
//         }
//     }
// }

