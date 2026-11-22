using System.IO;
using UnityEditor;
using UnityEngine;

namespace Babylon.EditorTools
{
    /// <summary>Procedural, tileable textures (albedo + height-derived normal maps) saved as PNGs.</summary>
    public static class TextureGen
    {
        public const string Dir = "Assets/_Babylon/Generated/Textures";
        const int S = 512;

        // ---------- tileable value noise ----------
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        static float VNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int Wrap(int v) => ((v % period) + period) % period;
            float a = Hash(Wrap(x0), Wrap(y0), seed), b = Hash(Wrap(x0 + 1), Wrap(y0), seed);
            float c = Hash(Wrap(x0), Wrap(y0 + 1), seed), d = Hash(Wrap(x0 + 1), Wrap(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal noise in [0,1], tileable over u,v in [0,1).</summary>
        public static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0, amp = 0.5f, norm = 0; int p = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += VNoise(u * p, v * p, p, seed + o * 17) * amp;
                norm += amp; amp *= 0.5f; p *= 2;
            }
            return sum / norm;
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        // ---------- output ----------
        public static Texture2D Save(string name, Color[] px, bool normal = false, bool alpha = false, int size = S)
        {
            Directory.CreateDirectory(Dir);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            string path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 4;
            imp.alphaIsTransparency = alpha;
            imp.sRGBTexture = !normal;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Color[] NormalFromHeight(float[] h, float strength, int size = S)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float L = h[y * size + (x - 1 + size) % size], R = h[y * size + (x + 1) % size];
                    float D = h[((y - 1 + size) % size) * size + x], U = h[((y + 1) % size) * size + x];
                    var n = new Vector3((L - R) * strength, (D - U) * strength, 1f).normalized;
                    px[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1);
                }
            return px;
        }

        /// <summary>Generates a texture pair: name (albedo) and name_N (normal).</summary>
        static void Pair(string name, System.Func<float, float, (Color c, float h)> f, float normalStrength)
        {
            var px = new Color[S * S]; var h = new float[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var r = f((float)x / S, (float)y / S);
                    px[y * S + x] = r.c; h[y * S + x] = r.h;
                }
            Save(name, px);
            Save(name + "_N", NormalFromHeight(h, normalStrength), true);
        }

        // ---------- patterns ----------
        static (Color, float) Bricks(float u, float v, int rows, int cols, Color baseC, Color dark, Color mortar, float mortarW, int seed)
        {
            float rv = v * rows; int row = Mathf.FloorToInt(rv);
            float cu = u * cols + (row % 2) * 0.5f; int col = Mathf.FloorToInt(cu);
            float fx = cu - col, fy = rv - row;
            float edge = Mathf.Min(Mathf.Min(fx, 1 - fx) * 2f / cols * rows, Mathf.Min(fy, 1 - fy));
            float n = Fbm(u, v, 8, 5, seed);
            float chip = Fbm(u, v, 32, 2, seed + 9) * 0.06f;
            float brickTint = Hash(((col % cols) + cols) % cols, row, seed) * 0.35f;
            bool isMortar = edge < mortarW + chip;
            Color c = isMortar ? Mul(mortar, 0.85f + n * 0.3f) : Color.Lerp(baseC, dark, brickTint + n * 0.4f);
            float hgt = isMortar ? 0.0f : 0.55f + n * 0.45f - Mathf.Clamp01((mortarW + 0.06f - edge) * 8f) * 0.3f;
            return (c, hgt);
        }

        public static void GenerateAll()
        {
            Color sand = Hex("#C9A46A"), sandDark = Hex("#8A6A3F"), shadow = Hex("#5A4127");
            Color lapis = Hex("#234E9C"), lapisDeep = Hex("#142659"), gold = Hex("#D4A537"), goldB = Hex("#F2C14E");

            Pair("Sandstone", (u, v) => Bricks(u, v, 8, 4, sand, sandDark, shadow, 0.05f, 11), 6f);
            Pair("MudBrick", (u, v) => Bricks(u, v, 12, 6, Hex("#B48A58"), Hex("#7A5734"), Hex("#4E3822"), 0.07f, 23), 7f);

            Pair("CarvedWall", (u, v) =>
            {
                var (c, h) = Bricks(u, v, 4, 2, sand, sandDark, shadow, 0.025f, 31);
                float w = Cuneiform(u, v, 18, 41);
                if (w > 0) { c = Color.Lerp(c, shadow, 0.55f * w); h -= 0.35f * w; }
                return (c, h);
            }, 8f);

            Pair("LapisTile", (u, v) =>
            {
                int tiles = 8; float tu = u * tiles, tv = v * tiles;
                int tx = Mathf.FloorToInt(tu), ty = Mathf.FloorToInt(tv);
                float fx = tu - tx, fy = tv - ty;
                float edge = Mathf.Min(Mathf.Min(fx, 1 - fx), Mathf.Min(fy, 1 - fy));
                float n = Fbm(u, v, 16, 4, 51);
                Color c = Color.Lerp(lapis, lapisDeep, Hash(tx, ty, 5) * 0.6f + n * 0.3f);
                float h = 0.7f + n * 0.1f;
                if (edge < 0.04f) { c = Mul(Hex("#C9B98A"), 0.6f); h = 0; }
                // Gold rosettes on the top and bottom rows, lions on the middle band.
                if (ty == 1 || ty == 6) { float r = Rosette(fx - 0.5f, fy - 0.5f); if (r > 0) { c = Color.Lerp(c, goldB, r); h += 0.2f * r; } }
                if (ty == 3 || ty == 4)
                {
                    float lu = (u * 4f) % 1f, lv = (v * tiles - 3f) / 2f;
                    if (Lion(lu, lv)) { c = Color.Lerp(gold, goldB, n); h += 0.25f; }
                }
                return (c, h);
            }, 4f);

            Pair("LapisStars", (u, v) =>
            {
                float n = Fbm(u, v, 8, 4, 61);
                Color c = Color.Lerp(lapisDeep, lapis, n * 0.7f);
                float h = 0.5f + n * 0.1f;
                for (int i = 0; i < 14; i++)
                {
                    float sx = Hash(i, 1, 7), sy = Hash(i, 2, 7);
                    float dx = Wrap01(u - sx), dy = Wrap01(v - sy);
                    float s = Star(dx * 18f, dy * 18f);
                    if (s > 0) { c = Color.Lerp(c, goldB, s); h += 0.3f * s; }
                }
                return (c, h);
            }, 4f);

            Pair("CrackedFloor", (u, v) =>
            {
                var (c, h) = Bricks(u, v, 4, 4, Hex("#A68B62"), Hex("#6B5536"), Hex("#3A2B1A"), 0.02f, 71);
                float crack = Mathf.Abs(Fbm(u, v, 6, 4, 77) - 0.5f);
                if (crack < 0.012f) { c = Mul(c, 0.35f); h -= 0.4f; }
                return (c, h);
            }, 6f);

            Pair("Sand", (u, v) =>
            {
                float warp = Fbm(u, v, 4, 3, 81) * 6f;
                float rip = Mathf.Sin((v * 40f + warp) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float grain = Hash(Mathf.FloorToInt(u * S), Mathf.FloorToInt(v * S), 3);
                Color c = Color.Lerp(Hex("#D9B57A"), Hex("#B88C55"), rip * 0.35f + Fbm(u, v, 8, 4, 85) * 0.4f) * (0.94f + grain * 0.08f);
                c.a = 1;
                return (c, rip * 0.6f + grain * 0.1f);
            }, 3f);

            Pair("CaveRock", (u, v) =>
            {
                float n = Fbm(u, v, 4, 6, 91);
                float ridge = 1f - Mathf.Abs(Fbm(u, v, 8, 4, 95) * 2 - 1);
                Color c = Color.Lerp(Hex("#2A2520"), Hex("#5A5045"), n * 0.7f + ridge * 0.3f);
                return (c, n * 0.7f + ridge * 0.4f);
            }, 9f);

            Pair("MossRock", (u, v) =>
            {
                float n = Fbm(u, v, 4, 6, 101);
                float m = Fbm(u, v, 6, 4, 105);
                Color c = Color.Lerp(Hex("#4A453E"), Hex("#7A7268"), n);
                float moss = Mathf.Clamp01((m - 0.52f) * 6f);
                c = Color.Lerp(c, Color.Lerp(Hex("#4E6B3A"), Hex("#2F4424"), n), moss);
                return (c, n * 0.8f + moss * 0.15f);
            }, 8f);

            Pair("GolemStone", (u, v) =>
            {
                var (c, h) = Bricks(u, v, 3, 2, Hex("#6E6658"), Hex("#4A443A"), Hex("#25211C"), 0.02f, 111);
                float m = Fbm(u, v, 5, 4, 115);
                float moss = Mathf.Clamp01((m - 0.55f) * 5f);
                c = Color.Lerp(c, Hex("#4E6B3A"), moss * 0.85f);
                float crack = Mathf.Abs(Fbm(u, v, 5, 4, 119) - 0.5f);
                if (crack < 0.01f) { c = Mul(c, 0.3f); h -= 0.4f; }
                return (c, h + moss * 0.1f);
            }, 7f);

            Pair("Wood", (u, v) =>
            {
                float n = Fbm(u, v, 4, 4, 121);
                float g = Mathf.Sin((u * 30f + n * 8f) * Mathf.PI) * 0.5f + 0.5f;
                return (Color.Lerp(Hex("#3B2716"), Hex("#6B4A2B"), g * 0.6f + n * 0.4f), g * 0.4f);
            }, 3f);

            Pair("Clay", (u, v) =>
            {
                float n = Fbm(u, v, 6, 5, 131);
                float w = Cuneiform(u, v, 10, 137);
                Color c = Color.Lerp(Hex("#A8754A"), Hex("#7A5232"), n * 0.6f);
                c = Color.Lerp(c, Hex("#4A2E1A"), w * 0.7f);
                return (c, n * 0.3f + 0.6f - w * 0.4f);
            }, 6f);

            Pair("Bronze", (u, v) =>
            {
                float n = Fbm(u, v, 8, 5, 141);
                float patina = Mathf.Clamp01((Fbm(u, v, 4, 3, 147) - 0.6f) * 4f);
                return (Color.Lerp(Color.Lerp(Hex("#8C5A2B"), Hex("#C48A4A"), n), Hex("#3E7A64"), patina * 0.6f), n * 0.3f);
            }, 2f);

            GenerateMasks();
        }

        /// <summary>Emission masks, foliage, icons, particles and decals.</summary>
        static void GenerateMasks()
        {
            var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float w = Cuneiform((float)x / S, (float)y / S, 10, 137);
                    px[y * S + x] = new Color(w, w, w, 1);
                }
            Save("CuneiformGlow", px);

            // Palm frond with alpha.
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float u = (float)x / S - 0.5f, v = (float)y / S;
                    float rib = Mathf.Abs(u) < 0.012f ? 1 : 0;
                    float leafletLen = 0.45f * Mathf.Sin(v * Mathf.PI) * (1 - v * 0.3f);
                    float phase = (v * 38f) % 1f;
                    bool leaflet = Mathf.Abs(u) < leafletLen && Mathf.Abs(phase - Mathf.Abs(u) * 1.6f) < 0.22f;
                    float a = Mathf.Max(rib, leaflet ? 1 : 0);
                    float shade = 0.75f + Fbm((float)x / S, v, 8, 3, 151) * 0.4f;
                    px[y * S + x] = new Color(0.26f * shade, 0.36f * shade, 0.14f * shade, a);
                }
            Save("PalmLeaf", px, false, true);

            int I = 128; var ip = new Color[I * I];
            for (int y = 0; y < I; y++) for (int x = 0; x < I; x++)
                {
                    float u = x / (float)I, v = y / (float)I;
                    float ring = Mathf.Abs(Vector2.Distance(new Vector2(u, v), new Vector2(0.32f, 0.65f)) - 0.17f) < 0.06f ? 1 : 0;
                    bool shaft = u > 0.42f && u < 0.9f && Mathf.Abs(v - 0.65f + (u - 0.42f) * 0.9f) < 0.05f;
                    bool teeth = (u > 0.7f && u < 0.76f || u > 0.82f && u < 0.88f) && v < 0.65f - (u - 0.42f) * 0.9f && v > 0.65f - (u - 0.42f) * 0.9f - 0.12f;
                    float a = ring > 0 || shaft || teeth ? 1 : 0;
                    ip[y * I + x] = new Color(0.95f, 0.76f, 0.3f, a);
                }
            Save("Icon_Key", ip, false, true, I);
            for (int y = 0; y < I; y++) for (int x = 0; x < I; x++)
                {
                    float u = x / (float)I, v = y / (float)I;
                    float d = Mathf.Abs(u - v);
                    bool blade = d < 0.05f && u > 0.3f && u < 0.92f;
                    bool guard = Mathf.Abs(u + v - 0.62f) < 0.04f && Mathf.Abs(u - v) < 0.2f;
                    bool grip = d < 0.03f && u > 0.12f && u <= 0.3f;
                    bool pommel = Vector2.Distance(new Vector2(u, v), new Vector2(0.1f, 0.1f)) < 0.05f;
                    Color c = blade ? new Color(0.85f, 0.6f, 0.3f, 1) : (guard || pommel) ? new Color(0.95f, 0.8f, 0.3f, 1) : grip ? new Color(0.2f, 0.3f, 0.65f, 1) : Color.clear;
                    ip[y * I + x] = c;
                }
            Save("Icon_Sword", ip, false, true, I);

            int P = 64; var pp = new Color[P * P];
            for (int y = 0; y < P; y++) for (int x = 0; x < P; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(P / 2f, P / 2f)) / (P / 2f);
                    float a = Mathf.Clamp01(1 - d); a *= a;
                    pp[y * P + x] = new Color(1, 1, 1, a);
                }
            Save("Particle_Soft", pp, false, true, P);

            int D = 256; var dp = new Color[D * D];
            for (int y = 0; y < D; y++) for (int x = 0; x < D; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(D / 2f, D / 2f)) / (D / 2f);
                    float ring = Mathf.Clamp01(1 - Mathf.Abs(d - 0.9f) * 14f);
                    float fill = d < 0.9f ? 0.25f + 0.15f * Mathf.Sin(d * 40f) : 0;
                    dp[y * D + x] = new Color(1, 1, 1, Mathf.Clamp01(ring + fill));
                }
            Save("Decal_Ring", dp, false, true, D);
        }

        static float Wrap01(float d) { d = d - Mathf.Round(d); return d; }

        /// <summary>Rows of wedge marks (cuneiform). Returns 0..1 coverage.</summary>
        static float Cuneiform(float u, float v, int rows, int seed)
        {
            float rv = v * rows; int row = Mathf.FloorToInt(rv); float fy = rv - row;
            if (fy < 0.15f || fy > 0.85f) return 0;
            float cu = u * rows * 2.5f; int cell = Mathf.FloorToInt(cu); float fx = cu - cell;
            float kind = Hash(cell, row, seed);
            if (kind < 0.2f) return 0;
            float lx = fx, ly = (fy - 0.15f) / 0.7f;
            if (kind < 0.6f)
            {   // vertical wedge: triangle head + tail
                bool head = ly > 0.65f && Mathf.Abs(lx - 0.5f) < (ly - 0.65f) * 1.2f;
                bool tail = Mathf.Abs(lx - 0.5f) < 0.06f && ly > 0.1f && ly <= 0.7f;
                return head || tail ? 1 : 0;
            }
            else
            {   // horizontal wedge
                bool head = lx < 0.35f && Mathf.Abs(ly - 0.5f) < (0.35f - lx) * 1.2f;
                bool tail = Mathf.Abs(ly - 0.5f) < 0.07f && lx > 0.3f && lx < 0.9f;
                return head || tail ? 1 : 0;
            }
        }

        static float Rosette(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float petal = 0.28f + 0.1f * Mathf.Cos(a * 8f);
            if (r < 0.08f) return 1;
            return r < petal && r > 0.1f ? 0.85f : 0;
        }

        static float Star(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float edge = 0.25f + 0.55f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 4f)), 6f);
            return r < edge ? 1 : 0;
        }

        /// <summary>Crude walking-lion silhouette in a unit cell (Ishtar Gate style).</summary>
        static bool Lion(float u, float v)
        {
            if (v < 0 || v > 1) return false;
            bool body = Sq((u - 0.5f) / 0.28f) + Sq((v - 0.5f) / 0.16f) < 1;
            bool head = Sq((u - 0.8f) / 0.09f) + Sq((v - 0.62f) / 0.12f) < 1;
            bool mane = Sq((u - 0.74f) / 0.1f) + Sq((v - 0.58f) / 0.17f) < 1;
            bool legs = v > 0.18f && v < 0.42f && (Mathf.Abs(u - 0.3f) < 0.035f || Mathf.Abs(u - 0.4f) < 0.035f || Mathf.Abs(u - 0.62f) < 0.035f || Mathf.Abs(u - 0.7f) < 0.035f);
            bool tail = u > 0.12f && u < 0.24f && Mathf.Abs(v - (0.55f + (0.24f - u) * 1.5f)) < 0.025f;
            return body || head || mane || legs || tail;
        }

        static float Sq(float x) => x * x;
    }
}
