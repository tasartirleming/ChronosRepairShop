using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ChronosRepairShop.EditorTools.BuilderUtil;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>Menu: Chronos > Build Main Menu. Title screen with slowly meshing background gears.</summary>
    public static class MainMenuBuilder
    {
        const string ScenePath = Root + "/Scenes/MainMenu.unity";

        [MenuItem("Chronos/Build Main Menu")]
        public static void Build()
        {
            ArtFactory.EnsureAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var square = ArtFactory.Get(ArtFactory.Square);
            var rounded = ArtFactory.Get(ArtFactory.Rounded);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            SpriteGo("Backdrop", ArtFactory.Get(ArtFactory.Backdrop), Vector3.zero, new Vector3(600f, 10.5f), Color.white, -100);
            SpriteGo("ClockRing", ArtFactory.Get(ArtFactory.Ring), new Vector3(0f, -0.5f), Vector3.one * 12.5f, new Color(1f, 1f, 1f, 0.12f), -50);

            // two chains of meshing gears, drifting slowly
            GearChain("TopChain", new Vector2(-3.0f, 5.6f), new[] { 1.9f, 1.2f, 1.7f, 1.0f, 1.5f }, new[] { -30f, -70f, -15f, -55f }, 9f);
            GearChain("BottomChain", new Vector2(3.2f, -5.8f), new[] { 2.1f, 1.3f, 1.8f, 1.1f }, new[] { 150f, 190f, 160f }, -8f);

            BuildUI(rounded, square);

            System.IO.Directory.CreateDirectory(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath, true);
            Debug.Log("Chronos: MainMenu built and placed first in Build Settings. Open it and press Play.");
        }

        // Gears placed rim to rim; neighbours counter-rotate with speeds inversely proportional to their radius.
        static void GearChain(string name, Vector2 start, float[] radii, float[] anglesDeg, float baseSpeed)
        {
            var root = new GameObject(name).transform;
            Vector2 pos = start;
            float speed = baseSpeed;
            for (int i = 0; i < radii.Length; i++)
            {
                if (i > 0)
                {
                    float a = anglesDeg[i - 1] * Mathf.Deg2Rad;
                    float dist = radii[i - 1] + radii[i] - 0.12f;
                    pos += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
                    speed = -speed * radii[i - 1] / radii[i];
                }
                string sprite = i % 2 == 0 ? ArtFactory.GearSlate : ArtFactory.GearCyan;
                var alpha = i % 2 == 0 ? 0.35f : 0.22f;
                var g = SpriteGo("Gear" + i, ArtFactory.Get(sprite), pos, Vector3.one * radii[i] * 2f, new Color(1f, 1f, 1f, alpha), -30);
                g.transform.SetParent(root, true);
                g.transform.rotation = Quaternion.Euler(0, 0, i * 360f / 28f);   // half a tooth offset so teeth interlock
                g.AddComponent<Spinner>().DegreesPerSecond = speed;
            }
        }

        static void BuildUI(Sprite rounded, Sprite square)
        {
            var res = UiResources();
            var canvasGo = MakeCanvas("Canvas", out var cr);

            MakeLabel(res, cr, "TitleTop", "CHRONOS", 150, TextAnchor.MiddleCenter, Amber,
                new Vector2(0, 0.66f), new Vector2(1, 0.80f), Vector2.zero, Vector2.zero);
            MakeLabel(res, cr, "TitleBottom", "REPAIR SHOP", 70, TextAnchor.MiddleCenter, Cyan,
                new Vector2(0, 0.605f), new Vector2(1, 0.67f), Vector2.zero, Vector2.zero);
            MakeLabel(res, cr, "Tagline", "Kırık zamanı tamir et.", 38, TextAnchor.MiddleCenter, new Color(0.65f, 0.72f, 0.9f),
                new Vector2(0, 0.56f), new Vector2(1, 0.605f), Vector2.zero, Vector2.zero);

            var eraLabel = MakeLabel(res, cr, "EraLabel", "", 42, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);

            var track = MakePanel(res, cr, "ProgressTrack", rounded, new Color(1, 1, 1, 0.10f),
                new Vector2(0.15f, 0.385f), new Vector2(0.85f, 0.385f), new Vector2(0, 0), new Vector2(0, 18));
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            Stretch(fillGo);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = square; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0; fill.fillAmount = 0f; fill.color = Amber; fill.raycastTarget = false;

            var progressLabel = MakeLabel(res, cr, "ProgressLabel", "", 32, TextAnchor.MiddleCenter, new Color(0.65f, 0.72f, 0.9f),
                new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.38f), Vector2.zero, Vector2.zero);

            var play = MakeButton(res, cr, "PlayButton", "OYNA", rounded, Cyan, Ink, 72,
                new Vector2(0.2f, 0.20f), new Vector2(0.8f, 0.20f), new Vector2(0, 0), new Vector2(0, 170));
            var quit = MakeButton(res, cr, "QuitButton", "ÇIKIŞ", rounded, Slate, Color.white, 40,
                new Vector2(0.3f, 0.11f), new Vector2(0.7f, 0.11f), new Vector2(0, 0), new Vector2(0, 110));
            MakeLabel(res, cr, "Version", "prototip v0.1", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.3f),
                new Vector2(0, 0), new Vector2(1, 0.05f), Vector2.zero, Vector2.zero);

            var ctrl = canvasGo.AddComponent<MainMenuController>();
            Ref(ctrl, "playButton", play.GetComponent<Button>());
            Ref(ctrl, "quitButton", quit.GetComponent<Button>());
            Ref(ctrl, "playLabel", play.GetComponentInChildren<Text>());
            Ref(ctrl, "eraLabel", eraLabel.GetComponent<Text>());
            Ref(ctrl, "progressLabel", progressLabel.GetComponent<Text>());
            Ref(ctrl, "progressFill", fill);
        }
    }
}
