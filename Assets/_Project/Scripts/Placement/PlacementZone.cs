using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>Axis-aligned area (BoxCollider2D, trigger, own layer) where parts may be placed.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlacementZone : MonoBehaviour
    {
        public static readonly List<PlacementZone> All = new List<PlacementZone>();

        BoxCollider2D box;

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool Contains(Bounds b)
        {
            var zone = box.bounds;
            return zone.Contains(new Vector3(b.min.x, b.min.y, zone.center.z))
                && zone.Contains(new Vector3(b.max.x, b.max.y, zone.center.z));
        }

        public static bool AnyContains(Bounds b)
        {
            foreach (var z in All) if (z.Contains(b)) return true;
            return false;
        }
    }
}
