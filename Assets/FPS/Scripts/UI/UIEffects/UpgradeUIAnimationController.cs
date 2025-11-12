using UnityEngine;
using System.Collections;

/// <summary>
/// 업그레이드 UI 전체 애니메이션 컨트롤러
/// HTML UI의 모든 애니메이션을 통합 관리
/// </summary>
public class UpgradeUIAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject upgradeContainer;
    [SerializeField] private CanvasGroup containerCanvasGroup;
    [SerializeField] private GameObject backgroundOverlay;

    [Header("Card References")]
    [SerializeField] private CardSlideInAnimation[] cards;

    [Header("Animation Timing")]
    [SerializeField] private float backgroundFadeInDuration = 0.3f;
    [SerializeField] private float cardSlideDelay = 0.1f; // 카드 간 지연
    [SerializeField] private float selectionFadeOutDuration = 0.5f;

    [Header("Scale Animation")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float initialScale = 0.9f;

    private void Awake()
    {
        // Canvas Group 자동 찾기
        if (containerCanvasGroup == null)
        {
            containerCanvasGroup = upgradeContainer.GetComponent<CanvasGroup>();
            if (containerCanvasGroup == null)
            {
                containerCanvasGroup = upgradeContainer.AddComponent<CanvasGroup>();
            }
        }

        // 카드 자동 찾기
        if (cards == null || cards.Length == 0)
        {
            cards = upgradeContainer.GetComponentsInChildren<CardSlideInAnimation>(true);
        }
    }

    /// <summary>
    /// UI 표시 애니메이션
    /// </summary>
    public void ShowUI()
    {
        upgradeContainer.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ShowUICoroutine());
    }

    private IEnumerator ShowUICoroutine()
    {
        // 초기 상태
        containerCanvasGroup.alpha = 0f;
        if (useScaleAnimation)
        {
            upgradeContainer.transform.localScale = Vector3.one * initialScale;
        }

        // 배경 페이드 인
        if (backgroundOverlay != null)
        {
            CanvasGroup bgGroup = backgroundOverlay.GetComponent<CanvasGroup>();
            if (bgGroup == null)
            {
                bgGroup = backgroundOverlay.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            while (elapsed < backgroundFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / backgroundFadeInDuration;
                bgGroup.alpha = t;
                yield return null;
            }
            bgGroup.alpha = 1f;
        }

        // 컨테이너 페이드 인 & 스케일
        float elapsed2 = 0f;
        float duration = 0.5f;

        while (elapsed2 < duration)
        {
            elapsed2 += Time.unscaledDeltaTime;
            float t = elapsed2 / duration;
            float easeT = EaseOutCubic(t);

            containerCanvasGroup.alpha = easeT;

            if (useScaleAnimation)
            {
                float scale = Mathf.Lerp(initialScale, 1f, easeT);
                upgradeContainer.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        containerCanvasGroup.alpha = 1f;
        upgradeContainer.transform.localScale = Vector3.one;

        // 카드 순차 등장
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
            {
                cards[i].PlaySlideIn(i * cardSlideDelay);
            }
        }
    }

    /// <summary>
    /// 카드 선택 시 애니메이션
    /// </summary>
    public void OnCardSelected(int cardIndex)
    {
        StopAllCoroutines();
        StartCoroutine(CardSelectedCoroutine(cardIndex));
    }

    private IEnumerator CardSelectedCoroutine(int selectedIndex)
    {
        // 선택된 카드 확대
        Transform selectedCard = cards[selectedIndex].transform;
        Vector3 targetScale = Vector3.one * 1.2f;

        // 다른 카드들 페이드 아웃
        for (int i = 0; i < cards.Length; i++)
        {
            if (i != selectedIndex)
            {
                CanvasGroup cardGroup = cards[i].GetComponent<CanvasGroup>();
                if (cardGroup != null)
                {
                    StartCoroutine(FadeOut(cardGroup, selectionFadeOutDuration));
                }
            }
        }

        // 선택된 카드 애니메이션
        float elapsed = 0f;
        Vector3 startScale = selectedCard.localScale;
        CanvasGroup selectedGroup = selectedCard.GetComponent<CanvasGroup>();

        while (elapsed < selectionFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / selectionFadeOutDuration;

            selectedCard.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (selectedGroup != null)
            {
                selectedGroup.alpha = 1f - t;
            }

            yield return null;
        }

        // UI 완전히 숨김
        HideUI();
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    public void HideUI()
    {
        StopAllCoroutines();
        StartCoroutine(HideUICoroutine());
    }

    private IEnumerator HideUICoroutine()
    {
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            containerCanvasGroup.alpha = 1f - t;

            yield return null;
        }

        containerCanvasGroup.alpha = 0f;
        upgradeContainer.SetActive(false);
    }

    /// <summary>
    /// 페이드 아웃 코루틴
    /// </summary>
    private IEnumerator FadeOut(CanvasGroup group, float duration)
    {
        float elapsed = 0f;
        float startAlpha = group.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            group.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        group.alpha = 0f;
    }

    // Easing 함수들
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>
    /// 테스트용 - Space 키로 UI 토글
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (upgradeContainer.activeSelf)
            {
                HideUI();
            }
            else
            {
                ShowUI();
            }
        }

        // 1, 2, 3 키로 카드 선택 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OnCardSelected(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && cards.Length > 1)
        {
            OnCardSelected(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && cards.Length > 2)
        {
            OnCardSelected(2);
        }
    }
}