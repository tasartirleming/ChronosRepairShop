using UnityEngine;
using UnityEngine.UI;

namespace ChronosRepairShop
{
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text titleLabel;
        [SerializeField] Text bodyLabel;
        [SerializeField] Text starsLabel;
        [SerializeField] Button nextButton;
        [SerializeField] Button retryButton;

        void Awake()
        {
            if (root) root.SetActive(false);
        }

        public void Show(LevelResult r, LevelData level, LevelController controller)
        {
            root.SetActive(true);
            nextButton.gameObject.SetActive(r.Won);
            nextButton.onClick.RemoveAllListeners();
            retryButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(controller.Next);
            retryButton.onClick.AddListener(controller.Retry);

            if (r.Won)
            {
                titleLabel.text = "Zaman Onarıldı";
                starsLabel.text = new string('★', r.Stars) + new string('☆', 3 - r.Stars);
                bodyLabel.text = level.storyFragment;
            }
            else
            {
                titleLabel.text = r.Reason == FailReason.Darkness ? "Evren Karardı" : "Zaman Sızıntısı";
                starsLabel.text = "";
                bodyLabel.text = r.Reason == FailReason.Stuck
                    ? "Enerji topu sıkıştı. Açıları ve çarkların dişlerini kontrol et."
                    : r.Reason == FailReason.Darkness
                        ? "Süre doldu, kapı açılmadı."
                        : "Enerji topu saatten sızdı.";
            }
        }
    }
}
