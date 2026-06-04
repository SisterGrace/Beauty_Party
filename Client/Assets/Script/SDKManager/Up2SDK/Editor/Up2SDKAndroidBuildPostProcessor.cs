using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

#if UNITY_ANDROID
namespace Up2SDK.Editor
{
    public class Up2SDKAndroidBuildPostProcessor : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespaceUri = "http://schemas.android.com/apk/res/android";
        private const string Up2ApplicationClassName = "com.unity3d.player.Up2Application";

        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
#if UNITY_2019_3_OR_NEWER
            SetApplication(path);
#endif
        }

        private static void SetApplication(string path)
        {
            var appManifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            if (!File.Exists(appManifestPath))
            {
                Debug.LogWarning("[Up2SDK] AndroidManifest.xml not found: " + appManifestPath);
                return;
            }

            var manifestFile = new XmlDocument();
            manifestFile.Load(appManifestPath);

            var applicationNode = manifestFile.SelectSingleNode("/manifest/application");
            if (applicationNode is XmlElement applicationElement)
            {
                applicationElement.SetAttribute("name", AndroidNamespaceUri, Up2ApplicationClassName);
                manifestFile.Save(appManifestPath);
            }
            else
            {
                Debug.LogWarning("[Up2SDK] application node not found in AndroidManifest.xml");
            }
        }
    }
}
#endif
