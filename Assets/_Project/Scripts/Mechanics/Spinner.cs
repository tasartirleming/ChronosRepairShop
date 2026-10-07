using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>Purely visual constant rotation (menu background gears, hint gears).</summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 10f;

        public float DegreesPerSecond { get => degreesPerSecond; set => degreesPerSecond = value; }

        void Update() => transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
