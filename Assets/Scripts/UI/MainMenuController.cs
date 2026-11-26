using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusanTaxi.UI
{
    /// <summary>
    /// 메인 메뉴 버튼(시작 / 설정 / 나가기) 동작.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string inGameSceneName = "InGame";
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        public void OnStart()
        {
            SceneManager.LoadScene(inGameSceneName);
        }

        public void OnSettings()
        {
            // TODO: 설정 패널 연결
            Debug.Log("[MainMenu] 설정 버튼 클릭 (설정 화면은 아직 없음)");
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
