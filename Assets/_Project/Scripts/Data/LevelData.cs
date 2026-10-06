using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>Per-era physics twist. Defaults are plain Earth physics.</summary>
    [System.Serializable]
    public class EraRules
    {
        [Tooltip("Multiplier on Physics2D.gravity")] public float gravityScale = 1f;
        [Tooltip("Linear drag on the ball (Egyptian sand)")] public float ballDrag = 0f;
        [Tooltip("Constant force on the ball (Renaissance pendulum draughts, etc.)")] public Vector2 wind = Vector2.zero;
        [Tooltip("Seconds between gravity inversions, 0 = never (Quantum clock)")] public float gravityFlipInterval = 0f;
    }

    [CreateAssetMenu(menuName = "Chronos/Level Data", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string sceneName;

        [Header("Parts given to the player")]
        public List<PartAllotment> parts = new List<PartAllotment>();

        [Header("Time pressure")]
        [Tooltip("Seconds to place parts before the run auto-starts. 0 = unlimited.")]
        public float placementTimeLimit = 45f;
        [Tooltip("Seconds the ball has to open the gate once started.")]
        public float runTimeLimit = 30f;
        [Range(0f, 1f)] public float threeStarRatio = 0.5f;
        [Range(0f, 1f)] public float twoStarRatio = 0.2f;

        [Header("Ball")]
        [Tooltip("Zero = drop from the spawn point, otherwise fire with this velocity.")]
        public Vector2 launchVelocity = Vector2.zero;

        [Header("Physics")]
        public EraRules rules = new EraRules();

        [Header("Story")]
        [TextArea(2, 6)] public string storyFragment;
    }
}
