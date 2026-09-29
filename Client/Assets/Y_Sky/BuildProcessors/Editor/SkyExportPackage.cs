using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sky;
using UnityEditor;
using UnityEngine;

namespace Sky
{
	/// <summary>
	/// 一键导出 UnityPackage，排除 StreamingAssets 及指定 Android 插件文件
	/// </summary>
	public static class SkyExportPackage
	{
		private const string AssetsRoot = "Assets";
		private const string StreamingAssetsPath = "Assets/StreamingAssets";
		private const string PluginsAndroidPath = "Assets/Plugins/Android";

		/// <summary>
		/// 【新增】导出范围过滤：
		///   留空 ""                 → 导出全部（走原有排除逻辑）
		///   填目录如 "Assets/Y_Sky"  → 只导出该目录及其子内容
		/// 注意：目录名大小写需与实际一致（Ordinal 匹配）。
		/// </summary>
		private const string ExportRootFilter = "Assets/Y_Sky";

		/// <summary>导出时是否包含 SkyGameConfigData.asset。不包含则接入方更新 SDK 时不会被覆盖。</summary>
		private const string PrefKeyIncludeGameConfig = "Sky.Export.IncludeGameConfigAsset";
		private static readonly string GameConfigAssetPath = "Assets/Sky/Base/Resources/SkyGameConfigData.asset";

		/// <summary>导出时是否自增 patch 版本号。默认关闭。</summary>
		private const string PrefKeyAutoIncrementVersion = "Sky.Export.AutoIncrementVersion";
		private static readonly string SDKMgrAssetPath = "Assets/Y_Sky/Base/SkySdkMgr.cs";

		/// <summary>
		/// 需要排除的 Plugins/Android 下的文件或目录（不含 Assets/Plugins/Android/ 前缀时按名称匹配）
		/// </summary>
		private static readonly HashSet<string> ExcludedAndroidRelative = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"AndroidManifest.xml",
			"baseProjectTemplate.gradle",
			"FirebaseApp.androidlib",
			"FirebaseCrashlytics.androidlib",
			"gradleTemplate.properties",
			"LauncherManifest.xml",
			"launcherTemplate.gradle",
			"mainTemplate.gradle",
			"MessagingUnityPlayerActivity.java",
			"settingsTemplate.gradle",
		};

		/* private const string MenuPathIncludeGameConfig = "Sky/导出设置/更新时覆盖游戏配置模板";

		 /// <summary>切换：导出 unitypackage 时是否包含 SkyGameConfigData.asset（包含则接入方更新时会被覆盖）</summary>
		 [MenuItem(MenuPathIncludeGameConfig, true, 0)]
		 public static bool ToggleIncludeGameConfigValidate()
		 {
			 Menu.SetChecked(MenuPathIncludeGameConfig, EditorPrefs.GetBool(PrefKeyIncludeGameConfig, false));
			 return true;
		 }

		 [MenuItem(MenuPathIncludeGameConfig, false, 0)]
		 public static void ToggleIncludeGameConfig()
		 {
			 bool next = !EditorPrefs.GetBool(PrefKeyIncludeGameConfig, false);
			 EditorPrefs.SetBool(PrefKeyIncludeGameConfig, next);
			 string msg = next
				 ? "已开启：导出包将包含 SkyGameConfigData.asset，接入方更新 SDK 时会被覆盖。"
				 : "已关闭：导出包不包含该配置，接入方更新 SDK 时不会被覆盖。";
			 EditorUtility.DisplayDialog("Sky 导出设置", msg, "确定");
		 }*/

		private const string MenuPathAutoIncrementVersion = "Sky/导出设置/导出时自增版本号";

		[MenuItem(MenuPathAutoIncrementVersion, true, 1)]
		public static bool ToggleAutoIncrementVersionValidate()
		{
			Menu.SetChecked(MenuPathAutoIncrementVersion, EditorPrefs.GetBool(PrefKeyAutoIncrementVersion, false));
			return true;
		}

		[MenuItem(MenuPathAutoIncrementVersion, false, 1)]
		public static void ToggleAutoIncrementVersion()
		{
			bool next = !EditorPrefs.GetBool(PrefKeyAutoIncrementVersion, false);
			EditorPrefs.SetBool(PrefKeyAutoIncrementVersion, next);
			string msg = next
				? "已开启：导出时将自动递增 patch 版本号（如 1.3.6 → 1.3.7）。"
				: "已关闭：导出时不修改版本号。";
			EditorUtility.DisplayDialog("Sky 导出设置", msg, "确定");
		}

		[MenuItem("Sky/Export UnityPackage", false, 100)]
		[MenuItem("Sky/Export UnityPackage", false, 100)]
		public static void ExportPackage()
		{
			// 1. 先收集要导出的资源列表（string[]）
			var allPaths = AssetDatabase.GetAllAssetPaths()
				.Where(p => p.StartsWith(AssetsRoot + "/", StringComparison.Ordinal))
				.Where(ShouldIncludeAsset)
				.ToArray();

			// 2. 决定版本号：开关打开则自增，且直接用返回的新值，避免编译延迟
			string version = null;
			if (EditorPrefs.GetBool(PrefKeyAutoIncrementVersion, false))
			{
				version = IncrementPatchVersion();
				if (version != null)
					UnityEngine.Debug.Log($"[SkyExportPackage] 版本号已自增: {version}");
				else
					UnityEngine.Debug.LogWarning("[SkyExportPackage] 版本号自增失败，将使用当前版本导出。");
			}
			if (string.IsNullOrEmpty(version))
				version = SkySdkMgr.GetSDKVersion();

			// 3. 拼文件名 & 导出路径
			string commitHash = GetLatestGitCommitHash();
			string fileName = $"Sky_{version}_{commitHash}.unitypackage";

			string projectPath = Application.dataPath;
			string projectRoot = Directory.GetParent(projectPath)?.FullName ?? projectPath;
			string exportPath = Path.Combine(projectRoot, fileName);

			// 4. 导出
			AssetDatabase.ExportPackage(allPaths, exportPath, ExportPackageOptions.Recurse);

			EditorUtility.DisplayDialog("导出完成", $"已导出: {fileName}\n路径: {exportPath}", "确定");
			EditorUtility.RevealInFinder(exportPath);
		}

		/// <summary>
		/// 读取 SkySdkMgr.cs 源文件，将 GetSDKVersion 中的版本号 patch 段 +1 并写回，
		/// 然后刷新 AssetDatabase 使新版本生效。成功返回新版本号，失败返回 null。
		/// </summary>
		private static string IncrementPatchVersion()
		{
			string fullPath = Path.Combine(Application.dataPath, "..", SDKMgrAssetPath);
			fullPath = Path.GetFullPath(fullPath);
			if (!File.Exists(fullPath)) return null;

			string content = File.ReadAllText(fullPath);
			var regex = new Regex(@"(return\s+"")(\d+)\.(\d+)\.(\d+)(""\s*;)");
			var match = regex.Match(content);
			if (!match.Success) return null;

			int major = int.Parse(match.Groups[2].Value);
			int minor = int.Parse(match.Groups[3].Value);
			int patch = int.Parse(match.Groups[4].Value) + 1;
			string newVersion = $"{major}.{minor}.{patch}";

			content = regex.Replace(content, $"${{1}}{newVersion}${{5}}", 1);
			File.WriteAllText(fullPath, content);
			AssetDatabase.ImportAsset(SDKMgrAssetPath, ImportAssetOptions.ForceUpdate);

			return newVersion;
		}

		private static string GetLatestGitCommitHash()
		{
			try
			{
				var psi = new ProcessStartInfo("git", "log -1 --format=%h")
				{
					WorkingDirectory = Application.dataPath,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using (var proc = Process.Start(psi))
				{
					string output = proc.StandardOutput.ReadToEnd().Trim();
					proc.WaitForExit();
					if (proc.ExitCode == 0 && !string.IsNullOrEmpty(output))
						return output;
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogWarning($"[SkyExportPackage] 获取 git commit hash 失败: {e.Message}");
			}
			return DateTime.Now.ToString("yyyyMMddHHmmss");
		}

		private static bool ShouldIncludeAsset(string assetPath)
		{
			// 【新增】范围过滤：指定目录则只导出该目录，留空则全量导出。
			// 不含目录节点本身（用 "/" 前缀），避免 Recurse 递归导出整个目录导致后续排除规则失效。
			if (!string.IsNullOrEmpty(ExportRootFilter))
			{
				if (!assetPath.StartsWith(ExportRootFilter + "/", StringComparison.Ordinal))
					return false;
			}

			// ===== 以下为原有排除逻辑，保留不变，作为二级过滤 =====

			// 排除 StreamingAssets 整个目录及其下所有内容（若传入目录会递归导出，必须排除）
			if (assetPath.StartsWith(StreamingAssetsPath + "/", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(assetPath, StreamingAssetsPath, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// SDK 控制：是否在包内包含游戏配置。不包含则接入方更新 unitypackage 时不会覆盖其已配置的 SkyGameConfigData.asset
			if (!EditorPrefs.GetBool(PrefKeyIncludeGameConfig, false))
			{
				if (string.Equals(assetPath, GameConfigAssetPath, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(assetPath, GameConfigAssetPath + ".meta", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			// 不传入 Plugins 或 Plugins/Android 目录本身，否则 Recurse 会导出整个目录导致排除失效
			const string pluginsPath = "Assets/Plugins";
			if (string.Equals(assetPath, pluginsPath, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(assetPath, PluginsAndroidPath, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// 排除 Plugins/Android 下指定文件/目录（含对应 .meta）
			if (assetPath.StartsWith(PluginsAndroidPath + "/", StringComparison.OrdinalIgnoreCase))
			{
				string relative = assetPath.Substring(PluginsAndroidPath.Length + 1);
				foreach (string excluded in ExcludedAndroidRelative)
				{
					if (string.Equals(relative, excluded, StringComparison.OrdinalIgnoreCase))
						return false;
					if (string.Equals(relative, excluded + ".meta", StringComparison.OrdinalIgnoreCase))
						return false;
					// 目录：如 FirebaseApp.androidlib/xxx 或 xxx.meta
					if (relative.StartsWith(excluded + "/", StringComparison.OrdinalIgnoreCase))
						return false;
				}
			}

			return true;
		}
	}
}
