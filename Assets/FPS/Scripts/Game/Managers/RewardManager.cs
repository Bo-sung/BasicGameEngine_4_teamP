using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    [SerializeField]
    WeaponArmory weaponArmory;  // 무기 프리펩 저장소

    // 이미 얻은거 저장
    List<int> selectedIndex = new List<int>();

    public Weapon[] RequestWeapon(int count)
    {
        Weapon[] result = new Weapon[count];
        int index = 0;
        while(index < count)
        {
            int rewardIndex = Random.Range(0, weaponArmory.Prefabs.Count);
            // 이미 얻은거 제외
            if (selectedIndex.Contains(rewardIndex))
                continue;
            result[index] = weaponArmory.Prefabs[rewardIndex];
            index++;
        }

        return result;
    }
}
