using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>Shared helpers for the one-click scene builders.</summary>
    public static class BuilderUtil
    {
        public const string Root = "Assets/_Project";

        public static readonly Color Cyan = new Color(0.24f, 0.88f, 0.95f);
        public static readonly Color Amber = new Color(1f, 0.78f, 0.34f);
        public static readonly Color Magenta = new Color(1f, 0.30f, 0.55f);
        public static readonly Color Ink = new Color(0.04f, 0.05f, 0.10f);
        public static readonly Color Slate = new Color(0.13f, 0.16f, 0.28f);

        // ---------------------------------------------------------------- assets
        public static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        public static void AddSceneToBuild(string path, bool first)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == path);
            var entry = new EditorBuildSettingsScene(path, true);
            if (first) list.Insert(0, entry); else list.Add(entry);
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------------------------------------------------------------- serialization
        static void Edit(Object target, string field, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"Field '{field}' not found on {target.GetType().Name}"); return; }
            set(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Ref(Object t, string f, Object v)
        {
            if (!v) Debug.LogError($"Chronos build: value for '{f}' on {t.GetType().Name} is null");
            Edit(t, f, p =>
            {
                p.objectReferenceValue = v;
                if (v && !p.objectReferenceValue) Debug.LogError($"Chronos build: could not assign '{f}' on {t.GetType().Name} ({v.GetType().Name} '{v.name}')");
            });
        }

        public static void Int(Object t, string f, int v) => Edit(t, f, p => p.intValue = v);
        public static void Float(Object t, string f, float v) => Edit(t, f, p => p.floatValue = v);
        public static void Bool(Object t, string f, bool v) => Edit(t, f, p => p.boolValue = v);
        public static void Col(Object t, string f, Color v) => Edit(t, f, p => p.colorValue = v);

        public static void RefList(Object t, string f, params Object[] v) => Edit(t, f, p =>
        {
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = v[i];
        });

        public static int LayerMaskOf(params string[] layers)
        {
            int m = 0;
            foreach (var l in layers) m |= 1 << LayerMask.NameToLayer(l);
            return m;
        }

        // ---------------------------------------------------------------- scene objects
        public static GameObject SpriteGo(string name, Sprite s, Vector3 pos, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = color; sr.sortingOrder = order;
            return go;
        }

        public static GameObject BoxGo(string name, Sprite s, Vector2 pos, Vector2 size, float angle, string layer,
                                       PhysicsMaterial2D mat, Color color, int order = 0)
        {
            var go = SpriteGo(name, s, pos, new Vector3(size.x, size.y, 1f), color, order);
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            go.layer = LayerMask.NameToLayer(layer);
            var c = go.AddComponent<BoxCollider2D>();
            c.size = Vector2.one;
            c.sharedMaterial = mat;
            return go;
        }

        // ---------------------------------------------------------------- UI
        public static DefaultControls.Resources UiResources() => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };

        public static void Place(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        public static void Stretch(GameObject go) => Place(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static GameObject MakeCanvas(string name, out Transform root)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            root = go.transform;
            return go;
        }

        public static GameObject MakeLabel(DefaultControls.Resources res, Transform parent, string name, string text, int size,
                                       TextAnchor anchor, Color color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = DefaultControls.CreateText(res);
            go.name = name; go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = size; t.alignment = anchor; t.color = color; t.raycastTarget = false;
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            Place(go, aMin, aMax, oMin, oMax);
            return go;
        }

        public static GameObject MakeButton(DefaultControls.Resources res, Transform parent, string name, string text, Sprite rounded,
                                        Color bg, Color textColor, int fontSize, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = DefaultControls.CreateButton(res);
            go.name = name; go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = rounded; img.type = Image.Type.Sliced; img.color = bg;
            var btn = go.GetComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            var t = go.GetComponentInChildren<Text>();
            t.text = text; t.fontSize = fontSize; t.color = textColor; t.fontStyle = FontStyle.Bold;
            Place(go, aMin, aMax, oMin, oMax);
            return go;
        }

        public static GameObject MakePanel(DefaultControls.Resources res, Transform parent, string name, Sprite rounded, Color color,
                                       Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = DefaultControls.CreatePanel(res);
            go.name = name; go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = rounded;
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            Place(go, aMin, aMax, oMin, oMax);
            return go;
        }

        public static void StyleSlider(Slider s, Sprite rounded, Color track, Color fill)
        {
            var bg = s.transform.Find("Background").GetComponent<Image>();
            bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.color = track;
            var fi = s.fillRect.GetComponent<Image>();
            fi.sprite = rounded; fi.type = Image.Type.Sliced; fi.color = fill;
            if (s.handleRect) s.handleRect.gameObject.SetActive(false);
        }
    }
}
