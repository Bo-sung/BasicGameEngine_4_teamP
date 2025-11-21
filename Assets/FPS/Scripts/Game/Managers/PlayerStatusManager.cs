using System;
using System.Collections.Generic;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerStatusManager : MonoBehaviour
{
    [SerializeField]
    private HashSet<Weapon> OwnedWeapons = new HashSet<Weapon>();

    private void Awake()
    {
        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        // RewardScene이면 Player없으니까 패스
        if (arg0.name == "RewardScene")
            return;

        // 씬 시작시 보유한 모든 무기 자동으로 세팅
        SetupPlayerWeapons();
    }

    private void SetupPlayerWeapons()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("Player를 찾을 수 없습니다.");
            return;
        }

        PlayerCharacterController playerCharacterController = player.GetComponent<PlayerCharacterController>();
        if(playerCharacterController == null)
        {
            Debug.LogWarning("PlayerCharacterController를 찾을 수 없습니다.");
            return;
        }

        PlayerWeaponsManager playerWeaponsManager = player.GetComponent<PlayerWeaponsManager>();
        if (playerWeaponsManager == null)
        {
            Debug.LogWarning("PlayerWeaponsManager 찾을 수 없습니다.");
            return;
        }

        UpdateWeapon(playerWeaponsManager);
    }

    private void UpdateWeapon(PlayerWeaponsManager playerWeaponsManager)
    {
        foreach (var weapon in OwnedWeapons)
        {
            // 무기 추가. 어짜피 중복된 무기는 추가 안됨
            playerWeaponsManager.AddWeapon(weapon);
        }
    }

    public void AddWeapon(Weapon weapon)
    {
        OwnedWeapons.Add(weapon);
    }
}
