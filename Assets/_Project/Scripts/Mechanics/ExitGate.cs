using System;
using UnityEngine;
using UnityEngine.Events;

namespace ChronosRepairShop
{
    /// <summary>Solid door (blocks the ball while closed) plus a trigger that wins the level when the ball enters an open gate.</summary>
    public class ExitGate : MonoBehaviour
    {
        [SerializeField] Collider2D door;
        [SerializeField] UnityEvent onOpened;
        [SerializeField] UnityEvent onClosed;

        public bool IsOpen { get; private set; }
        public event Action<EnergyBall> BallEntered;

        public void Open()
        {
            IsOpen = true;
            if (door) door.enabled = false;
            onOpened?.Invoke();
        }

        public void Close()
        {
            IsOpen = false;
            if (door) door.enabled = true;
            onClosed?.Invoke();
        }

        void OnTriggerEnter2D(Collider2D other) => Check(other);
        void OnTriggerStay2D(Collider2D other) => Check(other);   // ball may already be inside when the gate opens

        void Check(Collider2D other)
        {
            if (!IsOpen) return;
            var ball = other.GetComponent<EnergyBall>();
            if (ball) BallEntered?.Invoke(ball);
        }
    }
}
