using System;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>The Time Energy Ball. Tracks its pre-collision velocity and detects getting stuck.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnergyBall : MonoBehaviour
    {
        [SerializeField] float stuckSpeed = 0.15f;
        [SerializeField] float stuckSeconds = 1.5f;
        [SerializeField] float maxSpeed = 25f;

        public Rigidbody2D Body { get; private set; }
        /// <summary>Velocity at the start of the last physics step, i.e. before the current collision was resolved.</summary>
        public Vector2 PreviousVelocity { get; private set; }

        public event Action<EnergyBall> Stuck;

        Vector2 wind;
        float stuckTimer;
        bool active;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.simulated = false;
        }

        public void Release(EraRules rules, Vector2 launchVelocity)
        {
            Body.simulated = true;
            Body.linearDamping = rules.ballDrag;
            wind = rules.wind;
            Body.linearVelocity = launchVelocity;
            PreviousVelocity = launchVelocity;
            active = true;
        }

        public void Freeze()
        {
            active = false;
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
        }

        void FixedUpdate()
        {
            if (!active) return;

            PreviousVelocity = Body.linearVelocity;
            if (wind != Vector2.zero) Body.AddForce(wind);
            if (Body.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
                Body.linearVelocity = Body.linearVelocity.normalized * maxSpeed;

            stuckTimer = Body.linearVelocity.magnitude < stuckSpeed ? stuckTimer + Time.fixedDeltaTime : 0f;
            if (stuckTimer >= stuckSeconds)
            {
                stuckTimer = 0f;
                Stuck?.Invoke(this);
            }
        }
    }
}
