using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISelectCard : MonoBehaviour
{
    public class Data : UIScrollableTexts.Data
    {
        public int ID = 0;
        public string weaponName;
        public Sprite iconImage;
    }

    [SerializeField]
    Image icon;
    [SerializeField]
    TextMeshProUGUI titleText;

    [SerializeField]
    Button SelectButton;
    [SerializeField]
    UIScrollableTexts status;

    [SerializeField]
    public int ID => _id;
    private int _id = -1;

    public System.Action<int> OnSelectCard;

    public void SetData(Data data)
    {
        _id = data.ID;
        icon.sprite = data.iconImage;
        titleText.text = data.weaponName;
        status.SetData(data);
    }

    private void Awake()
    {
        RegisterEvents();
    }

    void RegisterEvents()
    {
        SelectButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        OnSelectCard?.Invoke(ID);
    }
}
