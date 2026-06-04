using UnityEngine;

[CreateAssetMenu(fileName = "TK_EngineGlobalSettings", menuName = "TK_Engine/TK_EngineSettings")]
public class TK_EngineSettings : ScriptableObject
{
    [Header("Framework")] [SerializeField] private FrameworkGlobalSettings m_FrameworkGlobalSettings;

    public FrameworkGlobalSettings FrameworkGlobalSettings => m_FrameworkGlobalSettings;

    [Header("HybridCLR")] [SerializeField] private HybridCLRCustomGlobalSettings m_HybridCLRCustomGlobalSettings;

    public HybridCLRCustomGlobalSettings BybridCLRCustomGlobalSettings => m_HybridCLRCustomGlobalSettings;

    public void Set(FrameworkGlobalSettings globalSettings,HybridCLRCustomGlobalSettings hybridClrCustomGlobalSettings)
    {
        m_FrameworkGlobalSettings = globalSettings;
        m_HybridCLRCustomGlobalSettings = hybridClrCustomGlobalSettings;
    }
}