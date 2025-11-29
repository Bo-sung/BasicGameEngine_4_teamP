
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Unity.FPS.UI
{
    public class LoadSceneButton : MonoBehaviour
    {
        [SerializeField]
        private string SceneName = "";
        [SerializeField]
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(LoadTargetScene);
        }

        void LoadTargetScene()
        {
            SceneManager.LoadScene(SceneName);
        }
    }
}