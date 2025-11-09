/// <summary>
/// 발사체 데이터 테이블 매니저
/// </summary>
public static class ProjectileDataTable
{
    private static System.Collections.Generic.Dictionary<string, ProjectileTableData> projectileDataDict;

    static ProjectileDataTable()
    {
        LoadProjectileDataTable();
    }

    private static void LoadProjectileDataTable()
    {
        projectileDataDict = new System.Collections.Generic.Dictionary<string, ProjectileTableData>();

        // 기본 총알
        projectileDataDict["bullet_standard"] = new ProjectileTableData
        {
            projectileID = "bullet_standard",
            projectileName = "표준 총알",
            prefabPath = "Projectiles/Bullet_Standard",
            speed = 100f,
            lifetime = 5f,
            damage = 25f,
            hitEffectPath = "Effects/BulletHit_Standard"
        };

        // 라이플 총알
        projectileDataDict["bullet_rifle"] = new ProjectileTableData
        {
            projectileID = "bullet_rifle",
            projectileName = "라이플 총알",
            prefabPath = "Projectiles/Bullet_Rifle",
            speed = 150f,
            lifetime = 8f,
            damage = 30f,
            hitEffectPath = "Effects/BulletHit_Rifle"
        };

        // 샷건 펠릿
        projectileDataDict["bullet_shotgun"] = new ProjectileTableData
        {
            projectileID = "bullet_shotgun",
            projectileName = "샷건 펠릿",
            prefabPath = "Projectiles/Bullet_Shotgun",
            speed = 80f,
            lifetime = 3f,
            damage = 15f,
            hitEffectPath = "Effects/BulletHit_Shotgun"
        };

        // 로켓
        projectileDataDict["rocket_explosive"] = new ProjectileTableData
        {
            projectileID = "rocket_explosive",
            projectileName = "폭발 로켓",
            prefabPath = "Projectiles/Rocket_Explosive",
            speed = 50f,
            lifetime = 10f,
            damage = 100f,
            hasExplosion = true,
            explosionRadius = 5f,
            hitEffectPath = "Effects/Explosion_Large"
        };
    }

    public static ProjectileTableData GetProjectileData(string projectileID)
    {
        projectileDataDict.TryGetValue(projectileID, out ProjectileTableData data);
        return data;
    }
}