
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class ToggleGameObjectButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject ToggleTarget;
        [SerializeField]
        private Button button;
        [SerializeField]
        bool _IsOn = false;
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Toggle);
        }

        private void Toggle()
        {
            _IsOn = !ToggleTarget.active;
            ToggleTarget.SetActive(_IsOn);
        }
    }
}