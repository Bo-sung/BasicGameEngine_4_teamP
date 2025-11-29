using System.Collections;
using UnityEngine;
using static FPS.Common.Table;

[AddComponentMenu("FPS/Magazine/Individual Load Magazine")]
public class IndividualLoadMagazine : MagazineBase
{
    [Header("개별 장전 설정")]
    [Tooltip("탄약 1발 장전 시간")]
    public float singleAmmoLoadTime = 0.5f;

    [Tooltip("첫 번째 탄약 장전 시간 (다를 수 있음)")]
    public float firstAmmoLoadTime = 0.7f;

    [Tooltip("재장전 시작 딜레이")]
    public float reloadStartDelay = 0.2f;

    [Tooltip("재장전 완료 딜레이")]
    public float reloadEndDelay = 0.3f;

    [Tooltip("재장전 중 발사 가능 여부")]
    public bool canShootWhileReloading = true;

    [Tooltip("한 번에 장전할 최대 탄약 수 (0 = 무제한)")]
    public int maxAmmoPerReload = 0;

    [Header("애니메이션")]
    [Tooltip("개별 장전 애니메이터")]
    public Animator reloadAnimator;

    [Tooltip("장전 시작 트리거")]
    public string startLoadTrigger = "StartLoad";

    [Tooltip("탄약 장전 트리거")]
    public string loadAmmotrigger = "LoadAmmo";

    [Tooltip("장전 완료 트리거")]
    public string endLoadTrigger = "EndLoad";

    [Header("추가 오디오")]
    [Tooltip("개별 탄약 장전 사운드")]
    public AudioClip singleLoadSound;

    [Tooltip("장전 시작 사운드")]
    public AudioClip startLoadSound;

    [Tooltip("장전 완료 사운드")]
    public AudioClip endLoadSound;

    private Coroutine reloadCoroutine;
    private int ammoBeingLoaded = 0;

    protected override void Awake()
    {
        base.Awake();
        reloadType = ReloadType.Individual;
    }

    protected override void Start()
    {
        base.Start();

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

        OnReloadStart();
        reloadCoroutine = StartCoroutine(IndividualReloadCoroutine());
    }

    private IEnumerator IndividualReloadCoroutine()
    {
        if (reloadAnimator != null)
        {
            reloadAnimator.SetTrigger(startLoadTrigger);
        }

        if (startLoadSound && audioSource)
        {
            audioSource.PlayOneShot(startLoadSound);
        }

        yield return new WaitForSeconds(reloadStartDelay);

        int ammoLoaded = 0;
        int maxToLoad = maxAmmoPerReload > 0 ? maxAmmoPerReload : (maxAmmo - currentAmmo);

        while (currentAmmo < maxAmmo &&
               carriedAmmo > 0 &&
               IsReloading &&
               ammoLoaded < maxToLoad)
        {
            float loadTime = (currentAmmo == 0 && ammoLoaded == 0) ?
                firstAmmoLoadTime : singleAmmoLoadTime;

            if (reloadAnimator != null)
            {
                reloadAnimator.SetTrigger(loadAmmotrigger);
            }

            yield return new WaitForSeconds(loadTime);

            if (IsReloading && carriedAmmo > 0)
            {
                LoadSingleAmmo();
                ammoLoaded++;
            }
        }

        if (IsReloading)
        {
            if (reloadAnimator != null)
            {
                reloadAnimator.SetTrigger(endLoadTrigger);
            }

            if (endLoadSound && audioSource)
            {
                audioSource.PlayOneShot(endLoadSound);
            }

            yield return new WaitForSeconds(reloadEndDelay);
            CompleteReload();
        }
    }

    private void LoadSingleAmmo()
    {
        carriedAmmo--;
        currentAmmo++;
        OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);

        if (singleLoadSound && audioSource)
        {
            audioSource.PlayOneShot(singleLoadSound);
        }
    }

    public override bool TryConsumeAmmo(int amount = 1)
    {
        if (canShootWhileReloading || !IsReloading)
        {
            if (currentAmmo >= amount)
            {
                currentAmmo -= amount;
                OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);

                if (ammoConsumeSound && audioSource)
                {
                    audioSource.PlayOneShot(ammoConsumeSound);
                }

                if (currentAmmo <= 0)
                {
                    OnAmmoEmpty?.Invoke();
                }

                return true;
            }
        }

        return false;
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

            if (reloadAnimator != null)
            {
                reloadAnimator.SetTrigger("CancelReload");
            }
        }
    }

    public void InterruptReload()
    {
        if (IsReloading && canShootWhileReloading)
        {
            CancelReload();
        }
    }

    public void LoadSingleAmmoManual()
    {
        if (!IsReloading && currentAmmo < maxAmmo && carriedAmmo > 0)
        {
            StartCoroutine(LoadSingleAmmoCoroutine());
        }
    }

    private IEnumerator LoadSingleAmmoCoroutine()
    {
        IsReloading = true;

        if (reloadAnimator != null)
        {
            reloadAnimator.SetTrigger(loadAmmotrigger);
        }

        yield return new WaitForSeconds(singleAmmoLoadTime);

        LoadSingleAmmo();
        IsReloading = false;
    }

    public void LoadSpecificAmount(int amount)
    {
        if (!IsReloading)
        {
            maxAmmoPerReload = amount;
            StartReload();
        }
    }
}