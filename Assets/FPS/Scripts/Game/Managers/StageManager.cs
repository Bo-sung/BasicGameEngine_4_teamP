using System;
using System.Collections.Generic;
using System.Linq;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [SerializeField]
    HashSet<Weapon> playerOwnedWeapons = new HashSet<Weapon>();
    [SerializeField]
    WeaponArmory armory;
    [SerializeField]
    int max_reward_count = 3;
    [SerializeField]
    string IntroSceneName = "IntroMenu";
    [SerializeField]
    string loseSceneName = "LoseScene";
    [SerializeField]
    string rewardSceneName = "RewardScene";
    [SerializeField]
    string[] stageScenesName;
    [SerializeField]
    int currentStage;
    [SerializeField]
    UISelectWeapon rewardUI;

    private void Awake()
    {
        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += OnSceneLoaded;
        rewardUI.SetData(new SelectWeaponPresenter(armory, this, max_reward_count));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
    {
        if (scene.name == IntroSceneName ||
            scene.name == loseSceneName)
        {
            currentStage = 0;
            return;
        }
        else if (scene.name == rewardSceneName)
        {
            rewardUI.gameObject.SetActive(true);
        }
        else
        {
            Invoke(nameof(SetupPlayerWeapons), 0f);
        }
    }

    public List<Weapon> PlayerWeapons => playerOwnedWeapons.ToList();

    public void SetupPlayerWeapons()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("Player를 찾을 수 없습니다.");
            return;
        }

        PlayerCharacterController playerCharacterController = player.GetComponent<PlayerCharacterController>();
        if (playerCharacterController == null)
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
        foreach (var weapon in playerOwnedWeapons)
        {
            // 무기 추가. 어짜피 중복된 무기는 추가 안됨
            playerWeaponsManager.AddWeapon(weapon);
        }
    }

    public void AddWeapon(Weapon weapon)
    {
        playerOwnedWeapons.Add(weapon);
    }

    public void LoadRewardScene()
    {
        SceneManager.LoadScene(rewardSceneName);
    }

    public void LoadLoseScene()
    {
        SceneManager.LoadScene(loseSceneName);
    }

    public void LoadIntroScene()
    {
        SceneManager.LoadScene(IntroSceneName);
    }

    public void NextStage()
    {
        if (currentStage >= stageScenesName.Length || currentStage < 0)
            return;
        SceneManager.LoadScene(stageScenesName[currentStage++]);
    }
}
