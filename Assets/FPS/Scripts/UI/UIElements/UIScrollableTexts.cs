using System.Collections.Generic;
using Gpm.Ui;
using UnityEngine;

public class UIScrollableTexts : MonoBehaviour
{
    public class Data
    {
        public List<UI_InfScrollItem_Text.Data> textList;
    }

    [SerializeField]
    private InfiniteScroll infScroll;
    [SerializeField]
    private UI_InfScrollItem_Text textPrefab;

    // 이벤트 래핑
    public System.Action<int, bool> onChangeActiveItem;
    public System.Action<int, int, bool, bool> onChangeValue;
    public System.Action<bool> onStartLine;
    public System.Action<bool> onEndLine;

    protected virtual void Awake()
    {
        RegisterEvent();
    }

    protected virtual void OnDestroy()
    {
        UnRegisterEvent();
    }

    void RegisterEvent()
    {
        infScroll.onChangeActiveItem.AddListener(OnChangedActiveItem);
        infScroll.onChangeValue.AddListener(OnChangeValue);

        infScroll.onStartLine.AddListener(OnStartLine);
        infScroll.onEndLine.AddListener(OnEndLine);
    }

    void UnRegisterEvent()
    {
        infScroll.onChangeActiveItem.RemoveListener(OnChangedActiveItem);
        infScroll.onChangeValue.RemoveListener(OnChangeValue);

        infScroll.onStartLine.RemoveListener(OnStartLine);
        infScroll.onEndLine.RemoveListener(OnEndLine);
    }

    void OnChangedActiveItem(int index, bool isActive)
    {
        onChangeActiveItem?.Invoke(index, isActive);
    }

    void OnChangeValue(int firstDataIndex, int lastDataIndex, bool isStartLine, bool isEndLine)
    {
        onChangeValue?.Invoke(firstDataIndex, lastDataIndex, isStartLine, isEndLine);
    }

    void OnStartLine(bool isStartLine)
    {
        onStartLine?.Invoke(isStartLine);
    }

    void OnEndLine(bool isEndLine)
    {
        onEndLine?.Invoke(isEndLine);
    }

    public void SetData(Data data)
    {
        infScroll.Clear();
        foreach (var item in data.textList)
        {
            infScroll.InsertData(item);
        }
    }
}
