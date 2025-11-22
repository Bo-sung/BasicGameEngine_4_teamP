using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectWeaponPresenter : UISelectWeapon.IPresenter
{
    UISelectWeapon.Data data;

    private System.Action onStageStarted;
    private WeaponArmory weaponArmory;
    private StageManager stageManager;

    private int rewardCount;
    private HashSet<int> rewardedList = new HashSet<int>();

    public SelectWeaponPresenter(WeaponArmory weaponArmory, StageManager playerStatusManager, int rewardCount)
    {
        this.weaponArmory = weaponArmory;
        this.rewardCount = rewardCount;
        this.stageManager = playerStatusManager;
    }

    public UISelectWeapon.Data GetData()
    {
        UISelectWeapon.Data result = new UISelectWeapon.Data();
        result.selectDataList = new List<UISelectCard.Data>();

        List<Weapon> weapons = weaponArmory.Prefabs;

        List<int> rewardIndexList = new List<int>();
        int index = 0;
        while (index < rewardCount)
        {
            int rewardIndex = UnityEngine.Random.Range(0, weaponArmory.Prefabs.Count);
            // 이미 얻은거 제외
            if (rewardedList.Contains(rewardIndex))
                continue;
            rewardIndexList.Add(rewardIndex);
            index++;
        }

        foreach (var rewardIndex in rewardIndexList)
        {
            var reward = weapons[rewardIndex]; 
            UISelectCard.Data cardData = new UISelectCard.Data();
            cardData.ID = rewardIndex;
            cardData.weaponName = reward.WeaponName;
            cardData.iconImage = reward.WeaponIcon;
            cardData.textList = new List<UI_InfScrollItem_Text.Data>();
            foreach(var desc in reward.weaponDescriptions)
            {
                cardData.textList.Add(new UI_InfScrollItem_Text.Data() { text = desc });
            }
            result.selectDataList.Add(cardData);
        }

        return result;
    }

    public void OnCardSelected(int cardID)
    {
        if (cardID > weaponArmory.Prefabs.Count || cardID < 0)
        {
            return;
        }
        rewardedList.Add(cardID);
        stageManager.AddWeapon(weaponArmory.Prefabs[cardID]);
    }

    public void LoadNextStage()
    {
        stageManager.NextStage();
    }
}


public class UISelectWeapon : MonoBehaviour
{
    public class Data
    {
        public string title;
        public List<UISelectCard.Data> selectDataList;
    }

    public interface IPresenter
    {
        void OnCardSelected(int cardID);
        Data GetData();
        void LoadNextStage();
    }

    [SerializeField]
    TextMeshProUGUI m_titleText;

    [SerializeField]
    UISelectCard m_prefab;

    [SerializeField]
    GameObject m_container;

    [SerializeField]
    int rewardCount = 3;

    [SerializeField]
    List<UISelectCard> selectCardList = new List<UISelectCard>();

    // 오브젝트 풀 (비활성화된 카드들)
    private List<UISelectCard> cardPool = new List<UISelectCard>();

    IPresenter presenter;

    public void SetData(SelectWeaponPresenter presenter)
    {
        this.presenter = presenter;
        Refresh();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if(presenter == null)
        {
            return;
        }
        var data = presenter.GetData();
        ClearAllCards();
        m_titleText.text = data.title;
        List<UISelectCard.Data> cardData = data.selectDataList;

        // 데이터와 실제 카드 수 불일치 시 리프레시
        if (cardData.Count != selectCardList.Count)
        {
            RefreshCards(cardData.Count);
        }

        // 각 카드에 데이터 할당
        for (int i = 0; i < cardData.Count; i++)
        {
            selectCardList[i].SetData(cardData[i]);
        }
    }

    /// <summary>
    /// 카드 개수 조정 (오브젝트 풀링)
    /// </summary>
    void RefreshCards(int targetCount)
    {
        int currentCount = selectCardList.Count;

        // 카드가 부족한 경우
        if (currentCount < targetCount)
        {
            int needCount = targetCount - currentCount;

            for (int i = 0; i < needCount; i++)
            {
                UISelectCard card = GetCardFromPool();
                card.gameObject.SetActive(true);
                selectCardList.Add(card);
            }
        }
        // 카드가 초과된 경우
        else if (currentCount > targetCount)
        {
            int excessCount = currentCount - targetCount;

            // 뒤에서부터 제거 (역순)
            for (int i = 0; i < excessCount; i++)
            {
                int lastIndex = selectCardList.Count - 1;
                UISelectCard card = selectCardList[lastIndex];

                // 풀로 반환
                ReturnCardToPool(card);
                selectCardList.RemoveAt(lastIndex);
            }
        }
    }

    /// <summary>
    /// 풀에서 카드 가져오기 (없으면 새로 생성)
    /// </summary>
    UISelectCard GetCardFromPool()
    {
        UISelectCard card = null;

        // 풀에 사용 가능한 카드가 있는지 확인
        if (cardPool.Count > 0)
        {
            card = cardPool[0];
            cardPool.RemoveAt(0);
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            card = CreateNewCard();

            // 카드 선택 이벤트 등록 (한 번만)
            RegisterCardEvent(card);
        }

        return card;
    }

    /// <summary>
    /// 카드 선택 이벤트 등록
    /// </summary>
    void RegisterCardEvent(UISelectCard card)
    {
        card.OnSelectCard += HandleCardSelected;
    }

    /// <summary>
    /// 카드 선택 이벤트 해제
    /// </summary>
    void UnregisterCardEvent(UISelectCard card)
    {
        card.OnSelectCard -= HandleCardSelected;
    }

    /// <summary>
    /// 카드 선택 핸들러
    /// </summary>
    void HandleCardSelected(int cardID)
    {
        Debug.Log($"Card Selected: ID = {cardID}");

        // Presenter에 전달
        presenter?.OnCardSelected(cardID);

        HideUI();
    }

    /// <summary>
    /// 카드를 풀로 반환
    /// </summary>
    void ReturnCardToPool(UISelectCard card)
    {
        card.gameObject.SetActive(false);

        // 이미 풀에 있는지 확인 (중복 방지)
        if (!cardPool.Contains(card))
        {
            cardPool.Add(card);
        }
    }

    /// <summary>
    /// 새 카드 생성
    /// </summary>
    UISelectCard CreateNewCard()
    {
        var instance = Instantiate(m_prefab.gameObject, m_container.transform);
        var card = instance.GetComponent<UISelectCard>();
        return card;
    }

    /// <summary>
    /// 모든 카드 제거 (풀로 반환)
    /// </summary>
    void ClearAllCards()
    {
        foreach (var card in selectCardList)
        {
            ReturnCardToPool(card);
        }
        selectCardList.Clear();
    }

    /// <summary>
    /// 풀 완전히 정리 (메모리 해제)
    /// </summary>
    void DestroyPool()
    {
        foreach (var card in cardPool)
        {
            if (card != null)
            {
                UnregisterCardEvent(card);
                Destroy(card.gameObject);
            }
        }
        cardPool.Clear();

        // 활성화된 카드들도 이벤트 해제
        foreach (var card in selectCardList)
        {
            if (card != null)
            {
                UnregisterCardEvent(card);
            }
        }
    }

    void HideUI()
    {
        this.transform.gameObject.SetActive(false);
        presenter.LoadNextStage();
    }

    private void OnDestroy()
    {
        // 씬 종료 시 풀 정리
        DestroyPool();
    }
}