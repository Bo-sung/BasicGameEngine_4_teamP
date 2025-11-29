// ========== 발사체 데이터 구조체 ==========
[System.Serializable]
public class ProjectileTableData
{
    public string projectileID;
    public string projectileName;
    public string prefabPath;        // "Projectiles/Bullet_Standard"
    public float speed = 100f;
    public float lifetime = 5f;
    public float damage = 25f;
    public bool hasExplosion = false;
    public float explosionRadius = 0f;
    public string hitEffectPath;     // "Effects/BulletHit_Default"
}