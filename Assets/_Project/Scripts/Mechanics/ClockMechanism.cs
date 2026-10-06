using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>
    /// The clock's main mechanism. Engages once every required (scene-fixed) gear is powered,
    /// which opens the exit gate. The player's parts exist to carry the ball's energy to these gears.
    /// </summary>
    public class ClockMechanism : MonoBehaviour
    {
        [SerializeField] List<Gear> requiredGears = new List<Gear>();
        [SerializeField] ExitGate gate;
        [SerializeField] Transform hand;
        [SerializeField] float handSpeed = -90f;

        public bool IsEngaged { get; private set; }
        public event Action<bool> EngagedChanged;

        void Update()
        {
            bool all = requiredGears.Count > 0 && requiredGears.TrueForAll(g => g && g.IsPowered);
            if (all != IsEngaged)
            {
                IsEngaged = all;
                if (gate) { if (all) gate.Open(); else gate.Close(); }
                EngagedChanged?.Invoke(all);
            }
            if (IsEngaged && hand) hand.Rotate(0f, 0f, handSpeed * Time.deltaTime);
        }
    }
}
