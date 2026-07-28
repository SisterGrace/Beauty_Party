using UnityEngine;

[CreateAssetMenu(fileName = "KeystoreConfig", menuName = "Sky/Keystore Config")]
public class KeystoreConfig : ScriptableObject
{
    public string keystorePass;
    public string keyaliasName;
    public string keyaliasPass;
    public string keystoreName; // 可以是绝对路径或相对于项目根目录的路径
}