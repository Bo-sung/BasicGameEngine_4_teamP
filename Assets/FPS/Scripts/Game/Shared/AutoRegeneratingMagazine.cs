using UnityEngine;

namespace Unity.FPS.Game
{
    [AddComponentMenu("FPS/Magazine/Auto Regenerating Magazine")]
    public class AutoRegeneratingMagazine : MagazineBase
    {
        [Header("자동 재생성 설정")]
        [Tooltip("재생성 시작 딜레이 (마지막 발사 후)")]
        public float regenerationDelay = 2f;

        [Tooltip("초당 재생성되는 탄약 수")]
        public float regenerationRate = 5f;

        [Tooltip("연속 재생성 (true) vs 스텝 재생성 (false)")]
        public bool continuousRegeneration = true;

        [Tooltip("재생성 중 발사 가능 여부")]
        public bool canShootWhileRegenerating = false;

        [Header("오버히트 시스템")]
        [Tooltip("오버히트 시스템 사용")]
        public bool useOverheat = true;

        [Tooltip("오버히트 임계값 (연속 발사 횟수)")]
        public int overheatThreshold = 20;

        [Tooltip("오버히트 쿨다운 시간")]
        public float overheatCooldownTime = 5f;

        [Tooltip("오버히트 경고 임계값 (비율)")]
        [Range(0.5f, 1f)]
        public float overheatWarningThreshold = 0.8f;

        [Header("이펙트")]
        [Tooltip("재생성 이펙트")]
        public GameObject regenerationEffect;

        [Tooltip("오버히트 이펙트")]
        public GameObject overheatEffect;

        [Tooltip("오버히트 경고 이펙트")]
        public GameObject overheatWarningEffect;

        [Header("추가 오디오")]
        [Tooltip("재생성 사운드")]
        public AudioClip regenerationSound;

        [Tooltip("오버히트 사운드")]
        public AudioClip overheatSound;

        [Tooltip("오버히트 경고 사운드")]
        public AudioClip overheatWarningSound;

        private float lastShotTime;
        private float regenerationTimer;
        private int continuousShotCount;
        private bool isOverheated;
        private bool overheatWarningActive;
        private float overheatTimer;
        private GameObject activeRegenerationEffect;
        private GameObject activeOverheatEffect;
        private GameObject activeWarningEffect;

        public bool IsRegenerating { get; private set; }
        public bool IsOverheated => isOverheated;
        public float OverheatProgress => useOverheat ? (float)continuousShotCount / overheatThreshold : 0f;
        public bool IsOverheatWarning => overheatWarningActive;

        protected override void Awake()
        {
            base.Awake();
            reloadType = ReloadType.Automatic;
            maxCarriedAmmo = int.MaxValue;
            startingCarriedAmmo = int.MaxValue;
        }

        public override void Initialize()
        {
            base.Initialize();
            carriedAmmo = int.MaxValue;
        }

        protected override void UpdateMagazine()
        {
            base.UpdateMagazine();
            UpdateRegeneration();
            UpdateOverheat();
            UpdateEffects();
        }

        private void UpdateRegeneration()
        {
            if (isOverheated) return;

            float timeSinceLastShot = Time.time - lastShotTime;
            bool shouldRegenerate = timeSinceLastShot >= regenerationDelay && currentAmmo < maxAmmo;

            if (shouldRegenerate)
            {
                if (!IsRegenerating)
                {
                    IsRegenerating = true;
                    regenerationTimer = 0f;

                    if (regenerationSound && audioSource)
                    {
                        audioSource.clip = regenerationSound;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                }

                if (continuousRegeneration)
                {
                    float ammoToAdd = regenerationRate * Time.deltaTime;
                    float newAmmo = currentAmmo + ammoToAdd;

                    if (newAmmo >= currentAmmo + 1f)
                    {
                        int ammoAdded = Mathf.FloorToInt(newAmmo) - currentAmmo;
                        currentAmmo = Mathf.Min(currentAmmo + ammoAdded, maxAmmo);
                        OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
                    }
                }
                else
                {
                    regenerationTimer += Time.deltaTime;
                    if (regenerationTimer >= 1f / regenerationRate)
                    {
                        currentAmmo = Mathf.Min(currentAmmo + 1, maxAmmo);
                        OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
                        regenerationTimer = 0f;
                    }
                }

                if (currentAmmo >= maxAmmo)
                {
                    StopRegeneration();
                }
            }
            else
            {
                if (IsRegenerating)
                {
                    StopRegeneration();
                }
            }
        }

        private void StopRegeneration()
        {
            IsRegenerating = false;

            if (audioSource && audioSource.clip == regenerationSound)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
        }

        private void UpdateOverheat()
        {
            if (!useOverheat) return;

            if (isOverheated)
            {
                overheatTimer += Time.deltaTime;
                if (overheatTimer >= overheatCooldownTime)
                {
                    isOverheated = false;
                    continuousShotCount = 0;
                    overheatTimer = 0f;
                    overheatWarningActive = false;
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

                        float overheatRatio = (float)continuousShotCount / overheatThreshold;
                        if (overheatRatio < overheatWarningThreshold)
                        {
                            overheatWarningActive = false;
                        }
                    }
                }
            }
        }

        private void UpdateEffects()
        {
            if (IsRegenerating && regenerationEffect != null)
            {
                if (activeRegenerationEffect == null)
                {
                    activeRegenerationEffect = Instantiate(regenerationEffect, transform);
                }
            }
            else if (activeRegenerationEffect != null)
            {
                Destroy(activeRegenerationEffect);
                activeRegenerationEffect = null;
            }

            if (isOverheated && overheatEffect != null)
            {
                if (activeOverheatEffect == null)
                {
                    activeOverheatEffect = Instantiate(overheatEffect, transform);
                }
            }
            else if (activeOverheatEffect != null)
            {
                Destroy(activeOverheatEffect);
                activeOverheatEffect = null;
            }

            if (overheatWarningActive && overheatWarningEffect != null)
            {
                if (activeWarningEffect == null)
                {
                    activeWarningEffect = Instantiate(overheatWarningEffect, transform);
                }
            }
            else if (activeWarningEffect != null)
            {
                Destroy(activeWarningEffect);
                activeWarningEffect = null;
            }
        }

        public override bool TryConsumeAmmo(int amount = 1)
        {
            if (isOverheated) return false;
            if (IsRegenerating && !canShootWhileRegenerating) return false;

            bool consumed = base.TryConsumeAmmo(amount);

            if (consumed)
            {
                lastShotTime = Time.time;
                StopRegeneration();

                if (useOverheat)
                {
                    continuousShotCount += amount;

                    float overheatRatio = (float)continuousShotCount / overheatThreshold;
                    if (overheatRatio >= overheatWarningThreshold && !overheatWarningActive)
                    {
                        overheatWarningActive = true;
                        if (overheatWarningSound && audioSource)
                        {
                            audioSource.PlayOneShot(overheatWarningSound);
                        }
                    }

                    if (continuousShotCount >= overheatThreshold)
                    {
                        TriggerOverheat();
                    }
                }
            }

            return consumed;
        }

        private void TriggerOverheat()
        {
            isOverheated = true;
            overheatTimer = 0f;
            StopRegeneration();

            if (overheatSound && audioSource)
            {
                audioSource.PlayOneShot(overheatSound);
            }
        }

        public override bool TryStartReload()
        {
            return false; // 자동 재생성 시스템에서는 수동 재장전 불가
        }

        public override bool CanReload()
        {
            return false; // 자동 재생성이므로 수동 재장전 불가
        }

        protected override void StartReload()
        {
            // 자동 재생성 시스템에서는 수동 재장전 없음
        }

        public void ResetCooldown()
        {
            continuousShotCount = 0;
            isOverheated = false;
            overheatWarningActive = false;
            overheatTimer = 0f;
        }

        public void InstantRegeneration()
        {
            currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
            ResetCooldown();
        }

        public override string GetDebugInfo()
        {
            string baseInfo = base.GetDebugInfo();
            return $"{baseInfo}, Regenerating: {IsRegenerating}, Overheated: {isOverheated}, Shots: {continuousShotCount}/{overheatThreshold}";
        }

        private void OnDestroy()
        {
            if (activeRegenerationEffect != null) Destroy(activeRegenerationEffect);
            if (activeOverheatEffect != null) Destroy(activeOverheatEffect);
            if (activeWarningEffect != null) Destroy(activeWarningEffect);
        }
    }
}