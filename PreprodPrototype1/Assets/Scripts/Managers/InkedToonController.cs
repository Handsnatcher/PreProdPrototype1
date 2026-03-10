using UnityEngine;

public class InkedToonController : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Drag your InkedToon material here")]
    public Material targetMaterial;

    [Header("Toon Shading")]
    public Color baseColor = Color.white;
    public Color shadowColor = new Color(0.2f, 0.2f, 0.3f, 1f);
    [Range(1, 8)]
    public int shadowSteps = 3;
    [Range(0f, 0.3f)]
    public float shadowSmoothness = 0.05f;

    [Header("Rim Light")]
    public Color rimColor = Color.white;
    [Range(0.1f, 8f)]
    public float rimPower = 3f;

    [Header("Outline")]
    public Color outlineColor = Color.black;
    [Range(0f, 0.1f)]
    public float outlineWidth = 0.02f;

    [Header("Ink")]
    public Texture2D inkTexture;
    [Range(0f, 1f)]
    public float inkStrength = 0.3f;
    [Range(0.1f, 10f)]
    public float inkScale = 3f;

    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_ShadowColor = Shader.PropertyToID("_ShadowColor");
    static readonly int ID_ShadowSteps = Shader.PropertyToID("_ShadowSteps");
    static readonly int ID_ShadowSmooth = Shader.PropertyToID("_ShadowSmooth");
    static readonly int ID_RimColor = Shader.PropertyToID("_RimColor");
    static readonly int ID_RimPower = Shader.PropertyToID("_RimPower");
    static readonly int ID_OutlineColor = Shader.PropertyToID("_OutlineColor");
    static readonly int ID_OutlineWidth = Shader.PropertyToID("_OutlineWidth");
    static readonly int ID_InkTex = Shader.PropertyToID("_InkTex");
    static readonly int ID_InkStrength = Shader.PropertyToID("_InkStrength");
    static readonly int ID_InkScale = Shader.PropertyToID("_InkScale");

    void Start()
    {
        ApplyToMaterial();
    }

    public void SetMaterial(Material mat)
    {
        targetMaterial = mat;
        ApplyToMaterial();
    }

    public void ApplyToMaterial()
    {
        if (targetMaterial == null)
        {
            Debug.LogWarning("InkedToonController: Assign a material to Target Material.");
            return;
        }

        targetMaterial.SetColor(ID_Color, baseColor);
        targetMaterial.SetColor(ID_ShadowColor, shadowColor);
        targetMaterial.SetFloat(ID_ShadowSteps, shadowSteps);
        targetMaterial.SetFloat(ID_ShadowSmooth, shadowSmoothness);
        targetMaterial.SetColor(ID_RimColor, rimColor);
        targetMaterial.SetFloat(ID_RimPower, rimPower);
        targetMaterial.SetColor(ID_OutlineColor, outlineColor);
        targetMaterial.SetFloat(ID_OutlineWidth, outlineWidth);
        targetMaterial.SetFloat(ID_InkStrength, inkStrength);
        targetMaterial.SetFloat(ID_InkScale, inkScale);

        if (inkTexture != null)
            targetMaterial.SetTexture(ID_InkTex, inkTexture);
    }
}