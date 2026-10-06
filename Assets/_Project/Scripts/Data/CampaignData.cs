using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>Put one asset named "Campaign" under Assets/Resources.</summary>
    [CreateAssetMenu(menuName = "Chronos/Campaign", fileName = "Campaign")]
    public class CampaignData : ScriptableObject
    {
        public List<EraData> eras = new List<EraData>();

        public LevelData GetNext(LevelData current)
        {
            bool found = false;
            foreach (var era in eras)
                foreach (var level in era.levels)
                {
                    if (found) return level;
                    if (level == current) found = true;
                }
            return null;
        }

        public bool IsUnlocked(LevelData level)
        {
            LevelData previous = null;
            foreach (var era in eras)
                foreach (var l in era.levels)
                {
                    if (l == level) return previous == null || SaveSystem.IsCompleted(previous);
                    previous = l;
                }
            return false;
        }
    }
}
