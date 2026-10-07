using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>Menu: Chronos > Build Level 01. Generates sprites, materials, prefabs, data assets and the playable scene.</summary>
    public static class Level01Builder
    {
        const string Root = "Assets/_Project";
        const string SceneName = "Level_Egypt_01";

        static string LGear, LParts, LBall, LStatic, LLeak, LZone;

        [MenuItem("Chronos/Build Level 01 (Egypt)")]
        public static void Build()
        {
            EnsureLayers("Ball", "Parts", "Gears", "Static", "PlacementZone", "Leak");
            SetupCollisionMatrix();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            var gearSprite = MakeSprite("Gear", 128, 128, 128, GearPixels);
            var ballSprite = MakeSprite("Ball", 64, 64, 64, BallPixels);
            var squareSprite = MakeSprite("Square", 4, 4, 4, (x, y, w, h) => new Color32(255, 255, 255, 255));
            var mirrorSprite = MakeSprite("Mirror", 4, 4, 4, (x, y, w, h) => new Color32(120, 220, 255, 255));

            var matBall = MakeMaterial("Ball", 0.35f, 0.2f);
            var matGear = MakeMaterial("Gear", 0.1f, 0.8f);
            var matMirror = MakeMaterial("Mirror", 0f, 0f);
            var matStatic = MakeMaterial("Static", 0.1f, 0.3f);

            // ---- prefabs
            var ballPrefab = MakeBallPrefab(ballSprite, matBall);
            var gearPrefab = MakeGearPrefab(gearSprite, matGear, 0.6f, "Part_Gear");
            var mirrorPrefab = MakeMirrorPrefab(mirrorSprite, matMirror);
            var slotPrefab = MakeSlotPrefab();

            // Re-load prefabs from disk so every asset stores a reference to the real saved object.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            gearPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Parts/Part_Gear.prefab").GetComponent<PlaceablePart>();
            mirrorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Parts/Part_Mirror.prefab").GetComponent<PlaceablePart>();
            if (!gearPrefab || !mirrorPrefab) { Debug.LogError("Chronos build: part prefabs failed to reload"); return; }

            // ---- data
            var gearDef = MakePart("Gear_Small", "Küçük Dişli", PartKind.Gear, gearSprite, gearPrefab, 15f);
            var mirrorDef = MakePart("Mirror", "Ayna", PartKind.Mirror, mirrorSprite, mirrorPrefab, 15f);

            var level = LoadOrCreate<LevelData>($"{Root}/ScriptableObjects/Levels/Level_Egypt_01.asset");
            level.id = "egypt_01";
            level.displayName = "Kum Saati I";
            level.sceneName = SceneName;
            level.parts = new System.Collections.Generic.List<PartAllotment>
            {
                new PartAllotment { part = gearDef, count = 2 },
                new PartAllotment { part = mirrorDef, count = 1 },
            };
            level.placementTimeLimit = 60f;
            level.runTimeLimit = 20f;
            level.rules = new EraRules();
            level.storyFragment = "Kum saatinin ilk çarkı yeniden dönüyor. Nil kıyısında zaman, uzun bir uykudan sonra derin bir nefes aldı.";
            EditorUtility.SetDirty(level);

            var era = LoadOrCreate<EraData>($"{Root}/ScriptableObjects/Eras/Era_Egypt.asset");
            era.eraName = "Antik Mısır - Kum Saati";
            era.levels = new System.Collections.Generic.List<LevelData> { level };
            EditorUtility.SetDirty(era);

            var campaign = LoadOrCreate<CampaignData>("Assets/Resources/Campaign.asset");
            campaign.eras = new System.Collections.Generic.List<EraData> { era };
            EditorUtility.SetDirty(campaign);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Re-load everything from disk so the scene stores references to the real saved assets.
            level = AssetDatabase.LoadAssetAtPath<LevelData>($"{Root}/ScriptableObjects/Levels/Level_Egypt_01.asset");
            ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Level/EnergyBall.prefab").GetComponent<EnergyBall>();
            slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/UI/PartSlot.prefab").GetComponent<PartSlotUI>();
            if (!level || !ballPrefab || !slotPrefab)
            {
                Debug.LogError($"Chronos build failed to reload assets: level={level} ball={ballPrefab} slot={slotPrefab}");
                return;
            }

            BuildScene(level, ballPrefab, slotPrefab, gearSprite, squareSprite, matStatic, matGear);
            Debug.Log("Chronos: Level 01 built. Open Assets/_Project/Scenes/" + SceneName + ".unity, set Game view to a portrait aspect (9:16) and press Play.");
        }

        // ------------------------------------------------------------------ scene
        static void BuildScene(LevelData level, EnergyBall ballPrefab, PartSlotUI slotPrefab,
                               Sprite gearSprite, Sprite square, PhysicsMaterial2D matStatic, PhysicsMaterial2D matGear)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene unloads unused assets, which turns every asset reference we hold into null.
            // Re-load everything from disk now.
            level = AssetDatabase.LoadAssetAtPath<LevelData>($"{Root}/ScriptableObjects/Levels/Level_Egypt_01.asset");
            ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Level/EnergyBall.prefab").GetComponent<EnergyBall>();
            slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/UI/PartSlot.prefab").GetComponent<PartSlotUI>();
            gearSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Gear.png");
            square = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Square.png");
            matStatic = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>($"{Root}/Physics/Static.physicsMaterial2D");
            matGear = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>($"{Root}/Physics/Gear.physicsMaterial2D");

            // camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.08f, 0.14f);
            camGo.transform.position = new Vector3(0f, -1f, -10f);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            // decoration: clock face
            var face = Sprite("ClockFace", square, new Vector3(0.2f, -0.5f), new Vector3(8f, 11f), new Color(0.35f, 0.27f, 0.12f, 0.35f), -20);

            // static geometry
            Box("WallLeft", square, new Vector2(-3.3f, -0.5f), new Vector2(0.2f, 15f), 0f, LStatic, matStatic, new Color(0.55f, 0.45f, 0.3f));
            Box("WallRight", square, new Vector2(3.3f, -0.5f), new Vector2(0.2f, 15f), 0f, LStatic, matStatic, new Color(0.55f, 0.45f, 0.3f));
            Box("Floor", square, new Vector2(1.9f, -4.9f), new Vector2(2.6f, 0.2f), -6f, LStatic, matStatic, new Color(0.55f, 0.45f, 0.3f));

            // placement zone
            var zone = new GameObject("PlacementZone") { layer = LayerMask.NameToLayer(LZone) };
            zone.transform.position = new Vector3(0.8f, 0.5f);
            zone.AddComponent<BoxCollider2D>().size = new Vector2(4.4f, 4f);
            zone.AddComponent<PlacementZone>();
            Sprite("ZoneVisual", square, new Vector3(0.8f, 0.5f), new Vector3(4.4f, 4f), new Color(0.4f, 1f, 0.6f, 0.08f), -10);

            // required gear
            var g0 = new GameObject("RequiredGear") { layer = LayerMask.NameToLayer(LGear) };
            g0.transform.position = new Vector3(1.5f, -2f);
            g0.transform.localScale = Vector3.one * 1.6f;
            var sr = g0.AddComponent<SpriteRenderer>(); sr.sprite = gearSprite; sr.color = new Color(1f, 0.55f, 0.25f);
            g0.AddComponent<Rigidbody2D>();
            var cc = g0.AddComponent<CircleCollider2D>(); cc.radius = 0.5f; cc.sharedMaterial = matGear;
            var gear0 = g0.AddComponent<Gear>();

            // exit gate: trigger + solid door
            var gateGo = new GameObject("ExitGate") { layer = LayerMask.NameToLayer(LStatic) };
            gateGo.transform.position = new Vector3(3.0f, -4.35f);
            var gt = gateGo.AddComponent<BoxCollider2D>(); gt.isTrigger = true; gt.size = new Vector2(0.5f, 1.4f);
            var gate = gateGo.AddComponent<ExitGate>();
            Sprite("GateGlow", square, gateGo.transform.position, new Vector3(0.5f, 1.4f), new Color(0.3f, 1f, 0.5f, 0.35f), -5);
            var door = Box("GateDoor", square, new Vector2(2.7f, -4.35f), new Vector2(0.2f, 1.6f), 0f, LStatic, matStatic, new Color(0.9f, 0.25f, 0.25f));

            var mech = new GameObject("ClockMechanism").AddComponent<ClockMechanism>();
            Ref(mech, "gate", gate);
            RefList(mech, "requiredGears", gear0);
            Ref(gate, "door", door.GetComponent<Collider2D>());

            // leak zone
            var leak = new GameObject("LeakZone") { layer = LayerMask.NameToLayer(LLeak) };
            leak.transform.position = new Vector3(0f, -8f);
            leak.AddComponent<BoxCollider2D>().size = new Vector2(10f, 2f);
            leak.AddComponent<LeakZone>();

            // ball spawn
            var spawn = new GameObject("BallSpawn");
            spawn.transform.position = new Vector3(0.2f, 5.2f);
            Sprite("SpawnMarker", gearSprite, spawn.transform.position, Vector3.one * 0.5f, new Color(1f, 1f, 1f, 0.3f), -5);

            // controllers
            var placementGo = new GameObject("Placement");
            var placement = placementGo.AddComponent<PlacementController>();
            Ref(placement, "cam", cam);
            int parts = Mask(LParts, LGear);
            Int(placement, "partsMask", parts);
            Int(placement, "blockingMask", parts | Mask(LStatic));
            Float(placement, "gridSnap", 0.25f);

            var lc = new GameObject("LevelController").AddComponent<LevelController>();
            Ref(lc, "fallbackLevel", level);
            Ref(lc, "ballPrefab", ballPrefab);
            Ref(lc, "ballSpawn", spawn.transform);
            Ref(lc, "mechanism", mech);
            Ref(lc, "gate", gate);
            Ref(lc, "placement", placement);

            BuildUI(lc, placement, slotPrefab);

            var path = $"{Root}/Scenes/{SceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        }

        static void BuildUI(LevelController lc, PlacementController placement, PartSlotUI slotPrefab)
        {
            var res = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            };

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var cr = canvasGo.transform;

            // darkness overlay (never blocks input)
            var overlay = DefaultControls.CreatePanel(res);
            overlay.name = "DarknessOverlay"; overlay.transform.SetParent(cr, false);
            Stretch(overlay);
            var oi = overlay.GetComponent<Image>(); oi.color = new Color(0, 0, 0, 0); oi.raycastTarget = false;

            var title = Label(res, cr, "LevelTitle", "", 56, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(30, -120), new Vector2(0, -30));
            var phase = Label(res, cr, "PhaseLabel", "", 44, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(30, -200), new Vector2(0, -120));
            var timer = Label(res, cr, "Timer", "", 96, TextAnchor.UpperRight, new Vector2(0.6f, 1), new Vector2(1, 1), new Vector2(0, -200), new Vector2(-30, -20));

            var sliderGo = DefaultControls.CreateSlider(res);
            sliderGo.name = "DarknessBar"; sliderGo.transform.SetParent(cr, false);
            Place(sliderGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -250), new Vector2(-40, -215));
            var slider = sliderGo.GetComponent<Slider>(); slider.interactable = false; slider.value = 0;

            // bottom placement bar
            var bar = DefaultControls.CreatePanel(res);
            bar.name = "PlacementUI"; bar.transform.SetParent(cr, false);
            Place(bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 430));
            bar.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.08f, 0.85f);

            var slotRoot = new GameObject("Slots", typeof(RectTransform));
            slotRoot.transform.SetParent(bar.transform, false);
            Place(slotRoot, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 160), new Vector2(-20, -15));
            var hl = slotRoot.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 40;
            hl.childControlWidth = false; hl.childControlHeight = false;

            var rotL = Btn(res, bar.transform, "RotateLeft", "Döndür -", new Vector2(0.02f, 0), new Vector2(0.30f, 0), new Vector2(0, 15), new Vector2(0, 140));
            var start = Btn(res, bar.transform, "Start", "BAŞLAT", new Vector2(0.34f, 0), new Vector2(0.66f, 0), new Vector2(0, 15), new Vector2(0, 140));
            var rotR = Btn(res, bar.transform, "RotateRight", "Döndür +", new Vector2(0.70f, 0), new Vector2(0.98f, 0), new Vector2(0, 15), new Vector2(0, 140));
            start.GetComponent<Image>().color = new Color(0.4f, 0.9f, 0.5f);

            // result panel
            var resultGo = new GameObject("ResultPanel", typeof(RectTransform));
            resultGo.transform.SetParent(cr, false);
            Stretch(resultGo);
            var panel = DefaultControls.CreatePanel(res);
            panel.name = "Root"; panel.transform.SetParent(resultGo.transform, false);
            Stretch(panel);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            var rTitle = Label(res, panel.transform, "Title", "", 90, TextAnchor.MiddleCenter, new Vector2(0, 0.7f), new Vector2(1, 0.8f), Vector2.zero, Vector2.zero);
            var rStars = Label(res, panel.transform, "Stars", "", 60, TextAnchor.MiddleCenter, new Vector2(0, 0.62f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);
            var rBody = Label(res, panel.transform, "Body", "", 48, TextAnchor.UpperCenter, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.6f), Vector2.zero, Vector2.zero);
            var rRetry = Btn(res, panel.transform, "Retry", "Tekrar", new Vector2(0.08f, 0.2f), new Vector2(0.48f, 0.2f), new Vector2(0, 0), new Vector2(0, 150));
            var rNext = Btn(res, panel.transform, "Next", "Sonraki", new Vector2(0.52f, 0.2f), new Vector2(0.92f, 0.2f), new Vector2(0, 0), new Vector2(0, 150));
            var result = resultGo.AddComponent<ResultPanel>();
            Ref(result, "root", panel);
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
            Ref(hud, "placementUI", bar);
            Ref(hud, "slotRoot", slotRoot.transform);
            Ref(hud, "slotPrefab", slotPrefab);
        }

        // ------------------------------------------------------------------ prefabs
        static EnergyBall MakeBallPrefab(Sprite sprite, PhysicsMaterial2D mat)
        {
            var go = new GameObject("EnergyBall") { layer = LayerMask.NameToLayer(LBall) };
            go.transform.localScale = Vector3.one * 0.4f;
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var c = go.AddComponent<CircleCollider2D>(); c.radius = 0.5f; c.sharedMaterial = mat;
            go.AddComponent<EnergyBall>();
            return SavePrefab(go, "Level/EnergyBall").GetComponent<EnergyBall>();
        }

        static PlaceablePart MakeGearPrefab(Sprite sprite, PhysicsMaterial2D mat, float radius, string name)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(LGear) };
            go.transform.localScale = Vector3.one * radius * 2f;
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            go.AddComponent<Rigidbody2D>();
            var c = go.AddComponent<CircleCollider2D>(); c.radius = 0.5f; c.sharedMaterial = mat;
            go.AddComponent<PlaceablePart>();
            go.AddComponent<Gear>();
            return SavePrefab(go, "Parts/" + name).GetComponent<PlaceablePart>();
        }

        static PlaceablePart MakeMirrorPrefab(Sprite sprite, PhysicsMaterial2D mat)
        {
            var go = new GameObject("Part_Mirror") { layer = LayerMask.NameToLayer(LParts) };
            go.transform.localScale = new Vector3(1.4f, 0.15f, 1f);
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            go.AddComponent<Rigidbody2D>();
            var c = go.AddComponent<BoxCollider2D>(); c.size = Vector2.one; c.sharedMaterial = mat;
            go.AddComponent<PlaceablePart>();
            go.AddComponent<Deflector>();
            return SavePrefab(go, "Parts/Part_Mirror").GetComponent<PlaceablePart>();
        }

        static PartSlotUI MakeSlotPrefab()
        {
            var go = new GameObject("PartSlot", typeof(RectTransform), typeof(CanvasGroup));
            ((RectTransform)go.transform).sizeDelta = new Vector2(220, 220);
            var bg = go.AddComponent<Image>();
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.25f, 0.2f, 0.35f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            Place(icon, Vector2.zero, Vector2.one, new Vector2(25, 40), new Vector2(-25, -20));
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;

            var count = new GameObject("Count", typeof(RectTransform), typeof(Text));
            count.transform.SetParent(go.transform, false);
            Place(count, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 50));
            var t = count.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleCenter; t.fontSize = 40; t.color = Color.white; t.raycastTarget = false;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var slot = go.AddComponent<PartSlotUI>();
            Ref(slot, "icon", icon.GetComponent<Image>());
            Ref(slot, "countLabel", t);
            Ref(slot, "group", go.GetComponent<CanvasGroup>());
            return SavePrefab(go, "UI/PartSlot").GetComponent<PartSlotUI>();
        }

        static GameObject SavePrefab(GameObject go, string relPath)
        {
            var path = $"{Root}/Prefabs/{relPath}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        static PartDefinition MakePart(string file, string display, PartKind kind, Sprite icon, PlaceablePart prefab, float step)
        {
            var d = LoadOrCreate<PartDefinition>($"{Root}/ScriptableObjects/Parts/Part_{file}.asset");
            d.displayName = display; d.kind = kind; d.icon = icon; d.prefab = prefab; d.rotationStep = step; d.canRotate = true;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ------------------------------------------------------------------ scene helpers
        static GameObject Sprite(string name, Sprite s, Vector3 pos, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = pos; go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.color = color; sr.sortingOrder = order;
            return go;
        }

        static GameObject Box(string name, Sprite s, Vector2 pos, Vector2 size, float angle, string layer, PhysicsMaterial2D mat, Color color)
        {
            var go = Sprite(name, s, pos, new Vector3(size.x, size.y, 1f), color, 0);
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            go.layer = LayerMask.NameToLayer(layer);
            var c = go.AddComponent<BoxCollider2D>(); c.size = Vector2.one; c.sharedMaterial = mat;
            return go;
        }

        // ------------------------------------------------------------------ UI helpers
        static void Place(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        static void Stretch(GameObject go) => Place(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        static GameObject Label(DefaultControls.Resources res, Transform parent, string name, string text, int size, TextAnchor anchor,
                                Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = DefaultControls.CreateText(res);
            go.name = name; go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            Place(go, aMin, aMax, oMin, oMax);
            return go;
        }

        static GameObject Btn(DefaultControls.Resources res, Transform parent, string name, string text,
                              Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = DefaultControls.CreateButton(res);
            go.name = name; go.transform.SetParent(parent, false);
            var t = go.GetComponentInChildren<Text>(); t.text = text; t.fontSize = 44;
            Place(go, aMin, aMax, oMin, oMax);
            return go;
        }

        // ------------------------------------------------------------------ serialization helpers
        static void Edit(Object target, string field, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"Field '{field}' not found on {target.GetType().Name}"); return; }
            set(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Ref(Object t, string f, Object v)
        {
            if (!v) Debug.LogError($"Chronos build: value for '{f}' on {t.GetType().Name} is null");
            Edit(t, f, p =>
            {
                p.objectReferenceValue = v;
                if (v && !p.objectReferenceValue) Debug.LogError($"Chronos build: could not assign '{f}' on {t.GetType().Name} ({v.GetType().Name} '{v.name}')");
            });
        }
        static void Int(Object t, string f, int v) => Edit(t, f, p => p.intValue = v);
        static void Float(Object t, string f, float v) => Edit(t, f, p => p.floatValue = v);
        static void RefList(Object t, string f, params Object[] v) => Edit(t, f, p =>
        {
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = v[i];
        });

        static int Mask(params string[] layers)
        {
            int m = 0;
            foreach (var l in layers) m |= 1 << LayerMask.NameToLayer(l);
            return m;
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
            LBall = "Ball"; LParts = "Parts"; LGear = "Gears"; LStatic = "Static"; LZone = "PlacementZone"; LLeak = "Leak";
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

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static PhysicsMaterial2D MakeMaterial(string name, float bounciness, float friction)
        {
            var m = LoadOrCreate2D($"{Root}/Physics/{name}.physicsMaterial2D");
            m.bounciness = bounciness; m.friction = friction;
            EditorUtility.SetDirty(m);
            return m;
        }

        static PhysicsMaterial2D LoadOrCreate2D(string path)
        {
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (m) return m;
            m = new PhysicsMaterial2D();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ procedural sprites
        delegate Color32 PixelFn(int x, int y, int w, int h);

        static Sprite MakeSprite(string name, int w, int h, int ppu, PixelFn fn)
        {
            var path = $"{Root}/Art/{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) px[y * w + x] = fn(x, y, w, h);
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = ppu;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color32 GearPixels(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f - 1f;
            float dx = x - cx, dy = y - cy, r = Mathf.Sqrt(dx * dx + dy * dy);
            float theta = Mathf.Atan2(dy, dx);
            float outer = R * (0.82f + (Mathf.Sin(12f * theta) > 0f ? 0.18f : 0f));
            float a = Mathf.Clamp01(outer - r + 0.5f);
            if (r < R * 0.14f) a = 0f;                       // axle hole
            bool ring = r < outer * 0.72f && r > outer * 0.62f;
            var c = ring ? new Color(0.55f, 0.38f, 0.1f) : new Color(0.9f, 0.7f, 0.25f);
            return new Color(c.r, c.g, c.b, a);
        }

        static Color32 BallPixels(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f - 1f;
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float a = Mathf.Clamp01(R - r + 0.5f);
            float glow = 1f - r / R;
            return new Color(0.4f + 0.6f * glow, 0.9f, 1f, a);
        }
    }
}
