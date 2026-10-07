using UnityEngine;
using UnityEngine.UI;

namespace ChronosRepairShop
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Text playLabel;
        [SerializeField] Text eraLabel;
        [SerializeField] Text progressLabel;
        [SerializeField] Image progressFill;

        void Start()
        {
            var gm = GameManager.Instance;
            var campaign = gm.Campaign;

            if (!campaign || campaign.eras.Count == 0)
            {
                eraLabel.text = "Kampanya bulunamadı";
                progressLabel.text = "Chronos > Build Level 01 menüsünü çalıştırın";
                playButton.interactable = false;
                return;
            }

            LevelData next = null;
            EraData nextEra = null;
            LevelData first = null;
            int done = 0, total = 0;
            foreach (var era in campaign.eras)
                foreach (var level in era.levels)
                {
                    if (!first) first = level;
                    total++;
                    if (SaveSystem.IsCompleted(level)) done++;
                    else if (!next) { next = level; nextEra = era; }
                }

            bool allDone = !next;
            if (allDone) next = first;

            eraLabel.text = allDone ? "Tüm zaman çizgileri onarıldı" : nextEra.eraName;
            float fraction = total > 0 ? (float)done / total : 0f;
            progressFill.fillAmount = fraction;
            progressLabel.text = "Zaman çizgisi %" + Mathf.RoundToInt(fraction * 100f) + " onarıldı";
            playLabel.text = done > 0 ? "DEVAM ET" : "OYNA";

            var target = next;
            playButton.onClick.AddListener(() => gm.LoadLevel(target));

#if UNITY_ANDROID || UNITY_IOS
            quitButton.gameObject.SetActive(false);
#else
            quitButton.onClick.AddListener(Quit);
#endif
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
