using System;
using UnityEngine;

namespace ChronosRepairShop
{
    public enum PartKind { Gear, Spring, Weight, Mirror, Ramp }

    [CreateAssetMenu(menuName = "Chronos/Part Definition", fileName = "Part_")]
    public class PartDefinition : ScriptableObject
    {
        public string displayName;
        public PartKind kind;
        public Sprite icon;
        public PlaceablePart prefab;
        public bool canRotate = true;
        public float rotationStep = 15f;
    }

    [Serializable]
    public struct PartAllotment
    {
        public PartDefinition part;
        public int count;
    }
}
