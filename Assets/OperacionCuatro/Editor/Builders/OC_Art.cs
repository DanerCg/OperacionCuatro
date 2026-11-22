using System.IO;
using UnityEditor;
using UnityEngine;

namespace OCBuilders
{
    // =====================================================================
    //  Canvas de pixel art
    // =====================================================================
    public class Canvas2D
    {
        public readonly int W;
        public readonly int H;
        public readonly Color32[] P;

        public Canvas2D(int w, int h)
        {
            W = w;
            H = h;
            P = new Color32[w * h];
            for (int i = 0; i < P.Length; i++) P[i] = new Color32(0, 0, 0, 0);
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            P[y * W + x] = c;
        }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Set(xx, yy, c);
        }

        public void RectOutline(int x, int y, int w, int h, Color32 c)
        {
            for (int xx = x; xx < x + w; xx++) { Set(xx, y, c); Set(xx, y + h - 1, c); }
            for (int yy = y; yy < y + h; yy++) { Set(x, yy, c); Set(x + w - 1, yy, c); }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - rx) - 1;
            int x1 = Mathf.CeilToInt(cx + rx) + 1;
            int y0 = Mathf.FloorToInt(cy - ry) - 1;
            int y1 = Mathf.CeilToInt(cy + ry) + 1;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / Mathf.Max(0.001f, rx);
                    float dy = (y - cy) / Mathf.Max(0.001f, ry);
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
            }
        }

        public void Line(Vector2 a, Vector2 b, float thickness, Color32 c)
        {
            float r = Mathf.Max(0.5f, thickness * 0.5f);
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r) - 1;
            int x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r) + 1;
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r) - 1;
            int y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r) + 1;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (SegDistance(new Vector2(x, y), a, b) <= r) Set(x, y, c);
                }
            }
        }

        private static float SegDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        public void AddOutline(Color32 c)
        {
            var copy = (Color32[])P.Clone();
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a != 0) continue;
                    bool n = Alpha(copy, x - 1, y) || Alpha(copy, x + 1, y) || Alpha(copy, x, y - 1) || Alpha(copy, x, y + 1);
                    if (n) P[y * W + x] = c;
                }
            }
        }

        private bool Alpha(Color32[] arr, int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return false;
            return arr[y * W + x].a > 0;
        }
    }

    public class CharStyle
    {
        public Color32 skin = new Color32(242, 201, 160, 255);
        public Color32 skinShade = new Color32(206, 160, 120, 255);
        public Color32 hair = new Color32(26, 26, 34, 255);
        public Color32 shirt = new Color32(46, 91, 214, 255);
        public Color32 shirtShade = new Color32(30, 60, 150, 255);
        public Color32 pants = new Color32(31, 58, 138, 255);
        public Color32 boots = new Color32(42, 42, 51, 255);
        public Color32 outline = new Color32(14, 14, 20, 255);
        public Color32 glasses = new Color32(20, 22, 30, 255);
        public Color32 accent = new Color32(230, 230, 235, 255);

        public bool hasGlasses;
        public bool ponytail;
        public bool helmet;
        public bool bandana;
        public float width = 1f;
        public int legLength = 13;
        public int armLength = 10;
    }

    public struct Pose
    {
        public float legFront;
        public float legBack;
        public float armFront;
        public float armBack;
        public int bodyDy;
        public bool gun;
        public bool bomb;

        public static Pose Idle() { return new Pose { legFront = 11f, legBack = -11f, armFront = 10f, armBack = -14f, bodyDy = 0 }; }
        public static Pose Idle2() { return new Pose { legFront = 11f, legBack = -11f, armFront = 14f, armBack = -10f, bodyDy = 1 }; }
        public static Pose Walk(float a, int dy) { return new Pose { legFront = a, legBack = -a, armFront = -a * 0.7f, armBack = a * 0.7f, bodyDy = dy }; }
        public static Pose Jump() { return new Pose { legFront = 45f, legBack = -12f, armFront = 135f, armBack = 150f, bodyDy = 1 }; }
        public static Pose Fall() { return new Pose { legFront = -22f, legBack = 34f, armFront = 70f, armBack = 50f, bodyDy = 0 }; }
        public static Pose Crouch() { return new Pose { legFront = 62f, legBack = -52f, armFront = 62f, armBack = 20f, bodyDy = -8 }; }
        public static Pose Shoot() { return new Pose { legFront = 16f, legBack = -18f, armFront = 88f, armBack = -30f, bodyDy = 0, gun = true }; }
        public static Pose Throw() { return new Pose { legFront = 12f, legBack = -14f, armFront = 158f, armBack = -20f, bodyDy = 0, bomb = true }; }
        public static Pose Hurt() { return new Pose { legFront = -18f, legBack = 22f, armFront = 150f, armBack = 160f, bodyDy = -2 }; }
        public static Pose Death() { return new Pose { legFront = 84f, legBack = 68f, armFront = 175f, armBack = 168f, bodyDy = -19 }; }
    }

    public static class PixelArt
    {
        public const int PPU = 16;

        public static Vector2 Dir(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), -Mathf.Cos(r));
        }

        public static Canvas2D DrawCharacter(CharStyle st, Pose p)
        {
            var c = new Canvas2D(52, 52);

            int cx = 26;
            int baseY = 3;
            int hipY = baseY + st.legLength + p.bodyDy;
            int shoulderY = hipY + 12;
            int headBottom = shoulderY + 2;
            int headH = 9;
            int headW = Mathf.Max(9, Mathf.RoundToInt(11f * st.width));

            Vector2 hip = new Vector2(cx, hipY);
            Vector2 shoulder = new Vector2(cx + 1, shoulderY);

            Vector2 handBack = shoulder + Dir(p.armBack) * st.armLength;
            c.Line(shoulder + new Vector2(-1f, 0f), handBack, 4f, st.shirtShade);
            c.Ellipse(handBack.x, handBack.y, 2f, 2f, st.skinShade);

            Vector2 footBack = hip + Dir(p.legBack) * st.legLength;
            c.Line(hip + new Vector2(-3f, 1f), footBack, 5f, Mul(st.pants, 0.72f));
            Boot(c, footBack, Mul(st.boots, 0.8f));

            int torsoW = Mathf.RoundToInt(11f * st.width);
            int tx = cx - torsoW / 2;
            c.Rect(tx, hipY - 1, torsoW, shoulderY - hipY + 2, st.shirt);
            c.Rect(tx + torsoW - 3, hipY - 1, 3, shoulderY - hipY + 2, st.shirtShade);
            c.Rect(tx, hipY - 2, torsoW, 2, Mul(st.pants, 0.55f));
            c.Rect(tx + 1, shoulderY - 1, torsoW - 2, 1, st.shirtShade);
            c.Rect(tx + torsoW / 2 - 1, hipY + 2, 2, shoulderY - hipY - 2, st.accent);
            c.Rect(tx, hipY, 2, shoulderY - hipY, Mul(st.shirt, 1.16f));

            Vector2 footFront = hip + Dir(p.legFront) * st.legLength;
            c.Line(hip + new Vector2(3f, 1f), footFront, 5f, st.pants);
            Boot(c, footFront, st.boots);

            float headCx = cx + 1f;
            float headCy = headBottom + headH * 0.5f;
            float hw = headW * 0.5f;

            c.Ellipse(headCx, headCy, hw, headH * 0.5f, st.skin);
            c.Ellipse(headCx, headCy - 2f, hw * 0.7f, 1.8f, st.skinShade);
            c.Rect(cx - 1, headBottom - 3, 5, 3, st.skinShade);

            c.Rect(Mathf.RoundToInt(headCx - hw - 1f), Mathf.RoundToInt(headCy - 1f), 2, 2, st.skinShade);
            c.Set(Mathf.RoundToInt(headCx + hw), Mathf.RoundToInt(headCy), st.skin);

            if (st.ponytail)
            {
                c.Ellipse(headCx - hw - 2f, headCy + 2f, 3f, 3.6f, st.hair);
                c.Line(new Vector2(headCx - hw - 2f, headCy + 1f), new Vector2(headCx - hw - 4f, headCy - 7f), 3f, st.hair);
                c.Ellipse(headCx - hw - 4f, headCy - 7.5f, 2.2f, 2.2f, st.hair);
            }

            c.Rect(Mathf.RoundToInt(headCx - hw - 1f), Mathf.RoundToInt(headCy + headH * 0.35f), Mathf.RoundToInt(headW + 2f), 3, st.hair);
            c.Rect(Mathf.RoundToInt(headCx - hw - 1f), Mathf.RoundToInt(headCy + 1f), 3, Mathf.RoundToInt(headH * 0.4f), st.hair);
            c.Rect(Mathf.RoundToInt(headCx + hw - 1f), Mathf.RoundToInt(headCy + 3f), 2, 2, st.hair);

            if (st.helmet)
            {
                c.Rect(Mathf.RoundToInt(headCx - hw - 2f), Mathf.RoundToInt(headCy + headH * 0.35f), Mathf.RoundToInt(headW + 4f), 3, st.accent);
                c.Rect(Mathf.RoundToInt(headCx - hw - 2f), Mathf.RoundToInt(headCy + 1f), 3, Mathf.RoundToInt(headH * 0.4f), st.accent);
                c.Rect(Mathf.RoundToInt(headCx - hw - 2f), Mathf.RoundToInt(headCy + 2f), Mathf.RoundToInt(headW + 4f), 1, Mul(st.accent, 0.7f));
            }

            if (st.bandana)
            {
                c.Rect(Mathf.RoundToInt(headCx - hw - 1f), Mathf.RoundToInt(headCy + headH * 0.38f), Mathf.RoundToInt(headW + 2f), 2, st.accent);
            }

            if (st.hasGlasses)
            {
                c.Rect(Mathf.RoundToInt(headCx + 1f), Mathf.RoundToInt(headCy - 1f), Mathf.RoundToInt(hw), 3, st.glasses);
                c.Set(Mathf.RoundToInt(headCx + 2f), Mathf.RoundToInt(headCy), new Color32(210, 235, 255, 255));
            }
            else
            {
                c.Rect(Mathf.RoundToInt(headCx + 2f), Mathf.RoundToInt(headCy - 1f), 2, 2, st.outline);
            }

            Vector2 handFront = shoulder + Dir(p.armFront) * st.armLength;
            c.Line(shoulder, handFront, 4.5f, st.shirt);
            c.Ellipse(handFront.x, handFront.y, 2.2f, 2.2f, st.skin);

            if (p.gun)
            {
                int gx = Mathf.RoundToInt(handFront.x);
                int gy = Mathf.RoundToInt(handFront.y);
                c.Rect(gx, gy - 1, 8, 3, new Color32(52, 56, 66, 255));
                c.Rect(gx + 3, gy - 3, 3, 3, new Color32(38, 40, 48, 255));
                c.Rect(gx + 7, gy, 2, 1, new Color32(150, 155, 170, 255));
            }

            if (p.bomb)
            {
                int gx = Mathf.RoundToInt(handFront.x);
                int gy = Mathf.RoundToInt(handFront.y);
                c.Ellipse(gx + 1f, gy + 1f, 3f, 3f, new Color32(48, 58, 44, 255));
                c.Set(gx + 3, gy + 3, new Color32(240, 210, 90, 255));
            }

            c.AddOutline(st.outline);
            return c;
        }

        private static void Boot(Canvas2D c, Vector2 foot, Color32 bootColor)
        {
            int fx = Mathf.RoundToInt(foot.x);
            int fy = Mathf.RoundToInt(foot.y);
            c.Rect(fx - 2, fy - 1, 6, 4, bootColor);
        }

        private static Color32 Mul(Color32 c, float f)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * f), 0, 255),
                c.a);
        }

        public static Sprite Save(Canvas2D c, string path, Vector2 pivot, bool repeat)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);
            tex.SetPixels32(c.P);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PPU;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteMode = (int)SpriteImportMode.Single;

                bool centered = Mathf.Approximately(pivot.x, 0.5f) && Mathf.Approximately(pivot.y, 0.5f);
                settings.spriteAlignment = centered ? (int)SpriteAlignment.Center : (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;

                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }

    // =====================================================================
    //  Generador de assets de arte
    // =====================================================================
    public static class OC_Art
    {
        private const string CharsRoot = "Assets/OperacionCuatro/Sprites/Characters";
        private const string EnemiesRoot = "Assets/OperacionCuatro/Sprites/Enemies";
        private const string WorldRoot = "Assets/OperacionCuatro/Sprites/World";
        private const string FxRoot = "Assets/OperacionCuatro/Sprites/FX";
        private const string PropsRoot = "Assets/OperacionCuatro/Sprites/Props";

        public static void ExecuteCharacters()
        {
            GeneratePlayer("Danner", Danner());
            GeneratePlayer("Cesar", Cesar());
            GeneratePlayer("Sebastian", Sebastian());
            GeneratePlayer("Ines", Ines());
            GenerateRaider();
            GenerateGunner();
            Debug.Log("[OC] Sprites de personajes y enemigos generados.");
        }

        private const string PortraitRoot = "Assets/OperacionCuatro/Resources/Portraits";

        /// <summary>
        /// Retratos del HUD: recorte del busto (cabeza y hombros) del sprite idle original de
        /// cada personaje, ampliado con vecino más cercano a 84x84 px, que es justo el tamaño
        /// del hueco del retrato en el panel. No genera arte nuevo ni altera los sprites.
        /// </summary>
        public static void ExecutePortraits()
        {
            SavePortrait("Danner", "Danner/Danner_idle_0");
            SavePortrait("Cesar", "Cesar/Cesar_idle_0");
            SavePortrait("Sebastian", "Sebastian/Sebastian_idle_0");
            SavePortrait("Ines", "Ines/Ines_idle_0");
            Debug.Log("[OC] Retratos del HUD generados en Resources/Portraits (84x84).");
        }

        private static void SavePortrait(string name, string sourceRelPath)
        {
            string sourcePath = CharsRoot + "/" + sourceRelPath + ".png";
            if (!File.Exists(sourcePath))
            {
                Debug.LogError("[OC] Sprite de origen no encontrado para el retrato: " + sourcePath);
                return;
            }

            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(sourcePath));

            int sw = src.width;
            int sh = src.height;
            Color32[] px = src.GetPixels32();
            Object.DestroyImmediate(src);

            // Alto real del personaje (fila opaca mas alta y mas baja).
            int minY = sh, maxY = -1;
            for (int y = 0; y < sh; y++)
            {
                for (int x = 0; x < sw; x++)
                {
                    if (px[y * sw + x].a < 8) continue;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxY < 0)
            {
                Debug.LogError("[OC] Sprite sin pixeles opacos: " + sourcePath);
                return;
            }

            int bodyH = maxY - minY + 1;

            // Centro horizontal de la cabeza: media de los pixeles de la banda superior.
            int headBandY = maxY - Mathf.Max(4, Mathf.RoundToInt(bodyH * 0.30f));
            int headMinX = sw, headMaxX = -1;
            for (int y = Mathf.Max(0, headBandY); y <= maxY; y++)
            {
                for (int x = 0; x < sw; x++)
                {
                    if (px[y * sw + x].a < 8) continue;
                    if (x < headMinX) headMinX = x;
                    if (x > headMaxX) headMaxX = x;
                }
            }

            if (headMaxX < 0) { headMinX = 0; headMaxX = sw - 1; }

            int side = Mathf.Clamp(Mathf.RoundToInt(bodyH * 0.46f), 16, 30);
            int headCx = (headMinX + headMaxX) / 2;
            int x0 = Mathf.Clamp(headCx - side / 2, 0, Mathf.Max(0, sw - side));
            int y1 = maxY;
            int y0 = Mathf.Clamp(y1 - side + 1, 0, Mathf.Max(0, sh - side));

            const int Out = 84;
            var c = new Canvas2D(Out, Out);
            for (int y = 0; y < Out; y++)
            {
                int sy = y0 + (y * side) / Out;
                if (sy < 0 || sy >= sh) continue;

                for (int x = 0; x < Out; x++)
                {
                    int sx = x0 + (x * side) / Out;
                    if (sx < 0 || sx >= sw) continue;

                    var col = px[sy * sw + sx];
                    if (col.a == 0) continue;
                    c.Set(x, y, col);
                }
            }

            PixelArt.Save(c, PortraitRoot + "/Portrait_" + name + ".png", new Vector2(0.5f, 0.5f), false);
        }

        public static void ExecuteWorld()
        {
            var rng = new System.Random(20260501);
            GroundTiles(rng);
            Platforms();
            Props(rng);
            Backgrounds(rng);
            Effects(rng);
            CheckpointAndGoal();
            Debug.Log("[OC] Sprites de escenario generados.");
        }

        // ---------- paletas ----------
        private static CharStyle Danner()
        {
            return new CharStyle
            {
                skin = new Color32(242, 201, 160, 255),
                skinShade = new Color32(205, 158, 118, 255),
                hair = new Color32(24, 24, 32, 255),
                shirt = new Color32(46, 91, 214, 255),
                shirtShade = new Color32(28, 56, 142, 255),
                pants = new Color32(22, 28, 58, 255),
                boots = new Color32(58, 60, 74, 255),
                glasses = new Color32(18, 20, 28, 255),
                accent = new Color32(226, 230, 240, 255),
                hasGlasses = true,
                width = 1f
            };
        }

        private static CharStyle Cesar()
        {
            return new CharStyle
            {
                skin = new Color32(232, 185, 138, 255),
                skinShade = new Color32(196, 148, 104, 255),
                hair = new Color32(43, 30, 22, 255),
                shirt = new Color32(47, 168, 79, 255),
                shirtShade = new Color32(26, 108, 48, 255),
                pants = new Color32(29, 92, 46, 255),
                boots = new Color32(58, 58, 42, 255),
                glasses = new Color32(20, 22, 26, 255),
                accent = new Color32(236, 240, 236, 255),
                hasGlasses = true,
                width = 1.18f
            };
        }

        private static CharStyle Sebastian()
        {
            return new CharStyle
            {
                skin = new Color32(245, 210, 174, 255),
                skinShade = new Color32(208, 168, 130, 255),
                hair = new Color32(107, 63, 30, 255),
                shirt = new Color32(214, 46, 46, 255),
                shirtShade = new Color32(148, 26, 26, 255),
                pants = new Color32(122, 26, 26, 255),
                boots = new Color32(42, 42, 51, 255),
                accent = new Color32(240, 226, 210, 255),
                hasGlasses = false,
                width = 1.02f
            };
        }

        private static CharStyle Ines()
        {
            return new CharStyle
            {
                skin = new Color32(247, 214, 181, 255),
                skinShade = new Color32(210, 172, 140, 255),
                hair = new Color32(20, 20, 28, 255),
                shirt = new Color32(139, 63, 214, 255),
                shirtShade = new Color32(92, 38, 152, 255),
                pants = new Color32(74, 31, 115, 255),
                boots = new Color32(42, 42, 51, 255),
                glasses = new Color32(24, 20, 34, 255),
                accent = new Color32(238, 228, 248, 255),
                hasGlasses = true,
                ponytail = true,
                width = 0.95f,
                legLength = 12
            };
        }

        private static CharStyle RaiderStyle()
        {
            return new CharStyle
            {
                skin = new Color32(206, 166, 128, 255),
                skinShade = new Color32(168, 128, 94, 255),
                hair = new Color32(30, 28, 24, 255),
                shirt = new Color32(86, 94, 52, 255),
                shirtShade = new Color32(56, 62, 34, 255),
                pants = new Color32(58, 64, 36, 255),
                boots = new Color32(34, 34, 30, 255),
                accent = new Color32(112, 120, 70, 255),
                helmet = true,
                width = 1.05f
            };
        }

        private static CharStyle GunnerStyle()
        {
            return new CharStyle
            {
                skin = new Color32(214, 172, 134, 255),
                skinShade = new Color32(174, 132, 96, 255),
                hair = new Color32(34, 24, 22, 255),
                shirt = new Color32(122, 52, 52, 255),
                shirtShade = new Color32(80, 32, 32, 255),
                pants = new Color32(60, 36, 36, 255),
                boots = new Color32(34, 30, 30, 255),
                accent = new Color32(206, 62, 62, 255),
                bandana = true,
                width = 1f
            };
        }

        // ---------- personajes ----------
        private static void GeneratePlayer(string name, CharStyle style)
        {
            string dir = CharsRoot + "/" + name;
            SaveFrame(dir, name + "_idle_0", style, Pose.Idle());
            SaveFrame(dir, name + "_idle_1", style, Pose.Idle2());
            SaveFrame(dir, name + "_walk_0", style, Pose.Walk(26f, 0));
            SaveFrame(dir, name + "_walk_1", style, Pose.Walk(2f, 1));
            SaveFrame(dir, name + "_walk_2", style, Pose.Walk(-24f, 0));
            SaveFrame(dir, name + "_walk_3", style, Pose.Walk(2f, 1));
            SaveFrame(dir, name + "_jump", style, Pose.Jump());
            SaveFrame(dir, name + "_fall", style, Pose.Fall());
            SaveFrame(dir, name + "_crouch", style, Pose.Crouch());
            SaveFrame(dir, name + "_shoot", style, Pose.Shoot());
            SaveFrame(dir, name + "_throw", style, Pose.Throw());
            SaveFrame(dir, name + "_hurt", style, Pose.Hurt());
            SaveFrame(dir, name + "_death", style, Pose.Death());
        }

        private static void GenerateRaider()
        {
            var style = RaiderStyle();
            string dir = EnemiesRoot + "/Raider";
            SaveFrame(dir, "Raider_idle_0", style, Pose.Idle());
            SaveFrame(dir, "Raider_walk_0", style, Pose.Walk(24f, 0));
            SaveFrame(dir, "Raider_walk_1", style, Pose.Walk(2f, 1));
            SaveFrame(dir, "Raider_walk_2", style, Pose.Walk(-22f, 0));
            SaveFrame(dir, "Raider_walk_3", style, Pose.Walk(2f, 1));
            SaveFrame(dir, "Raider_hurt", style, Pose.Hurt());
            SaveFrame(dir, "Raider_death", style, Pose.Death());
        }

        private static void GenerateGunner()
        {
            var style = GunnerStyle();
            string dir = EnemiesRoot + "/Gunner";
            SaveFrame(dir, "Gunner_idle_0", style, Pose.Idle());
            SaveFrame(dir, "Gunner_walk_0", style, Pose.Walk(18f, 0));
            SaveFrame(dir, "Gunner_walk_1", style, Pose.Walk(-16f, 1));
            SaveFrame(dir, "Gunner_shoot", style, Pose.Shoot());
            SaveFrame(dir, "Gunner_hurt", style, Pose.Hurt());
            SaveFrame(dir, "Gunner_death", style, Pose.Death());
        }

        private static void SaveFrame(string dir, string fileName, CharStyle style, Pose pose)
        {
            var canvas = PixelArt.DrawCharacter(style, pose);
            PixelArt.Save(canvas, dir + "/" + fileName + ".png", new Vector2(0.5f, 3f / 52f), false);
        }

        // ---------- escenario ----------
        private static void GroundTiles(System.Random rng)
        {
            var c = new Canvas2D(16, 16);
            c.Rect(0, 0, 16, 16, new Color32(74, 78, 92, 255));
            c.Rect(0, 14, 16, 2, new Color32(132, 138, 154, 255));
            c.Rect(0, 13, 16, 1, new Color32(46, 48, 60, 255));
            c.Rect(0, 0, 16, 2, new Color32(38, 40, 50, 255));

            for (int i = 0; i < 26; i++)
            {
                int x = rng.Next(0, 16);
                int y = rng.Next(2, 13);
                c.Set(x, y, i % 3 == 0 ? new Color32(90, 94, 110, 255) : new Color32(58, 62, 76, 255));
            }

            for (int y = 3; y < 13; y++)
                c.Set(6 + (y % 3 == 0 ? 1 : 0), y, new Color32(44, 46, 58, 255));

            PixelArt.Save(c, WorldRoot + "/tile_ground.png", new Vector2(0.5f, 0.5f), true);

            var c2 = new Canvas2D(16, 16);
            c2.Rect(0, 0, 16, 16, new Color32(80, 84, 98, 255));
            c2.Rect(0, 15, 16, 1, new Color32(168, 176, 196, 255));
            c2.Rect(0, 13, 16, 2, new Color32(104, 110, 128, 255));
            c2.Rect(0, 0, 16, 2, new Color32(40, 42, 52, 255));
            for (int x = 1; x < 16; x += 5) c2.Set(x, 12, new Color32(58, 62, 76, 255));
            PixelArt.Save(c2, WorldRoot + "/tile_ground_top.png", new Vector2(0.5f, 0.5f), true);

            var c3 = new Canvas2D(16, 16);
            c3.Rect(0, 0, 16, 16, new Color32(48, 50, 62, 255));
            for (int i = 0; i < 18; i++) c3.Set(rng.Next(0, 16), rng.Next(0, 16), new Color32(40, 42, 52, 255));
            PixelArt.Save(c3, WorldRoot + "/tile_ground_fill.png", new Vector2(0.5f, 0.5f), true);
        }

        private static void Platforms()
        {
            var c = new Canvas2D(16, 8);
            c.Rect(0, 0, 16, 8, new Color32(66, 70, 84, 255));
            c.Rect(0, 7, 16, 1, new Color32(160, 168, 188, 255));
            c.Rect(0, 5, 16, 1, new Color32(96, 102, 120, 255));
            c.Rect(0, 0, 16, 1, new Color32(34, 36, 46, 255));
            for (int x = 2; x < 16; x += 4)
            {
                c.Rect(x, 1, 1, 4, new Color32(44, 46, 58, 255));
                c.Set(x, 6, new Color32(190, 196, 214, 255));
            }
            PixelArt.Save(c, WorldRoot + "/tile_platform.png", new Vector2(0.5f, 0.5f), true);
        }

        private static void Props(System.Random rng)
        {
            ContainerProp("container_red", new Color32(150, 52, 46, 255), new Color32(96, 30, 28, 255), new Color32(190, 96, 62, 255));
            ContainerProp("container_blue", new Color32(40, 78, 132, 255), new Color32(24, 48, 88, 255), new Color32(90, 72, 48, 255));
            ContainerProp("container_rust", new Color32(112, 92, 62, 255), new Color32(72, 58, 38, 255), new Color32(158, 96, 48, 255));

            var crate = new Canvas2D(16, 16);
            crate.Rect(0, 0, 16, 16, new Color32(126, 88, 52, 255));
            crate.RectOutline(0, 0, 16, 16, new Color32(78, 52, 30, 255));
            crate.Rect(1, 1, 14, 1, new Color32(158, 116, 70, 255));
            crate.Line(new Vector2(1, 1), new Vector2(14, 14), 2f, new Color32(104, 72, 42, 255));
            crate.Line(new Vector2(14, 1), new Vector2(1, 14), 2f, new Color32(104, 72, 42, 255));
            PixelArt.Save(crate, PropsRoot + "/crate.png", new Vector2(0.5f, 0.5f), false);

            var barrel = new Canvas2D(12, 18);
            barrel.Rect(1, 0, 10, 18, new Color32(120, 64, 44, 255));
            barrel.Rect(1, 15, 10, 1, new Color32(160, 96, 62, 255));
            barrel.Rect(1, 2, 10, 1, new Color32(160, 96, 62, 255));
            barrel.Rect(1, 8, 10, 1, new Color32(160, 96, 62, 255));
            barrel.Rect(0, 0, 12, 1, new Color32(58, 34, 26, 255));
            barrel.Rect(0, 17, 12, 1, new Color32(58, 34, 26, 255));
            barrel.Rect(5, 10, 2, 5, new Color32(86, 46, 32, 255));
            PixelArt.Save(barrel, PropsRoot + "/barrel.png", new Vector2(0.5f, 0f), false);

            var bags = new Canvas2D(20, 12);
            bags.Ellipse(5, 4, 5f, 4f, new Color32(122, 112, 76, 255));
            bags.Ellipse(14, 4, 5f, 4f, new Color32(108, 98, 66, 255));
            bags.Ellipse(9, 8, 5f, 4f, new Color32(132, 122, 84, 255));
            bags.AddOutline(new Color32(52, 46, 30, 255));
            PixelArt.Save(bags, PropsRoot + "/sandbags.png", new Vector2(0.5f, 0f), false);

            var fence = new Canvas2D(16, 20);
            fence.Rect(0, 0, 2, 20, new Color32(72, 76, 88, 255));
            fence.Rect(14, 0, 2, 20, new Color32(72, 76, 88, 255));
            for (int y = 2; y < 19; y += 3) fence.Rect(0, y, 16, 1, new Color32(58, 62, 74, 255));
            PixelArt.Save(fence, PropsRoot + "/fence.png", new Vector2(0.5f, 0f), true);
        }

        private static void ContainerProp(string name, Color32 body, Color32 shade, Color32 rust)
        {
            var c = new Canvas2D(48, 32);
            c.Rect(0, 0, 48, 32, body);
            c.Rect(0, 0, 48, 2, shade);
            c.Rect(0, 30, 48, 2, new Color32(
                (byte)Mathf.Min(255, body.r + 40),
                (byte)Mathf.Min(255, body.g + 40),
                (byte)Mathf.Min(255, body.b + 40), 255));

            for (int x = 3; x < 48; x += 5) c.Rect(x, 2, 1, 28, shade);

            c.RectOutline(30, 4, 16, 24, shade);
            c.Rect(38, 4, 1, 24, shade);
            c.Rect(33, 14, 3, 3, new Color32(70, 74, 86, 255));

            for (int i = 0; i < 22; i++) c.Set(1 + (i * 7) % 46, 3 + (i * 5) % 26, rust);

            c.AddOutline(new Color32(20, 22, 30, 255));
            PixelArt.Save(c, PropsRoot + "/" + name + ".png", new Vector2(0.5f, 0f), false);
        }

        private static void Backgrounds(System.Random rng)
        {
            var sky = new Canvas2D(512, 288);
            for (int y = 0; y < 288; y++)
            {
                float t = (float)y / 288f;
                Color32 col;
                if (t > 0.72f) col = Lerp(new Color32(18, 22, 46, 255), new Color32(30, 42, 84, 255), (t - 0.72f) / 0.28f);
                else if (t > 0.42f) col = Lerp(new Color32(46, 40, 74, 255), new Color32(18, 22, 46, 255), (t - 0.42f) / 0.30f);
                else col = Lerp(new Color32(122, 74, 66, 255), new Color32(46, 40, 74, 255), t / 0.42f);

                for (int x = 0; x < 512; x++) sky.Set(x, y, col);
            }

            for (int i = 0; i < 160; i++)
                sky.Set(rng.Next(0, 512), rng.Next(170, 288), new Color32(220, 228, 255, (byte)rng.Next(70, 200)));

            sky.Ellipse(408f, 226f, 22f, 22f, new Color32(238, 236, 214, 255));
            sky.Ellipse(400f, 232f, 18f, 18f, new Color32(30, 42, 84, 255));
            PixelArt.Save(sky, WorldRoot + "/bg_sky.png", new Vector2(0.5f, 0.5f), true);

            var ruins = new Canvas2D(512, 288);
            int x0 = 0;
            while (x0 < 512)
            {
                int w = rng.Next(26, 58);
                int h = rng.Next(50, 150);
                ruins.Rect(x0, 0, w, h, new Color32(36, 34, 56, 255));

                for (int wy = 12; wy < h - 8; wy += 14)
                {
                    for (int wx = x0 + 6; wx < x0 + w - 6; wx += 10)
                    {
                        ruins.Rect(wx, wy, 3, 4, rng.Next(0, 4) == 0
                            ? new Color32(120, 116, 72, 255)
                            : new Color32(24, 24, 40, 255));
                    }
                }

                x0 += w + rng.Next(2, 14);
            }

            for (int x = 0; x < 512; x++)
            {
                int bump = 120 + Mathf.RoundToInt(Mathf.Sin(x * 0.05f) * 18f) + rng.Next(-6, 7);
                for (int y = 0; y < bump; y++) ruins.Set(x, y, new Color32(36, 34, 56, 255));
            }
            PixelArt.Save(ruins, WorldRoot + "/bg_ruins.png", new Vector2(0.5f, 0.5f), true);

            var harbor = new Canvas2D(512, 288);
            for (int i = 0; i < 5; i++)
            {
                int cx = 40 + i * 105 + rng.Next(-14, 14);
                int craneH = rng.Next(120, 170);
                harbor.Rect(cx, 0, 5, craneH, new Color32(24, 26, 40, 255));
                harbor.Rect(cx, craneH - 4, 70, 5, new Color32(24, 26, 40, 255));
                harbor.Rect(cx + 62, craneH - 30, 3, 26, new Color32(24, 26, 40, 255));
                harbor.Line(new Vector2(cx + 2f, craneH - 4f), new Vector2(cx + 20f, craneH - 34f), 3f, new Color32(24, 26, 40, 255));
                harbor.Line(new Vector2(cx + 20f, craneH - 34f), new Vector2(cx + 64f, craneH - 34f), 3f, new Color32(24, 26, 40, 255));
            }

            for (int i = 0; i < 9; i++)
            {
                int bx = rng.Next(0, 480);
                int by = 0;
                int stacks = rng.Next(1, 4);
                for (int s = 0; s < stacks; s++)
                {
                    var col = new Color32((byte)rng.Next(28, 56), (byte)rng.Next(30, 58), (byte)rng.Next(48, 76), 255);
                    harbor.Rect(bx, by, 46, 24, col);
                    harbor.RectOutline(bx, by, 46, 24, new Color32(18, 20, 32, 255));
                    by += 25;
                }
            }

            harbor.Rect(0, 0, 512, 26, new Color32(20, 22, 34, 255));
            PixelArt.Save(harbor, WorldRoot + "/bg_harbor.png", new Vector2(0.5f, 0.5f), true);

            var fog = new Canvas2D(512, 96);
            for (int y = 0; y < 96; y++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Lerp(70f, 0f, (float)y / 96f));
                for (int x = 0; x < 512; x++) fog.Set(x, y, new Color32(140, 160, 200, a));
            }
            PixelArt.Save(fog, WorldRoot + "/bg_fog.png", new Vector2(0.5f, 0.5f), true);
        }

        private static void Effects(System.Random rng)
        {
            var bullet = new Canvas2D(8, 3);
            bullet.Rect(0, 1, 8, 1, new Color32(255, 244, 180, 255));
            bullet.Rect(4, 0, 4, 3, new Color32(255, 214, 90, 255));
            bullet.Set(0, 1, new Color32(255, 255, 255, 255));
            PixelArt.Save(bullet, FxRoot + "/bullet_player.png", new Vector2(0.5f, 0.5f), false);

            var ebullet = new Canvas2D(8, 3);
            ebullet.Rect(0, 1, 8, 1, new Color32(255, 168, 168, 255));
            ebullet.Rect(4, 0, 4, 3, new Color32(240, 74, 74, 255));
            PixelArt.Save(ebullet, FxRoot + "/bullet_enemy.png", new Vector2(0.5f, 0.5f), false);

            var bomb = new Canvas2D(12, 12);
            bomb.Ellipse(6f, 5f, 5f, 5f, new Color32(56, 66, 52, 255));
            bomb.Ellipse(5f, 6f, 3f, 2.5f, new Color32(78, 92, 70, 255));
            bomb.Rect(6, 10, 2, 2, new Color32(120, 120, 130, 255));
            bomb.Set(9, 11, new Color32(255, 220, 90, 255));
            bomb.AddOutline(new Color32(18, 20, 24, 255));
            PixelArt.Save(bomb, FxRoot + "/bomb.png", new Vector2(0.5f, 0.5f), false);

            var muzzle = new Canvas2D(14, 12);
            muzzle.Ellipse(4f, 6f, 5f, 4f, new Color32(255, 226, 130, 255));
            muzzle.Ellipse(7f, 6f, 5f, 3f, new Color32(255, 176, 60, 255));
            muzzle.Rect(10, 5, 4, 2, new Color32(255, 248, 210, 255));
            PixelArt.Save(muzzle, FxRoot + "/muzzle_flash.png", new Vector2(0.5f, 0.5f), false);

            var impact = new Canvas2D(12, 12);
            impact.Ellipse(6f, 6f, 3f, 3f, new Color32(255, 240, 190, 255));
            for (int i = 0; i < 6; i++)
            {
                float ang = i * 60f * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                impact.Line(new Vector2(6f, 6f) + dir * 3f, new Vector2(6f, 6f) + dir * 5.4f, 1.6f, new Color32(255, 200, 90, 255));
            }
            PixelArt.Save(impact, FxRoot + "/impact.png", new Vector2(0.5f, 0.5f), false);

            for (int f = 0; f < 4; f++)
            {
                var ex = new Canvas2D(48, 48);
                float radius = 10f + f * 5f;

                ex.Ellipse(24f, 24f, radius, radius, new Color32(255, 128, 40, 255));
                ex.Ellipse(24f, 24f, radius * 0.78f, radius * 0.78f, new Color32(255, 186, 62, 255));
                ex.Ellipse(24f, 24f, radius * 0.5f, radius * 0.5f, new Color32(255, 244, 190, 255));

                for (int i = 0; i < 12; i++)
                {
                    float ang = (i * 30f + f * 9f) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    ex.Line(new Vector2(24f, 24f) + dir * radius * 0.7f,
                            new Vector2(24f, 24f) + dir * (radius + 4f + f * 2f),
                            3f, new Color32(255, 150, 50, 255));
                }

                for (int i = 0; i < 8; i++)
                    ex.Set(24 + rng.Next(-16, 16), 24 + rng.Next(-16, 16), new Color32(90, 84, 80, 190));

                PixelArt.Save(ex, FxRoot + "/explosion_" + f + ".png", new Vector2(0.5f, 0.5f), false);
            }

            var shadow = new Canvas2D(24, 8);
            shadow.Ellipse(12f, 4f, 10f, 3.2f, new Color32(8, 8, 14, 120));
            PixelArt.Save(shadow, FxRoot + "/shadow.png", new Vector2(0.5f, 0.5f), false);
        }

        private static void CheckpointAndGoal()
        {
            var flag = new Canvas2D(20, 44);
            flag.Rect(2, 0, 3, 44, new Color32(84, 88, 100, 255));
            flag.Rect(2, 42, 3, 2, new Color32(160, 168, 188, 255));
            flag.Rect(5, 30, 13, 11, new Color32(52, 150, 92, 255));
            flag.Rect(5, 30, 13, 2, new Color32(120, 210, 150, 255));
            flag.AddOutline(new Color32(18, 20, 26, 255));
            PixelArt.Save(flag, WorldRoot + "/checkpoint_flag.png", new Vector2(0.5f, 0f), false);

            var gate = new Canvas2D(48, 64);
            gate.Rect(2, 0, 44, 62, new Color32(58, 62, 76, 255));
            gate.RectOutline(2, 0, 44, 62, new Color32(28, 30, 40, 255));
            gate.Rect(6, 4, 36, 54, new Color32(34, 36, 48, 255));
            for (int y = 8; y < 56; y += 6) gate.Rect(8, y, 32, 2, new Color32(96, 130, 90, 255));
            gate.Rect(4, 58, 4, 4, new Color32(240, 90, 60, 255));
            gate.Rect(40, 58, 4, 4, new Color32(240, 90, 60, 255));
            gate.Rect(20, 60, 8, 2, new Color32(120, 240, 140, 255));
            PixelArt.Save(gate, WorldRoot + "/goal_gate.png", new Vector2(0.5f, 0f), false);

            var sign = new Canvas2D(40, 20);
            sign.Rect(0, 0, 40, 20, new Color32(30, 32, 42, 255));
            sign.RectOutline(0, 0, 40, 20, new Color32(190, 160, 60, 255));
            sign.Rect(4, 8, 32, 3, new Color32(210, 180, 70, 255));
            sign.Rect(4, 13, 22, 2, new Color32(150, 130, 60, 255));
            PixelArt.Save(sign, WorldRoot + "/sign.png", new Vector2(0.5f, 0f), false);
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }
    }
}
