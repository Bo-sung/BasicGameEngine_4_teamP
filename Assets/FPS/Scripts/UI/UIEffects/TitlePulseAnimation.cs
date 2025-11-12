using UnityEngine;
using TMPro;

/// <summary>
/// 타이틀 텍스트 펄스 애니메이션 (HTML의 titlePulse 재현)
/// Outline 크기와 발광 효과 애니메이션
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class TitlePulseAnimation : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private float minOutlineWidth = 0.2f;
    [SerializeField] private float maxOutlineWidth = 0.4f;
    [SerializeField] private float pulseSpeed = 1f; // 2초 주기 = 0.5 speed

    [Header("Glow Settings")]
    [SerializeField] private float minGlowAlpha = 0.5f;
    [SerializeField] private float maxGlowAlpha = 1f;

    [Header("Scale Pulse (선택)")]
    [SerializeField] private bool enableScalePulse = false;
    [SerializeField] private float scaleAmount = 0.02f;

    private TextMeshProUGUI textMesh;
    private Material outlineMaterial;
    private Vector3 originalScale;
    private float time = 0f;

    private void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();

        // Material 복사 (공유 Material 수정 방지)
        outlineMaterial = textMesh.fontSharedMaterial;
        if (outlineMaterial != null)
        {
            textMesh.fontSharedMaterial = new Material(outlineMaterial);
            outlineMaterial = textMesh.fontSharedMaterial;
        }

        originalScale = transform.localScale;
    }

    private void Update()
    {
        time += Time.unscaledDeltaTime * pulseSpeed;

        // Sine wave (0 → 1 → 0)
        float wave = (Mathf.Sin(time * Mathf.PI) + 1f) / 2f;

        // Outline Width 조정
        if (outlineMaterial != null)
        {
            float outlineWidth = Mathf.Lerp(minOutlineWidth, maxOutlineWidth, wave);
            outlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        }

        // Glow (Alpha) 조정
        Color currentColor = textMesh.color;
        currentColor.a = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, wave);
        textMesh.color = currentColor;

        // Scale Pulse (선택사항)
        if (enableScalePulse)
        {
            float scale = 1f + (wave * scaleAmount);
            transform.localScale = originalScale * scale;
        }
    }

    private void OnDestroy()
    {
        // Material 정리
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }
}