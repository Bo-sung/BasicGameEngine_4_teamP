using Gpm.Ui;
using TMPro;
using UnityEngine;

public class UI_InfScrollItem_Text : InfiniteScrollItem
{
    public class Data : InfiniteScrollData
    {
        public string text;
    }

    [SerializeField]
    private TextMeshProUGUI m_text;


    public override void UpdateData(InfiniteScrollData scrollData)
    {
        base.UpdateData(scrollData);

        if (scrollData is Data)
        {
            var data = (Data)scrollData;
            m_text.text = data.text;
            // 비활성화시 활성화 처리
            this.SetActive(true);
        }
    }

    void Awake()
    {
        if (m_text == null)
            m_text = GetComponent<TextMeshProUGUI>();
    }
}
