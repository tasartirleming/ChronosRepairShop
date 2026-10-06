using UnityEngine;
using UnityEngine.EventSystems;

namespace ChronosRepairShop
{
    /// <summary>Touch / mouse abstraction (legacy Input Manager). Swap the internals if the project moves to the new Input System.</summary>
    public static class PointerInput
    {
        public struct State
        {
            public bool Down, Held, Up;
            public Vector2 Position;
            public int FingerId;
        }

        public static State Primary()
        {
            var s = new State();
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                s.Position = t.position;
                s.FingerId = t.fingerId;
                s.Down = t.phase == TouchPhase.Began;
                s.Up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                s.Held = !s.Up;
            }
            else
            {
                s.Position = Input.mousePosition;
                s.FingerId = -1;
                s.Down = Input.GetMouseButtonDown(0);
                s.Held = Input.GetMouseButton(0);
                s.Up = Input.GetMouseButtonUp(0);
            }
            return s;
        }

        /// <summary>Two-finger twist since the last frame, in degrees.</summary>
        public static bool TryGetTwist(out float degrees)
        {
            degrees = 0f;
            if (Input.touchCount < 2) return false;
            var a = Input.GetTouch(0);
            var b = Input.GetTouch(1);
            Vector2 now = b.position - a.position;
            Vector2 before = (b.position - b.deltaPosition) - (a.position - a.deltaPosition);
            if (now.sqrMagnitude < 1f || before.sqrMagnitude < 1f) return false;
            degrees = Vector2.SignedAngle(before, now);
            return true;
        }

        public static bool IsOverUI(int fingerId) =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}
