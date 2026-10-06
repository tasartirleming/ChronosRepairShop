using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>Trigger below / around the clock. The ball touching it means time is leaking out: level failed.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LeakZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<EnergyBall>() && LevelController.Instance)
                LevelController.Instance.Fail(FailReason.TimeLeak);
        }
    }
}
