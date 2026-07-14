using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：生成加密的CDN URL密文
/// 运行后从Console复制密文，粘贴到 StreamingAssets/Configs/url_config.txt
/// </summary>
public class URLEncryptorTool
{
    [MenuItem("Tools/Encrypt CDN URL")]
    static void EncryptURL()
    {
        string plain = "https://game.20260602.icu/BeautyParty/Byte/";
        string encrypted = StringEncryptor.EncryptToBase64(plain);
        Debug.Log("=== CDN URL ===\n" +
                  "原文: " + plain + "\n" +
                  "密文（复制到url_config.txt）:\n" + encrypted);
    }
}
