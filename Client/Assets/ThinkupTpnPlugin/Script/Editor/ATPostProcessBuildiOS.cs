#if UNITY_IOS || UNITY_IPHONE

using AnyThink.Scripts.IntegrationManager.Editor;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.iOS.Xcode.Extensions;
#endif
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AnyThink.Scripts.Editor
{
    [Serializable]
    public class TopOnSkAdNetworkInfo
    {
        [SerializeField] public string[] ids;
    }

    public class TopOnPostProcessBuildiOS
    {
        private static string sBuildPath;

        private static readonly string[] AtsRequiringNetworkNames = {
            "AdColony",
            "ByteDance",
            "Fyber",
            "Google",
            "GoogleAdManager",
            "HyprMX",
            "InMobi",
            "IronSource",
            "Smaato"
        };

        private static readonly string[] FixedEmbedFrameworks = {
            "KSAdSDK/KSAdSDK.xcframework",
            "StartAppSDK/StartApp.xcframework",
            "BigoADS/BigoADS/BigoADS.xcframework",
            "BigoADS/BigoADS/OMSDK_Bigosg.xcframework",
            "Fyber_Marketplace_SDK/IASDKCore/IASDKCore.xcframework",
            "InMobiSDK/InMobiSDK.xcframework",
            "GDTMobSDK/GDTFramework/GDTMobSDK.xcframework",
            "GDTMobSDK/GDTFramework/Tquic.xcframework",
            "ATOM-Standalone/ATOM.xcframework"
        };

        private static List<string> CollectDynamicFrameworkPaths(string buildRoot)
        {
            var result = new List<string>();
            foreach (var fw in FixedEmbedFrameworks)
            {
                result.Add(Path.Combine("Pods/", fw));
            }

            AppendVersionedFramework(result, buildRoot,
                "Pods/HyBid/PubnativeLite/PubnativeLite", "OMSDK-*",
                dir => "HyBid/PubnativeLite/PubnativeLite/" + dir + "/OMSDK_Pubnativenet.xcframework");

            AppendVersionedFramework(result, buildRoot,
                "Pods/AppLovinSDK", "applovin-ios-sdk-*",
                dir => "AppLovinSDK/" + dir + "/AppLovinSDK.xcframework");

            return result;
        }

        private static void AppendVersionedFramework(List<string> target, string buildRoot,
            string searchBase, string pattern, Func<string, string> pathBuilder)
        {
            var basePath = Path.Combine(buildRoot, searchBase);
            if (!Directory.Exists(basePath)) return;
            var dirs = Directory.GetDirectories(basePath, pattern);
            if (dirs.Length == 0) return;
            var dirName = Path.GetFileName(dirs[0]);
            target.Add(Path.Combine("Pods/", pathBuilder(dirName)));
        }

        private static readonly string[] BundleResources = {
            "BigoADS/BigoADS/BigoADSRes.bundle",
            "Ads-Global/SDK/PAGAdSDK.bundle",
            "Ads-CN/SDK/CSJAdSDK.bundle",
            "Ads-CN-Beta/SDK/CSJAdSDK.bundle"
        };

        private static readonly List<string> SwiftLanguageNetworks = new List<string>
        {
            "MoPub"
        };

        private static readonly List<string> EmbedSwiftStandardLibrariesNetworks = new List<string>
        {
            "Facebook",
            "MoPub"
        };

        [PostProcessBuildAttribute(int.MaxValue)]
        public static void TopOnPostProcessPbxProject(BuildTarget buildTarget, string buildPath)
        {
            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
            var unityMainTargetGuid = project.GetUnityMainTargetGuid();
            var unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
#else
            var unityMainTargetGuid = project.TargetGuidByName(UnityMainTargetName);
            var unityFrameworkTargetGuid = project.TargetGuidByName(UnityMainTargetName);
#endif

            project.SetBuildProperty(unityMainTargetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
            project.SetBuildProperty(unityMainTargetGuid, "ENABLE_BITCODE", "NO");

            project.SetBuildProperty(unityFrameworkTargetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
            project.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");

            ProcessDynamicFrameworkEmbedding(buildPath, project, unityMainTargetGuid);
            IntegrateBundleResources(buildPath, project, unityMainTargetGuid);

            project.WriteToFile(projectPath);
        }

        [PostProcessBuildAttribute(int.MaxValue)]
        public static void TopOnPostProcessPlist(BuildTarget buildTarget, string path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

#if UNITY_2018_2_OR_NEWER
            ConfigureAdmobAppIdentifier(plist);
#endif

            plist.WriteToFile(plistPath);
        }

        private static void IntegrateBundleResources(string buildPath, PBXProject project, string targetGuid)
        {
#if UNITY_2019_3_OR_NEWER
            int added = 0;
            foreach (var res in BundleResources)
            {
                var fullPath = Path.Combine("Pods/", res);
                if (!Directory.Exists(Path.Combine(buildPath, fullPath))) continue;
                var guid = project.AddFile(fullPath, fullPath, PBXSourceTree.Source);
                project.AddFileToBuild(targetGuid, guid);
                added++;
            }
            if (added > 0) ATLog.log("IntegrateBundleResources: added " + added + " bundles");
#endif
        }

        private static void ProcessDynamicFrameworkEmbedding(string buildPath, PBXProject project, string targetGuid)
        {
            ATLog.log("ProcessDynamicFrameworkEmbedding() >>> buildPath: " + buildPath);
            var frameworkPaths = CollectDynamicFrameworkPaths(buildPath);
            var existing = frameworkPaths.Where(p => Directory.Exists(Path.Combine(buildPath, p))).ToList();
            if (existing.Count == 0) return;

#if UNITY_2019_3_OR_NEWER
            foreach (var fwPath in existing)
            {
                var guid = project.AddFile(fwPath, fwPath);
                project.AddFileToEmbedFrameworks(targetGuid, guid);
            }
#else
            string searchPaths;
#if UNITY_2018_2_OR_NEWER
            searchPaths = project.GetBuildPropertyForAnyConfig(targetGuid, "LD_RUNPATH_SEARCH_PATHS");
#else
            searchPaths = "$(inherited)";
#endif
            searchPaths += string.IsNullOrEmpty(searchPaths) ? "" : " ";
            if (searchPaths.Contains("@executable_path/Frameworks")) return;
            searchPaths += "@executable_path/Frameworks";
            project.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", searchPaths);
#endif
        }

#if UNITY_2018_2_OR_NEWER

        private static void ConfigureAdmobAppIdentifier(PlistDocument plist)
        {
            if (!ATConfig.isNetworkInstalledByName("Admob", ATConfig.OS_IOS))
            {   
                ATLog.log("addGoogleApplicationIdIfNeeded() >>> Admob not install.");
                return;
            }
            //获取admob app id
            var appId = ATConfig.getAdmobAppIdByOs(ATConfig.OS_IOS);

            if (string.IsNullOrEmpty(appId) || !appId.StartsWith("ca-app-pub-"))
            {
                ATLog.logError("AdMob App ID is not set. Please enter a valid app ID within the ThinkupTpn Integration Manager window.");
                return;
            }

            const string googleApplicationIdentifier = "GADApplicationIdentifier";
            plist.root.SetString(googleApplicationIdentifier, appId);
        }
#endif
    }
}
#endif
