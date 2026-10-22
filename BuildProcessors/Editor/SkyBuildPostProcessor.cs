using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Sky;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

#if UNITY_ANDROID && !LAUNCHER_AD

namespace Sky
{
	public class SkyAndroid : IPostGenerateGradleAndroidProject
	{
		public int callbackOrder => int.MaxValue;

		public void OnPostGenerateGradleAndroidProject(string path)
		{
#if UNITY_2019_3_OR_NEWER
			// 【修改点1】：在这里定义你想强制移除的所有高危权限
			string[] dangerousPermissions = new string[] {
				"android.permission.QUERY_ALL_PACKAGES",
				"android.permission.REQUEST_INSTALL_PACKAGES"
                // 如果以后发现还有其他流氓权限，直接在这里加一行即可
            };

			// 1. 无论有没有 Admob ID，第一步先强制干掉高危权限！
			RemovePermissions(path, dangerousPermissions);

			// 2. 下面再走原来的 Admob 逻辑
			var projectAdmobAppId = SkyConfigMgr.Instance.GetAdmobAppId();
			Debug.Log("=== 当前读取到的 AdMob ID 是: " + projectAdmobAppId + " ===");
			if (string.IsNullOrEmpty(projectAdmobAppId))
			{
				Debug.LogError("=== [调试] ID为空！请检查 SkyConfigMgr 是否加载了 nonchina 的 JSON 数据 ===");
				Debug.Log("=== 未配置 AdMob ID，跳过 Manifest 修改 ===");
				return; // 这里放心 return，因为权限移除在上一步已经执行完了
			}

			AddAdmobAppId(path);
#endif
		}

		// 【修改点2】：将原来的 FixQueryAllPackagesPermission 替换为这个通用的 RemovePermissions 方法
		private void RemovePermissions(string path, string[] permissionNames)
		{
			var appManifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
			if (!File.Exists(appManifestPath))
			{
				Debug.LogError($"=== [PostProcess] 找不到 Manifest 文件: {appManifestPath} ===");
				return;
			}

			var manifestFile = new XmlDocument();
			manifestFile.Load(appManifestPath);

			var manifestNode = manifestFile.SelectSingleNode("/manifest") as XmlElement;
			if (manifestNode == null) return;

			// 检查根节点是否声明了 xmlns:tools，如果没有则补上，防止打包报错
			const string toolsNamespace = "http://schemas.android.com/tools";
			if (string.IsNullOrEmpty(manifestNode.GetAttribute("xmlns:tools")))
			{
				var xmlnsAttr = manifestFile.CreateAttribute("xmlns", "tools", "http://www.w3.org/2000/xmlns/");
				xmlnsAttr.Value = toolsNamespace;
				manifestNode.SetAttributeNode(xmlnsAttr);
				Debug.Log("=== [PostProcess] 向 Manifest 补充了 xmlns:tools 命名空间 ===");
			}

			// 遍历数组，挨个强制移除权限
			foreach (var permissionName in permissionNames)
			{
				var xpath = $"/manifest/uses-permission[@android:name='{permissionName}']";
				var node = manifestFile.DocumentElement?.SelectSingleNode(xpath, GetNamespaceManager(manifestFile));

				// 1. 如果主 Manifest 已经存在这个权限，先把它删掉（清理 Unity 或你自己写的）
				if (node != null)
				{
					node.ParentNode?.RemoveChild(node);
					Debug.Log($"=== [PostProcess] 从主 Manifest 移除了 {permissionName} ===");
				}

				// 2. 关键一步：强行注入带有 tools:node="remove" 的节点，用来对抗第三方广告SDK的合并
				var newPermissionNode = manifestFile.CreateElement("uses-permission");

				// 添加 android:name 属性
				var nameAttr = manifestFile.CreateAttribute("android", "name", "http://schemas.android.com/apk/res/android");
				nameAttr.InnerText = permissionName;
				newPermissionNode.SetAttributeNode(nameAttr);

				// 添加 tools:node="remove" 属性
				var toolsAttr = manifestFile.CreateAttribute("tools", "node", toolsNamespace);
				toolsAttr.InnerText = "remove";
				newPermissionNode.SetAttributeNode(toolsAttr);

				manifestNode.AppendChild(newPermissionNode);
				Debug.Log($"=== [PostProcess] 成功注入 {permissionName} 的 tools:node='remove' 声明 ===");
			}

			// 最后保存文件
			manifestFile.Save(appManifestPath);
		}

		// ... 后面的 AddAdmobAppId 和辅助方法保持完全不变 ...
		private void AddAdmobAppId(string path)
		{
			var appManifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
			var manifestFile = new XmlDocument();
			manifestFile.Load(appManifestPath);
			const string gmsAdsApplicationID = "com.google.android.gms.ads.APPLICATION_ID";
			var admobAppId = GetMetaDataValue(manifestFile, gmsAdsApplicationID);
			var projectAdmobAppId = SkyConfigMgr.Instance.GetAdmobAppId();

			if (string.IsNullOrEmpty(admobAppId))
			{
				AddMetaDataValue(manifestFile, gmsAdsApplicationID, projectAdmobAppId);
			}
			else
			{
				UpdateMetaDataValue(manifestFile, gmsAdsApplicationID, projectAdmobAppId);
			}

			var androidManifestFileArray = GetAllFileWhereSuffix(path, "AndroidManifest.xml");
			foreach (var tempPath in androidManifestFileArray)
			{
				var newText = File.ReadAllText(tempPath).Replace("INSERT_YOUR_ADMOB_APP_ID_HERE", projectAdmobAppId);
				File.WriteAllText(tempPath, newText);
			}

			const string OPTIMIZE_INITIALIZATION = "com.google.android.gms.ads.flag.OPTIMIZE_INITIALIZATION";
			var optimizeInitialization = GetMetaDataValue(manifestFile, OPTIMIZE_INITIALIZATION);
			if (string.IsNullOrEmpty(optimizeInitialization))
			{
				AddMetaDataValue(manifestFile, OPTIMIZE_INITIALIZATION, "true");
			}
			else
			{
				UpdateMetaDataValue(manifestFile, OPTIMIZE_INITIALIZATION, "true");
			}

			const string OPTIMIZE_AD_LOADING = "com.google.android.gms.ads.flag.OPTIMIZE_AD_LOADING";
			var optimizeAdLoading = GetMetaDataValue(manifestFile, OPTIMIZE_AD_LOADING);
			if (string.IsNullOrEmpty(optimizeAdLoading))
			{
				AddMetaDataValue(manifestFile, OPTIMIZE_AD_LOADING, "true");
			}
			else
			{
				UpdateMetaDataValue(manifestFile, OPTIMIZE_AD_LOADING, "true");
			}

			manifestFile.Save(appManifestPath);

			if (string.IsNullOrEmpty(projectAdmobAppId))
			{
				SkyLogUtils.Debug("Google advertising channel was used, but admobId was not configured in the configuration file.");
			}
		}

		private bool UpdateMetaDataValue(XmlDocument manifest, string name, string value)
		{
			var xpath = $"/manifest/application/meta-data[@android:name='{name}']";
			var node = manifest.DocumentElement?.SelectSingleNode(xpath, GetNamespaceManager(manifest));
			if (!(node is XmlElement element)) return false;
			var attributeNode = element.GetAttributeNode("android:value");
			if (attributeNode != null)
			{
				attributeNode.Value = value;
			}
			return false;
		}

		private bool AddMetaDataValue(XmlDocument manifest, string name, string value)
		{
			var metaNode = manifest.CreateElement("meta-data");
			var nameAttribute = manifest.CreateAttribute("android", "name", "http://schemas.android.com/apk/res/android");
			nameAttribute.InnerText = name;
			metaNode.SetAttributeNode(nameAttribute);
			var valueAttribute = manifest.CreateAttribute("android", "value", "http://schemas.android.com/apk/res/android");
			valueAttribute.InnerText = value;
			metaNode.SetAttributeNode(valueAttribute);
			var applicationNode = manifest.SelectSingleNode("/manifest/application");
			if (applicationNode == null) return false;
			applicationNode.AppendChild(metaNode);
			return true;
		}

		private static string GetMetaDataValue(XmlDocument manifest, string name)
		{
			var xpath = $"/manifest/application/meta-data[@android:name='{name}']";
			var node = manifest.DocumentElement?.SelectSingleNode(xpath, GetNamespaceManager(manifest));
			if (node is XmlElement element)
			{
				return element.GetAttribute("android:value");
			}
			return "";
		}

		private static XmlNamespaceManager GetNamespaceManager(XmlDocument manifest)
		{
			var namespaceManager = new XmlNamespaceManager(manifest.NameTable);
			namespaceManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");
			return namespaceManager;
		}

		public static List<string> GetAllFileWhereSuffix(string projectPath, string suffix)
		{
			return ListFiles(projectPath).Where(file => file.EndsWith(suffix)).ToList();
		}

		public static List<string> ListFiles(string path, List<string> fileList = null)
		{
			fileList ??= new List<string>();
			try
			{
				var files = Directory.GetFiles(path);
				fileList.AddRange(files);
				var subdirectories = Directory.GetDirectories(path);
				foreach (var subdirectory in subdirectories)
				{
					ListFiles(subdirectory, fileList);
				}
			}
			catch (Exception e)
			{
			}
			return fileList;
		}
	}
}
#endif
