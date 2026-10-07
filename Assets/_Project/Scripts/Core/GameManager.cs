using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChronosRepairShop
{
    /// <summary>Persistent singleton: campaign data and level loading. Created lazily, so any level scene can be played directly from the editor.</summary>
    public class GameManager : MonoBehaviour
    {
        const string MenuScene = "MainMenu";
        static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (!instance) new GameObject("GameManager").AddComponent<GameManager>();
                return instance;
            }
        }

        public CampaignData Campaign { get; private set; }
        public LevelData CurrentLevel { get; private set; }

        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Campaign = Resources.Load<CampaignData>("Campaign");
        }

        public void LoadLevel(LevelData level)
        {
            CurrentLevel = level;
            SceneManager.LoadScene(level.sceneName);
        }

        public void LoadMenu() => SceneManager.LoadScene(MenuScene);

        public void Retry()
        {
            if (CurrentLevel) LoadLevel(CurrentLevel);
        }

        public void LoadNext()
        {
            var next = Campaign ? Campaign.GetNext(CurrentLevel) : null;
            if (next) LoadLevel(next);
            else SceneManager.LoadScene(MenuScene);
        }

        // Used by LevelController when a scene is opened directly in the editor.
        public void SetCurrentIfNone(LevelData level)
        {
            if (!CurrentLevel) CurrentLevel = level;
        }
    }
}
