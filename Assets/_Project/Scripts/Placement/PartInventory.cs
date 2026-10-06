using System;
using System.Collections.Generic;

namespace ChronosRepairShop
{
    public class PartInventory
    {
        readonly Dictionary<PartDefinition, int> remaining = new Dictionary<PartDefinition, int>();
        readonly List<PartDefinition> order = new List<PartDefinition>();

        public IReadOnlyList<PartDefinition> Parts => order;
        public event Action Changed;

        public PartInventory(IEnumerable<PartAllotment> allotments)
        {
            foreach (var a in allotments)
            {
                if (!a.part) continue;
                if (!remaining.ContainsKey(a.part)) order.Add(a.part);
                remaining.TryGetValue(a.part, out int have);
                remaining[a.part] = have + a.count;
            }
        }

        public int Remaining(PartDefinition def) => remaining.TryGetValue(def, out int n) ? n : 0;

        public bool TryTake(PartDefinition def)
        {
            if (Remaining(def) <= 0) return false;
            remaining[def]--;
            Changed?.Invoke();
            return true;
        }

        public void Return(PartDefinition def)
        {
            if (!remaining.ContainsKey(def)) return;
            remaining[def]++;
            Changed?.Invoke();
        }
    }
}
