#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace CoreLite
{
	public class CoreIosBuildPostProcessor
	{
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
		{
			if (buildTarget != BuildTarget.iOS) return;

#if UNITY_IOS

			SetTeamIdAutoSign(path);
			ModifyFrameworks(path);
			ModifyBuildSettings(path);
			ModifyInfoPlist(path);
			ModifyUnitySettings(path);
			ModifyURLTypes(path);
			// ModifyCapability(path);
			// AddGameCenterEntitlements(path);
			// AddFirebaseShellFile(path);
			ModifyBundle(path);
#endif
		}

		private static void AddFirebaseShellFile(string path)
		{
			string sourcePath = Path.Combine(Application.dataPath, "Plugins/iOS/run");
			string sourcePath2 = Path.Combine(Application.dataPath, "Plugins/iOS/upload-symbols");
			if (!File.Exists(sourcePath)|| !File.Exists(sourcePath2))
			{
				Debug.LogWarning("run 文件未找到，请确保文件位于 Assets/Plugins/iOS/ 或 Assets/ 目录下");
				return;
			}
			string destPath = Path.Combine(path, "run");
			string destPath2 = Path.Combine(path, "upload-symbols");

			try
			{
				File.Copy(sourcePath, destPath, true);
				File.Copy(sourcePath2, destPath2, true);

			}
			catch (System.Exception e)
			{
				Debug.LogError($"复制 run 文件失败: {e.Message}");
			}
		}
		
		[PostProcessBuildAttribute(45)]//must be between 40 and 50 to ensure that it's not overriden by Podfile generation (40) and that it's added before "pod install" (50)
		private static void PostProcessBuild_iOS(BuildTarget target, string buildPath)
		{
			if (target == BuildTarget.iOS)
			{
				using (StreamWriter sw = File.AppendText(buildPath + "/Podfile"))
				{   
					sw.WriteLine("\nuse_frameworks!");
				}
			}
		}
		private const string entitlementsPath = "game.entitlements";
		private static void ModifyCapability(string projectPath)
		{
			var capabilityManager = new ProjectCapabilityManager(PBXProject.GetPBXProjectPath(projectPath), entitlementsPath, "Unity-iPhone");
			capabilityManager.AddSignInWithApple();
			capabilityManager.AddPushNotifications(false);
			capabilityManager.WriteToFile();
		}

		static void AddGameCenterEntitlements(string path)
		{
			string projPath = PBXProject.GetPBXProjectPath(path);
			PBXProject proj = new PBXProject();
			proj.ReadFromFile(projPath);
			var mainTarget = proj.GetUnityMainTargetGuid();
			string entitlementPath = Path.Combine(path, entitlementsPath); ;
			PlistDocument entitlementsDoc = new PlistDocument();
			entitlementsDoc.ReadFromString(File.ReadAllText(entitlementPath));
			PlistElementDict entitlementDict = entitlementsDoc.root;
			entitlementDict.SetBoolean("com.apple.developer.game-center",true);
			File.WriteAllText(entitlementPath, entitlementsDoc.WriteToString());


		}

		private static void ModifyFrameworks(string _path)
		{
#if UNITY_IOS
		string projPath = PBXProject.GetPBXProjectPath(_path);
		PBXProject proj = new PBXProject();


		proj.ReadFromString(File.ReadAllText(projPath));
#if UNITY_2019_3_OR_NEWER
		string target = proj.GetUnityFrameworkTargetGuid();
#else
		string target = proj.TargetGuidByName("Unity-iPhone");
#endif

		proj.AddFrameworkToProject(target, "GLKit.framework", false);
		proj.AddFrameworkToProject(target, "CoreGraphics.framework", false);
		proj.AddFrameworkToProject(target, "SystemConfiguration.framework", false);
		proj.AddFrameworkToProject(target, "Photos.framework", false);
		proj.AddFrameworkToProject(target, "AppTrackingTransparency.framework", false);
		proj.AddFrameworkToProject(target, "StoreKit.framework", false);
		proj.AddFrameworkToProject(target, "CoreData.framework", false);
		proj.AddFrameworkToProject(target, "AVFoundation.framework", false);
		proj.AddFrameworkToProject(target, "Accelerate.framework", false);
		proj.AddFrameworkToProject(target, "AdSupport.framework", false);
		proj.AddFrameworkToProject(target, "AudioToolbox.framework", false);
		proj.AddFrameworkToProject(target, "CoreLocation.framework", false);
		proj.AddFrameworkToProject(target, "CoreMedia.framework", false);
		proj.AddFrameworkToProject(target, "CoreMotion.framework", false);
		proj.AddFrameworkToProject(target, "CoreTelephony.framework", false);
		proj.AddFrameworkToProject(target, "MobileCoreServices.framework", false);
		proj.AddFrameworkToProject(target, "SafariServices.framework", false);
		proj.AddFrameworkToProject(target, "VideoToolbox.framework", false);
		proj.AddFrameworkToProject(target, "WebKit.framework", false);
		proj.AddFrameworkToProject(target, "DeviceCheck.framework", false);


		string fileGuidSqlite = proj.AddFile("usr/lib/libxml2.tbd", "Libraries/libxml2.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libsqlite3.tbd", "Libraries/libsqlite3.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libc++.tbd", "Libraries/libc++.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libz.tbd", "Libraries/libz.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libresolv.tbd", "Libraries/libresolv.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libbz2.1.0.tbd", "Libraries/libbz2.1.0.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libresolv.9.tbd", "Libraries/libresolv.9.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);
		fileGuidSqlite = proj.AddFile("usr/lib/libc++abi.tbd", "Libraries/libc++abi.tbd", PBXSourceTree.Sdk);
		proj.AddFileToBuild(target, fileGuidSqlite);

		

#if UNITY_2019_3_OR_NEWER
		string mainTarget = proj.GetUnityMainTargetGuid();
#else
		string targetName = UnityEditor.iOS.Xcode.PBXProject.GetUnityTargetName();
		string mainTarget = proj.TargetGuidByName(targetName);
#endif

#endif
		}


		private static void ModifyUnitySettings(string _path)
		{
#if UNITY_IOS
		string projPath = PBXProject.GetPBXProjectPath(_path);
		PBXProject proj = new PBXProject();
		proj.ReadFromString(File.ReadAllText(projPath));
		string target = proj.GetUnityMainTargetGuid();
		proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
		File.WriteAllText(projPath, proj.WriteToString());

#endif
		}

		private static void ModifyBundle(string _path)
		{
#if UNITY_IOS
		string projectPath = PBXProject.GetPBXProjectPath(_path);

		PBXProject pbxProject = new PBXProject();
		pbxProject.ReadFromFile(projectPath);
		// 获取主 target
#if UNITY_2019_3_OR_NEWER
		string mainTarget = pbxProject.GetUnityMainTargetGuid();
#else
			string targetName = UnityEditor.iOS.Xcode.PBXProject.GetUnityTargetName();
			string mainTarget = pbxProject.TargetGuidByName(targetName);
#endif
		EmbedDynamicLibrariesIfNeeded(_path, pbxProject, mainTarget);

		pbxProject.WriteToFile(projectPath);
#endif
		}

		private static void EmbedDynamicLibrariesIfNeeded(string buildPath, PBXProject project, string targetGuid)
		{
			var dynamicLibraryPathsPresentInProject = DynamicLibraryPathsToEmbed;

#if UNITY_2019_3_OR_NEWER
			foreach (var dynamicLibraryPath in dynamicLibraryPathsPresentInProject)
			{
				var fileGuid = project.AddFile(dynamicLibraryPath, dynamicLibraryPath);
				project.AddFileToEmbedFrameworks(targetGuid, fileGuid);
			}
#else
            string runpathSearchPaths;
#if UNITY_2018_2_OR_NEWER
            runpathSearchPaths = project.GetBuildPropertyForAnyConfig(targetGuid, "LD_RUNPATH_SEARCH_PATHS");
#else
            runpathSearchPaths = "$(inherited)";          
#endif
            runpathSearchPaths += string.IsNullOrEmpty(runpathSearchPaths) ? "" : " ";

            // Check if runtime search paths already contains the required search paths for dynamic libraries.
            if (runpathSearchPaths.Contains("@executable_path/Frameworks")) return;

            runpathSearchPaths += "@executable_path/Frameworks";
            project.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", runpathSearchPaths);
#endif
		}

		private static List<string> DynamicLibraryPathsToEmbed
		{
			get
			{
				var dynamicLibraryPathsToEmbed = new List<string>();
				// dynamicLibraryPathsToEmbed.Add("Pods/AdjustSignature/AdjustSigSdk.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKOutstream.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKBanner.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKCore.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKInterstitial.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKNative.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKRewardedAds.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKVideo.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKInAppBidding.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKRichMedia.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/SmaatoSDKOpenMeasurement.xcframework");
                // dynamicLibraryPathsToEmbed.Add("Pods/ATOM-Standalone/ATOM.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/InMobiSDK/InMobiSDK.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/AppLovinSDK/applovin-ios-sdk-13.5.1/AppLovinSDK.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/ATOM-Standalone/ATOM.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/MolocoSDKiOS/MolocoSDK.xcframework/ios-arm64/MolocoSDK.framework");
				// dynamicLibraryPathsToEmbed.Add("Pods/OMSDK_Appodeal/Static/OMSDK_Appodeal.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/HyBid/PubnativeLite/PubnativeLite/OMSDK-1.5.4/OMSDK_Pubnativenet.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/vendor/OMSDK_Smaato.xcframework");

				// dynamicLibraryPathsToEmbed.Add("Pods/HyBid/PubnativeLite/PubnativeLite/OMSDK-1.5.2/OMSDK_Pubnativenet.xcframework");
				// dynamicLibraryPathsToEmbed.Add("Pods/smaato-ios-sdk/vendor/OMSDK_Smaato.xcframework");
                return dynamicLibraryPathsToEmbed;
			}
		}
		
		private static void ModifyBuildSettings(string _path)
		{
#if UNITY_IOS
			string projPath = PBXProject.GetPBXProjectPath(_path);
			PBXProject proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));

#if UNITY_2019_3_OR_NEWER
			string target = proj.GetUnityFrameworkTargetGuid();
#else
		string target = proj.TargetGuidByName("Unity-iPhone");
#endif

			proj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
			proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
			proj.SetBuildProperty(target, "GCC_C_LANGUAGE_STANDARD", "gnu99");
			proj.SetBuildProperty(target, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
			proj.SetBuildProperty(target, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
			proj.SetBuildProperty(target, "EXCLUDED_ARCHS", "EXCLUDED_ARCHS__EFFECTIVE_PLATFORM_SUFFIX_simulator__NATIVE_ARCH_64_BIT_x86_64=arm64 arm64e armv7 armv7s armv6 armv8 EXCLUDED_ARCHS=$(inherited) $(EXCLUDED_ARCHS__EFFECTIVE_PLATFORM_SUFFIX_$(EFFECTIVE_PLATFORM_SUFFIX)__NATIVE_ARCH_64_BIT_$(NATIVE_ARCH_64_BIT))");
			File.WriteAllText(projPath, proj.WriteToString());
#endif
		}

		private static void SetTeamIdAutoSign(string _path)
		{
#if UNITY_IOS
		string projPath = PBXProject.GetPBXProjectPath(_path);
		PBXProject proj = new PBXProject();
		proj.ReadFromString(File.ReadAllText(projPath));
		File.WriteAllText(projPath, proj.WriteToString());
#endif
		}

		private static void ModifyInfoPlist(string _path)
		{
#if UNITY_IOS
		//2、修改Info.plist文件
		var plistPath = Path.Combine(_path, "Info.plist");
		var plist = new PlistDocument();
		plist.ReadFromFile(plistPath);

		plist.root.SetString("NSCalendarsUsageDescription", "for better experience the game");
		plist.root.SetString("NSLocationAlwaysUsageDescription", "for better experience the game");
		plist.root.SetString("NSCameraUsageDescription", "for better experience the game");
		plist.root.SetString("NSLocationWhenInUseUsageDescription", "for better experience the game");
		plist.root.SetString("NSUserTrackingUsageDescription", "This identifier will be used to deliver personalized ads to you.");
		plist.root.SetString("NSPhotoLibraryUsageDescription", "In order to experience the game better, please allow the APP to save pictures to your album.");
		plist.root.SetString("GADApplicationIdentifier", CoreConfigMgr.Instance.GetAdmobAppId());

		File.WriteAllText(plistPath, plist.WriteToString());
#endif
		}

		private static void ModifyURLTypes(string _path)
		{
			var plistPath = Path.Combine(_path, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);
			plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

			PlistElementArray urlTypes = plist.root.CreateArray("CFBundleURLTypes");

			PlistElementDict dict = urlTypes.AddDict();
			dict.SetString("CFBundleURLName", "ooopscroco");
			PlistElementArray schemesArray = dict.CreateArray("CFBundleURLSchemes");
			schemesArray.AddString("ooopscroco");

			//NSAppTransportSecurity set yes
			string pPath = Path.Combine(_path, "Info.plist");
			if (File.Exists(plistPath))
			{
				PlistDocument doc = new PlistDocument();
				doc.ReadFromFile(plistPath);
				PlistElementDict rootDict = doc.root.AsDict();
				if (rootDict != null)
				{
					if (rootDict["NSAppTransportSecurity"] != null)
					{

						PlistElementDict atf = plist.root["NSAppTransportSecurity"].AsDict();
						atf.SetBoolean("NSAllowsArbitraryLoads", true);
					}

					doc.WriteToFile(plistPath);
				}
			}


			#region iOS 14
			PlistElementArray URLWhiteListArr2 = plist.root.CreateArray("SKAdNetworkItems");

			//admob （1）
			PlistElementDict dic01;
			dic01 = URLWhiteListArr2.AddDict();
			dic01.SetString("SKAdNetworkIdentifier", "cstr6suwn9.skadnetwork");

			//ironsource（1）
			PlistElementDict dic02;
			dic02 = URLWhiteListArr2.AddDict();
			dic02.SetString("SKAdNetworkIdentifier", "su67r6k2v3.skadnetwork");

			//mintegral  （19）
			PlistElementDict dic03;
			dic03 = URLWhiteListArr2.AddDict();
			dic03.SetString("SKAdNetworkIdentifier", "kbd757ywx3.skadnetwork");

			PlistElementDict dic04;
			dic04 = URLWhiteListArr2.AddDict();
			dic04.SetString("SKAdNetworkIdentifier", "wg4vff78zm.skadnetwork");

			PlistElementDict dic05;
			dic05 = URLWhiteListArr2.AddDict();
			dic05.SetString("SKAdNetworkIdentifier", "737z793b9f.skadnetwork");

			PlistElementDict dic06;
			dic06 = URLWhiteListArr2.AddDict();
			dic06.SetString("SKAdNetworkIdentifier", "ydx93a7ass.skadnetwork");

			PlistElementDict dic07;
			dic07 = URLWhiteListArr2.AddDict();
			dic07.SetString("SKAdNetworkIdentifier", "prcb7njmu6.skadnetwork");

			PlistElementDict dic08;
			dic08 = URLWhiteListArr2.AddDict();
			dic08.SetString("SKAdNetworkIdentifier", "7ug5zh24hu.skadnetwork");

			PlistElementDict dic09;
			dic09 = URLWhiteListArr2.AddDict();
			dic09.SetString("SKAdNetworkIdentifier", "44jx6755aq.skadnetwork");
			
			PlistElementDict dic10;
			dic10 = URLWhiteListArr2.AddDict();
			dic10.SetString("SKAdNetworkIdentifier", "w9q455wk68.skadnetwork");
			
			PlistElementDict dic11;
			dic11 = URLWhiteListArr2.AddDict();
			dic11.SetString("SKAdNetworkIdentifier", "8s468mfl3y.skadnetwork");

			PlistElementDict dic12;
			dic12 = URLWhiteListArr2.AddDict();
			dic12.SetString("SKAdNetworkIdentifier", "glqzh8vgby.skadnetwork");

			PlistElementDict dic13;
			dic13 = URLWhiteListArr2.AddDict();
			dic13.SetString("SKAdNetworkIdentifier", "c6k4g5qg8m.skadnetwork");

			PlistElementDict dic14;
			dic14 = URLWhiteListArr2.AddDict();
			dic14.SetString("SKAdNetworkIdentifier", "mlmmfzh3r3.skadnetwork");

			PlistElementDict dic15;
			dic15 = URLWhiteListArr2.AddDict();
			dic15.SetString("SKAdNetworkIdentifier", "4pfyvq9l8r.skadnetwork");

			PlistElementDict dic16;
			dic16 = URLWhiteListArr2.AddDict();
			dic16.SetString("SKAdNetworkIdentifier", "av6w8kgt66.skadnetwork");

			PlistElementDict dic17;
			dic17 = URLWhiteListArr2.AddDict();
			dic17.SetString("SKAdNetworkIdentifier", "6xzpu9s2p8.skadnetwork");

			PlistElementDict dic18;
			dic18 = URLWhiteListArr2.AddDict();
			dic18.SetString("SKAdNetworkIdentifier", "hs6bdukanm.skadnetwork");
			
			//adcolony   （19）
			PlistElementDict dic19;
			dic19 = URLWhiteListArr2.AddDict();
			dic19.SetString("SKAdNetworkIdentifier", "v72qych5uu.skadnetwork");
			
			PlistElementDict dic20;
			dic20 = URLWhiteListArr2.AddDict();
			dic20.SetString("SKAdNetworkIdentifier", "ppxm28t8ap.skadnetwork");
			
			PlistElementDict dic21;
			dic21 = URLWhiteListArr2.AddDict();
			dic21.SetString("SKAdNetworkIdentifier", "klf5c3l5u5.skadnetwork");
			
			PlistElementDict dic22;
			dic22 = URLWhiteListArr2.AddDict();
			dic22.SetString("SKAdNetworkIdentifier", "4468km3ulz.skadnetwork");

			PlistElementDict dic23;
			dic23 = URLWhiteListArr2.AddDict();
			dic23.SetString("SKAdNetworkIdentifier", "3rd42ekr43.skadnetwork");

			//unity ads (2)
			PlistElementDict dic24;
			dic24 = URLWhiteListArr2.AddDict();
			dic24.SetString("SKAdNetworkIdentifier", "4dzt52r2t5.skadnetwork");

			PlistElementDict dic25;
			dic25 = URLWhiteListArr2.AddDict();
			dic25.SetString("SKAdNetworkIdentifier", "bvpn9ufa9b.skadnetwork");

			//maio (1)
			PlistElementDict dic26;
			dic26 = URLWhiteListArr2.AddDict();
			dic26.SetString("SKAdNetworkIdentifier", "V4NXQHLYQP.skadnetwork");

			//facebook(2)
			PlistElementDict dic27;
			dic27 = URLWhiteListArr2.AddDict();
			dic27.SetString("SKAdNetworkIdentifier", "v9wttpbfk9.skadnetwork");

			PlistElementDict dic28;
			dic28 = URLWhiteListArr2.AddDict();
			dic28.SetString("SKAdNetworkIdentifier", "n38lu8286q.skadnetwork");
			//vungle (1)
			PlistElementDict dic29;
			dic29 = URLWhiteListArr2.AddDict();
			dic29.SetString("SKAdNetworkIdentifier", "gta9lk7p23.skadnetwork");

			//pangle (2)
			PlistElementDict dic30;
			dic30 = URLWhiteListArr2.AddDict();
			dic30.SetString("SKAdNetworkIdentifier", "238da6jt44.skadnetwork");

			PlistElementDict dic31;
			dic31 = URLWhiteListArr2.AddDict();
			dic31.SetString("SKAdNetworkIdentifier", "22mmun2rn5.skadnetwork");

			//applovin(19)

			PlistElementDict dic32;
			dic32 = URLWhiteListArr2.AddDict();
			dic32.SetString("SKAdNetworkIdentifier", "2u9pt9hc89.skadnetwork");

			PlistElementDict dic33;
			dic33 = URLWhiteListArr2.AddDict();
			dic33.SetString("SKAdNetworkIdentifier", "275upjj5gd.skadnetwork");

			PlistElementDict dic34;
			dic34 = URLWhiteListArr2.AddDict();
			dic34.SetString("SKAdNetworkIdentifier", "4fzdc2evr5.skadnetwork");


			PlistElementDict dic35;
			dic35 = URLWhiteListArr2.AddDict();
			dic35.SetString("SKAdNetworkIdentifier", "9rd848q2bz.skadnetwork");

			PlistElementDict dic36;
			dic36 = URLWhiteListArr2.AddDict();
			dic36.SetString("SKAdNetworkIdentifier", "9t245vhmpl.skadnetwork");

			PlistElementDict dic37;
			dic37 = URLWhiteListArr2.AddDict();
			dic37.SetString("SKAdNetworkIdentifier", "f38h382jlk.skadnetwork");

			PlistElementDict dic38;
			dic38 = URLWhiteListArr2.AddDict();
			dic38.SetString("SKAdNetworkIdentifier", "ludvb6z3bs.skadnetwork");

			PlistElementDict dic39;
			dic39 = URLWhiteListArr2.AddDict();
			dic39.SetString("SKAdNetworkIdentifier", "m8dbw4sv7c.skadnetwork");

			PlistElementDict dic40;
			dic40 = URLWhiteListArr2.AddDict();
			dic40.SetString("SKAdNetworkIdentifier", "t38b2kh725.skadnetwork");

			PlistElementDict dic41;
			dic41 = URLWhiteListArr2.AddDict();
			dic41.SetString("SKAdNetworkIdentifier", "tl55sbb4fm.skadnetwork");

			PlistElementDict dic42;
			dic42 = URLWhiteListArr2.AddDict();
			dic42.SetString("SKAdNetworkIdentifier", "wzmmz9fp6w.skadnetwork");

			PlistElementDict dic43;
			dic43 = URLWhiteListArr2.AddDict();
			dic43.SetString("SKAdNetworkIdentifier", "yclnxrl5pm.skadnetwork");

			PlistElementDict dic44;
			dic44 = URLWhiteListArr2.AddDict();
			dic44.SetString("SKAdNetworkIdentifier", "2fnua5tdw4.skadnetwork");

			PlistElementDict dic45;
			dic45 = URLWhiteListArr2.AddDict();
			dic45.SetString("SKAdNetworkIdentifier", "3qcr597p9d.skadnetwork");

			PlistElementDict dic46;
			dic46 = URLWhiteListArr2.AddDict();
			dic46.SetString("SKAdNetworkIdentifier", "3qy4746246.skadnetwork");

			PlistElementDict dic47;
			dic47 = URLWhiteListArr2.AddDict();
			dic47.SetString("SKAdNetworkIdentifier", "3sh42y64q3.skadnetwork");

			PlistElementDict dic48;
			dic48 = URLWhiteListArr2.AddDict();
			dic48.SetString("SKAdNetworkIdentifier", "424m5254lk.skadnetwork");

			PlistElementDict dic49;
			dic49 = URLWhiteListArr2.AddDict();
			dic49.SetString("SKAdNetworkIdentifier", "5a6flpkh64.skadnetwork");

			PlistElementDict dic50;
			dic50 = URLWhiteListArr2.AddDict();
			dic50.SetString("SKAdNetworkIdentifier", "e5fvkxwrpn.skadnetwork");

			PlistElementDict dic51;
			dic51 = URLWhiteListArr2.AddDict();
			dic51.SetString("SKAdNetworkIdentifier", "ecpz2srf59.skadnetwork");

			PlistElementDict dic52;
			dic52 = URLWhiteListArr2.AddDict();
			dic52.SetString("SKAdNetworkIdentifier", "hjevpa356n.skadnetwork");
			PlistElementDict dic53;
			dic53 = URLWhiteListArr2.AddDict();
			dic53.SetString("SKAdNetworkIdentifier", "k674qkevps.skadnetwork");


			PlistElementDict dic54;
			dic54 = URLWhiteListArr2.AddDict();
			dic54.SetString("SKAdNetworkIdentifier", "n6fk4nfna4.skadnetwork");

			PlistElementDict dic55;
			dic55 = URLWhiteListArr2.AddDict();
			dic55.SetString("SKAdNetworkIdentifier", "p78axxw29g.skadnetwork");

			PlistElementDict dic56;
			dic56 = URLWhiteListArr2.AddDict();
			dic56.SetString("SKAdNetworkIdentifier", "s39g8k73mm.skadnetwork");
			PlistElementDict dic57;
			dic57 = URLWhiteListArr2.AddDict();
			dic57.SetString("SKAdNetworkIdentifier", "uw77j35x4d.skadnetwork");

			PlistElementDict dic58;
			dic58 = URLWhiteListArr2.AddDict();
			dic58.SetString("SKAdNetworkIdentifier", "y2ed4ez56y.skadnetwork");

			PlistElementDict dic59;
			dic59 = URLWhiteListArr2.AddDict();
			dic59.SetString("SKAdNetworkIdentifier", "zq492l623r.skadnetwork");

			PlistElementDict dic60;
			dic60 = URLWhiteListArr2.AddDict();
			dic60.SetString("SKAdNetworkIdentifier", "24t9a8vw3c.skadnetwork");

			PlistElementDict dic61;
			dic61 = URLWhiteListArr2.AddDict();
			dic61.SetString("SKAdNetworkIdentifier", "32z4fx6l9h.skadnetwork");

			PlistElementDict dic62;
			dic62 = URLWhiteListArr2.AddDict();
			dic62.SetString("SKAdNetworkIdentifier", "523jb4fst2.skadnetwork");

			PlistElementDict dic63;
			dic63 = URLWhiteListArr2.AddDict();
			dic63.SetString("SKAdNetworkIdentifier", "54nzkqm89y.skadnetwork");

			PlistElementDict dic64;
			dic64 = URLWhiteListArr2.AddDict();
			dic64.SetString("SKAdNetworkIdentifier", "578prtvx9j.skadnetwork");

			PlistElementDict dic65;
			dic65 = URLWhiteListArr2.AddDict();
			dic65.SetString("SKAdNetworkIdentifier", "5l3tpt7t6e.skadnetwork");

			PlistElementDict dic66;
			dic66 = URLWhiteListArr2.AddDict();
			dic66.SetString("SKAdNetworkIdentifier", "5lm9lj6jb7.skadnetwork");

			PlistElementDict dic67;
			dic67 = URLWhiteListArr2.AddDict();
			dic67.SetString("SKAdNetworkIdentifier", "79pbpufp6p.skadnetwork");

			PlistElementDict dic68;
			dic68 = URLWhiteListArr2.AddDict();
			dic68.SetString("SKAdNetworkIdentifier", "7rz58n8ntl.skadnetwork");

			PlistElementDict dic69;
			dic69 = URLWhiteListArr2.AddDict();
			dic69.SetString("SKAdNetworkIdentifier", "n66cz3y3bx.skadnetwork");

			PlistElementDict dic70;
			dic70 = URLWhiteListArr2.AddDict();
			dic70.SetString("SKAdNetworkIdentifier", "9b89h5y424.skadnetwork");

			PlistElementDict dic71;
			dic71 = URLWhiteListArr2.AddDict();
			dic71.SetString("SKAdNetworkIdentifier", "9nlqeag3gk.skadnetwork");

			PlistElementDict dic72;
			dic72 = URLWhiteListArr2.AddDict();
			dic72.SetString("SKAdNetworkIdentifier", "cg4yq2srnc.skadnetwork");

			PlistElementDict dic73;
			dic73 = URLWhiteListArr2.AddDict();
			dic73.SetString("SKAdNetworkIdentifier", "cj5566h2ga.skadnetwork");

			PlistElementDict dic74;
			dic74 = URLWhiteListArr2.AddDict();
			dic74.SetString("SKAdNetworkIdentifier", "5tjdwbrq8w.skadnetwork");
			PlistElementDict dic75;
			dic75 = URLWhiteListArr2.AddDict();
			dic75.SetString("SKAdNetworkIdentifier", "ejvt5qm6ak.skadnetwork");

			PlistElementDict dic76;
			dic76 = URLWhiteListArr2.AddDict();
			dic76.SetString("SKAdNetworkIdentifier", "feyaarzu9v.skadnetwork");

			PlistElementDict dic77;
			dic77 = URLWhiteListArr2.AddDict();
			dic77.SetString("SKAdNetworkIdentifier", "g28c52eehv.skadnetwork");

			PlistElementDict dic78;
			dic78 = URLWhiteListArr2.AddDict();
			dic78.SetString("SKAdNetworkIdentifier", "ggvn48r87g.skadnetwork");

			PlistElementDict dic79;
			dic79 = URLWhiteListArr2.AddDict();
			dic79.SetString("SKAdNetworkIdentifier", "mtkv5xtk9e.skadnetwork");

			PlistElementDict dic80;
			dic80 = URLWhiteListArr2.AddDict();
			dic80.SetString("SKAdNetworkIdentifier", "n9x2a789qt.skadnetwork");

			PlistElementDict dic81;
			dic81 = URLWhiteListArr2.AddDict();
			dic81.SetString("SKAdNetworkIdentifier", "pwa73g5rt2.skadnetwork");

			PlistElementDict dic82;
			dic82 = URLWhiteListArr2.AddDict();
			dic82.SetString("SKAdNetworkIdentifier", "xy9t38ct57.skadnetwork");

			PlistElementDict dic83;
			dic83 = URLWhiteListArr2.AddDict();
			dic83.SetString("SKAdNetworkIdentifier", "zmvfpc5aq8.skadnetwork");

			PlistElementDict dic84;
			dic84 = URLWhiteListArr2.AddDict();
			dic84.SetString("SKAdNetworkIdentifier", "44n7hlldy6.skadnetwork");

			PlistElementDict dic85;
			dic85 = URLWhiteListArr2.AddDict();
			dic85.SetString("SKAdNetworkIdentifier", "488r3q3dtq.skadnetwork");

			PlistElementDict dic86;
			dic86 = URLWhiteListArr2.AddDict();
			dic86.SetString("SKAdNetworkIdentifier", "f73kdq92p3.skadnetwork");

			PlistElementDict dic87;
			dic87 = URLWhiteListArr2.AddDict();
			dic87.SetString("SKAdNetworkIdentifier", "f7s53z58qe.skadnetwork");

			PlistElementDict dic88;
			dic88 = URLWhiteListArr2.AddDict();
			dic88.SetString("SKAdNetworkIdentifier", "lr83yxwka7.skadnetwork");

			PlistElementDict dic89;
			dic89 = URLWhiteListArr2.AddDict();
			dic89.SetString("SKAdNetworkIdentifier", "v79kvwwj4g.skadnetwork");

			PlistElementDict dic90;
			dic90 = URLWhiteListArr2.AddDict();
			dic90.SetString("SKAdNetworkIdentifier", "7953jerfzd.skadnetwork");

			PlistElementDict dic91;
			dic91 = URLWhiteListArr2.AddDict();
			dic91.SetString("SKAdNetworkIdentifier", "9yg77x724h.skadnetwork");

			PlistElementDict dic92;
			dic92 = URLWhiteListArr2.AddDict();
			dic92.SetString("SKAdNetworkIdentifier", "mls7yz5dvl.skadnetwork");

			PlistElementDict dic93;
			dic93 = URLWhiteListArr2.AddDict();
			dic93.SetString("SKAdNetworkIdentifier", "x8jxxk4ff5.skadnetwork");

			PlistElementDict dic94;
			dic94 = URLWhiteListArr2.AddDict();
			dic94.SetString("SKAdNetworkIdentifier", "mp6xlyr22a.skadnetwork");

			PlistElementDict dic95;
			dic95 = URLWhiteListArr2.AddDict();
			dic95.SetString("SKAdNetworkIdentifier", "x44k69ngh6.skadnetwork");

			PlistElementDict dic96;
			dic96 = URLWhiteListArr2.AddDict();
			dic96.SetString("SKAdNetworkIdentifier", "6g9af3uyq4.skadnetwork");

			PlistElementDict dic97;
			dic97 = URLWhiteListArr2.AddDict();
			dic97.SetString("SKAdNetworkIdentifier", "qqp299437r.skadnetwork");

			PlistElementDict dic98;
			dic98 = URLWhiteListArr2.AddDict();
			dic98.SetString("SKAdNetworkIdentifier", "252b5q8x7y.skadnetwork");

			PlistElementDict dic99;
			dic99 = URLWhiteListArr2.AddDict();
			dic99.SetString("SKAdNetworkIdentifier", "rx5hdcabgc.skadnetwork");

			PlistElementDict dic100;
			dic100 = URLWhiteListArr2.AddDict();
			dic100.SetString("SKAdNetworkIdentifier", "294l99pt4k.skadnetwork");

			PlistElementDict dic101;
			dic101 = URLWhiteListArr2.AddDict();
			dic101.SetString("SKAdNetworkIdentifier", "kbmxgpxpgc.skadnetwork");

			PlistElementDict dic102;
			dic102 = URLWhiteListArr2.AddDict();
			dic102.SetString("SKAdNetworkIdentifier", "52fl2v3hgk.skadnetwork");

			PlistElementDict dic103;
			dic103 = URLWhiteListArr2.AddDict();
			dic103.SetString("SKAdNetworkIdentifier", "r45fhb6rf7.skadnetwork");

			PlistElementDict dic104;
			dic104 = URLWhiteListArr2.AddDict();
			dic104.SetString("SKAdNetworkIdentifier", "rvh3l7un93.skadnetwork");

			PlistElementDict dic105;
			dic105 = URLWhiteListArr2.AddDict();
			dic105.SetString("SKAdNetworkIdentifier", "97r2b46745.skadnetwork");

			PlistElementDict dic106;
			dic106 = URLWhiteListArr2.AddDict();
			dic106.SetString("SKAdNetworkIdentifier", "9g2aggbj52.skadnetwork");

			PlistElementDict dic107;
			dic107 = URLWhiteListArr2.AddDict();
			dic107.SetString("SKAdNetworkIdentifier", "dzg6xy7pwj.skadnetwork");

			PlistElementDict dic108;
			dic108 = URLWhiteListArr2.AddDict();
			dic108.SetString("SKAdNetworkIdentifier", "x8uqf25wch.skadnetwork");

			PlistElementDict dic109;
			dic109 = URLWhiteListArr2.AddDict();
			dic109.SetString("SKAdNetworkIdentifier", "58922nb4gd.skadnetwork");

			PlistElementDict dic110;
			dic110 = URLWhiteListArr2.AddDict();
			dic110.SetString("SKAdNetworkIdentifier", "gvmwg8q7h5.skadnetwork");

			PlistElementDict dic111;
			dic111 = URLWhiteListArr2.AddDict();
			dic111.SetString("SKAdNetworkIdentifier", "hdw39hrw9y.skadnetwork");

			PlistElementDict dic112;
			dic112 = URLWhiteListArr2.AddDict();
			dic112.SetString("SKAdNetworkIdentifier", "8c4e2ghe7u.skadnetwork");

			PlistElementDict dic113;
			dic113 = URLWhiteListArr2.AddDict();
			dic113.SetString("SKAdNetworkIdentifier", "v4nxqhlyqp.skadnetwork");

			PlistElementDict dic114;
			dic114 = URLWhiteListArr2.AddDict();
			dic114.SetString("SKAdNetworkIdentifier", "c3frkrj4fj.skadnetwork");

			PlistElementDict dic115;
			dic115 = URLWhiteListArr2.AddDict();
			dic115.SetString("SKAdNetworkIdentifier", "nzq8sh4pbs.skadnetwork");

			PlistElementDict dic116;
			dic116 = URLWhiteListArr2.AddDict();
			dic116.SetString("SKAdNetworkIdentifier", "pu4na253f3.skadnetwork");

			PlistElementDict dic117;
			dic117 = URLWhiteListArr2.AddDict();
			dic117.SetString("SKAdNetworkIdentifier", "6964rsfnh4.skadnetwork");

			PlistElementDict dic118;
			dic118 = URLWhiteListArr2.AddDict();
			dic118.SetString("SKAdNetworkIdentifier", "74b6s63p6l.skadnetwork");

			PlistElementDict dic119;
			dic119 = URLWhiteListArr2.AddDict();
			dic119.SetString("SKAdNetworkIdentifier", "84993kbrcf.skadnetwork");

			PlistElementDict dic120;
			dic120 = URLWhiteListArr2.AddDict();
			dic120.SetString("SKAdNetworkIdentifier", "y45688jllp.skadnetwork");

			PlistElementDict dic121;
			dic121 = URLWhiteListArr2.AddDict();
			dic121.SetString("SKAdNetworkIdentifier", "yrqqpx2mcb.skadnetwork");

			PlistElementDict dic122;
			dic122 = URLWhiteListArr2.AddDict();
			dic122.SetString("SKAdNetworkIdentifier", "z4gj7hsk7h.skadnetwork");

			PlistElementDict dic123;
			dic123 = URLWhiteListArr2.AddDict();
			dic123.SetString("SKAdNetworkIdentifier", "u679fj5vs4.skadnetwork");

			PlistElementDict dic124;
			dic124 = URLWhiteListArr2.AddDict();
			dic124.SetString("SKAdNetworkIdentifier", "a7xqa6mtl2.skadnetwork");

			PlistElementDict dic125;
			dic125 = URLWhiteListArr2.AddDict();
			dic125.SetString("SKAdNetworkIdentifier", "g2y4y55b64.skadnetwork");

			PlistElementDict dic126;
			dic126 = URLWhiteListArr2.AddDict();
			dic126.SetString("SKAdNetworkIdentifier", "pwdxu55a5a.skadnetwork");

			PlistElementDict dic127;
			dic127 = URLWhiteListArr2.AddDict();
			dic127.SetString("SKAdNetworkIdentifier", "b9bk5wbcq9.skadnetwork");

			PlistElementDict dic128;
			dic128 = URLWhiteListArr2.AddDict();
			dic128.SetString("SKAdNetworkIdentifier", "x5l83yy675.skadnetwork");

			PlistElementDict dic129;
			dic129 = URLWhiteListArr2.AddDict();
			dic129.SetString("SKAdNetworkIdentifier", "x2jnk7ly8j.skadnetwork");

			PlistElementDict dic130;
			dic130 = URLWhiteListArr2.AddDict();
			dic130.SetString("SKAdNetworkIdentifier", "4w7y6s5ca2.skadnetwork");

			PlistElementDict dic131;
			dic131 = URLWhiteListArr2.AddDict();
			dic131.SetString("SKAdNetworkIdentifier", "7fmhfwg9en.skadnetwork");

			PlistElementDict dic132;
			dic132 = URLWhiteListArr2.AddDict();
			dic132.SetString("SKAdNetworkIdentifier", "qu637u8glc.skadnetwork");

			PlistElementDict dic133;
			dic133 = URLWhiteListArr2.AddDict();
			dic133.SetString("SKAdNetworkIdentifier", "krvm3zuq6h.skadnetwork");

			PlistElementDict dic134;
			dic134 = URLWhiteListArr2.AddDict();
			dic134.SetString("SKAdNetworkIdentifier", "3l6bd9hu43.skadnetwork");

			PlistElementDict dic135;
			dic135 = URLWhiteListArr2.AddDict();
			dic135.SetString("SKAdNetworkIdentifier", "a8cz6cu7e5.skadnetwork");

			PlistElementDict dic136;
			dic136 = URLWhiteListArr2.AddDict();
			dic136.SetString("SKAdNetworkIdentifier", "dkc879ngq3.skadnetwork");

			PlistElementDict dic137;
			dic137 = URLWhiteListArr2.AddDict();
			dic137.SetString("SKAdNetworkIdentifier", "m5mvw97r93.skadnetwork");

			PlistElementDict dic138;
			dic138 = URLWhiteListArr2.AddDict();
			dic138.SetString("SKAdNetworkIdentifier", "vcra2ehyfk.skadnetwork");

			PlistElementDict dic139;
			dic139 = URLWhiteListArr2.AddDict();
			dic139.SetString("SKAdNetworkIdentifier", "x5l83yy675.skadnetwork");


			PlistElementDict dic140;
			dic140 = URLWhiteListArr2.AddDict();
			dic140.SetString("SKAdNetworkIdentifier", "6p4ks3rnbw.skadnetwork");

			PlistElementDict dic141;
			dic141 = URLWhiteListArr2.AddDict();
			dic141.SetString("SKAdNetworkIdentifier", "a2p9lx4jpn.skadnetwork");

			PlistElementDict dic142;
			dic142 = URLWhiteListArr2.AddDict();
			dic142.SetString("SKAdNetworkIdentifier", "bxvub5ada5.skadnetwork");

			PlistElementDict dic143;
			dic143 = URLWhiteListArr2.AddDict();
			dic143.SetString("SKAdNetworkIdentifier", "s69wq72ugq.skadnetwork");

			PlistElementDict dic144;
			dic144 = URLWhiteListArr2.AddDict();
			dic144.SetString("SKAdNetworkIdentifier", "47vhws6wlr.skadnetwork");

			PlistElementDict dic145;
			dic145 = URLWhiteListArr2.AddDict();
			dic145.SetString("SKAdNetworkIdentifier", "a2p9lx4jpn.skadnetwork");

			PlistElementDict dic146;
			dic146 = URLWhiteListArr2.AddDict();
			dic146.SetString("SKAdNetworkIdentifier", "cp8zw746q7.skadnetwork");

			PlistElementDict dic147;
			dic147 = URLWhiteListArr2.AddDict();
			dic147.SetString("SKAdNetworkIdentifier", "y5ghdn5j9k.skadnetwork");

			PlistElementDict dic148;
			dic148 = URLWhiteListArr2.AddDict();
			dic148.SetString("SKAdNetworkIdentifier", "a2p9lx4jpn.skadnetwork");

			PlistElementDict dic149;
			dic149 = URLWhiteListArr2.AddDict();
			dic149.SetString("SKAdNetworkIdentifier", "nu4557a4je.skadnetwork");

			PlistElementDict dic150;
			dic150 = URLWhiteListArr2.AddDict();
			dic150.SetString("SKAdNetworkIdentifier", "8r8llnkz5a.skadnetwork");
			PlistElementDict dic151;
			
			dic151 = URLWhiteListArr2.AddDict();
			dic151.SetString("SKAdNetworkIdentifier", "22mmun2rn5.skadnetwork");

			PlistElementDict dic152;
			dic152 = URLWhiteListArr2.AddDict();
			dic152.SetString("SKAdNetworkIdentifier", "238da6jt44.skadnetwork");

			PlistElementDict dic153;
			dic153 = URLWhiteListArr2.AddDict();
			dic153.SetString("SKAdNetworkIdentifier", "23zd986j2c.skadnetwork");

			PlistElementDict dic154;
			dic154 = URLWhiteListArr2.AddDict();
			dic154.SetString("SKAdNetworkIdentifier", "24t9a8vw3c.skadnetwork");

			PlistElementDict dic155;
			dic155 = URLWhiteListArr2.AddDict();
			dic155.SetString("SKAdNetworkIdentifier", "24zw6aqk47.skadnetwork");

			PlistElementDict dic156;
			dic156 = URLWhiteListArr2.AddDict();
			dic156.SetString("SKAdNetworkIdentifier", "252b5q8x7y.skadnetwork");

			PlistElementDict dic157;
			dic157 = URLWhiteListArr2.AddDict();
			dic157.SetString("SKAdNetworkIdentifier", "275upjj5gd.skadnetwork");

			PlistElementDict dic158;
			dic158 = URLWhiteListArr2.AddDict();
			dic158.SetString("SKAdNetworkIdentifier", "294l99pt4k.skadnetwork");

			PlistElementDict dic159;
			dic159 = URLWhiteListArr2.AddDict();
			dic159.SetString("SKAdNetworkIdentifier", "2fnua5tdw4.skadnetwork");

			PlistElementDict dic160;
			dic160 = URLWhiteListArr2.AddDict();
			dic160.SetString("SKAdNetworkIdentifier", "2q884k2j68.skadnetwork");

			PlistElementDict dic161;
			dic161 = URLWhiteListArr2.AddDict();
			dic161.SetString("SKAdNetworkIdentifier", "2rq3zucswp.skadnetwork");

			PlistElementDict dic162;
			dic162 = URLWhiteListArr2.AddDict();
			dic162.SetString("SKAdNetworkIdentifier", "2tdux39lx8.skadnetwork");

			PlistElementDict dic163;
			dic163 = URLWhiteListArr2.AddDict();
			dic163.SetString("SKAdNetworkIdentifier", "2u9pt9hc89.skadnetwork");

			PlistElementDict dic164;
			dic164 = URLWhiteListArr2.AddDict();
			dic164.SetString("SKAdNetworkIdentifier", "32z4fx6l9h.skadnetwork");

			PlistElementDict dic165;
			dic165 = URLWhiteListArr2.AddDict();
			dic165.SetString("SKAdNetworkIdentifier", "33r6p7g8nc.skadnetwork");

			PlistElementDict dic166;
			dic166 = URLWhiteListArr2.AddDict();
			dic166.SetString("SKAdNetworkIdentifier", "3cgn6rq224.skadnetwork");

			PlistElementDict dic167;
			dic167 = URLWhiteListArr2.AddDict();
			dic167.SetString("SKAdNetworkIdentifier", "3l6bd9hu43.skadnetwork");

			PlistElementDict dic168;
			dic168 = URLWhiteListArr2.AddDict();
			dic168.SetString("SKAdNetworkIdentifier", "3qcr597p9d.skadnetwork");

			PlistElementDict dic169;
			dic169 = URLWhiteListArr2.AddDict();
			dic169.SetString("SKAdNetworkIdentifier", "3qy4746246.skadnetwork");

			PlistElementDict dic170;
			dic170 = URLWhiteListArr2.AddDict();
			dic170.SetString("SKAdNetworkIdentifier", "3rd42ekr43.skadnetwork");

			PlistElementDict dic171;
			dic171 = URLWhiteListArr2.AddDict();
			dic171.SetString("SKAdNetworkIdentifier", "3sh42y64q3.skadnetwork");

			PlistElementDict dic172;
			dic172 = URLWhiteListArr2.AddDict();
			dic172.SetString("SKAdNetworkIdentifier", "424m5254lk.skadnetwork");

			PlistElementDict dic173;
			dic173 = URLWhiteListArr2.AddDict();
			dic173.SetString("SKAdNetworkIdentifier", "4468km3ulz.skadnetwork");

			PlistElementDict dic174;
			dic174 = URLWhiteListArr2.AddDict();
			dic174.SetString("SKAdNetworkIdentifier", "44jx6755aq.skadnetwork");

			PlistElementDict dic175;
			dic175 = URLWhiteListArr2.AddDict();
			dic175.SetString("SKAdNetworkIdentifier", "44n7hlldy6.skadnetwork");

			PlistElementDict dic176;
			dic176 = URLWhiteListArr2.AddDict();
			dic176.SetString("SKAdNetworkIdentifier", "47vhws6wlr.skadnetwork");

			PlistElementDict dic177;
			dic177 = URLWhiteListArr2.AddDict();
			dic177.SetString("SKAdNetworkIdentifier", "488r3q3dtq.skadnetwork");

			PlistElementDict dic178;
			dic178 = URLWhiteListArr2.AddDict();
			dic178.SetString("SKAdNetworkIdentifier", "4dzt52r2t5.skadnetwork");

			PlistElementDict dic179;
			dic179 = URLWhiteListArr2.AddDict();
			dic179.SetString("SKAdNetworkIdentifier", "4fzdc2evr5.skadnetwork");

			PlistElementDict dic180;
			dic180 = URLWhiteListArr2.AddDict();
			dic180.SetString("SKAdNetworkIdentifier", "4mn522wn87.skadnetwork");

			PlistElementDict dic181;
			dic181 = URLWhiteListArr2.AddDict();
			dic181.SetString("SKAdNetworkIdentifier", "4pfyvq9l8r.skadnetwork");

			PlistElementDict dic182;
			dic182 = URLWhiteListArr2.AddDict();
			dic182.SetString("SKAdNetworkIdentifier", "4w7y6s5ca2.skadnetwork");

			PlistElementDict dic183;
			dic183 = URLWhiteListArr2.AddDict();
			dic183.SetString("SKAdNetworkIdentifier", "523jb4fst2.skadnetwork");

			PlistElementDict dic184;
			dic184 = URLWhiteListArr2.AddDict();
			dic184.SetString("SKAdNetworkIdentifier", "52fl2v3hgk.skadnetwork");

			PlistElementDict dic185;
			dic185 = URLWhiteListArr2.AddDict();
			dic185.SetString("SKAdNetworkIdentifier", "54nzkqm89y.skadnetwork");

			PlistElementDict dic186;
			dic186 = URLWhiteListArr2.AddDict();
			dic186.SetString("SKAdNetworkIdentifier", "55644vm79v.skadnetwork");

			PlistElementDict dic187;
			dic187 = URLWhiteListArr2.AddDict();
			dic187.SetString("SKAdNetworkIdentifier", "55y65gfgn7.skadnetwork");

			PlistElementDict dic188;
			dic188 = URLWhiteListArr2.AddDict();
			dic188.SetString("SKAdNetworkIdentifier", "577p5t736z.skadnetwork");

			PlistElementDict dic189;
			dic189 = URLWhiteListArr2.AddDict();
			dic189.SetString("SKAdNetworkIdentifier", "578prtvx9j.skadnetwork");

			PlistElementDict dic190;
			dic190 = URLWhiteListArr2.AddDict();
			dic190.SetString("SKAdNetworkIdentifier", "5a6flpkh64.skadnetwork");

			PlistElementDict dic191;
			dic191 = URLWhiteListArr2.AddDict();
			dic191.SetString("SKAdNetworkIdentifier", "5f5u5tfb26.skadnetwork");

			PlistElementDict dic192;
			dic192 = URLWhiteListArr2.AddDict();
			dic192.SetString("SKAdNetworkIdentifier", "5ghnmfs3dh.skadnetwork");

			PlistElementDict dic193;
			dic193 = URLWhiteListArr2.AddDict();
			dic193.SetString("SKAdNetworkIdentifier", "5l3tpt7t6e.skadnetwork");

			PlistElementDict dic194;
			dic194 = URLWhiteListArr2.AddDict();
			dic194.SetString("SKAdNetworkIdentifier", "5lm9lj6jb7.skadnetwork");

			PlistElementDict dic195;
			dic195 = URLWhiteListArr2.AddDict();
			dic195.SetString("SKAdNetworkIdentifier", "5mv394q32t.skadnetwork");

			PlistElementDict dic196;
			dic196 = URLWhiteListArr2.AddDict();
			dic196.SetString("SKAdNetworkIdentifier", "5tjdwbrq8w.skadnetwork");

			PlistElementDict dic197;
			dic197 = URLWhiteListArr2.AddDict();
			dic197.SetString("SKAdNetworkIdentifier", "627r9wr2y5.skadnetwork");

			PlistElementDict dic198;
			dic198 = URLWhiteListArr2.AddDict();
			dic198.SetString("SKAdNetworkIdentifier", "633vhxswh4.skadnetwork");

			PlistElementDict dic199;
			dic199 = URLWhiteListArr2.AddDict();
			dic199.SetString("SKAdNetworkIdentifier", "67369282zy.skadnetwork");

			PlistElementDict dic200;
			dic200 = URLWhiteListArr2.AddDict();
			dic200.SetString("SKAdNetworkIdentifier", "6964rsfnh4.skadnetwork");

			PlistElementDict dic201;
			dic201 = URLWhiteListArr2.AddDict();
			dic201.SetString("SKAdNetworkIdentifier", "6g9af3uyq4.skadnetwork");

			PlistElementDict dic202;
			dic202 = URLWhiteListArr2.AddDict();
			dic202.SetString("SKAdNetworkIdentifier", "6p4ks3rnbw.skadnetwork");

			PlistElementDict dic203;
			dic203 = URLWhiteListArr2.AddDict();
			dic203.SetString("SKAdNetworkIdentifier", "6qx585k4p6.skadnetwork");

			PlistElementDict dic204;
			dic204 = URLWhiteListArr2.AddDict();
			dic204.SetString("SKAdNetworkIdentifier", "6rd35atwn8.skadnetwork");

			PlistElementDict dic205;
			dic205 = URLWhiteListArr2.AddDict();
			dic205.SetString("SKAdNetworkIdentifier", "6v7lgmsu45.skadnetwork");

			PlistElementDict dic206;
			dic206 = URLWhiteListArr2.AddDict();
			dic206.SetString("SKAdNetworkIdentifier", "6xzpu9s2p8.skadnetwork");

			PlistElementDict dic207;
			dic207 = URLWhiteListArr2.AddDict();
			dic207.SetString("SKAdNetworkIdentifier", "6yxyv74ff7.skadnetwork");

			PlistElementDict dic208;
			dic208 = URLWhiteListArr2.AddDict();
			dic208.SetString("SKAdNetworkIdentifier", "737z793b9f.skadnetwork");

			PlistElementDict dic209;
			dic209 = URLWhiteListArr2.AddDict();
			dic209.SetString("SKAdNetworkIdentifier", "74b6s63p6l.skadnetwork");

			PlistElementDict dic210;
			dic210 = URLWhiteListArr2.AddDict();
			dic210.SetString("SKAdNetworkIdentifier", "7953jerfzd.skadnetwork");

			PlistElementDict dic211;
			dic211 = URLWhiteListArr2.AddDict();
			dic211.SetString("SKAdNetworkIdentifier", "79pbpufp6p.skadnetwork");

			PlistElementDict dic212;
			dic212 = URLWhiteListArr2.AddDict();
			dic212.SetString("SKAdNetworkIdentifier", "79w64w269u.skadnetwork");

			PlistElementDict dic213;
			dic213 = URLWhiteListArr2.AddDict();
			dic213.SetString("SKAdNetworkIdentifier", "7bxrt786m8.skadnetwork");

			PlistElementDict dic214;
			dic214 = URLWhiteListArr2.AddDict();
			dic214.SetString("SKAdNetworkIdentifier", "7fbxrn65az.skadnetwork");

			PlistElementDict dic215;
			dic215 = URLWhiteListArr2.AddDict();
			dic215.SetString("SKAdNetworkIdentifier", "7fmhfwg9en.skadnetwork");

			PlistElementDict dic216;
			dic216 = URLWhiteListArr2.AddDict();
			dic216.SetString("SKAdNetworkIdentifier", "7k3cvf297u.skadnetwork");

			PlistElementDict dic217;
			dic217 = URLWhiteListArr2.AddDict();
			dic217.SetString("SKAdNetworkIdentifier", "7rz58n8ntl.skadnetwork");

			PlistElementDict dic218;
			dic218 = URLWhiteListArr2.AddDict();
			dic218.SetString("SKAdNetworkIdentifier", "7tnzynbdc7.skadnetwork");

			PlistElementDict dic219;
			dic219 = URLWhiteListArr2.AddDict();
			dic219.SetString("SKAdNetworkIdentifier", "7ug5zh24hu.skadnetwork");

			PlistElementDict dic220;
			dic220 = URLWhiteListArr2.AddDict();
			dic220.SetString("SKAdNetworkIdentifier", "84993kbrcf.skadnetwork");

			PlistElementDict dic221;
			dic221 = URLWhiteListArr2.AddDict();
			dic221.SetString("SKAdNetworkIdentifier", "866k9ut3g3.skadnetwork");

			PlistElementDict dic222;
			dic222 = URLWhiteListArr2.AddDict();
			dic222.SetString("SKAdNetworkIdentifier", "87u5trcl3r.skadnetwork");

			PlistElementDict dic223;
			dic223 = URLWhiteListArr2.AddDict();
			dic223.SetString("SKAdNetworkIdentifier", "88k8774x49.skadnetwork");

			PlistElementDict dic224;
			dic224 = URLWhiteListArr2.AddDict();
			dic224.SetString("SKAdNetworkIdentifier", "899vrgt9g8.skadnetwork");

			PlistElementDict dic225;
			dic225 = URLWhiteListArr2.AddDict();
			dic225.SetString("SKAdNetworkIdentifier", "89z7zv988g.skadnetwork");

			PlistElementDict dic226;
			dic226 = URLWhiteListArr2.AddDict();
			dic226.SetString("SKAdNetworkIdentifier", "8c4e2ghe7u.skadnetwork");

			PlistElementDict dic227;
			dic227 = URLWhiteListArr2.AddDict();
			dic227.SetString("SKAdNetworkIdentifier", "8m87ys6875.skadnetwork");

			PlistElementDict dic228;
			dic228 = URLWhiteListArr2.AddDict();
			dic228.SetString("SKAdNetworkIdentifier", "8qiegk9qfv.skadnetwork");

			PlistElementDict dic229;
			dic229 = URLWhiteListArr2.AddDict();
			dic229.SetString("SKAdNetworkIdentifier", "8r8llnkz5a.skadnetwork");

			PlistElementDict dic230;
			dic230 = URLWhiteListArr2.AddDict();
			dic230.SetString("SKAdNetworkIdentifier", "8s468mfl3y.skadnetwork");

			PlistElementDict dic231;
			dic231 = URLWhiteListArr2.AddDict();
			dic231.SetString("SKAdNetworkIdentifier", "8w3np9l82g.skadnetwork");

			PlistElementDict dic232;
			dic232 = URLWhiteListArr2.AddDict();
			dic232.SetString("SKAdNetworkIdentifier", "97r2b46745.skadnetwork");

			PlistElementDict dic233;
			dic233 = URLWhiteListArr2.AddDict();
			dic233.SetString("SKAdNetworkIdentifier", "9b89h5y424.skadnetwork");

			PlistElementDict dic234;
			dic234 = URLWhiteListArr2.AddDict();
			dic234.SetString("SKAdNetworkIdentifier", "9g2aggbj52.skadnetwork");

			PlistElementDict dic235;
			dic235 = URLWhiteListArr2.AddDict();
			dic235.SetString("SKAdNetworkIdentifier", "9nlqeag3gk.skadnetwork");

			PlistElementDict dic236;
			dic236 = URLWhiteListArr2.AddDict();
			dic236.SetString("SKAdNetworkIdentifier", "9rd848q2bz.skadnetwork");

			PlistElementDict dic237;
			dic237 = URLWhiteListArr2.AddDict();
			dic237.SetString("SKAdNetworkIdentifier", "9t245vhmpl.skadnetwork");

			PlistElementDict dic238;
			dic238 = URLWhiteListArr2.AddDict();
			dic238.SetString("SKAdNetworkIdentifier", "9vvzujtq5s.skadnetwork");

			PlistElementDict dic239;
			dic239 = URLWhiteListArr2.AddDict();
			dic239.SetString("SKAdNetworkIdentifier", "9wsyqb3ku7.skadnetwork");

			PlistElementDict dic240;
			dic240 = URLWhiteListArr2.AddDict();
			dic240.SetString("SKAdNetworkIdentifier", "9yg77x724h.skadnetwork");

			PlistElementDict dic241;
			dic241 = URLWhiteListArr2.AddDict();
			dic241.SetString("SKAdNetworkIdentifier", "a2p9lx4jpn.skadnetwork");

			PlistElementDict dic242;
			dic242 = URLWhiteListArr2.AddDict();
			dic242.SetString("SKAdNetworkIdentifier", "a7xqa6mtl2.skadnetwork");

			PlistElementDict dic243;
			dic243 = URLWhiteListArr2.AddDict();
			dic243.SetString("SKAdNetworkIdentifier", "a8cz6cu7e5.skadnetwork");

			PlistElementDict dic244;
			dic244 = URLWhiteListArr2.AddDict();
			dic244.SetString("SKAdNetworkIdentifier", "au67k4efj4.skadnetwork");

			PlistElementDict dic245;
			dic245 = URLWhiteListArr2.AddDict();
			dic245.SetString("SKAdNetworkIdentifier", "av6w8kgt66.skadnetwork");

			PlistElementDict dic246;
			dic246 = URLWhiteListArr2.AddDict();
			dic246.SetString("SKAdNetworkIdentifier", "axh5283zss.skadnetwork");

			PlistElementDict dic247;
			dic247 = URLWhiteListArr2.AddDict();
			dic247.SetString("SKAdNetworkIdentifier", "b53axzn49s.skadnetwork");

			PlistElementDict dic248;
			dic248 = URLWhiteListArr2.AddDict();
			dic248.SetString("SKAdNetworkIdentifier", "b55w3d8y8z.skadnetwork");

			PlistElementDict dic249;
			dic249 = URLWhiteListArr2.AddDict();
			dic249.SetString("SKAdNetworkIdentifier", "b9bk5wbcq9.skadnetwork");

			PlistElementDict dic250;
			dic250 = URLWhiteListArr2.AddDict();
			dic250.SetString("SKAdNetworkIdentifier", "bvpn9ufa9b.skadnetwork");

			PlistElementDict dic251;
			dic251 = URLWhiteListArr2.AddDict();
			dic251.SetString("SKAdNetworkIdentifier", "bxvub5ada5.skadnetwork");

			PlistElementDict dic252;
			dic252 = URLWhiteListArr2.AddDict();
			dic252.SetString("SKAdNetworkIdentifier", "c3frkrj4fj.skadnetwork");

			PlistElementDict dic253;
			dic253 = URLWhiteListArr2.AddDict();
			dic253.SetString("SKAdNetworkIdentifier", "c6k4g5qg8m.skadnetwork");

			PlistElementDict dic254;
			dic254 = URLWhiteListArr2.AddDict();
			dic254.SetString("SKAdNetworkIdentifier", "c7g47wypnu.skadnetwork");

			PlistElementDict dic255;
			dic255 = URLWhiteListArr2.AddDict();
			dic255.SetString("SKAdNetworkIdentifier", "cad8qz2s3j.skadnetwork");

			PlistElementDict dic256;
			dic256 = URLWhiteListArr2.AddDict();
			dic256.SetString("SKAdNetworkIdentifier", "ce8ybjwass.skadnetwork");

			PlistElementDict dic257;
			dic257 = URLWhiteListArr2.AddDict();
			dic257.SetString("SKAdNetworkIdentifier", "cg4yq2srnc.skadnetwork");

			PlistElementDict dic258;
			dic258 = URLWhiteListArr2.AddDict();
			dic258.SetString("SKAdNetworkIdentifier", "cj5566h2ga.skadnetwork");

			PlistElementDict dic259;
			dic259 = URLWhiteListArr2.AddDict();
			dic259.SetString("SKAdNetworkIdentifier", "cp8zw746q7.skadnetwork");

			PlistElementDict dic260;
			dic260 = URLWhiteListArr2.AddDict();
			dic260.SetString("SKAdNetworkIdentifier", "cs644xg564.skadnetwork");

			PlistElementDict dic261;
			dic261 = URLWhiteListArr2.AddDict();
			dic261.SetString("SKAdNetworkIdentifier", "cstr6suwn9.skadnetwork");

			PlistElementDict dic262;
			dic262 = URLWhiteListArr2.AddDict();
			dic262.SetString("SKAdNetworkIdentifier", "cwn433xbcr.skadnetwork");

			PlistElementDict dic263;
			dic263 = URLWhiteListArr2.AddDict();
			dic263.SetString("SKAdNetworkIdentifier", "d7g9azk84q.skadnetwork");

			PlistElementDict dic264;
			dic264 = URLWhiteListArr2.AddDict();
			dic264.SetString("SKAdNetworkIdentifier", "dbu4b84rxf.skadnetwork");

			PlistElementDict dic265;
			dic265 = URLWhiteListArr2.AddDict();
			dic265.SetString("SKAdNetworkIdentifier", "dd3a75yxkv.skadnetwork");

			PlistElementDict dic266;
			dic266 = URLWhiteListArr2.AddDict();
			dic266.SetString("SKAdNetworkIdentifier", "dkc879ngq3.skadnetwork");

			PlistElementDict dic267;
			dic267 = URLWhiteListArr2.AddDict();
			dic267.SetString("SKAdNetworkIdentifier", "dmv22haz9p.skadnetwork");

			PlistElementDict dic268;
			dic268 = URLWhiteListArr2.AddDict();
			dic268.SetString("SKAdNetworkIdentifier", "dn942472g5.skadnetwork");

			PlistElementDict dic269;
			dic269 = URLWhiteListArr2.AddDict();
			dic269.SetString("SKAdNetworkIdentifier", "dr774724x4.skadnetwork");

			PlistElementDict dic270;
			dic270 = URLWhiteListArr2.AddDict();
			dic270.SetString("SKAdNetworkIdentifier", "dt3cjx1a9i.skadnetwork");

			PlistElementDict dic271;
			dic271 = URLWhiteListArr2.AddDict();
			dic271.SetString("SKAdNetworkIdentifier", "dticjx1a9i.skadnetwork");

			PlistElementDict dic272;
			dic272 = URLWhiteListArr2.AddDict();
			dic272.SetString("SKAdNetworkIdentifier", "dzg6xy7pwj.skadnetwork");

			PlistElementDict dic273;
			dic273 = URLWhiteListArr2.AddDict();
			dic273.SetString("SKAdNetworkIdentifier", "e5fvkxwrpn.skadnetwork");

			PlistElementDict dic274;
			dic274 = URLWhiteListArr2.AddDict();
			dic274.SetString("SKAdNetworkIdentifier", "ecpz2srf59.skadnetwork");

			PlistElementDict dic275;
			dic275 = URLWhiteListArr2.AddDict();
			dic275.SetString("SKAdNetworkIdentifier", "eh6m2bh4zr.skadnetwork");

			PlistElementDict dic276;
			dic276 = URLWhiteListArr2.AddDict();
			dic276.SetString("SKAdNetworkIdentifier", "ejvt5qm6ak.skadnetwork");

			PlistElementDict dic277;
			dic277 = URLWhiteListArr2.AddDict();
			dic277.SetString("SKAdNetworkIdentifier", "eqhxz8m8av.skadnetwork");

			PlistElementDict dic278;
			dic278 = URLWhiteListArr2.AddDict();
			dic278.SetString("SKAdNetworkIdentifier", "f2zub97jtl.skadnetwork");

			PlistElementDict dic279;
			dic279 = URLWhiteListArr2.AddDict();
			dic279.SetString("SKAdNetworkIdentifier", "f38h382jlk.skadnetwork");

			PlistElementDict dic280;
			dic280 = URLWhiteListArr2.AddDict();
			dic280.SetString("SKAdNetworkIdentifier", "f3h7jpbg97.skadnetwork");

			PlistElementDict dic281;
			dic281 = URLWhiteListArr2.AddDict();
			dic281.SetString("SKAdNetworkIdentifier", "f73kdq92p3.skadnetwork");

			PlistElementDict dic282;
			dic282 = URLWhiteListArr2.AddDict();
			dic282.SetString("SKAdNetworkIdentifier", "f7s53z58qe.skadnetwork");

			PlistElementDict dic283;
			dic283 = URLWhiteListArr2.AddDict();
			dic283.SetString("SKAdNetworkIdentifier", "feyaarzu9v.skadnetwork");

			PlistElementDict dic284;
			dic284 = URLWhiteListArr2.AddDict();
			dic284.SetString("SKAdNetworkIdentifier", "fkak3gfpt6.skadnetwork");

			PlistElementDict dic285;
			dic285 = URLWhiteListArr2.AddDict();
			dic285.SetString("SKAdNetworkIdentifier", "fq6vru337s.skadnetwork");

			PlistElementDict dic286;
			dic286 = URLWhiteListArr2.AddDict();
			dic286.SetString("SKAdNetworkIdentifier", "fz2k2k5tej.skadnetwork");

			PlistElementDict dic287;
			dic287 = URLWhiteListArr2.AddDict();
			dic287.SetString("SKAdNetworkIdentifier", "g28c52eehv.skadnetwork");

			PlistElementDict dic288;
			dic288 = URLWhiteListArr2.AddDict();
			dic288.SetString("SKAdNetworkIdentifier", "g2y4y55b64.skadnetwork");

			PlistElementDict dic289;
			dic289 = URLWhiteListArr2.AddDict();
			dic289.SetString("SKAdNetworkIdentifier", "g3y426g9ed.skadnetwork");

			PlistElementDict dic290;
			dic290 = URLWhiteListArr2.AddDict();
			dic290.SetString("SKAdNetworkIdentifier", "g69uk9uh2b.skadnetwork");

			PlistElementDict dic291;
			dic291 = URLWhiteListArr2.AddDict();
			dic291.SetString("SKAdNetworkIdentifier", "g6gcrrvk4p.skadnetwork");

			PlistElementDict dic292;
			dic292 = URLWhiteListArr2.AddDict();
			dic292.SetString("SKAdNetworkIdentifier", "gfat3222tu.skadnetwork");

			PlistElementDict dic293;
			dic293 = URLWhiteListArr2.AddDict();
			dic293.SetString("SKAdNetworkIdentifier", "ggvn48r87g.skadnetwork");

			PlistElementDict dic294;
			dic294 = URLWhiteListArr2.AddDict();
			dic294.SetString("SKAdNetworkIdentifier", "glqzh8vgby.skadnetwork");

			PlistElementDict dic295;
			dic295 = URLWhiteListArr2.AddDict();
			dic295.SetString("SKAdNetworkIdentifier", "gta8lk7p23.skadnetwork");

			PlistElementDict dic296;
			dic296 = URLWhiteListArr2.AddDict();
			dic296.SetString("SKAdNetworkIdentifier", "gta9lk7p23.skadnetwork");

			PlistElementDict dic297;
			dic297 = URLWhiteListArr2.AddDict();
			dic297.SetString("SKAdNetworkIdentifier", "gvmwg8q7h5.skadnetwork");

			PlistElementDict dic298;
			dic298 = URLWhiteListArr2.AddDict();
			dic298.SetString("SKAdNetworkIdentifier", "h5jmj969g5.skadnetwork");

			PlistElementDict dic299;
			dic299 = URLWhiteListArr2.AddDict();
			dic299.SetString("SKAdNetworkIdentifier", "h65wbv5k3f.skadnetwork");

			PlistElementDict dic300;
			dic300 = URLWhiteListArr2.AddDict();
			dic300.SetString("SKAdNetworkIdentifier", "h8vml93bkz.skadnetwork");

			PlistElementDict dic301;
			dic301 = URLWhiteListArr2.AddDict();
			dic301.SetString("SKAdNetworkIdentifier", "hb56zgv37p.skadnetwork");

			PlistElementDict dic302;
			dic302 = URLWhiteListArr2.AddDict();
			dic302.SetString("SKAdNetworkIdentifier", "hdw39hrw9y.skadnetwork");

			PlistElementDict dic303;
			dic303 = URLWhiteListArr2.AddDict();
			dic303.SetString("SKAdNetworkIdentifier", "hjevpa356n.skadnetwork");

			PlistElementDict dic304;
			dic304 = URLWhiteListArr2.AddDict();
			dic304.SetString("SKAdNetworkIdentifier", "hs6bdukanm.skadnetwork");

			PlistElementDict dic305;
			dic305 = URLWhiteListArr2.AddDict();
			dic305.SetString("SKAdNetworkIdentifier", "jb7bn6koa5.skadnetwork");

			PlistElementDict dic306;
			dic306 = URLWhiteListArr2.AddDict();
			dic306.SetString("SKAdNetworkIdentifier", "jk2fsx2rgz.skadnetwork");

			PlistElementDict dic307;
			dic307 = URLWhiteListArr2.AddDict();
			dic307.SetString("SKAdNetworkIdentifier", "k674qkevps.skadnetwork");

			PlistElementDict dic308;
			dic308 = URLWhiteListArr2.AddDict();
			dic308.SetString("SKAdNetworkIdentifier", "k6y4y55b64.skadnetwork");

			PlistElementDict dic309;
			dic309 = URLWhiteListArr2.AddDict();
			dic309.SetString("SKAdNetworkIdentifier", "kbd757ywx3.skadnetwork");

			PlistElementDict dic310;
			dic310 = URLWhiteListArr2.AddDict();
			dic310.SetString("SKAdNetworkIdentifier", "kbmxgpxpgc.skadnetwork");

			PlistElementDict dic311;
			dic311 = URLWhiteListArr2.AddDict();
			dic311.SetString("SKAdNetworkIdentifier", "klf5c3l5u5.skadnetwork");

			PlistElementDict dic312;
			dic312 = URLWhiteListArr2.AddDict();
			dic312.SetString("SKAdNetworkIdentifier", "krvm3zuq6h.skadnetwork");

			PlistElementDict dic313;
			dic313 = URLWhiteListArr2.AddDict();
			dic313.SetString("SKAdNetworkIdentifier", "l6nv3x923s.skadnetwork");

			PlistElementDict dic314;
			dic314 = URLWhiteListArr2.AddDict();
			dic314.SetString("SKAdNetworkIdentifier", "l93v5h6a4m.skadnetwork");

			PlistElementDict dic315;
			dic315 = URLWhiteListArr2.AddDict();
			dic315.SetString("SKAdNetworkIdentifier", "ln5gz23vtd.skadnetwork");

			PlistElementDict dic316;
			dic316 = URLWhiteListArr2.AddDict();
			dic316.SetString("SKAdNetworkIdentifier", "lr83yxwka7.skadnetwork");

			PlistElementDict dic317;
			dic317 = URLWhiteListArr2.AddDict();
			dic317.SetString("SKAdNetworkIdentifier", "ludvb6z3bs.skadnetwork");

			PlistElementDict dic318;
			dic318 = URLWhiteListArr2.AddDict();
			dic318.SetString("SKAdNetworkIdentifier", "m297p6643m.skadnetwork");

			PlistElementDict dic319;
			dic319 = URLWhiteListArr2.AddDict();
			dic319.SetString("SKAdNetworkIdentifier", "m5mvw97r93.skadnetwork");

			PlistElementDict dic320;
			dic320 = URLWhiteListArr2.AddDict();
			dic320.SetString("SKAdNetworkIdentifier", "m8dbw4sv7c.skadnetwork");

			PlistElementDict dic321;
			dic321 = URLWhiteListArr2.AddDict();
			dic321.SetString("SKAdNetworkIdentifier", "mj797d8u6f.skadnetwork");

			PlistElementDict dic322;
			dic322 = URLWhiteListArr2.AddDict();
			dic322.SetString("SKAdNetworkIdentifier", "mlmmfzh3r3.skadnetwork");

			PlistElementDict dic323;
			dic323 = URLWhiteListArr2.AddDict();
			dic323.SetString("SKAdNetworkIdentifier", "mls7yz5dvl.skadnetwork");

			PlistElementDict dic324;
			dic324 = URLWhiteListArr2.AddDict();
			dic324.SetString("SKAdNetworkIdentifier", "mp6xlyr22a.skadnetwork");

			PlistElementDict dic325;
			dic325 = URLWhiteListArr2.AddDict();
			dic325.SetString("SKAdNetworkIdentifier", "mqn7fxpca7.skadnetwork");

			PlistElementDict dic326;
			dic326 = URLWhiteListArr2.AddDict();
			dic326.SetString("SKAdNetworkIdentifier", "mtkv5xtk9e.skadnetwork");

			PlistElementDict dic327;
			dic327 = URLWhiteListArr2.AddDict();
			dic327.SetString("SKAdNetworkIdentifier", "mwcgp9hppj.skadnetwork");

			PlistElementDict dic328;
			dic328 = URLWhiteListArr2.AddDict();
			dic328.SetString("SKAdNetworkIdentifier", "n38lu8286q.skadnetwork");

			PlistElementDict dic329;
			dic329 = URLWhiteListArr2.AddDict();
			dic329.SetString("SKAdNetworkIdentifier", "n66cz3y3bx.skadnetwork");

			PlistElementDict dic330;
			dic330 = URLWhiteListArr2.AddDict();
			dic330.SetString("SKAdNetworkIdentifier", "n6fk4nfna4.skadnetwork");

			PlistElementDict dic331;
			dic331 = URLWhiteListArr2.AddDict();
			dic331.SetString("SKAdNetworkIdentifier", "n9x2a789qt.skadnetwork");

			PlistElementDict dic332;
			dic332 = URLWhiteListArr2.AddDict();
			dic332.SetString("SKAdNetworkIdentifier", "nfqy3847ph.skadnetwork");

			PlistElementDict dic333;
			dic333 = URLWhiteListArr2.AddDict();
			dic333.SetString("SKAdNetworkIdentifier", "nrt9jy4kw9.skadnetwork");

			PlistElementDict dic334;
			dic334 = URLWhiteListArr2.AddDict();
			dic334.SetString("SKAdNetworkIdentifier", "ns5j362hk7.skadnetwork");

			PlistElementDict dic335;
			dic335 = URLWhiteListArr2.AddDict();
			dic335.SetString("SKAdNetworkIdentifier", "nu4557a4je.skadnetwork");

			PlistElementDict dic336;
			dic336 = URLWhiteListArr2.AddDict();
			dic336.SetString("SKAdNetworkIdentifier", "nzq8sh4pbs.skadnetwork");

			PlistElementDict dic337;
			dic337 = URLWhiteListArr2.AddDict();
			dic337.SetString("SKAdNetworkIdentifier", "p78axxw29g.skadnetwork");

			PlistElementDict dic338;
			dic338 = URLWhiteListArr2.AddDict();
			dic338.SetString("SKAdNetworkIdentifier", "pd25vrrwzn.skadnetwork");

			PlistElementDict dic339;
			dic339 = URLWhiteListArr2.AddDict();
			dic339.SetString("SKAdNetworkIdentifier", "ppxm28t8ap.skadnetwork");

			PlistElementDict dic340;
			dic340 = URLWhiteListArr2.AddDict();
			dic340.SetString("SKAdNetworkIdentifier", "prcb7njmu6.skadnetwork");

			PlistElementDict dic341;
			dic341 = URLWhiteListArr2.AddDict();
			dic341.SetString("SKAdNetworkIdentifier", "pt89h2hlb7.skadnetwork");

			PlistElementDict dic342;
			dic342 = URLWhiteListArr2.AddDict();
			dic342.SetString("SKAdNetworkIdentifier", "pu4na253f3.skadnetwork");

			PlistElementDict dic343;
			dic343 = URLWhiteListArr2.AddDict();
			dic343.SetString("SKAdNetworkIdentifier", "pwa73g5rt2.skadnetwork");

			PlistElementDict dic344;
			dic344 = URLWhiteListArr2.AddDict();
			dic344.SetString("SKAdNetworkIdentifier", "pwdxu55a5a.skadnetwork");

			PlistElementDict dic345;
			dic345 = URLWhiteListArr2.AddDict();
			dic345.SetString("SKAdNetworkIdentifier", "qlbq5gtkt8.skadnetwork");

			PlistElementDict dic346;
			dic346 = URLWhiteListArr2.AddDict();
			dic346.SetString("SKAdNetworkIdentifier", "qqp299437r.skadnetwork");

			PlistElementDict dic347;
			dic347 = URLWhiteListArr2.AddDict();
			dic347.SetString("SKAdNetworkIdentifier", "qu637u8glc.skadnetwork");

			PlistElementDict dic348;
			dic348 = URLWhiteListArr2.AddDict();
			dic348.SetString("SKAdNetworkIdentifier", "qwpu75vrh2.skadnetwork");

			PlistElementDict dic349;
			dic349 = URLWhiteListArr2.AddDict();
			dic349.SetString("SKAdNetworkIdentifier", "r26jy69rpl.skadnetwork");

			PlistElementDict dic350;
			dic350 = URLWhiteListArr2.AddDict();
			dic350.SetString("SKAdNetworkIdentifier", "r45fhb6rf7.skadnetwork");

			PlistElementDict dic351;
			dic351 = URLWhiteListArr2.AddDict();
			dic351.SetString("SKAdNetworkIdentifier", "r8lj5b58b5.skadnetwork");

			PlistElementDict dic352;
			dic352 = URLWhiteListArr2.AddDict();
			dic352.SetString("SKAdNetworkIdentifier", "rvh3l7un93.skadnetwork");

			PlistElementDict dic353;
			dic353 = URLWhiteListArr2.AddDict();
			dic353.SetString("SKAdNetworkIdentifier", "rx5hdcabgc.skadnetwork");

			PlistElementDict dic354;
			dic354 = URLWhiteListArr2.AddDict();
			dic354.SetString("SKAdNetworkIdentifier", "s39g8k73mm.skadnetwork");

			PlistElementDict dic355;
			dic355 = URLWhiteListArr2.AddDict();
			dic355.SetString("SKAdNetworkIdentifier", "s69wq72ugq.skadnetwork");

			PlistElementDict dic356;
			dic356 = URLWhiteListArr2.AddDict();
			dic356.SetString("SKAdNetworkIdentifier", "sczv5946wb.skadnetwork");

			PlistElementDict dic357;
			dic357 = URLWhiteListArr2.AddDict();
			dic357.SetString("SKAdNetworkIdentifier", "su67r6k2v3.skadnetwork");

			PlistElementDict dic358;
			dic358 = URLWhiteListArr2.AddDict();
			dic358.SetString("SKAdNetworkIdentifier", "t38b2kh725.skadnetwork");

			PlistElementDict dic359;
			dic359 = URLWhiteListArr2.AddDict();
			dic359.SetString("SKAdNetworkIdentifier", "t3b3f7n3x8.skadnetwork");

			PlistElementDict dic360;
			dic360 = URLWhiteListArr2.AddDict();
			dic360.SetString("SKAdNetworkIdentifier", "t6d3zquu66.skadnetwork");

			PlistElementDict dic361;
			dic361 = URLWhiteListArr2.AddDict();
			dic361.SetString("SKAdNetworkIdentifier", "t7ky8fmwkd.skadnetwork");

			PlistElementDict dic362;
			dic362 = URLWhiteListArr2.AddDict();
			dic362.SetString("SKAdNetworkIdentifier", "tl55sbb4fm.skadnetwork");

			PlistElementDict dic363;
			dic363 = URLWhiteListArr2.AddDict();
			dic363.SetString("SKAdNetworkIdentifier", "tmhh9296z4.skadnetwork");

			PlistElementDict dic364;
			dic364 = URLWhiteListArr2.AddDict();
			dic364.SetString("SKAdNetworkIdentifier", "tskbem2b5g.skadnetwork");

			PlistElementDict dic365;
			dic365 = URLWhiteListArr2.AddDict();
			dic365.SetString("SKAdNetworkIdentifier", "tvvz7th9br.skadnetwork");

			PlistElementDict dic366;
			dic366 = URLWhiteListArr2.AddDict();
			dic366.SetString("SKAdNetworkIdentifier", "u679fj5vs4.skadnetwork");

			PlistElementDict dic367;
			dic367 = URLWhiteListArr2.AddDict();
			dic367.SetString("SKAdNetworkIdentifier", "uw77j35x4d.skadnetwork");

			PlistElementDict dic368;
			dic368 = URLWhiteListArr2.AddDict();
			dic368.SetString("SKAdNetworkIdentifier", "uzqba5354d.skadnetwork");

			PlistElementDict dic369;
			dic369 = URLWhiteListArr2.AddDict();
			dic369.SetString("SKAdNetworkIdentifier", "v4nxqhlyqp.skadnetwork");

			PlistElementDict dic370;
			dic370 = URLWhiteListArr2.AddDict();
			dic370.SetString("SKAdNetworkIdentifier", "v72qych5uu.skadnetwork");

			PlistElementDict dic371;
			dic371 = URLWhiteListArr2.AddDict();
			dic371.SetString("SKAdNetworkIdentifier", "v7896pgt74.skadnetwork");

			PlistElementDict dic372;
			dic372 = URLWhiteListArr2.AddDict();
			dic372.SetString("SKAdNetworkIdentifier", "v79kvwwj4g.skadnetwork");

			PlistElementDict dic373;
			dic373 = URLWhiteListArr2.AddDict();
			dic373.SetString("SKAdNetworkIdentifier", "v9wttpbfk9.skadnetwork");

			PlistElementDict dic374;
			dic374 = URLWhiteListArr2.AddDict();
			dic374.SetString("SKAdNetworkIdentifier", "vc83br9sjg.skadnetwork");

			PlistElementDict dic375;
			dic375 = URLWhiteListArr2.AddDict();
			dic375.SetString("SKAdNetworkIdentifier", "vcra2ehyfk.skadnetwork");

			PlistElementDict dic376;
			dic376 = URLWhiteListArr2.AddDict();
			dic376.SetString("SKAdNetworkIdentifier", "vhf287vqwu.skadnetwork");

			PlistElementDict dic377;
			dic377 = URLWhiteListArr2.AddDict();
			dic377.SetString("SKAdNetworkIdentifier", "vutu7akeur.skadnetwork");

			PlistElementDict dic378;
			dic378 = URLWhiteListArr2.AddDict();
			dic378.SetString("SKAdNetworkIdentifier", "w28pnjg2k4.skadnetwork");

			PlistElementDict dic379;
			dic379 = URLWhiteListArr2.AddDict();
			dic379.SetString("SKAdNetworkIdentifier", "w7jznl3r6g.skadnetwork");

			PlistElementDict dic380;
			dic380 = URLWhiteListArr2.AddDict();
			dic380.SetString("SKAdNetworkIdentifier", "w9q455wk68.skadnetwork");

			PlistElementDict dic381;
			dic381 = URLWhiteListArr2.AddDict();
			dic381.SetString("SKAdNetworkIdentifier", "wg4vff78zm.skadnetwork");

			PlistElementDict dic382;
			dic382 = URLWhiteListArr2.AddDict();
			dic382.SetString("SKAdNetworkIdentifier", "wzmmz9fp6w.skadnetwork");

			PlistElementDict dic383;
			dic383 = URLWhiteListArr2.AddDict();
			dic383.SetString("SKAdNetworkIdentifier", "x2jnk7ly8j.skadnetwork");

			PlistElementDict dic384;
			dic384 = URLWhiteListArr2.AddDict();
			dic384.SetString("SKAdNetworkIdentifier", "x44k69ngh6.skadnetwork");

			PlistElementDict dic385;
			dic385 = URLWhiteListArr2.AddDict();
			dic385.SetString("SKAdNetworkIdentifier", "x5854y7y24.skadnetwork");

			PlistElementDict dic386;
			dic386 = URLWhiteListArr2.AddDict();
			dic386.SetString("SKAdNetworkIdentifier", "x5l83yy675.skadnetwork");

			PlistElementDict dic387;
			dic387 = URLWhiteListArr2.AddDict();
			dic387.SetString("SKAdNetworkIdentifier", "x8jxxk4ff5.skadnetwork");

			PlistElementDict dic388;
			dic388 = URLWhiteListArr2.AddDict();
			dic388.SetString("SKAdNetworkIdentifier", "x8uqf25wch.skadnetwork");

			PlistElementDict dic389;
			dic389 = URLWhiteListArr2.AddDict();
			dic389.SetString("SKAdNetworkIdentifier", "x8yj322td6.skadnetwork");

			PlistElementDict dic390;
			dic390 = URLWhiteListArr2.AddDict();
			dic390.SetString("SKAdNetworkIdentifier", "xga6mpmplv.skadnetwork");

			PlistElementDict dic391;
			dic391 = URLWhiteListArr2.AddDict();
			dic391.SetString("SKAdNetworkIdentifier", "xmn954pzmp.skadnetwork");

			PlistElementDict dic392;
			dic392 = URLWhiteListArr2.AddDict();
			dic392.SetString("SKAdNetworkIdentifier", "xx9sdjej2w.skadnetwork");

			PlistElementDict dic393;
			dic393 = URLWhiteListArr2.AddDict();
			dic393.SetString("SKAdNetworkIdentifier", "xy9t38ct57.skadnetwork");

			PlistElementDict dic394;
			dic394 = URLWhiteListArr2.AddDict();
			dic394.SetString("SKAdNetworkIdentifier", "y45688jllp.skadnetwork");

			PlistElementDict dic395;
			dic395 = URLWhiteListArr2.AddDict();
			dic395.SetString("SKAdNetworkIdentifier", "y5ghdn5j9k.skadnetwork");

			PlistElementDict dic396;
			dic396 = URLWhiteListArr2.AddDict();
			dic396.SetString("SKAdNetworkIdentifier", "y755zyxw56.skadnetwork");

			PlistElementDict dic397;
			dic397 = URLWhiteListArr2.AddDict();
			dic397.SetString("SKAdNetworkIdentifier", "yclnxrl5pm.skadnetwork");

			PlistElementDict dic398;
			dic398 = URLWhiteListArr2.AddDict();
			dic398.SetString("SKAdNetworkIdentifier", "ydx93a7ass.skadnetwork");

			PlistElementDict dic399;
			dic399 = URLWhiteListArr2.AddDict();
			dic399.SetString("SKAdNetworkIdentifier", "yrqqpx2mcb.skadnetwork");

			PlistElementDict dic400;
			dic400 = URLWhiteListArr2.AddDict();
			dic400.SetString("SKAdNetworkIdentifier", "z24wtl6j62.skadnetwork");

			PlistElementDict dic401;
			dic401 = URLWhiteListArr2.AddDict();
			dic401.SetString("SKAdNetworkIdentifier", "z4gj7hsk7h.skadnetwork");

			PlistElementDict dic402;
			dic402 = URLWhiteListArr2.AddDict();
			dic402.SetString("SKAdNetworkIdentifier", "z5b3gh5ugf.skadnetwork");

			PlistElementDict dic403;
			dic403 = URLWhiteListArr2.AddDict();
			dic403.SetString("SKAdNetworkIdentifier", "z959bm4gru.skadnetwork");

			PlistElementDict dic404;
			dic404 = URLWhiteListArr2.AddDict();
			dic404.SetString("SKAdNetworkIdentifier", "zh3b7bxvad.skadnetwork");

			PlistElementDict dic405;
			dic405 = URLWhiteListArr2.AddDict();
			dic405.SetString("SKAdNetworkIdentifier", "zmvfpc5aq8.skadnetwork");

			PlistElementDict dic406;
			dic406 = URLWhiteListArr2.AddDict();
			dic406.SetString("SKAdNetworkIdentifier", "zq492l623r.skadnetwork");
			
			#endregion
			
			PlistElementArray QueriesSchemes = plist.root.CreateArray("LSApplicationQueriesSchemes");
			QueriesSchemes.AddString("openapp.jdmobile");
			QueriesSchemes.AddString("vipshop");
			QueriesSchemes.AddString("weixinULAPI");
			QueriesSchemes.AddString("weixinURLParamsAPI");
			QueriesSchemes.AddString("tbopen");
			QueriesSchemes.AddString("tmall");
			QueriesSchemes.AddString("suning");
			QueriesSchemes.AddString("ctrip");
			QueriesSchemes.AddString("qunariphone");
			QueriesSchemes.AddString("imeituan");
			QueriesSchemes.AddString("dianping");
			QueriesSchemes.AddString("lianjia");
			QueriesSchemes.AddString("pddopen");
			QueriesSchemes.AddString("xhsdiscover");
			QueriesSchemes.AddString("baiduhaokan");
			QueriesSchemes.AddString("bdminivideo");
			QueriesSchemes.AddString("baiduboxlite");
			QueriesSchemes.AddString("baiduboxmission");
			QueriesSchemes.AddString("com.baidu.tieba");
			QueriesSchemes.AddString("zhihu");
			QueriesSchemes.AddString("wireless1688");
			QueriesSchemes.AddString("iqiyi");
			QueriesSchemes.AddString("weixin");
			QueriesSchemes.AddString("wechat");
			QueriesSchemes.AddString("qihooloan");
			QueriesSchemes.AddString("weishi");
			QueriesSchemes.AddString("travelguide");
			QueriesSchemes.AddString("wbmain");
			QueriesSchemes.AddString("taobaotravel");
			QueriesSchemes.AddString("kwai");
			QueriesSchemes.AddString("ksnebula");
			QueriesSchemes.AddString("sinaweibo");
			QueriesSchemes.AddString("alipays");
			QueriesSchemes.AddString("youku");
			QueriesSchemes.AddString("openjdlite");
			QueriesSchemes.AddString("com.360buy.jdpingou");
			QueriesSchemes.AddString("cainiao");
			QueriesSchemes.AddString("kaola");
			QueriesSchemes.AddString("OneTravel");
			QueriesSchemes.AddString("lianjiabeike");
			QueriesSchemes.AddString("iosamap");

			File.WriteAllText(plistPath, plist.WriteToString());
		}
	}

	public class GoogleJson
	{
		public string Name { get; set; }

	}
}
#endif

