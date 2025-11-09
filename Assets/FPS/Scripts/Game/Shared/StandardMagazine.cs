using System.Collections;
using UnityEngine;

[AddComponentMenu("FPS/Magazine/Standard Magazine")]
public class StandardMagazine : MagazineBase
{
    [Header("탄창 교체 설정")]
    [Tooltip("탄창을 완전히 교체하는 방식")]
    public bool replaceEntireMagazine = true;

    [Tooltip("재장전 중 취소 가능 여부")]
    public bool canCancelReload = true;

    [Tooltip("전술적 재장전 (탄약이 남아있을 때 재장전)")]
    public bool allowTacticalReload = true;

    [Header("애니메이션")]
    [Tooltip("재장전 애니메이터")]
    public Animator reloadAnimator;

    [Tooltip("재장전 애니메이션 트리거")]
    public string reloadTrigger = "Reload";

    [Tooltip("빈 탄창 재장전 애니메이션 트리거")]
    public string emptyReloadTrigger = "EmptyReload";

    [Header("이펙트")]
    [Tooltip("탄창 떨어뜨리기 이펙트")]
    public GameObject magazineDropPrefab;

    [Tooltip("탄창 떨어뜨릴 위치")]
    public Transform magazineDropPoint;

    private Coroutine reloadCoroutine;
    private bool wasEmpty;

    protected override void Awake()
    {
        base.Awake();
        reloadType = ReloadType.Magazine;
    }

    protected override void Start()
    {
        base.Start();

        // 애니메이터가 없으면 무기에서 찾기
        if (reloadAnimator == null && weapon != null)
        {
            reloadAnimator = weapon.GetComponentInChildren<Animator>();
        }
    }

    protected override void StartReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }

        wasEmpty = currentAmmo <= 0;
        OnReloadStart();

        float reloadDuration = wasEmpty ? emptyReloadTime : reloadTime;
        reloadCoroutine = StartCoroutine(ReloadCoroutine(reloadDuration));
    }

    private IEnumerator ReloadCoroutine(float duration)
    {
        // 애니메이션 트리거
        if (reloadAnimator != null)
        {
            string trigger = wasEmpty ? emptyReloadTrigger : reloadTrigger;
            reloadAnimator.SetTrigger(trigger);
        }

        // 탄창 떨어뜨리기 이펙트
        if (magazineDropPrefab != null && magazineDropPoint != null)
        {
            SpawnMagazineDrop();
        }

        yield return new WaitForSeconds(duration);

        if (IsReloading) // 재장전이 취소되지 않았다면
        {
            PerformReload();
            CompleteReload();
        }
    }

    private void PerformReload()
    {
        if (replaceEntireMagazine)
        {
            // 전체 탄창 교체
            int ammoToReload = Mathf.Min(maxAmmo, carriedAmmo);
            carriedAmmo -= ammoToReload;
            currentAmmo = ammoToReload;
        }
        else
        {
            // 부족한 탄약만 보충
            int ammoNeeded = maxAmmo - currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, carriedAmmo);
            carriedAmmo -= ammoToReload;
            currentAmmo += ammoToReload;
        }
    }

    private void SpawnMagazineDrop()
    {
        if (magazineDropPrefab != null && magazineDropPoint != null)
        {
            GameObject droppedMag = Instantiate(magazineDropPrefab, magazineDropPoint.position, magazineDropPoint.rotation);

            // 물리 효과 추가
            Rigidbody rb = droppedMag.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dropForce = Random.insideUnitSphere * 2f + Vector3.down;
                rb.AddForce(dropForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }

            // 일정 시간 후 제거
            Destroy(droppedMag, 10f);
        }
    }

    public override void CancelReload()
    {
        if (IsReloading && canCancelReload)
        {
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }

            base.CancelReload();

            // 애니메이션 중단
            if (reloadAnimator != null)
            {
                reloadAnimator.SetTrigger("CancelReload");
            }
        }
    }

    public override bool CanReload()
    {
        bool baseCan = base.CanReload();

        if (!allowTacticalReload && currentAmmo > 0)
        {
            return false;
        }

        return baseCan;
    }

    // 강제 재장전 (치트나 특수 상황용)
    public void ForceReload()
    {
        if (carriedAmmo > 0)
        {
            CancelReload();
            StartReload();
        }
    }

    // 탄창 용량 업그레이드
    public void UpgradeMagazineCapacity(int newCapacity)
    {
        maxAmmo = newCapacity;
        OnValidate(); // 값 검증
    }
}