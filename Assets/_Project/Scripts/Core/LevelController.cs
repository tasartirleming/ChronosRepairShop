using System;
using UnityEngine;

namespace ChronosRepairShop
{
    /// <summary>
    /// Owns the level loop: Placement (countdown, optional) -> Running (darkness countdown) -> Won / Failed.
    /// One per level scene.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        [SerializeField] LevelData fallbackLevel;   // used when the scene is played directly
        [SerializeField] EnergyBall ballPrefab;
        [SerializeField] Transform ballSpawn;
        [SerializeField] ClockMechanism mechanism;
        [SerializeField] ExitGate gate;
        [SerializeField] PlacementController placement;

        public LevelData Level { get; private set; }
        public PartInventory Inventory { get; private set; }
        public GameState State { get; private set; }
        public float TimeRemaining { get; private set; }
        public float TimeTotal { get; private set; }
        public PlacementController Placement => placement;

        public event Action<GameState> StateChanged;
        public event Action<LevelResult> Finished;
        public event Action GravityFlipped;

        EnergyBall ball;
        Vector2 baseGravity;
        float flipTimer;
        float gravitySign = 1f;

        void Awake()
        {
            Instance = this;
            var gm = GameManager.Instance;
            gm.SetCurrentIfNone(fallbackLevel);
            Level = gm.CurrentLevel;
            if (!Level) { Debug.LogError("LevelController: no LevelData. Assign 'fallbackLevel'.", this); enabled = false; return; }
            Inventory = new PartInventory(Level.parts);
            baseGravity = Physics2D.gravity;
        }

        void Start()
        {
            if (!Level) return;
            if (placement) placement.Bind(Inventory);
            if (gate) gate.BallEntered += OnBallEnteredGate;
            ApplyGravity();
            EnterPlacement();
        }

        void OnDestroy()
        {
            if (gate) gate.BallEntered -= OnBallEnteredGate;
            Physics2D.gravity = baseGravity;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Placement:
                    if (Level.placementTimeLimit <= 0f) return;
                    TimeRemaining -= Time.deltaTime;
                    if (TimeRemaining <= 0f) StartRun();   // darkness falls: the run starts with whatever is placed
                    break;

                case GameState.Running:
                    TimeRemaining -= Time.deltaTime;
                    if (TimeRemaining <= 0f) Fail(FailReason.Darkness);
                    TickGravityFlip();
                    break;
            }
        }

        void EnterPlacement()
        {
            TimeTotal = Level.placementTimeLimit;
            TimeRemaining = TimeTotal;
            SetState(GameState.Placement);
        }

        /// <summary>"Başlat" button. Locks the layout and releases the energy ball.</summary>
        public void StartRun()
        {
            if (State != GameState.Placement) return;

            if (placement) placement.Lock();
            foreach (var part in PlaceablePart.All) part.BeginRun();

            ball = Instantiate(ballPrefab, ballSpawn.position, Quaternion.identity);
            ball.Stuck += OnBallStuck;
            ball.Release(Level.rules, Level.launchVelocity);

            TimeTotal = Level.runTimeLimit;
            TimeRemaining = TimeTotal;
            flipTimer = 0f;
            SetState(GameState.Running);
        }

        public void Fail(FailReason reason)
        {
            if (State != GameState.Running) return;
            if (ball) ball.Freeze();
            SetState(GameState.Failed);
            Finished?.Invoke(new LevelResult { Won = false, Reason = reason, TimeLeft = TimeRemaining });
        }

        void Win()
        {
            if (State != GameState.Running) return;
            if (ball) ball.Freeze();

            float ratio = TimeTotal > 0f ? TimeRemaining / TimeTotal : 1f;
            int stars = ratio >= Level.threeStarRatio ? 3 : ratio >= Level.twoStarRatio ? 2 : 1;
            SaveSystem.SaveResult(Level, stars);

            SetState(GameState.Won);
            Finished?.Invoke(new LevelResult { Won = true, Stars = stars, TimeLeft = TimeRemaining });
        }

        void OnBallEnteredGate(EnergyBall b) => Win();
        void OnBallStuck(EnergyBall b) => Fail(FailReason.Stuck);

        void TickGravityFlip()
        {
            float interval = Level.rules.gravityFlipInterval;
            if (interval <= 0f) return;
            flipTimer += Time.deltaTime;
            if (flipTimer < interval) return;
            flipTimer = 0f;
            gravitySign = -gravitySign;
            ApplyGravity();
            GravityFlipped?.Invoke();
        }

        void ApplyGravity() => Physics2D.gravity = baseGravity * Level.rules.gravityScale * gravitySign;

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        public void Retry() => GameManager.Instance.Retry();
        public void Next() => GameManager.Instance.LoadNext();
    }
}
