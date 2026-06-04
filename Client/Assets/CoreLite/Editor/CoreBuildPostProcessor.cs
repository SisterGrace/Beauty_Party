using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CoreLite;
using UnityEditor;
using UnityEditor.Android;

#if UNITY_ANDROID

namespace CoreLite
{
    public class CoreAndroid : IPostGenerateGradleAndroidProject
    {

        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
#if UNITY_2019_3_OR_NEWER
            AddAdmobAppId(path);
#endif
        }

        private void AddAdmobAppId(string path)
        {
            var appManifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

            var manifestFile = new XmlDocument();
            manifestFile.Load(appManifestPath);
            const string gmsAdsApplicationID = "com.google.android.gms.ads.APPLICATION_ID";
            var admobAppId = GetMetaDataValue(manifestFile, gmsAdsApplicationID);

            var projectAdmobAppId = CoreConfigMgr.Instance.GetAdmobAppId();

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
                CoreLogUtils.Debug("Google advertising channel was used, but admobId was not configured in the configuration file.");
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
                // 获取当前目录下所有文件
                var files = Directory.GetFiles(path);
                fileList.AddRange(files);

                // 递归获取子文件夹中的所有文件
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