using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>
    /// A kinematic gear. The ball drives it by hitting it; powered gears pass spin on to meshing neighbours
    /// (touching, but not overlapping too much - that would jam). Spin is real: the kinematic body's
    /// angularVelocity drags the ball along the rim through friction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Gear : MonoBehaviour
    {
        static readonly List<Gear> All = new List<Gear>();
        static float resolvedAt = -1f;

        [SerializeField] float spinSpeed = 180f;
        [SerializeField] float minImpactSpeed = 1.5f;
        [Tooltip("Max gap between rims that still counts as meshing")]
        [SerializeField] float meshPadding = 0.15f;
        [Tooltip("Centre distance below this fraction of (r1+r2) jams the gears")]
        [SerializeField, Range(0.5f, 1f)] float jamFraction = 0.75f;
        [Tooltip("Stay powered once hit. Off = powered only while the ball keeps driving it.")]
        [SerializeField] bool latch = true;
        [SerializeField] float holdTime = 2f;

        Rigidbody2D body;
        CircleCollider2D circle;
        int driveDir;
        float lastDriveTime;
        int appliedDir;

        public event Action<Gear> PoweredChanged;
        public bool IsPowered => appliedDir != 0;
        public float Radius => circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 Pos => transform.position;
        bool IsDriven => driveDir != 0 && (latch || Time.fixedTime - lastDriveTime <= holdTime);

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        void OnEnable() { All.Add(this); resolvedAt = -1f; }
        void OnDisable() { All.Remove(this); }

        void FixedUpdate() => Resolve();

        public bool IsJammedWith(Gear other) =>
            Vector2.Distance(Pos, other.Pos) < (Radius + other.Radius) * jamFraction;

        bool Meshes(Gear other)
        {
            float d = Vector2.Distance(Pos, other.Pos);
            float sum = Radius + other.Radius;
            return d <= sum + meshPadding && d >= sum * jamFraction;
        }

        // Breadth-first spread from every ball-driven gear. Runs once per physics step.
        static void Resolve()
        {
            if (Mathf.Approximately(resolvedAt, Time.fixedTime)) return;
            resolvedAt = Time.fixedTime;

            var dirs = new Dictionary<Gear, int>();
            var queue = new Queue<Gear>();
            foreach (var g in All)
                if (g.IsDriven) { dirs[g] = g.driveDir; queue.Enqueue(g); }

            while (queue.Count > 0)
            {
                var g = queue.Dequeue();
                foreach (var n in All)
                {
                    if (n == g || dirs.ContainsKey(n) || !g.Meshes(n)) continue;
                    dirs[n] = -dirs[g];     // meshing gears counter-rotate
                    queue.Enqueue(n);
                }
            }

            foreach (var g in All)
                g.Apply(dirs.TryGetValue(g, out var d) ? d : 0);
        }

        void Apply(int dir)
        {
            if (dir == appliedDir) return;
            bool wasPowered = appliedDir != 0;
            appliedDir = dir;
            body.angularVelocity = dir * spinSpeed;
            if (wasPowered != (dir != 0)) PoweredChanged?.Invoke(this);
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            var ball = c.collider.GetComponent<EnergyBall>();
            if (!ball || c.relativeVelocity.magnitude < minImpactSpeed) return;

            // Which way round the rim is the ball travelling? Counter-clockwise (+) or clockwise (-).
            Vector2 r = c.GetContact(0).point - Pos;
            Vector2 v = ball.PreviousVelocity;
            driveDir = (r.x * v.y - r.y * v.x) >= 0f ? 1 : -1;
            lastDriveTime = Time.fixedTime;
        }

        void OnCollisionStay2D(Collision2D c)
        {
            // Resting contact keeps a non-latching gear alive.
            if (driveDir != 0 && c.collider.GetComponent<EnergyBall>()) lastDriveTime = Time.fixedTime;
        }
    }
}
