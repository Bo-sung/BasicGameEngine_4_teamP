using UnityEngine;
using System.Collections;

/// <summary>
/// 카드 등장 애니메이션 (HTML의 cardSlideIn 재현)
/// translateY(50px) scale(0.8) → translateY(0) scale(1)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CardSlideInAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float startOffsetY = 500f;
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Delay")]
    [SerializeField] private float delayBeforeStart = 0f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private Vector3 targetScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        // 목표 위치와 스케일 저장
        targetPosition = rectTransform.anchoredPosition;
        targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        // 초기 상태 설정
        rectTransform.anchoredPosition = new Vector2(targetPosition.x, targetPosition.y + startOffsetY);
        transform.localScale = targetScale * startScale;
        canvasGroup.alpha = 0f;

        // 애니메이션 시작
        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        // 지연
        if (delayBeforeStart > 0)
        {
            yield return new WaitForSecondsRealtime(delayBeforeStart);
        }

        float elapsed = 0f;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector3 startScale = transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float curveValue = easeCurve.Evaluate(t);

            // 위치 보간
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveValue);

            // 스케일 보간
            transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

            // 알파 보간
            canvasGroup.alpha = curveValue;

            yield return null;
        }

        // 최종 값 정확히 설정
        rectTransform.anchoredPosition = targetPosition;
        transform.localScale = targetScale;
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 외부에서 애니메이션 재생
    /// </summary>
    public void PlaySlideIn(float delay = 0f)
    {
        delayBeforeStart = delay;
        StopAllCoroutines();
        StartCoroutine(PlayAnimation());
    }
}