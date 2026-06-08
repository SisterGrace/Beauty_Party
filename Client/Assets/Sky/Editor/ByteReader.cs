/*using UnityEngine;
using UnityEditor;
using System.IO;
using Bright.Serialization;
using Config.config;

public class TbConstantReader : EditorWindow
{
    [MenuItem("Tools/Read TbConstant from Bytes Folder")]
    static void ReadTbConstant()
    {
        // 这里填写你的 .bytes 文件的实际路径（完整路径或相对于项目根目录的路径）
        string filePath = "D:/PistaGame/DreamTileNew/Client/Assets/AssetRaw/Configs/bytes/config_tbconstant.bytes"; // 替换成真实文件名

        if (!File.Exists(filePath))
        {
            Debug.LogError($"文件不存在: {filePath}");
            return;
        }

        byte[] bytes = File.ReadAllBytes(filePath);
        ByteBuf buf = new ByteBuf(bytes);
        TbConstant tb = new TbConstant(buf);

        Debug.Log($"PrivacyUrl = {tb.PrivacyUrl}");
        Debug.Log($"RegisterUrl = {tb.RegisterUrl}");
        // 打印更多字段...
    }
}*/