using UnityEngine;
using UnityEngine.Events;

namespace ChronosRepairShop
{
    /// <summary>Spring pad: launches the ball along its local up axis when hit from the front. Rotating the part aims it.</summary>
    public class SpringBouncer : MonoBehaviour
    {
        [SerializeField] float launchSpeed = 12f;
        [Range(0f, 1f)] [SerializeField] float keepSideways = 0.4f;
        [SerializeField] UnityEvent onBounce;

        void OnCollisionEnter2D(Collision2D c)
        {
            var ball = c.collider.GetComponent<EnergyBall>();
            if (!ball) return;

            // Only the pad face launches; hitting the back or sides is a plain collision.
            Vector2 local = transform.InverseTransformPoint(ball.transform.position);
            if (local.y <= 0f) return;

            Vector2 up = transform.up;
            Vector2 right = transform.right;
            Vector2 sideways = Vector2.Dot(ball.PreviousVelocity, right) * right;
            ball.Body.velocity = up * launchSpeed + sideways * keepSideways;
            onBounce?.Invoke();
        }
    }
}
