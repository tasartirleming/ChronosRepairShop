using UnityEngine;
using UnityEngine.UI;

namespace ChronosRepairShop
{
    /// <summary>In-level HUD: parts bar, Start button, rotate buttons and the "darkness" countdown.</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] LevelController level;
        [SerializeField] PlacementController placement;
        [SerializeField] ResultPanel result;

        [Header("Widgets")]
        [SerializeField] Text levelTitle;
        [SerializeField] Text phaseLabel;
        [SerializeField] Text timerLabel;
        [SerializeField] Slider darknessBar;
        [SerializeField] Image darknessOverlay;      // full-screen black image, alpha follows darkness
        [SerializeField] Button startButton;
        [SerializeField] Button rotateLeftButton;
        [SerializeField] Button rotateRightButton;
        [SerializeField] GameObject placementUI;

        [Header("Parts bar")]
        [SerializeField] Transform slotRoot;
        [SerializeField] PartSlotUI slotPrefab;

        void Start()
        {
            if (!Validate()) { enabled = false; return; }
            levelTitle.text = level.Level.displayName;

            foreach (var def in level.Inventory.Parts)
                Instantiate(slotPrefab, slotRoot).Setup(def, level.Inventory, placement);

            startButton.onClick.AddListener(level.StartRun);
            rotateLeftButton.onClick.AddListener(() => placement.RotateSelected(1));
            rotateRightButton.onClick.AddListener(() => placement.RotateSelected(-1));

            level.StateChanged += OnStateChanged;
            level.Finished += r => result.Show(r, level.Level, level);
            OnStateChanged(level.State);
        }

        bool Validate()
        {
            bool ok = true;
            void Need(Object o, string n) { if (!o) { Debug.LogError("HudController: '" + n + "' is not assigned. Re-run Chronos > Build Level 01.", this); ok = false; } }
            Need(level, "level"); Need(placement, "placement"); Need(result, "result");
            Need(levelTitle, "levelTitle"); Need(phaseLabel, "phaseLabel"); Need(timerLabel, "timerLabel");
            Need(darknessBar, "darknessBar"); Need(startButton, "startButton");
            Need(rotateLeftButton, "rotateLeftButton"); Need(rotateRightButton, "rotateRightButton");
            Need(placementUI, "placementUI"); Need(slotRoot, "slotRoot"); Need(slotPrefab, "slotPrefab");
            if (ok && level.Level == null) { Debug.LogError("HudController: LevelController has no LevelData (assign fallbackLevel).", this); ok = false; }
            return ok;
        }

        void OnStateChanged(GameState s)
        {
            placementUI.SetActive(s == GameState.Placement);
            phaseLabel.text = s == GameState.Placement ? "Yerleştir" : s == GameState.Running ? "Çalışıyor" : "";
        }

        void Update()
        {
            if (!level) return;
            bool timed = level.TimeTotal > 0f && (level.State == GameState.Placement || level.State == GameState.Running);
            timerLabel.gameObject.SetActive(timed);
            if (!timed) return;

            float darkness = 1f - Mathf.Clamp01(level.TimeRemaining / level.TimeTotal);
            timerLabel.text = Mathf.CeilToInt(Mathf.Max(0f, level.TimeRemaining)).ToString();
            darknessBar.value = darkness;
            if (darknessOverlay)
            {
                var c = darknessOverlay.color;
                c.a = darkness * 0.7f;
                darknessOverlay.color = c;
            }
        }
    }
}
