using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    [CreateAssetMenu(menuName = "Chronos/Era Data", fileName = "Era_")]
    public class EraData : ScriptableObject
    {
        public string eraName;                 // e.g. "Ancient Egypt - Sand Hourglass"
        public Sprite background;
        public Color tint = Color.white;
        [TextArea(2, 6)] public string timelineRestoredText;   // shown once the whole era is repaired
        public List<LevelData> levels = new List<LevelData>();

        /// <summary>0..1 share of this era's levels already repaired - drives the "timeline healing" visual.</summary>
        public float RepairedFraction()
        {
            if (levels.Count == 0) return 0f;
            int done = 0;
            foreach (var l in levels) if (SaveSystem.IsCompleted(l)) done++;
            return (float)done / levels.Count;
        }
    }
}
