using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼 클릭 시 중앙에서 퍼지는 물결 효과 (HTML의 button::before 효과 재현)
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonRippleEffect : MonoBehaviour, IPointerDownHandler
{
    [Header("Ripple Settings")]
    [SerializeField] private Color rippleColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private float rippleDuration = 0.6f;
    [SerializeField] private float maxRippleSize = 300f;

    [Header("Button Hover")]
    [SerializeField] private float hoverLift = -2f;
    [SerializeField] private float hoverSpeed = 10f;

    private Image rippleImage;
    private RectTransform rippleTransform;
    private Button button;
    private RectTransform buttonTransform;
    private Vector3 originalPosition;
    private bool isHovering = false;

    private void Start()
    {
        button = GetComponent<Button>();
        buttonTransform = GetComponent<RectTransform>();
        originalPosition = buttonTransform.localPosition;

        CreateRipple();
    }

    private void Update()
    {
        // 호버 효과 (약간 위로)
        Vector3 targetPosition = isHovering ?
            originalPosition + new Vector3(0, hoverLift, 0) : originalPosition;

        buttonTransform.localPosition = Vector3.Lerp(
            buttonTransform.localPosition,
            targetPosition,
            Time.unscaledDeltaTime * hoverSpeed
        );
    }

    /// <summary>
    /// 물결 이미지 생성
    /// </summary>
    private void CreateRipple()
    {
        GameObject rippleObj = new GameObject("Ripple");
        rippleObj.transform.SetParent(transform, false);

        rippleTransform = rippleObj.AddComponent<RectTransform>();
        rippleTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rippleTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rippleTransform.pivot = new Vector2(0.5f, 0.5f);
        rippleTransform.sizeDelta = new Vector2(10, 10);
        rippleTransform.SetAsFirstSibling(); // 맨 뒤에 배치

        rippleImage = rippleObj.AddComponent<Image>();
        rippleImage.color = rippleColor;
        rippleImage.raycastTarget = false;

        // 원형 스프라이트 생성
        Texture2D circleTexture = CreateCircleTexture(100);
        Sprite circleSprite = Sprite.Create(
            circleTexture,
            new Rect(0, 0, circleTexture.width, circleTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        rippleImage.sprite = circleSprite;

        rippleImage.enabled = false;
    }

    /// <summary>
    /// 원형 텍스처 생성
    /// </summary>
    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                if (dist <= radius)
                {
                    // 가장자리를 부드럽게
                    float alpha = 1f - Mathf.Clamp01((dist - radius + 5f) / 5f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 클릭 시 물결 효과 재생
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // 클릭 위치 (버튼 로컬 좌표)
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            buttonTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        StopAllCoroutines();
        StartCoroutine(PlayRipple(localPoint));
    }

    /// <summary>
    /// 물결 애니메이션
    /// </summary>
    private System.Collections.IEnumerator PlayRipple(Vector2 position)
    {
        rippleImage.enabled = true;
        rippleTransform.anchoredPosition = position;
        rippleTransform.localScale = Vector3.zero;

        Color startColor = rippleColor;
        startColor.a = rippleColor.a;
        rippleImage.color = startColor;

        float elapsed = 0f;

        while (elapsed < rippleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / rippleDuration;

            // 크기 증가
            float scale = Mathf.Lerp(0f, maxRippleSize, t);
            rippleTransform.sizeDelta = new Vector2(scale, scale);

            // 페이드 아웃
            Color color = rippleColor;
            color.a = rippleColor.a * (1f - t);
            rippleImage.color = color;

            yield return null;
        }

        rippleImage.enabled = false;
    }

    // EventTrigger 대신 사용
    public void OnPointerEnter()
    {
        isHovering = true;
    }

    public void OnPointerExit()
    {
        isHovering = false;
    }
}