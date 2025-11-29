// WeaponTableData.cs
using UnityEngine;

namespace FPS.Common
{
    [System.Serializable]
    public class WeaponTableData : Table.IValidatable
    {
        [Header("기본 정보")]
        public string weaponID;
        public string weaponName;

        [Header("발사 타입")]
        public WeaponShootType shootType = WeaponShootType.Manual;

        [Header("발사 설정")]
        public float fireRate = 0.5f;
        public int bulletsPerShot = 1;
        public float spread = 1f;
        public int maxAmmo = 15;
        public float recoil = 0.3f;
        public float damage = 25f;

        [Header("재장전")]
        public float reloadSpeed = 1f;
        public float reloadDelay = 1f;
        public bool autoReload = true;

        [Header("투사체 설정")]  // 새로 추가
        public string projectileID;
        public float projectileSpeed = 20f;        // 추가된 필드
        public float projectileLifetime = 5f;      // 추가된 필드

        [Header("에셋 경로")]
        public string fireSoundPath;
        public string muzzleFlashPath;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(weaponID) &&
                   !string.IsNullOrEmpty(weaponName) &&
                   damage > 0 &&
                   maxAmmo > 0;
        }
    }
}