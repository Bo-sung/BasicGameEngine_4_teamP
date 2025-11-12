using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이콘 위로 빛이 지나가는 효과 (HTML의 iconShine 재현)
/// </summary>
public class IconShineEffect : MonoBehaviour
{
    [Header("Shine Settings")]
    [SerializeField] private float shineInterval = 3f;
    [SerializeField] private float shineDuration = 1f;
    [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Shine Prefab (선택)")]
    [SerializeField] private GameObject shinePrefab;

    private float timer = 0f;
    private Image shineImage;
    private RectTransform shineTransform;

    private void Start()
    {
        CreateShineImage();
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= shineInterval)
        {
            timer = 0f;
            PlayShineEffect();
        }
    }

    /// <summary>
    /// Shine 이미지 생성
    /// </summary>
    private void CreateShineImage()
    {
        if (shinePrefab != null)
        {
            // Prefab 사용
            return;
        }

        // 프로그래밍 방식으로 생성
        GameObject shineObj = new GameObject("Shine");
        shineObj.transform.SetParent(transform, false);

        shineTransform = shineObj.AddComponent<RectTransform>();
        shineTransform.anchorMin = new Vector2(0, 0);
        shineTransform.anchorMax = new Vector2(1, 1);
        shineTransform.sizeDelta = Vector2.zero;
        shineTransform.localScale = new Vector3(2, 1, 1); // 가로로 길게

        shineImage = shineObj.AddComponent<Image>();
        shineImage.color = shineColor;
        shineImage.raycastTarget = false;

        // 그라데이션 효과를 위한 Material (선택)
        // shineImage.material = CreateShineGradientMaterial();

        shineImage.enabled = false;
    }

    /// <summary>
    /// Shine 효과 재생
    /// </summary>
    private void PlayShineEffect()
    {
        if (shinePrefab != null)
        {
            PlayShineWithPrefab();
        }
        else
        {
            PlayShineWithImage();
        }
    }

    /// <summary>
    /// Image를 사용한 Shine 효과
    /// </summary>
    private void PlayShineWithImage()
    {
        if (shineImage == null || shineTransform == null)
            return;

        StopAllCoroutines();
        StartCoroutine(ShineCoroutine());
    }

    private System.Collections.IEnumerator ShineCoroutine()
    {
        shineImage.enabled = true;

        float elapsed = 0f;
        RectTransform rectTransform = GetComponent<RectTransform>();
        float width = rectTransform.rect.width;

        while (elapsed < shineDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / shineDuration;

            // 좌측에서 우측으로 이동
            float xPos = Mathf.Lerp(-width, width, t);
            shineTransform.anchoredPosition = new Vector2(xPos, 0);

            // 페이드 아웃
            Color color = shineColor;
            color.a = shineColor.a * (1f - t);
            shineImage.color = color;

            yield return null;
        }

        shineImage.enabled = false;
    }

    /// <summary>
    /// Prefab을 사용한 Shine 효과
    /// </summary>
    private void PlayShineWithPrefab()
    {
        GameObject shine = Instantiate(shinePrefab, transform);
        RectTransform shineRect = shine.GetComponent<RectTransform>();

        if (shineRect != null)
        {
            StartCoroutine(ShinePrefabCoroutine(shine, shineRect));
        }
        else
        {
            Destroy(shine, shineDuration);
        }
    }

    private System.Collections.IEnumerator ShinePrefabCoroutine(GameObject shine, RectTransform shineRect)
    {
        float elapsed = 0f;
        RectTransform rectTransform = GetComponent<RectTransform>();
        float width = rectTransform.rect.width;

        CanvasGroup canvasGroup = shine.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = shine.AddComponent<CanvasGroup>();
        }

        while (elapsed < shineDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / shineDuration;

            // 이동
            float xPos = Mathf.Lerp(-width, width, t);
            shineRect.anchoredPosition = new Vector2(xPos, 0);

            // 페이드
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        Destroy(shine);
    }

    /// <summary>
    /// Shine 효과를 즉시 재생 (외부 호출용)
    /// </summary>
    public void TriggerShine()
    {
        timer = shineInterval; // 다음 프레임에 재생
    }
}