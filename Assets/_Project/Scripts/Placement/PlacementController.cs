using System.Collections.Generic;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>
    /// Drag a part out of the inventory bar, drop it in a PlacementZone, drag it again to move it,
    /// twist with two fingers (or Q/E, mouse wheel, HUD buttons) to rotate. Dropping on the UI bar refunds the part.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] LayerMask partsMask;      // layer of placed parts (picking)
        [SerializeField] LayerMask blockingMask;   // parts + level geometry that parts must not overlap
        [SerializeField] float gridSnap = 0.25f;   // 0 = free placement

        PartInventory inventory;
        PlaceablePart held;
        PlaceablePart selected;
        bool heldIsNew;
        bool locked;
        Vector3 originPos;
        Quaternion originRot;
        Vector3 grabOffset;
        ContactFilter2D blockers;

        public PlaceablePart Selected => selected;

        void Awake()
        {
            if (!cam) cam = Camera.main;
            blockers = new ContactFilter2D { useLayerMask = true, layerMask = blockingMask, useTriggers = false };
        }

        public void Bind(PartInventory inv) => inventory = inv;

        /// <summary>Called by an inventory slot when the player presses it: spawns a part under the finger.</summary>
        public void BeginSpawn(PartDefinition def, Vector2 screenPos)
        {
            if (locked || held) return;
            if (!def.prefab) { Debug.LogError("PartDefinition '" + def.name + "' has no prefab. Re-run Chronos > Build Level 01.", def); return; }
            if (!inventory.TryTake(def)) return;
            var part = Instantiate(def.prefab);
            part.Init(def);
            part.transform.position = SnapPosition(ScreenToWorld(screenPos));
            held = part;
            heldIsNew = true;
            grabOffset = Vector3.zero;
            Select(part);
        }

        public void RotateSelected(int direction)
        {
            if (locked || !selected || !selected.Definition) return;
            selected.Rotate(direction * selected.Definition.rotationStep);
            if (selected == held) UpdatePreview();
        }

        /// <summary>Called on "Başlat": drop whatever is in hand and stop accepting input.</summary>
        public void Lock()
        {
            if (held) Release(PointerInput.Primary(), forceRefund: false);
            locked = true;
            if (selected) selected.ClearPreview();
            selected = null;
        }

        void Update()
        {
            if (locked) return;

            var p = PointerInput.Primary();
            if (!held && p.Down && !PointerInput.IsOverUI(p.FingerId)) TryPickUp(p);
            if (held) HandleHeld(p);

            HandleRotationInput();
        }

        void TryPickUp(PointerInput.State p)
        {
            Vector3 world = ScreenToWorld(p.Position);
            var hit = Physics2D.OverlapPoint(world, partsMask);
            var part = hit ? hit.GetComponentInParent<PlaceablePart>() : null;
            if (!part) { Select(null); return; }

            held = part;
            heldIsNew = false;
            originPos = part.transform.position;
            originRot = part.transform.rotation;
            grabOffset = part.transform.position - world;
            Select(part);
        }

        void HandleHeld(PointerInput.State p)
        {
            if (Input.touchCount < 2)   // while twisting, don't drag
            {
                Vector3 world = ScreenToWorld(p.Position) + grabOffset;
                held.transform.position = SnapPosition(world);
            }
            UpdatePreview();

            if (p.Up) Release(p, forceRefund: false);
        }

        void Release(PointerInput.State p, bool forceRefund)
        {
            var part = held;
            held = null;

            bool overBar = PointerInput.IsOverUI(p.FingerId);
            bool valid = part.IsPlacementValid(blockers);

            if (forceRefund || overBar)
            {
                inventory.Return(part.Definition);
                Destroy(part.gameObject);
                if (selected == part) selected = null;
                return;
            }

            if (valid)
            {
                part.ClearPreview();
                return;
            }

            if (heldIsNew)
            {
                inventory.Return(part.Definition);
                Destroy(part.gameObject);
                if (selected == part) selected = null;
            }
            else
            {
                part.transform.SetPositionAndRotation(originPos, originRot);
                part.ClearPreview();
            }
        }

        void HandleRotationInput()
        {
            var target = held ? held : selected;
            if (!target || !target.Definition) return;

            if (PointerInput.TryGetTwist(out float twist)) target.Rotate(twist);

            // Editor / desktop shortcuts
            int dir = 0;
            if (Input.GetKeyDown(KeyCode.Q)) dir = 1;
            if (Input.GetKeyDown(KeyCode.E)) dir = -1;
            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f) dir = (int)Mathf.Sign(Input.mouseScrollDelta.y);
            if (dir != 0) target.Rotate(dir * target.Definition.rotationStep);

            if (target == held) UpdatePreview();
        }

        void UpdatePreview() => held.SetPreview(held.IsPlacementValid(blockers));

        void Select(PlaceablePart part)
        {
            if (selected && selected != part && selected != held) selected.ClearPreview();
            selected = part;
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            var w = cam.ScreenToWorldPoint(screen);
            w.z = 0f;
            return w;
        }

        Vector3 SnapPosition(Vector3 w)
        {
            if (gridSnap <= 0f) return w;
            return new Vector3(Mathf.Round(w.x / gridSnap) * gridSnap, Mathf.Round(w.y / gridSnap) * gridSnap, 0f);
        }
    }
}
