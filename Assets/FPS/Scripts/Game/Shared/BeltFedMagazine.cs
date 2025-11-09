using System.Collections;
using UnityEngine;

[AddComponentMenu("FPS/Magazine/Belt Fed Magazine")]
public class BeltFedMagazine : MagazineBase
{
    [Header("벨트 급탄 설정")]
    [Tooltip("벨트 체인 길이 (연결된 탄약 수)")]
    public int beltChainLength = 100;

    [Tooltip("벨트 재장전 시간")]
    public float beltReloadTime = 5f;

    [Tooltip("탄약 벨트 교체 애니메이션 시간")]
    public float beltChangeAnimationTime = 2f;

    [Tooltip("과열 방지를 위한 연속 발사 제한")]
    public int maxContinuousShots = 50;

    [Tooltip("과열 쿨다운 시간")]
    public float overheatCooldownTime = 3f;

    [Tooltip("급탄 실패 확률 (0-1)")]
    [Range(0f, 0.1f)]
    public float jamProbability = 0.02f;

    private int currentBeltLength;
    private int continuousShotCount;
    private bool isOverheated;
    private bool isJammed;
    private float overheatTimer;
    private Coroutine reloadCoroutine;

    public bool IsOverheated => isOverheated;
    public bool IsJammed => isJammed;
    public int CurrentBeltLength => currentBeltLength;
    public float OverheatProgress => (float)continuousShotCount / maxContinuousShots;

    protected override void Awake()
    {
        base.Awake();
        reloadType = ReloadType.BeltFed;
        currentBeltLength = beltChainLength;
        maxAmmo = beltChainLength;
        currentAmmo = maxAmmo;
    }

    public override void Initialize()
    {
        base.Initialize();
        carriedAmmo = int.MaxValue;
    }

    protected override void UpdateMagazine()
    {
        base.UpdateMagazine();
        UpdateOverheat();
        UpdateJamStatus();
    }

    private void UpdateOverheat()
    {
        if (isOverheated)
        {
            overheatTimer += Time.deltaTime;
            if (overheatTimer >= overheatCooldownTime)
            {
                isOverheated = false;
                continuousShotCount = 0;
                overheatTimer = 0f;
            }
        }
        else
        {
            if (continuousShotCount > 0)
            {
                overheatTimer += Time.deltaTime;
                if (overheatTimer >= 1f)
                {
                    continuousShotCount = Mathf.Max(0, continuousShotCount - 1);
                    overheatTimer = 0f;
                }
            }
        }
    }

    private void UpdateJamStatus()
    {
        // 잼 상태는 수동으로 해제해야 함
    }

    public override bool TryConsumeAmmo(int amount = 1)
    {
        if (isOverheated || isJammed || IsReloading)
            return false;

        bool consumed = base.TryConsumeAmmo(amount);

        if (consumed)
        {
            continuousShotCount += amount;

            if (Random.Range(0f, 1f) < jamProbability)
            {
                TriggerJam();
                return false;
            }

            if (continuousShotCount >= maxContinuousShots)
            {
                TriggerOverheat();
            }
        }

        return consumed;
    }

    protected override void StartReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }

        OnReloadStart();
        reloadCoroutine = StartCoroutine(BeltReloadCoroutine());
    }

    private IEnumerator BeltReloadCoroutine()
    {
        yield return new WaitForSeconds(beltChangeAnimationTime);
        yield return new WaitForSeconds(beltReloadTime - beltChangeAnimationTime);

        if (IsReloading)
        {
            PerformBeltReload();
            CompleteReload();
        }
    }

    private void PerformBeltReload()
    {
        int ammoToReload = Mathf.Min(beltChainLength, carriedAmmo);
        carriedAmmo -= ammoToReload;
        currentAmmo = ammoToReload;
        currentBeltLength = ammoToReload;

        isOverheated = false;
        isJammed = false;
        continuousShotCount = 0;
        overheatTimer = 0f;
    }

    private void TriggerOverheat()
    {
        isOverheated = true;
        overheatTimer = 0f;
    }

    private void TriggerJam()
    {
        isJammed = true;
    }

    public void ClearJam()
    {
        if (isJammed)
        {
            isJammed = false;
        }
    }

    public void ForceCooldown()
    {
        isOverheated = false;
        continuousShotCount = 0;
        overheatTimer = 0f;
    }

    public override bool CanReload()
    {
        return base.CanReload() && !isJammed;
    }

    public override void CancelReload()
    {
        if (IsReloading)
        {
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }
            base.CancelReload();
        }
    }

    public override string GetDebugInfo()
    {
        string baseInfo = base.GetDebugInfo();
        return $"{baseInfo}, Belt: {currentBeltLength}, Overheated: {isOverheated}, Jammed: {isJammed}, ContinuousShots: {continuousShotCount}";
    }
}