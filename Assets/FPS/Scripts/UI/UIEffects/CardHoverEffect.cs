using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// HTML UI의 hover 효과를 재현하는 스크립트
/// transform: translateY(-10px) scale(1.05)
/// </summary>
public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverOffsetY = -10f;
    [SerializeField] private float animationSpeed = 5f;

    [Header("Shadow Effect")]
    [SerializeField] private UnityEngine.UI.Shadow[] shadows;
    [SerializeField] private Vector2 normalShadowDistance = new Vector2(0, 10);
    [SerializeField] private Vector2 hoverShadowDistance = new Vector2(0, 20);

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isHovering = false;

    private void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        // Shadow 컴포넌트 자동 찾기
        if (shadows == null || shadows.Length == 0)
        {
            shadows = GetComponentsInChildren<UnityEngine.UI.Shadow>();
        }
    }

    private void Update()
    {
        // 목표 값 계산
        Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;
        Vector3 targetPosition = isHovering ?
            originalPosition + new Vector3(0, hoverOffsetY, 0) : originalPosition;

        // 부드러운 보간
        float deltaSpeed = animationSpeed * Time.unscaledDeltaTime;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, deltaSpeed);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, deltaSpeed);

        // Shadow 거리 조정
        Vector2 targetShadowDistance = isHovering ? hoverShadowDistance : normalShadowDistance;
        foreach (var shadow in shadows)
        {
            if (shadow != null)
            {
                shadow.effectDistance = Vector2.Lerp(shadow.effectDistance, targetShadowDistance, deltaSpeed);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}