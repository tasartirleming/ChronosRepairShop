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
