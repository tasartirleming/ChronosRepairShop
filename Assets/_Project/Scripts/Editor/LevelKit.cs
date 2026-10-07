using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ChronosRepairShop.EditorTools.BuilderUtil;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>Everything the per-level builders share: project setup, part prefabs, scene scaffold, gate, zones and HUD.</summary>
    public static class LevelKit
    {
        public const string LBall = "Ball", LParts = "Parts", LGear = "Gears", LStatic = "Static", LZone = "PlacementZone", LLeak = "Leak";

        public sealed class Kit
        {
            public Camera Cam;
            public Sprite Square, Rounded, Glow, GearCyan, GearAmber;
            public PhysicsMaterial2D MatStatic, MatGear;
        }

        // ------------------------------------------------------------------ project-wide assets
        /// <summary>Layers, collision matrix, art, materials, part prefabs and part definitions. Safe to run repeatedly.</summary>
        public static void PrepareAssets()
        {
            EnsureLayers(LBall, LParts, LGear, LStatic, LZone, LLeak);
            SetupCollisionMatrix();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            ArtFactory.EnsureAll();
            var trailMat = ArtFactory.TrailMaterial();
            var matBall = MakeMaterial("Ball", 0.35f, 0.2f);
            var matGear = MakeMaterial("Gear", 0.1f, 0.8f);
            var matMirror = MakeMaterial("Mirror", 0f, 0f);
            var matSpring = MakeMaterial("Spring", 0f, 0.1f);
            MakeMaterial("Static", 0.1f, 0.3f);

            MakeBallPrefab(ArtFactory.Get(ArtFactory.Ball), matBall, trailMat);
            MakeGearPrefab(ArtFactory.Get(ArtFactory.GearAmber), ArtFactory.Get(ArtFactory.Glow), matGear, 0.6f, "Part_Gear");
            MakeMirrorPrefab(ArtFactory.Get(ArtFactory.Mirror), matMirror);
            MakeSpringPrefab(ArtFactory.Get(ArtFactory.Spring), matSpring);
            MakeSlotPrefab(ArtFactory.Get(ArtFactory.Rounded));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MakePart("Gear_Small", "Küçük Dişli", PartKind.Gear, ArtFactory.GearAmber, "Part_Gear");
            MakePart("Mirror", "Ayna", PartKind.Mirror, ArtFactory.Mirror, "Part_Mirror");
            MakePart("Spring", "Yay", PartKind.Spring, ArtFactory.Spring, "Part_Spring");
            AssetDatabase.SaveAssets();
        }

        public static PartDefinition Part(string file) =>
            Load<PartDefinition>($"{Root}/ScriptableObjects/Parts/Part_{file}.asset");

        public static void RegisterLevel(LevelData level)
        {
            var era = LoadOrCreate<EraData>($"{Root}/ScriptableObjects/Eras/Era_Egypt.asset");
            era.eraName = "Antik Mısır - Kum Saati";
            if (!era.levels.Contains(level)) era.levels.Add(level);
            era.levels.RemoveAll(l => !l);
            era.levels.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            EditorUtility.SetDirty(era);

            var campaign = LoadOrCreate<CampaignData>("Assets/Resources/Campaign.asset");
            if (!campaign.eras.Contains(era)) campaign.eras.Add(era);
            EditorUtility.SetDirty(campaign);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ scene scaffold
        public static Kit BeginScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene unloads unused assets: always load fresh from disk after it.
            var kit = new Kit
            {
                Square = ArtFactory.Get(ArtFactory.Square),
                Rounded = ArtFactory.Get(ArtFactory.Rounded),
                Glow = ArtFactory.Get(ArtFactory.Glow),
                GearCyan = ArtFactory.Get(ArtFactory.GearCyan),
                GearAmber = ArtFactory.Get(ArtFactory.GearAmber),
                MatStatic = Load<PhysicsMaterial2D>($"{Root}/Physics/Static.physicsMaterial2D"),
                MatGear = Load<PhysicsMaterial2D>($"{Root}/Physics/Gear.physicsMaterial2D"),
            };

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            kit.Cam = camGo.AddComponent<Camera>();
            kit.Cam.orthographic = true;
            kit.Cam.orthographicSize = 7f;
            kit.Cam.clearFlags = CameraClearFlags.SolidColor;
            kit.Cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            camGo.transform.position = new Vector3(0f, -1f, -10f);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            SpriteGo("Backdrop", ArtFactory.Get(ArtFactory.Backdrop), new Vector3(0f, -1f), new Vector3(600f, 10.5f), Color.white, -100);
            SpriteGo("ClockRing", ArtFactory.Get(ArtFactory.Ring), new Vector3(0.2f, -0.5f), Vector3.one * 10.5f, new Color(1f, 1f, 1f, 0.10f), -50);
            SpriteGo("DecoGear", ArtFactory.Get(ArtFactory.GearSlate), new Vector3(-2.2f, 3.4f), Vector3.one * 3f, new Color(1f, 1f, 1f, 0.10f), -45)
                .AddComponent<Spinner>().DegreesPerSecond = 6f;
            SpriteGo("DecoGear2", ArtFactory.Get(ArtFactory.GearSlate), new Vector3(-0.35f, 5.3f), Vector3.one * 1.8f, new Color(1f, 1f, 1f, 0.10f), -45)
                .AddComponent<Spinner>().DegreesPerSecond = -10f;

            var wall = new Color(0.17f, 0.21f, 0.38f);
            BoxGo("WallLeft", kit.Square, new Vector2(-3.3f, -0.5f), new Vector2(0.2f, 15f), 0f, LStatic, kit.MatStatic, wall);
            BoxGo("WallRight", kit.Square, new Vector2(3.3f, -0.5f), new Vector2(0.2f, 15f), 0f, LStatic, kit.MatStatic, wall);

            var leak = new GameObject("LeakZone") { layer = LayerMask.NameToLayer(LLeak) };
            leak.transform.position = new Vector3(0f, -8f);
            leak.AddComponent<BoxCollider2D>().size = new Vector2(10f, 2f);
            leak.AddComponent<LeakZone>();
            return kit;
        }

        public static void AddFloor(Kit kit, Vector2 center, float width, float angle) =>
            BoxGo("Floor", kit.Square, center, new Vector2(width, 0.2f), angle, LStatic, kit.MatStatic, new Color(0.17f, 0.21f, 0.38f));

        public static void AddZone(Kit kit, string name, Vector2 center, Vector2 size)
        {
            var zone = new GameObject(name) { layer = LayerMask.NameToLayer(LZone) };
            zone.transform.position = center;
            zone.AddComponent<BoxCollider2D>().size = size;
            zone.AddComponent<PlacementZone>();
            var zv = SpriteGo(name + "Visual", kit.Rounded, center, Vector3.one, new Color(0.24f, 0.88f, 0.95f, 0.07f), -40);
            var sr = zv.GetComponent<SpriteRenderer>();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
        }

        public static Gear AddRequiredGear(Kit kit, Vector2 pos, float radius)
        {
            var g0 = new GameObject("RequiredGear") { layer = LayerMask.NameToLayer(LGear) };
            g0.transform.position = pos;
            g0.transform.localScale = Vector3.one * radius * 2f;
            var sr = g0.AddComponent<SpriteRenderer>(); sr.sprite = kit.GearCyan; sr.sortingOrder = 1;
            g0.AddComponent<Rigidbody2D>();
            var cc = g0.AddComponent<CircleCollider2D>(); cc.radius = 0.5f; cc.sharedMaterial = kit.MatGear;
            var gear = g0.AddComponent<Gear>();

            var glow = new GameObject("Glow");
            glow.transform.SetParent(g0.transform, false);
            glow.transform.localScale = Vector3.one * 1.7f;
            var gsr = glow.AddComponent<SpriteRenderer>(); gsr.sprite = kit.Glow; gsr.sortingOrder = 0; gsr.color = new Color(1, 1, 1, 0);
            var gv = g0.AddComponent<GearVisual>();
            Ref(gv, "glow", gsr);
            Col(gv, "glowColor", Cyan);
            return gear;
        }

        /// <summary>Solid door + trigger at the bottom right, and the clock mechanism that opens it.</summary>
        public static ClockMechanism AddGateAndMechanism(Kit kit, out ExitGate gate, params Gear[] requiredGears)
        {
            var gateGo = new GameObject("ExitGate") { layer = LayerMask.NameToLayer(LStatic) };
            gateGo.transform.position = new Vector3(3.0f, -4.6f);
            var gt = gateGo.AddComponent<BoxCollider2D>(); gt.isTrigger = true; gt.size = new Vector2(0.5f, 1.8f);
            gate = gateGo.AddComponent<ExitGate>();
            SpriteGo("GateGlow", kit.Glow, gateGo.transform.position, new Vector3(1.6f, 3.2f, 1f), new Color(0.24f, 0.88f, 0.95f, 0.45f), -5);
            var door = BoxGo("GateDoor", kit.Square, new Vector2(2.7f, -4.6f), new Vector2(0.2f, 2.0f), 0f, LStatic, kit.MatStatic, Magenta, 2);

            var mech = new GameObject("ClockMechanism").AddComponent<ClockMechanism>();
            Ref(mech, "gate", gate);
            var refs = new Object[requiredGears.Length];
            for (int i = 0; i < refs.Length; i++) refs[i] = requiredGears[i];
            RefList(mech, "requiredGears", refs);
            Ref(gate, "door", door.GetComponent<Collider2D>());
            return mech;
        }

        public static GameObject AddHintGhost(Sprite sprite, string name, Vector2 pos, float scale, float rotation, float spin)
        {
            var g = SpriteGo(name, sprite, pos, Vector3.one * scale, new Color(1f, 1f, 1f, 0.2f), -30);
            g.transform.rotation = Quaternion.Euler(0, 0, rotation);
            if (spin != 0f) g.AddComponent<Spinner>().DegreesPerSecond = spin;
            return g;
        }

        public static Transform AddBallSpawn(Kit kit, Vector2 pos)
        {
            var spawn = new GameObject("BallSpawn");
            spawn.transform.position = pos;
            SpriteGo("SpawnMarker", kit.Glow, pos, Vector3.one * 0.9f, new Color(0.24f, 0.88f, 0.95f, 0.35f), -5);
            return spawn.transform;
        }

        /// <summary>Creates controllers + HUD, saves the scene and puts it in Build Settings.</summary>
        public static void Finish(Kit kit, LevelData level, Transform spawn, ClockMechanism mech, ExitGate gate)
        {
            var ballPrefab = Load<GameObject>($"{Root}/Prefabs/Level/EnergyBall.prefab").GetComponent<EnergyBall>();
            var slotPrefab = Load<GameObject>($"{Root}/Prefabs/UI/PartSlot.prefab").GetComponent<PartSlotUI>();

            var placement = new GameObject("Placement").AddComponent<PlacementController>();
            Ref(placement, "cam", kit.Cam);
            int parts = LayerMaskOf(LParts, LGear);
            Int(placement, "partsMask", parts);
            Int(placement, "blockingMask", parts | LayerMaskOf(LStatic));
            Float(placement, "gridSnap", 0.25f);

            var lc = new GameObject("LevelController").AddComponent<LevelController>();
            Ref(lc, "fallbackLevel", level);
            Ref(lc, "ballPrefab", ballPrefab);
            Ref(lc, "ballSpawn", spawn);
            Ref(lc, "mechanism", mech);
            Ref(lc, "gate", gate);
            Ref(lc, "placement", placement);

            BuildUI(lc, placement, slotPrefab, kit.Rounded);

            Directory.CreateDirectory(Root + "/Scenes");
            var path = $"{Root}/Scenes/{level.sceneName}.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            AddSceneToBuild(path, false);
        }

        // ------------------------------------------------------------------ HUD
        static void BuildUI(LevelController lc, PlacementController placement, PartSlotUI slotPrefab, Sprite rounded)
        {
            var res = UiResources();
            var canvasGo = MakeCanvas("Canvas", out var cr);

            var overlay = MakePanel(res, cr, "DarknessOverlay", null, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.GetComponent<Image>().raycastTarget = false;
            var oi = overlay.GetComponent<Image>();

            var menu = MakeButton(res, cr, "MenuButton", "MENÜ", rounded, Slate, Color.white, 36,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -130), new Vector2(230, -40));
            var title = MakeLabel(res, cr, "LevelTitle", "", 48, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.25f, 1), new Vector2(0.75f, 1), new Vector2(0, -130), new Vector2(0, -40));
            var phase = MakeLabel(res, cr, "PhaseLabel", "", 36, TextAnchor.MiddleCenter, new Color(0.6f, 0.7f, 0.85f),
                new Vector2(0.25f, 1), new Vector2(0.75f, 1), new Vector2(0, -190), new Vector2(0, -130));
            var timer = MakeLabel(res, cr, "Timer", "", 84, TextAnchor.MiddleRight, Cyan,
                new Vector2(0.72f, 1), new Vector2(1, 1), new Vector2(0, -150), new Vector2(-30, -20));

            var sliderGo = DefaultControls.CreateSlider(res);
            sliderGo.name = "DarknessBar"; sliderGo.transform.SetParent(cr, false);
            Place(sliderGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -250), new Vector2(-40, -215));
            var slider = sliderGo.GetComponent<Slider>(); slider.interactable = false; slider.value = 0;
            StyleSlider(slider, rounded, new Color(1, 1, 1, 0.08f), Amber);

            var bar = MakePanel(res, cr, "PlacementUI", rounded, new Color(0.04f, 0.05f, 0.10f, 0.88f),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 20), new Vector2(-20, 450));

            var slotRoot = new GameObject("Slots", typeof(RectTransform));
            slotRoot.transform.SetParent(bar.transform, false);
            Place(slotRoot, Vector2.zero, Vector2.one, new Vector2(20, 170), new Vector2(-20, -15));
            var hl = slotRoot.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 40;
            hl.childControlWidth = false; hl.childControlHeight = false;

            var rotL = MakeButton(res, bar.transform, "RotateLeft", "SOLA DÖN", rounded, Slate, Color.white, 38,
                new Vector2(0.03f, 0), new Vector2(0.30f, 0), new Vector2(0, 20), new Vector2(0, 150));
            var start = MakeButton(res, bar.transform, "Start", "BAŞLAT", rounded, Cyan, Ink, 50,
                new Vector2(0.34f, 0), new Vector2(0.66f, 0), new Vector2(0, 20), new Vector2(0, 150));
            var rotR = MakeButton(res, bar.transform, "RotateRight", "SAĞA DÖN", rounded, Slate, Color.white, 38,
                new Vector2(0.70f, 0), new Vector2(0.97f, 0), new Vector2(0, 20), new Vector2(0, 150));

            var resultGo = new GameObject("ResultPanel", typeof(RectTransform));
            resultGo.transform.SetParent(cr, false);
            Stretch(resultGo);
            var dim = MakePanel(res, resultGo.transform, "Root", null, new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var card = MakePanel(res, dim.transform, "Card", rounded, new Color(0.08f, 0.10f, 0.18f, 0.98f),
                new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
            var rTitle = MakeLabel(res, card.transform, "Title", "", 80, TextAnchor.MiddleCenter, Amber,
                new Vector2(0, 0.78f), new Vector2(1, 0.95f), Vector2.zero, Vector2.zero);
            var rStars = MakeLabel(res, card.transform, "Stars", "", 48, TextAnchor.MiddleCenter, Cyan,
                new Vector2(0, 0.68f), new Vector2(1, 0.78f), Vector2.zero, Vector2.zero);
            var rBody = MakeLabel(res, card.transform, "Body", "", 42, TextAnchor.UpperCenter, new Color(0.85f, 0.9f, 1f),
                new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);
            var rRetry = MakeButton(res, card.transform, "Retry", "TEKRAR", rounded, Slate, Color.white, 44,
                new Vector2(0.06f, 0.06f), new Vector2(0.48f, 0.06f), new Vector2(0, 0), new Vector2(0, 140));
            var rNext = MakeButton(res, card.transform, "Next", "SONRAKİ", rounded, Cyan, Ink, 44,
                new Vector2(0.52f, 0.06f), new Vector2(0.94f, 0.06f), new Vector2(0, 0), new Vector2(0, 140));
            var result = resultGo.AddComponent<ResultPanel>();
            Ref(result, "root", dim);
            Ref(result, "titleLabel", rTitle.GetComponent<Text>());
            Ref(result, "bodyLabel", rBody.GetComponent<Text>());
            Ref(result, "starsLabel", rStars.GetComponent<Text>());
            Ref(result, "nextButton", rNext.GetComponent<Button>());
            Ref(result, "retryButton", rRetry.GetComponent<Button>());

            var hud = canvasGo.AddComponent<HudController>();
            Ref(hud, "level", lc);
            Ref(hud, "placement", placement);
            Ref(hud, "result", result);
            Ref(hud, "levelTitle", title.GetComponent<Text>());
            Ref(hud, "phaseLabel", phase.GetComponent<Text>());
            Ref(hud, "timerLabel", timer.GetComponent<Text>());
            Ref(hud, "darknessBar", slider);
            Ref(hud, "darknessOverlay", oi);
            Ref(hud, "startButton", start.GetComponent<Button>());
            Ref(hud, "rotateLeftButton", rotL.GetComponent<Button>());
            Ref(hud, "rotateRightButton", rotR.GetComponent<Button>());
            Ref(hud, "menuButton", menu.GetComponent<Button>());
            Ref(hud, "placementUI", bar);
            Ref(hud, "slotRoot", slotRoot.transform);
            Ref(hud, "slotPrefab", slotPrefab);
            Ref(hud, "slotSprite", rounded);
        }

        // ------------------------------------------------------------------ prefabs
        static void MakeBallPrefab(Sprite sprite, PhysicsMaterial2D mat, Material trailMat)
        {
            var go = new GameObject("EnergyBall") { layer = LayerMask.NameToLayer(LBall) };
            go.transform.localScale = Vector3.one * 0.6f;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 5;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var c = go.AddComponent<CircleCollider2D>(); c.radius = 0.3333f; c.sharedMaterial = mat;   // 0.2 world units
            go.AddComponent<EnergyBall>();

            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.45f;
            tr.minVertexDistance = 0.05f;
            tr.widthMultiplier = 0.3f;
            tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            tr.numCapVertices = 4;
            tr.sortingOrder = 4;
            tr.sharedMaterial = trailMat;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Cyan, 0f), new GradientColorKey(new Color(0.3f, 0.4f, 1f), 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            SavePrefab(go, "Level/EnergyBall");
        }

        static void MakeGearPrefab(Sprite sprite, Sprite glow, PhysicsMaterial2D mat, float radius, string name)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(LGear) };
            go.transform.localScale = Vector3.one * radius * 2f;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 1;
            go.AddComponent<Rigidbody2D>();
            var c = go.AddComponent<CircleCollider2D>(); c.radius = 0.5f; c.sharedMaterial = mat;
            go.AddComponent<PlaceablePart>();
            go.AddComponent<Gear>();

            var g = new GameObject("Glow");
            g.transform.SetParent(go.transform, false);
            g.transform.localScale = Vector3.one * 1.7f;
            var gsr = g.AddComponent<SpriteRenderer>(); gsr.sprite = glow; gsr.sortingOrder = 0; gsr.color = new Color(1, 1, 1, 0);
            var gv = go.AddComponent<GearVisual>();
            Ref(gv, "glow", gsr);
            Col(gv, "glowColor", Amber);
            SavePrefab(go, "Parts/" + name);
        }

        static void MakeMirrorPrefab(Sprite sprite, PhysicsMaterial2D mat)
        {
            var go = new GameObject("Part_Mirror") { layer = LayerMask.NameToLayer(LParts) };
            go.transform.localScale = new Vector3(1.4f, 0.15f, 1f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 1;
            go.AddComponent<Rigidbody2D>();
            var c = go.AddComponent<BoxCollider2D>(); c.size = Vector2.one; c.sharedMaterial = mat;
            go.AddComponent<PlaceablePart>();
            go.AddComponent<Deflector>();
            SavePrefab(go, "Parts/Part_Mirror");
        }

        static void MakeSpringPrefab(Sprite sprite, PhysicsMaterial2D mat)
        {
            var go = new GameObject("Part_Spring") { layer = LayerMask.NameToLayer(LParts) };
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);               // sprite is 1 x 0.5 units
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 1;
            go.AddComponent<Rigidbody2D>();
            var c = go.AddComponent<BoxCollider2D>(); c.size = new Vector2(1f, 0.5f); c.sharedMaterial = mat;
            go.AddComponent<PlaceablePart>();
            var sb = go.AddComponent<SpringBouncer>();
            Float(sb, "launchSpeed", 12f);
            Float(sb, "keepSideways", 0.15f);
            Bool(sb, "singleUse", true);
            SavePrefab(go, "Parts/Part_Spring");
        }

        static void MakeSlotPrefab(Sprite rounded)
        {
            var go = new GameObject("PartSlot", typeof(RectTransform), typeof(CanvasGroup));
            ((RectTransform)go.transform).sizeDelta = new Vector2(220, 220);
            var bg = go.AddComponent<Image>();
            bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.color = Slate;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            Place(icon, Vector2.zero, Vector2.one, new Vector2(25, 50), new Vector2(-25, -20));
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;

            var count = new GameObject("Count", typeof(RectTransform), typeof(Text));
            count.transform.SetParent(go.transform, false);
            Place(count, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 50));
            var t = count.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleCenter; t.fontSize = 40; t.color = Color.white; t.raycastTarget = false;
            t.fontStyle = FontStyle.Bold;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var slot = go.AddComponent<PartSlotUI>();
            Ref(slot, "icon", icon.GetComponent<Image>());
            Ref(slot, "countLabel", t);
            Ref(slot, "group", go.GetComponent<CanvasGroup>());
            SavePrefab(go, "UI/PartSlot");
        }

        static void SavePrefab(GameObject go, string relPath)
        {
            var path = $"{Root}/Prefabs/{relPath}.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void MakePart(string file, string display, PartKind kind, string iconSprite, string prefabName)
        {
            var d = LoadOrCreate<PartDefinition>($"{Root}/ScriptableObjects/Parts/Part_{file}.asset");
            d.displayName = display; d.kind = kind;
            d.icon = ArtFactory.Get(iconSprite);
            d.prefab = Load<GameObject>($"{Root}/Prefabs/Parts/{prefabName}.prefab").GetComponent<PlaceablePart>();
            d.rotationStep = 15f; d.canRotate = true;
            if (!d.icon || !d.prefab) Debug.LogError($"Chronos build: part '{file}' is missing its icon or prefab");
            EditorUtility.SetDirty(d);
        }

        // ------------------------------------------------------------------ project setup
        static void EnsureLayers(params string[] names)
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            foreach (var n in names)
            {
                bool exists = false;
                for (int i = 8; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == n) { exists = true; break; }
                if (exists) continue;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    var el = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(el.stringValue)) { el.stringValue = n; break; }
                }
            }
            tm.ApplyModifiedProperties();
        }

        static void SetupCollisionMatrix()
        {
            string[] all = { LBall, LParts, LGear, LStatic, LZone, LLeak };
            for (int i = 0; i < all.Length; i++)
                for (int j = i; j < all.Length; j++)
                    Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(all[i]), LayerMask.NameToLayer(all[j]), true);
            foreach (var other in new[] { LParts, LGear, LStatic, LLeak })
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(LBall), LayerMask.NameToLayer(other), false);
        }

        static PhysicsMaterial2D MakeMaterial(string name, float bounciness, float friction)
        {
            var path = $"{Root}/Physics/{name}.physicsMaterial2D";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (!m)
            {
                Directory.CreateDirectory($"{Root}/Physics");
                m = new PhysicsMaterial2D();
                AssetDatabase.CreateAsset(m, path);
            }
            m.bounciness = bounciness; m.friction = friction;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
