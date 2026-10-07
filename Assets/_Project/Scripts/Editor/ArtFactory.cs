using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>Generates the project's placeholder art procedurally: smooth gears, glows, rings, gradients, rounded UI shapes.</summary>
    public static class ArtFactory
    {
        public const string GearAmber = "GearAmber";
        public const string GearCyan = "GearCyan";
        public const string GearSlate = "GearSlate";
        public const string Ball = "Ball";
        public const string Glow = "Glow";
        public const string Ring = "ClockRing";
        public const string Backdrop = "Backdrop";
        public const string Square = "Square";
        public const string Rounded = "Rounded";
        public const string Mirror = "Mirror";
        public const string Spring = "SpringPad";

        delegate Color32 PixelFn(int x, int y, int w, int h);

        public static Sprite Get(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{BuilderUtil.Root}/Art/{name}.png");

        public static void EnsureAll()
        {
            Directory.CreateDirectory($"{BuilderUtil.Root}/Art");
            Make(GearAmber, 256, 256, 256, GearPixels(new Color(1f, 0.84f, 0.45f), new Color(0.95f, 0.45f, 0.2f)));
            Make(GearCyan, 256, 256, 256, GearPixels(new Color(0.55f, 0.97f, 1f), new Color(0.15f, 0.45f, 0.95f)));
            Make(GearSlate, 256, 256, 256, GearPixels(new Color(0.45f, 0.5f, 0.7f), new Color(0.2f, 0.23f, 0.4f)));
            Make(Ball, 128, 128, 128, BallPixels);
            Make(Glow, 128, 128, 128, GlowPixels);
            Make(Ring, 512, 512, 512, RingPixels);
            Make(Backdrop, 4, 256, 100, BackdropPixels);
            Make(Square, 4, 4, 4, (x, y, w, h) => new Color32(255, 255, 255, 255), point: true);
            Make(Mirror, 4, 4, 4, (x, y, w, h) => new Color32(150, 235, 255, 245), point: true);
            Make(Rounded, 64, 64, 64, RoundedPixels, border: 24);
            Make(Spring, 64, 32, 64, SpringPixels);
        }

        public static Material TrailMaterial()
        {
            var path = $"{BuilderUtil.Root}/Art/Trail.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;
            m = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void Make(string name, int w, int h, int ppu, PixelFn fn, bool point = false, int border = 0)
        {
            var path = $"{BuilderUtil.Root}/Art/{name}.png";
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
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            imp.spriteBorder = new Vector4(border, border, border, border);
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
        }

        // ---- pixel shaders -------------------------------------------------------------
        static PixelFn GearPixels(Color top, Color bottom) => (x, y, w, h) =>
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f - 2f;
            float dx = x - cx, dy = y - cy;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float theta = Mathf.Atan2(dy, dx);

            float tooth = Mathf.SmoothStep(-0.2f, 0.2f, Mathf.Cos(14f * theta));   // flat, rounded teeth
            float outer = R * (0.84f + 0.16f * tooth);
            float a = Mathf.Clamp01(outer - r + 0.75f);

            a *= Mathf.Clamp01(r - R * 0.11f + 0.75f);                              // axle hole
            for (int k = 0; k < 5; k++)                                             // lightening holes
            {
                float ang = k * Mathf.PI * 2f / 5f + 0.3f;
                float hx = Mathf.Cos(ang) * R * 0.5f, hy = Mathf.Sin(ang) * R * 0.5f;
                float d = Mathf.Sqrt((dx - hx) * (dx - hx) + (dy - hy) * (dy - hy));
                a *= Mathf.Clamp01(d - R * 0.15f + 0.75f);
            }

            float t = Mathf.Clamp01(0.5f + (-dx + dy) / (2f * R) * 0.7f);          // light from the top-left
            Color col = Color.Lerp(bottom, top, t);
            float groove = Mathf.Clamp01(1f - Mathf.Abs(r - R * 0.72f) / 1.5f) * 0.25f;
            col = Color.Lerp(col, Color.black, groove);
            if (r < R * 0.24f) col = Color.Lerp(col, Color.white, 0.12f);
            return new Color(col.r, col.g, col.b, a);
        };

        static Color32 BallPixels(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f - 1f;
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float core = Mathf.Clamp01(1f - r / (R * 0.38f));
            float glow = Mathf.Pow(Mathf.Clamp01(1f - r / R), 2.2f);
            Color c = Color.Lerp(new Color(0.25f, 0.9f, 1f), Color.white, core);
            return new Color(c.r, c.g, c.b, Mathf.Clamp01(glow * 1.4f + core));
        }

        static Color32 GlowPixels(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f - 1f;
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - r / R), 2f));
        }

        static Color32 RingPixels(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, R = w * 0.5f - 2f;
            float dx = x - cx, dy = y - cy;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float theta = Mathf.Atan2(dy, dx);
            float a = 0f;
            a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(r - R * 0.93f) / 1.6f));                  // outer ring
            float tickAngle = Mathf.Abs(Mathf.Repeat(theta + Mathf.PI / 60f, Mathf.PI / 6f) - Mathf.PI / 60f);   // 12 big ticks
            float arc = tickAngle * r;
            if (r > R * 0.82f && r < R * 0.91f) a = Mathf.Max(a, Mathf.Clamp01(1f - arc / 2.5f));
            float minor = Mathf.Abs(Mathf.Repeat(theta + Mathf.PI / 60f, Mathf.PI / 30f) - Mathf.PI / 60f);   // 60 small ticks
            if (r > R * 0.87f && r < R * 0.91f) a = Mathf.Max(a, Mathf.Clamp01(1f - minor * r / 1.2f) * 0.6f);
            return new Color(1f, 1f, 1f, a);
        }

        static Color32 BackdropPixels(int x, int y, int w, int h)
        {
            float t = y / (float)(h - 1);
            Color top = new Color(0.10f, 0.12f, 0.24f);
            Color bottom = new Color(0.03f, 0.04f, 0.09f);
            return Color.Lerp(bottom, top, t);
        }

        // Magenta pad with a white chevron pointing along the launch direction (+Y).
        static Color32 SpringPixels(int x, int y, int w, int h)
        {
            float px = Mathf.Abs(x - (w - 1) * 0.5f) - ((w - 1) * 0.5f - 10f);
            float py = Mathf.Abs(y - (h - 1) * 0.5f) - ((h - 1) * 0.5f - 10f);
            float d = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f)) - 10f;
            float a = Mathf.Clamp01(0.5f - d);
            Color col = Color.Lerp(new Color(0.85f, 0.2f, 0.45f), new Color(1f, 0.45f, 0.65f), y / (float)(h - 1));
            float line = 22f - Mathf.Abs(x - (w - 1) * 0.5f) * 0.8f;
            float chev = Mathf.Clamp01(1.6f - Mathf.Abs(y - line)) * (Mathf.Abs(x - (w - 1) * 0.5f) < 14f ? 1f : 0f);
            col = Color.Lerp(col, Color.white, chev * 0.9f);
            return new Color(col.r, col.g, col.b, a);
        }

        static Color32 RoundedPixels(int x, int y, int w, int h)
        {
            float radius = 20f;
            float px = Mathf.Abs(x - (w - 1) * 0.5f) - ((w - 1) * 0.5f - radius);
            float py = Mathf.Abs(y - (h - 1) * 0.5f) - ((h - 1) * 0.5f - radius);
            float d = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f)) - radius;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
        }
    }
}
