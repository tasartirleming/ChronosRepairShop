using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>
    /// Root component of every player-placeable prefab (Rigidbody2D + Collider2D + Gear/SpringBouncer/Deflector...).
    /// During placement the body is kinematic, so weights hang in place; BeginRun() restores the prefab's real body type.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlaceablePart : MonoBehaviour
    {
        public static readonly List<PlaceablePart> All = new List<PlaceablePart>();

        [Tooltip("On for weights that should fall once the run starts. Gears, mirrors and springs stay fixed.")]
        [SerializeField] bool dynamicWhenRunning = false;
        [SerializeField] Color validTint = new Color(0.6f, 1f, 0.6f, 1f);
        [SerializeField] Color invalidTint = new Color(1f, 0.4f, 0.4f, 1f);

        Rigidbody2D body;
        Collider2D col;
        SpriteRenderer[] sprites;

        public PartDefinition Definition { get; private set; }
        public Bounds Bounds => col.bounds;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sprites = GetComponentsInChildren<SpriteRenderer>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Init(PartDefinition def) => Definition = def;

        public void Rotate(float degrees)
        {
            if (Definition && !Definition.canRotate) return;
            transform.Rotate(0f, 0f, degrees);
        }

        public void SetPreview(bool valid)
        {
            var tint = valid ? validTint : invalidTint;
            foreach (var s in sprites) s.color = tint;
        }

        public void ClearPreview()
        {
            foreach (var s in sprites) s.color = Color.white;
        }

        public bool IsPlacementValid(ContactFilter2D blockers)
        {
            if (!PlacementZone.AnyContains(col.bounds)) return false;

            var hits = new List<Collider2D>();
            col.OverlapCollider(blockers, hits);
            var self = GetComponent<Gear>();
            foreach (var h in hits)
            {
                // Gears are allowed to touch (that is meshing) but not to jam into each other.
                var other = h.GetComponentInParent<Gear>();
                if (self && other) { if (self.IsJammedWith(other)) return false; continue; }
                return false;
            }
            return true;
        }

        public void BeginRun()
        {
            body.bodyType = dynamicWhenRunning ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.WakeUp();
            ClearPreview();
        }
    }
}
