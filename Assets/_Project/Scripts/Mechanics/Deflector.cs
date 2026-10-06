using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>
    /// Ramp: ordinary slope, give the collider a low-bounce PhysicsMaterial2D.
    /// Mirror: perfect reflection (angle in = angle out), so the placed angle matters.
    /// </summary>
    public class Deflector : MonoBehaviour
    {
        public enum Mode { Ramp, Mirror }

        [SerializeField] Mode mode = Mode.Mirror;
        [SerializeField] float mirrorGain = 1f;

        void OnCollisionEnter2D(Collision2D c)
        {
            if (mode != Mode.Mirror) return;
            var ball = c.collider.GetComponent<EnergyBall>();
            if (!ball) return;

            Vector2 n = c.GetContact(0).normal;
            Vector2 incoming = ball.PreviousVelocity;
            if (Vector2.Dot(n, incoming) > 0f) n = -n;      // make sure the normal faces the ball

            ball.Body.linearVelocity = Vector2.Reflect(incoming, n) * mirrorGain;
        }
    }
}
