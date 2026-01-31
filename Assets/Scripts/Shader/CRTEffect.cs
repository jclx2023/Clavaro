using UnityEngine;

/// <summary>
/// Balatro风格的CRT效果控制器
/// 挂载到主游戏相机上，提供像素化、扫描线和色差效果
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CRTEffect : MonoBehaviour
{
    [Header("像素化设置")]
    [Tooltip("像素化倍数，数值越大越模糊（推荐2-4）")]
    [Range(0, 8)]
    public int pixelationScale = 3;

    [Header("扫描线设置")]
    [Tooltip("是否启用扫描线效果")]
    public bool enableScanlines = true;
    
    [Tooltip("扫描线强度（推荐0.2-0.4）")]
    [Range(0f, 1f)]
    public float scanlineIntensity = 0.3f;
    
    [Tooltip("扫描线密度（每像素扫描线数量）")]
    [Range(0.5f, 4f)]
    public float scanlineCount = 2f;
    
    [Tooltip("扫描线滚动速度")]
    [Range(-2f, 2f)]
    public float scanlineSpeed = 0.1f;

    [Header("色差设置")]
    [Tooltip("是否启用色差效果")]
    public bool enableChromaticAberration = true;
    
    [Tooltip("色差偏移强度（像素）")]
    [Range(0f, 5f)]
    public float chromaticAberration = 1.5f;
    
    [Tooltip("色差角度（0=水平，90=垂直）")]
    [Range(0f, 360f)]
    public float chromaticAngle = 0f;

    [Header("CRT曲面设置")]
    [Tooltip("是否启用CRT屏幕弯曲效果")]
    public bool enableCurvature = true;
    
    [Tooltip("屏幕弯曲强度")]
    [Range(0f, 0.2f)]
    public float curvature = 0.08f;

    [Header("其他设置")]
    [Tooltip("屏幕边缘暗角强度")]
    [Range(0f, 1f)]
    public float vignette = 0.3f;
    
    [Tooltip("整体亮度调整")]
    [Range(0.5f, 1.5f)]
    public float brightness = 1.0f;

    private Material crtMaterial;
    private Shader crtShader;
    private RenderTexture pixelatedRT;

    void OnEnable()
    {
        // 查找shader
        crtShader = Shader.Find("Hidden/CRTEffect");
        
        if (crtShader == null)
        {
            Debug.LogError("CRTEffect: 找不到 'Hidden/CRTEffect' Shader！请确保CRTShader.shader在项目中。");
            enabled = false;
            return;
        }

        // 创建材质
        if (crtMaterial == null)
        {
            crtMaterial = new Material(crtShader);
            crtMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    void OnDisable()
    {
        // 清理资源
        if (crtMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(crtMaterial);
            else
                DestroyImmediate(crtMaterial);
        }

        if (pixelatedRT != null)
        {
            pixelatedRT.Release();
            if (Application.isPlaying)
                Destroy(pixelatedRT);
            else
                DestroyImmediate(pixelatedRT);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (crtMaterial == null || crtShader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 计算像素化后的分辨率
        int pixelWidth = source.width / Mathf.Max(1, pixelationScale);
        int pixelHeight = source.height / Mathf.Max(1, pixelationScale);

        // 创建或重建像素化RenderTexture
        if (pixelatedRT == null || 
            pixelatedRT.width != pixelWidth || 
            pixelatedRT.height != pixelHeight)
        {
            if (pixelatedRT != null)
            {
                pixelatedRT.Release();
                DestroyImmediate(pixelatedRT);
            }

            pixelatedRT = new RenderTexture(pixelWidth, pixelHeight, 0, RenderTextureFormat.ARGB32);
            pixelatedRT.filterMode = FilterMode.Point; // 关键：点采样保持像素化
            pixelatedRT.Create();
        }

        // 第一步：渲染到低分辨率纹理（像素化）
        Graphics.Blit(source, pixelatedRT);

        // 设置shader参数
        crtMaterial.SetTexture("_MainTex", pixelatedRT);
        crtMaterial.SetFloat("_ScanlineIntensity", enableScanlines ? scanlineIntensity : 0f);
        crtMaterial.SetFloat("_ScanlineCount", scanlineCount);
        crtMaterial.SetFloat("_ScanlineSpeed", scanlineSpeed);
        crtMaterial.SetFloat("_ChromaticAberration", enableChromaticAberration ? chromaticAberration : 0f);
        crtMaterial.SetFloat("_ChromaticAngle", chromaticAngle * Mathf.Deg2Rad);
        crtMaterial.SetFloat("_Curvature", enableCurvature ? curvature : 0f);
        crtMaterial.SetFloat("_Vignette", vignette);
        crtMaterial.SetFloat("_Brightness", brightness);
        crtMaterial.SetFloat("_CRTTime", Time.time); // 使用_CRTTime避免与Unity内置变量冲突
        crtMaterial.SetVector("_ScreenSize", new Vector4(pixelWidth, pixelHeight, source.width, source.height));

        // 第二步：应用CRT效果到最终输出
        Graphics.Blit(pixelatedRT, destination, crtMaterial);
    }

    // 在Inspector中实时预览
    void OnValidate()
    {
        pixelationScale = Mathf.Max(1, pixelationScale);
    }

    // 编辑器预览支持
    #if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
        {
            // 在编辑模式下强制重绘
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
    }
    #endif
}