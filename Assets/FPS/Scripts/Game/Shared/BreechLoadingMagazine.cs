using System.Collections;
using UnityEngine;

[AddComponentMenu("FPS/Magazine/Breech Loading Magazine")]
public class BreechLoadingMagazine : MagazineBase
{
    [Header("약실 장전 설정")]
    [Tooltip("약실 열기 시간")]
    public float breechOpenTime = 0.8f;

    [Tooltip("탄약 장전 시간")]
    public float loadTime = 1.2f;

    [Tooltip("약실 닫기 시간")]
    public float breechCloseTime = 0.6f;

    [Tooltip("발사 후 자동 배출 여부")]
    public bool autoEjectAfterShot = true;

    [Tooltip("배출 시간")]
    public float ejectTime = 0.5f;

    [Tooltip("다양한 탄약 타입 지원")]
    public bool supportMultipleAmmoTypes = false;

    [Tooltip("현재 장전된 탄약 타입")]
    public int currentAmmoType = 0;

    private bool isBreechOpen;
    private bool hasSpentCasing;
    private bool isEjecting;
    private Coroutine reloadCoroutine;

    public bool IsBreechOpen => isBreechOpen;
    public bool HasSpentCasing => hasSpentCasing;
    public bool IsEjecting => isEjecting;
    public int CurrentAmmoType => currentAmmoType;

    protected override void Awake()
    {
        base.Awake();
        reloadType = ReloadType.BreechLoading;
        maxAmmo = 1;
        currentAmmo = 0;
        isBreechOpen = false;
        hasSpentCasing = false;
    }

    public override bool TryConsumeAmmo(int amount = 1)
    {
        if (isBreechOpen || hasSpentCasing || IsReloading || isEjecting)
            return false;

        bool consumed = base.TryConsumeAmmo(amount);

        if (consumed)
        {
            hasSpentCasing = true;
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
        reloadCoroutine = StartCoroutine(BreechLoadingCoroutine());
    }

    private IEnumerator BreechLoadingCoroutine()
    {
        if (!isBreechOpen)
        {
            yield return StartCoroutine(OpenBreech());
        }

        if (hasSpentCasing)
        {
            yield return StartCoroutine(EjectCasing());
        }

        if (carriedAmmo > 0)
        {
            yield return StartCoroutine(LoadNewAmmo());
        }

        yield return StartCoroutine(CloseBreech());

        if (IsReloading)
        {
            CompleteReload();
        }
    }

    private IEnumerator OpenBreech()
    {
        isBreechOpen = true;
        yield return new WaitForSeconds(breechOpenTime);
    }

    private IEnumerator EjectCasing()
    {
        isEjecting = true;
        hasSpentCasing = false;
        yield return new WaitForSeconds(ejectTime);
        isEjecting = false;
    }

    private IEnumerator LoadNewAmmo()
    {
        yield return new WaitForSeconds(loadTime);

        if (carriedAmmo > 0)
        {
            carriedAmmo--;
            currentAmmo = 1;
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
        }
    }

    private IEnumerator CloseBreech()
    {
        yield return new WaitForSeconds(breechCloseTime);
        isBreechOpen = false;
    }

    public void ToggleBreech()
    {
        if (IsReloading || isEjecting) return;

        if (isBreechOpen)
        {
            StartCoroutine(CloseBreech());
        }
        else
        {
            StartCoroutine(OpenBreech());
        }
    }

    public void ManualEject()
    {
        if (hasSpentCasing && isBreechOpen && !isEjecting)
        {
            StartCoroutine(EjectCasing());
        }
    }

    public void ChangeAmmoType(int newAmmoType)
    {
        if (supportMultipleAmmoTypes && currentAmmo == 0)
        {
            currentAmmoType = newAmmoType;
        }
    }

    public override bool CanReload()
    {
        return !IsReloading &&
               !isEjecting &&
               carriedAmmo > 0 &&
               (currentAmmo == 0 || hasSpentCasing);
    }

    public void ForceEject()
    {
        if (currentAmmo > 0 && !IsReloading)
        {
            currentAmmo = 0;
            hasSpentCasing = false;
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);

            if (!isBreechOpen)
            {
                StartCoroutine(OpenBreech());
            }
        }
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

    public void HandlePostShot()
    {
        if (autoEjectAfterShot && hasSpentCasing)
        {
            StartCoroutine(AutoEjectSequence());
        }
    }

    private IEnumerator AutoEjectSequence()
    {
        yield return StartCoroutine(OpenBreech());
        yield return StartCoroutine(EjectCasing());
    }

    public override string GetDebugInfo()
    {
        string baseInfo = base.GetDebugInfo();
        return $"{baseInfo}, BreechOpen: {isBreechOpen}, SpentCasing: {hasSpentCasing}, AmmoType: {currentAmmoType}";
    }
}