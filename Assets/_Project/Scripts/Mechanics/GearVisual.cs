using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>Glow that fades in and pulses while the gear is powered.</summary>
    [RequireComponent(typeof(Gear))]
    public class GearVisual : MonoBehaviour
    {
        [SerializeField] SpriteRenderer glow;
        [SerializeField] Color glowColor = new Color(0.24f, 0.88f, 0.95f);
        [SerializeField] float maxAlpha = 0.55f;

        Gear gear;
        float level;

        void Awake() => gear = GetComponent<Gear>();

        void Update()
        {
            if (!glow) return;
            level = Mathf.MoveTowards(level, gear.IsPowered ? 1f : 0f, Time.deltaTime * 4f);
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 6f);
            var c = glowColor;
            c.a = level * maxAlpha * pulse;
            glow.color = c;
        }
    }
}
