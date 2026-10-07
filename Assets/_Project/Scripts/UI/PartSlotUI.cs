using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChronosRepairShop
{
    /// <summary>One slot in the parts bar. Pressing it spawns the part under the finger; dragging continues in PlacementController.</summary>
    public class PartSlotUI : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] Image icon;
        [SerializeField] Text countLabel;
        [SerializeField] CanvasGroup group;

        PartDefinition def;
        PartInventory inventory;
        PlacementController placement;

        public void Setup(PartDefinition definition, PartInventory inv, PlacementController placementController)
        {
            def = definition;
            inventory = inv;
            placement = placementController;
            icon.sprite = def.icon;
            inventory.Changed += Refresh;
            Refresh();
        }

        /// <summary>Builds a slot in code, so levels work even without a slot prefab.</summary>
        public static PartSlotUI CreateDefault(Transform parent, Sprite background = null)
        {
            var go = new GameObject("PartSlot", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(220, 220);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.13f, 0.16f, 0.28f);
            if (background) { bg.sprite = background; bg.type = Image.Type.Sliced; }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var irt = (RectTransform)iconGo.transform;
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(25, 50); irt.offsetMax = new Vector2(-25, -20);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;

            var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
            countGo.transform.SetParent(go.transform, false);
            var crt = (RectTransform)countGo.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = new Vector2(1, 0);
            crt.offsetMin = Vector2.zero; crt.offsetMax = new Vector2(0, 50);
            var text = countGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter; text.fontSize = 40; text.color = Color.white; text.raycastTarget = false;

            var slot = go.AddComponent<PartSlotUI>();
            slot.icon = icon; slot.countLabel = text; slot.group = go.GetComponent<CanvasGroup>();
            return slot;
        }

        void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        void Refresh()
        {
            int n = inventory.Remaining(def);
            countLabel.text = "x" + n;
            group.alpha = n > 0 ? 1f : 0.35f;
        }

        public void OnPointerDown(PointerEventData e) => placement.BeginSpawn(def, e.position);
    }
}
