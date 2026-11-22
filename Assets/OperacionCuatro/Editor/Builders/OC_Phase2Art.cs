using UnityEditor;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>
    /// Fase 2 — arte original del COMPLEJO DE GUERRA.
    ///
    /// Genera los sprites de los tres enemigos nuevos, los tiles metálicos del complejo, los
    /// props industriales, los fondos de parallax bg02_* y los efectos propios del nivel.
    /// Mantiene el mismo pipeline que la Fase 1 (PixelArt.Save: PPU 16, Point, sin compresión).
    /// </summary>
    public static class OC_Phase2Art
    {
        private const string EnemiesRoot = "Assets/OperacionCuatro/Sprites/Enemies/";
        private const string WorldRoot = "Assets/OperacionCuatro/Sprites/World/";
        private const string PropsRoot = "Assets/OperacionCuatro/Sprites/Props/";
        private const string FxRoot = "Assets/OperacionCuatro/Sprites/FX/";

        // Paleta industrial
        private static readonly Color32 Steel = new Color32(72, 82, 100, 255);
        private static readonly Color32 SteelDark = new Color32(42, 50, 64, 255);
        private static readonly Color32 SteelLight = new Color32(126, 140, 164, 255);
        private static readonly Color32 SteelEdge = new Color32(24, 28, 38, 255);
        private static readonly Color32 Warning = new Color32(214, 158, 40, 255);
        private static readonly Color32 HazardRed = new Color32(196, 62, 52, 255);
        private static readonly Color32 GlowCyan = new Color32(94, 224, 255, 255);
        private static readonly Color32 GlowOrange = new Color32(255, 158, 62, 255);

        public static void Build()
        {
            BuildArmored();
            BuildDrone();
            BuildTurret();
            BuildTiles();
            BuildProps();
            BuildBackgrounds();
            BuildEffects();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Arte de Fase 2 generado: 3 enemigos, tiles industriales, props, fondos y FX.");
        }

        // ==================================================================
        //  ENEMIGO A — SOLDADO BLINDADO (humanoide con escudo frontal)
        // ==================================================================
        private static void BuildArmored()
        {
            var style = new CharStyle
            {
                skin = new Color32(214, 178, 142, 255),
                skinShade = new Color32(176, 138, 104, 255),
                hair = new Color32(30, 30, 34, 255),
                shirt = new Color32(78, 88, 108, 255),
                shirtShade = new Color32(46, 54, 70, 255),
                pants = new Color32(52, 58, 74, 255),
                boots = new Color32(26, 28, 36, 255),
                glasses = new Color32(18, 20, 26, 255),
                accent = new Color32(230, 120, 52, 255),
                helmet = true,
                width = 1.26f,
                legLength = 12,
                armLength = 10
            };

            string dir = EnemiesRoot + "Armored";
            SaveFrame(dir, "Armored_idle_0", style, Pose.Idle());
            SaveFrame(dir, "Armored_idle_1", style, Pose.Idle2());
            SaveFrame(dir, "Armored_walk_0", style, Pose.Walk(22f, 0));
            SaveFrame(dir, "Armored_walk_1", style, Pose.Walk(3f, 1));
            SaveFrame(dir, "Armored_walk_2", style, Pose.Walk(-20f, 0));
            SaveFrame(dir, "Armored_walk_3", style, Pose.Walk(3f, 1));
            SaveFrame(dir, "Armored_shoot", style, Pose.Shoot());
            SaveFrame(dir, "Armored_hurt", style, Pose.Hurt());
            SaveFrame(dir, "Armored_death", style, Pose.Death());
        }

        // ==================================================================
        //  ENEMIGO B — DRON MILITAR (volador, 34x26)
        // ==================================================================
        private static void BuildDrone()
        {
            string dir = EnemiesRoot + "Drone";

            PixelArt.Save(DroneFrame(0, false, false), dir + "/Drone_idle_0.png", new Vector2(0.5f, 0.5f), false);
            PixelArt.Save(DroneFrame(1, false, false), dir + "/Drone_idle_1.png", new Vector2(0.5f, 0.5f), false);
            PixelArt.Save(DroneFrame(0, false, false), dir + "/Drone_walk_0.png", new Vector2(0.5f, 0.5f), false);
            PixelArt.Save(DroneFrame(2, false, false), dir + "/Drone_walk_1.png", new Vector2(0.5f, 0.5f), false);
            PixelArt.Save(DroneFrame(1, true, false), dir + "/Drone_shoot.png", new Vector2(0.5f, 0.5f), false);
            PixelArt.Save(DroneFrame(0, false, true), dir + "/Drone_hurt.png", new Vector2(0.5f, 0.5f), false);
            PixelArt.Save(DroneWreck(), dir + "/Drone_death.png", new Vector2(0.5f, 0.5f), false);
        }

        private static Canvas2D DroneFrame(int rotorPhase, bool firing, bool hurt)
        {
            var c = new Canvas2D(40, 30);

            Color32 body = hurt ? new Color32(168, 74, 62, 255) : Steel;
            Color32 bodyDark = hurt ? new Color32(112, 46, 40, 255) : SteelDark;

            // Sombra / contorno del casco
            c.Ellipse(20f, 16f, 13f, 8f, bodyDark);
            c.Ellipse(20f, 17f, 12f, 6.6f, body);

            // Placa superior
            c.Ellipse(18f, 19.5f, 8f, 3.4f, SteelLight);
            c.Rect(12, 20, 12, 1, SteelEdge);

            // Ojo sensor
            c.Ellipse(27f, 16f, 3.4f, 3.4f, SteelEdge);
            c.Ellipse(27f, 16f, 2.2f, 2.2f, firing ? Warning : HazardRed);
            if (firing) c.Ellipse(27f, 16f, 1.2f, 1.2f, new Color32(255, 248, 200, 255));

            // Cañón inferior
            c.Rect(24, 9, 7, 2, SteelEdge);
            c.Rect(30, 9, 2, 2, SteelLight);

            // Hélices
            int spin = rotorPhase == 1 ? 11 : (rotorPhase == 2 ? 9 : 13);
            c.Line(new Vector2(20f - spin, 24f), new Vector2(20f + spin, 24f), 2f, SteelLight);
            c.Rect(19, 23, 2, 3, SteelEdge);
            c.Line(new Vector2(4f, 9f), new Vector2(14f, 9f), 1.6f, SteelEdge);
            c.Line(new Vector2(26f, 9f), new Vector2(36f, 9f), 1.6f, SteelEdge);

            // Antena
            c.Rect(10, 22, 1, 5, SteelEdge);
            c.Set(10, 27, GlowCyan);

            // Luces de posición
            c.Set(12, 14, GlowOrange);
            c.Set(11, 14, GlowCyan);

            c.AddOutline(SteelEdge);
            return c;
        }

        private static Canvas2D DroneWreck()
        {
            var c = new Canvas2D(40, 30);
            c.Ellipse(20f, 13f, 12f, 5f, SteelDark);
            c.Ellipse(20f, 13f, 8f, 3f, SteelEdge);
            c.Line(new Vector2(8f, 8f), new Vector2(16f, 12f), 2f, Steel);
            c.Line(new Vector2(30f, 7f), new Vector2(24f, 11f), 2f, Steel);
            c.Set(14, 16, HazardRed);
            c.Set(26, 15, GlowOrange);
            c.Set(20, 19, new Color32(120, 120, 130, 200));
            c.AddOutline(SteelEdge);
            return c;
        }

        // ==================================================================
        //  ENEMIGO C — TORRETA AUTOMÁTICA (base 32x30 + cañón giratorio)
        // ==================================================================
        private static void BuildTurret()
        {
            string dir = EnemiesRoot + "Turret";

            PixelArt.Save(TurretBase(false), dir + "/Turret_idle_0.png", new Vector2(0.5f, 0.05f), false);
            PixelArt.Save(TurretBase(true), dir + "/Turret_shoot.png", new Vector2(0.5f, 0.05f), false);
            PixelArt.Save(TurretBase(false), dir + "/Turret_hurt.png", new Vector2(0.5f, 0.05f), false);
            PixelArt.Save(TurretWreck(), dir + "/Turret_death.png", new Vector2(0.5f, 0.05f), false);

            // Cañón: pivote a la izquierda para rotar desde la cúpula.
            var barrel = new Canvas2D(24, 9);
            barrel.Rect(0, 2, 24, 5, Steel);
            barrel.Rect(0, 5, 24, 2, SteelLight);
            barrel.Rect(0, 2, 24, 1, SteelDark);
            barrel.Rect(19, 1, 5, 7, SteelDark);
            barrel.Rect(22, 3, 2, 3, Warning);
            barrel.AddOutline(SteelEdge);
            PixelArt.Save(barrel, dir + "/Turret_barrel.png", new Vector2(0.08f, 0.5f), false);
        }

        private static Canvas2D TurretBase(bool firing)
        {
            var c = new Canvas2D(34, 30);

            // Pies de anclaje
            c.Rect(3, 0, 28, 3, SteelDark);
            c.Rect(1, 0, 4, 6, Steel);
            c.Rect(29, 0, 4, 6, Steel);

            // Columna
            c.Rect(8, 3, 18, 9, Steel);
            c.Rect(8, 3, 18, 2, SteelDark);
            c.Rect(8, 10, 18, 2, Warning);

            // Cúpula
            c.Ellipse(17f, 17f, 10f, 7f, Steel);
            c.Ellipse(17f, 19.5f, 7f, 3.4f, SteelLight);
            c.Rect(7, 12, 20, 2, SteelDark);

            // Ojo / visor
            c.Ellipse(21f, 17f, 2.6f, 2.6f, SteelEdge);
            c.Ellipse(21f, 17f, 1.6f, 1.6f, firing ? Warning : HazardRed);

            // Remaches
            for (int i = 10; i < 26; i += 4) c.Set(i, 11, SteelEdge);

            // Refuerzo trasero
            c.Rect(4, 14, 3, 6, SteelDark);
            c.Rect(2, 20, 1, 3, GlowCyan);

            c.AddOutline(SteelEdge);
            return c;
        }

        private static Canvas2D TurretWreck()
        {
            var c = new Canvas2D(34, 30);
            c.Rect(3, 0, 28, 3, SteelDark);
            c.Rect(8, 3, 18, 5, SteelDark);
            c.Ellipse(17f, 10f, 9f, 4f, Steel);
            c.Rect(9, 8, 4, 2, SteelEdge);
            c.Set(14, 12, HazardRed);
            c.Set(22, 9, GlowOrange);
            c.AddOutline(SteelEdge);
            return c;
        }

        // ==================================================================
        //  TILES DEL COMPLEJO
        // ==================================================================
        private static void BuildTiles()
        {
            var metal = new Canvas2D(16, 16);
            metal.Rect(0, 0, 16, 16, new Color32(78, 86, 102, 255));
            metal.Rect(0, 0, 16, 1, SteelDark);
            metal.Rect(0, 15, 16, 1, SteelDark);
            metal.Rect(0, 0, 1, 16, SteelDark);
            metal.Rect(15, 0, 1, 16, SteelDark);
            metal.Rect(2, 2, 12, 1, SteelLight);
            metal.Rect(2, 12, 12, 1, SteelDark);
            metal.Rect(7, 2, 1, 12, SteelDark);
            metal.Set(3, 7, SteelLight);
            metal.Set(12, 7, SteelLight);
            PixelArt.Save(metal, WorldRoot + "tile_metal.png", new Vector2(0.5f, 0.5f), true);

            var top = new Canvas2D(16, 16);
            top.Rect(0, 0, 16, 16, new Color32(64, 72, 88, 255));
            top.Rect(0, 14, 16, 2, SteelLight);
            top.Rect(0, 13, 16, 1, new Color32(96, 106, 126, 255));
            top.Rect(0, 0, 16, 2, SteelDark);
            for (int x = 1; x < 16; x += 4) top.Rect(x, 11, 1, 3, SteelEdge);
            for (int x = 3; x < 16; x += 8) top.Set(x, 15, Warning);
            PixelArt.Save(top, WorldRoot + "tile_metal_top.png", new Vector2(0.5f, 0.5f), true);

            var fill = new Canvas2D(16, 16);
            fill.Rect(0, 0, 16, 16, new Color32(44, 50, 64, 255));
            for (int i = 0; i < 20; i++) fill.Set((i * 7) % 16, (i * 5) % 16, new Color32(36, 42, 54, 255));
            fill.Rect(0, 6, 16, 1, new Color32(38, 44, 56, 255));
            PixelArt.Save(fill, WorldRoot + "tile_metal_fill.png", new Vector2(0.5f, 0.5f), true);

            var plate = new Canvas2D(16, 8);
            plate.Rect(0, 0, 16, 8, new Color32(70, 78, 94, 255));
            plate.Rect(0, 7, 16, 1, SteelLight);
            plate.Rect(0, 5, 16, 1, new Color32(102, 112, 132, 255));
            plate.Rect(0, 0, 16, 1, SteelEdge);
            for (int x = 1; x < 16; x += 5) plate.Rect(x, 1, 1, 4, SteelDark);
            for (int x = 3; x < 16; x += 5) plate.Set(x, 6, Warning);
            PixelArt.Save(plate, WorldRoot + "tile_platform_metal.png", new Vector2(0.5f, 0.5f), true);

            // Pasarela con barandilla (para las pasarelas de la sección de defensa)
            var catwalk = new Canvas2D(16, 16);
            catwalk.Rect(2, 0, 12, 4, new Color32(72, 80, 96, 255));
            catwalk.Rect(2, 3, 12, 1, SteelLight);
            catwalk.Rect(0, 6, 16, 1, new Color32(58, 66, 82, 255));
            catwalk.Rect(0, 11, 16, 1, new Color32(58, 66, 82, 255));
            catwalk.Rect(0, 6, 1, 8, new Color32(58, 66, 82, 255));
            catwalk.Rect(15, 6, 1, 8, new Color32(58, 66, 82, 255));
            catwalk.Set(8, 6, Warning);
            PixelArt.Save(catwalk, WorldRoot + "tile_catwalk.png", new Vector2(0.5f, 0.5f), true);
        }

        // ==================================================================
        //  PROPS INDUSTRIALES
        // ==================================================================
        private static void BuildProps()
        {
            // Caja metálica
            var crate = new Canvas2D(18, 16);
            crate.Rect(0, 0, 18, 16, new Color32(86, 94, 112, 255));
            crate.RectOutline(0, 0, 18, 16, SteelEdge);
            crate.Rect(1, 12, 16, 2, SteelLight);
            crate.Rect(1, 2, 16, 1, Warning);
            crate.Line(new Vector2(2, 3), new Vector2(16, 12), 1.4f, SteelDark);
            crate.Line(new Vector2(16, 3), new Vector2(2, 12), 1.4f, SteelDark);
            PixelArt.Save(crate, PropsRoot + "crate_metal.png", new Vector2(0.5f, 0f), false);

            // Bidón de acero
            var drum = new Canvas2D(14, 20);
            drum.Rect(1, 0, 12, 20, new Color32(96, 104, 122, 255));
            drum.RectOutline(1, 0, 12, 20, SteelEdge);
            drum.Rect(1, 15, 12, 2, SteelLight);
            drum.Rect(1, 9, 12, 1, SteelDark);
            drum.Rect(1, 5, 12, 1, SteelDark);
            drum.Rect(4, 12, 6, 2, Warning);
            PixelArt.Save(drum, PropsRoot + "drum_steel.png", new Vector2(0.5f, 0f), false);

            // Tubería vertical con bridas
            var pipe = new Canvas2D(12, 48);
            pipe.Rect(3, 0, 6, 48, new Color32(84, 92, 110, 255));
            pipe.Rect(3, 0, 2, 48, SteelLight);
            pipe.Rect(3, 0, 1, 48, SteelDark);
            for (int y = 6; y < 48; y += 14) pipe.Rect(1, y, 10, 3, SteelDark);
            pipe.Rect(4, 46, 4, 2, HazardRed);
            pipe.AddOutline(SteelEdge);
            PixelArt.Save(pipe, PropsRoot + "pipe_vertical.png", new Vector2(0.5f, 0f), false);

            // Rejilla de ventilación / salida de vapor
            var vent = new Canvas2D(20, 8);
            vent.Rect(0, 0, 20, 8, SteelDark);
            vent.RectOutline(0, 0, 20, 8, SteelEdge);
            for (int x = 2; x < 19; x += 3) vent.Rect(x, 1, 1, 6, new Color32(96, 106, 126, 255));
            vent.Set(1, 6, Warning);
            vent.Set(18, 6, Warning);
            PixelArt.Save(vent, PropsRoot + "vent_grate.png", new Vector2(0.5f, 0f), false);

            // Terminal de control
            var terminal = new Canvas2D(22, 26);
            terminal.Rect(2, 0, 18, 16, new Color32(70, 78, 96, 255));
            terminal.RectOutline(2, 0, 18, 16, SteelEdge);
            terminal.Rect(5, 8, 12, 8, new Color32(24, 46, 56, 255));
            for (int y = 10; y < 15; y += 2) terminal.Rect(6, y, 8, 1, GlowCyan);
            terminal.Rect(3, 16, 4, 8, SteelDark);
            terminal.Rect(8, 20, 10, 6, new Color32(58, 66, 82, 255));
            terminal.RectOutline(8, 20, 10, 6, SteelEdge);
            PixelArt.Save(terminal, PropsRoot + "terminal.png", new Vector2(0.5f, 0f), false);

            // Generador central
            PixelArt.Save(GeneratorSprite(), PropsRoot + "generator.png", new Vector2(0.5f, 0f), false);

            // Cúpula de escudo del generador
            var shield = new Canvas2D(64, 64);
            for (int i = 0; i < 3; i++)
            {
                float r = 30f - i * 3f;
                byte a = (byte)(70 - i * 18);
                shield.Ellipse(32f, 32f, r, r, new Color32(96, 200, 255, a));
            }
            for (int i = 0; i < 16; i++)
            {
                float ang = i * 22.5f * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                shield.Line(new Vector2(32f, 32f) + d * 26f, new Vector2(32f, 32f) + d * 31f, 1.4f, new Color32(150, 226, 255, 150));
            }
            PixelArt.Save(shield, PropsRoot + "generator_shield.png", new Vector2(0.5f, 0.5f), false);

            // Placa de escudo frontal del soldado blindado
            var plate = new Canvas2D(20, 26);
            plate.Rect(0, 0, 20, 26, new Color32(96, 104, 124, 255));
            plate.RectOutline(0, 0, 20, 26, SteelEdge);
            plate.Rect(2, 22, 16, 2, SteelLight);
            plate.Rect(2, 2, 16, 2, SteelDark);
            plate.Rect(8, 7, 4, 13, new Color32(122, 132, 154, 255));
            plate.Set(4, 18, Warning);
            plate.Set(15, 18, Warning);
            plate.Set(4, 5, Warning);
            plate.Set(15, 5, Warning);
            PixelArt.Save(plate, PropsRoot + "shield_plate.png", new Vector2(0.5f, 0.5f), false);

            // Cartel de aviso del complejo
            var sign = new Canvas2D(44, 20);
            sign.Rect(0, 0, 44, 20, new Color32(34, 38, 50, 255));
            sign.RectOutline(0, 0, 44, 20, Warning);
            for (int x = 4; x < 40; x += 8) sign.Rect(x, 3, 4, 2, Warning);
            sign.Rect(6, 9, 32, 4, new Color32(210, 180, 70, 255));
            sign.Rect(6, 15, 24, 2, new Color32(150, 130, 60, 255));
            PixelArt.Save(sign, PropsRoot + "warning_sign.png", new Vector2(0.5f, 0f), false);
        }

        private static Canvas2D GeneratorSprite()
        {
            var c = new Canvas2D(56, 72);

            // Base
            c.Rect(2, 0, 52, 8, SteelDark);
            c.Rect(4, 2, 10, 4, Steel);
            c.Rect(42, 2, 10, 4, Steel);
            c.Rect(2, 8, 52, 2, Warning);

            // Cuerpo
            c.Rect(6, 10, 44, 46, new Color32(76, 84, 102, 255));
            c.Rect(6, 10, 44, 2, SteelLight);
            c.Rect(6, 10, 2, 46, SteelDark);
            c.Rect(48, 10, 2, 46, SteelDark);

            // Núcleo
            c.Ellipse(28f, 36f, 15f, 17f, SteelEdge);
            c.Ellipse(28f, 36f, 13f, 15f, new Color32(40, 92, 108, 255));
            c.Ellipse(28f, 36f, 9f, 11f, new Color32(64, 168, 196, 255));
            c.Ellipse(28f, 36f, 5f, 6f, GlowCyan);
            c.Ellipse(28f, 36f, 2.4f, 3f, new Color32(238, 254, 255, 255));

            // Aros del núcleo
            c.Rect(12, 20, 32, 2, SteelDark);
            c.Rect(12, 50, 32, 2, SteelDark);

            // Tuberías laterales
            c.Rect(8, 14, 3, 40, new Color32(96, 104, 122, 255));
            c.Rect(45, 14, 3, 40, new Color32(96, 104, 122, 255));

            // Remates y luces
            c.Rect(10, 56, 36, 8, Steel);
            c.Rect(10, 62, 36, 2, SteelLight);
            c.Set(14, 59, HazardRed);
            c.Set(28, 59, GlowOrange);
            c.Set(42, 59, GlowCyan);

            // Sobre el remate, antena
            c.Rect(27, 64, 2, 8, SteelDark);
            c.Set(27, 71, HazardRed);

            c.AddOutline(SteelEdge);
            return c;
        }

        // ==================================================================
        //  FONDOS DE PARALLAX
        // ==================================================================
        private static void BuildBackgrounds()
        {
            var rng = new System.Random(20261122);

            // Cielo nocturno industrial
            var sky = new Canvas2D(512, 288);
            for (int y = 0; y < 288; y++)
            {
                float t = (float)y / 288f;
                Color32 col;
                if (t > 0.7f) col = Lerp(new Color32(16, 22, 38, 255), new Color32(30, 44, 70, 255), (t - 0.7f) / 0.3f);
                else if (t > 0.35f) col = Lerp(new Color32(28, 34, 52, 255), new Color32(16, 22, 38, 255), (t - 0.35f) / 0.35f);
                else col = Lerp(new Color32(58, 44, 46, 255), new Color32(28, 34, 52, 255), t / 0.35f);

                for (int x = 0; x < 512; x++) sky.Set(x, y, col);
            }

            for (int i = 0; i < 120; i++)
                sky.Set(rng.Next(0, 512), rng.Next(150, 288), new Color32(210, 224, 255, (byte)rng.Next(60, 190)));

            // Haz de reflector
            sky.Line(new Vector2(96f, 40f), new Vector2(150f, 250f), 22f, new Color32(120, 150, 190, 26));
            sky.Line(new Vector2(340f, 40f), new Vector2(300f, 240f), 18f, new Color32(120, 150, 190, 22));

            sky.Ellipse(430f, 232f, 20f, 20f, new Color32(226, 232, 240, 255));
            sky.Ellipse(424f, 236f, 17f, 17f, new Color32(30, 44, 70, 255));
            PixelArt.Save(sky, WorldRoot + "bg02_sky.png", new Vector2(0.5f, 0.5f), true);

            // Complejo: naves, chimeneas y ventanas
            var facility = new Canvas2D(512, 288);
            int x0 = 0;
            while (x0 < 512)
            {
                int w = rng.Next(40, 84);
                int h = rng.Next(60, 150);
                var body = new Color32((byte)rng.Next(30, 46), (byte)rng.Next(36, 52), (byte)rng.Next(50, 68), 255);
                facility.Rect(x0, 0, w, h, body);
                facility.Rect(x0, h - 3, w, 3, new Color32(
                    (byte)Mathf.Min(255, body.r + 34),
                    (byte)Mathf.Min(255, body.g + 34),
                    (byte)Mathf.Min(255, body.b + 34), 255));

                // Ventanas iluminadas
                for (int wy = 10; wy < h - 10; wy += 13)
                {
                    for (int wx = x0 + 5; wx < x0 + w - 5; wx += 9)
                    {
                        bool lit = rng.Next(0, 5) == 0;
                        facility.Rect(wx, wy, 3, 4, lit ? new Color32(226, 178, 76, 255) : new Color32(20, 26, 38, 255));
                    }
                }

                // Chimenea ocasional
                if (rng.Next(0, 3) == 0)
                {
                    int cx = x0 + rng.Next(6, Mathf.Max(7, w - 10));
                    facility.Rect(cx, h, 6, rng.Next(20, 50), new Color32(26, 30, 42, 255));
                    facility.Rect(cx - 1, h + 8, 8, 2, new Color32(40, 46, 60, 255));
                }

                x0 += w + rng.Next(2, 12);
            }

            for (int x = 0; x < 512; x++)
            {
                int bump = 96 + Mathf.RoundToInt(Mathf.Sin(x * 0.03f) * 14f) + rng.Next(-5, 6);
                for (int y = 0; y < bump; y++) facility.Set(x, y, new Color32(24, 28, 40, 255));
            }
            PixelArt.Save(facility, WorldRoot + "bg02_facility.png", new Vector2(0.5f, 0.5f), true);

            // Red de tuberías cercana
            var pipes = new Canvas2D(512, 288);
            for (int i = 0; i < 7; i++)
            {
                int y = 30 + i * 34 + rng.Next(-6, 6);
                int thickness = rng.Next(5, 10);
                pipes.Rect(0, y, 512, thickness, new Color32(52, 58, 74, 255));
                pipes.Rect(0, y + thickness - 2, 512, 2, new Color32(74, 82, 102, 255));

                for (int bx = rng.Next(20, 90); bx < 512; bx += rng.Next(90, 170))
                {
                    pipes.Rect(bx, y - 3, 7, thickness + 6, new Color32(38, 44, 58, 255));
                    pipes.Set(bx + 3, y + 2, new Color32(196, 122, 48, 255));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                int px = 60 + i * 120 + rng.Next(-20, 20);
                pipes.Rect(px, 0, 12, 288, new Color32(44, 50, 66, 255));
                pipes.Rect(px + 3, 0, 3, 288, new Color32(60, 68, 86, 255));
            }
            PixelArt.Save(pipes, WorldRoot + "bg02_pipes.png", new Vector2(0.5f, 0.5f), true);

            // Bruma cálida de fondo
            var haze = new Canvas2D(512, 96);
            for (int y = 0; y < 96; y++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Lerp(64f, 0f, (float)y / 96f));
                for (int x = 0; x < 512; x++) haze.Set(x, y, new Color32(180, 150, 130, a));
            }
            PixelArt.Save(haze, WorldRoot + "bg02_haze.png", new Vector2(0.5f, 0.5f), true);
        }

        // ==================================================================
        //  EFECTOS
        // ==================================================================
        private static void BuildEffects()
        {
            // Bala pesada
            var heavy = new Canvas2D(10, 4);
            heavy.Rect(0, 1, 10, 2, new Color32(255, 236, 170, 255));
            heavy.Rect(2, 0, 8, 4, new Color32(255, 168, 60, 255));
            heavy.Set(0, 1, new Color32(255, 255, 255, 255));
            heavy.Set(9, 2, new Color32(255, 255, 255, 255));
            PixelArt.Save(heavy, FxRoot + "bullet_heavy.png", new Vector2(0.5f, 0.5f), false);

            // Bala de torreta (más grande y roja)
            var turret = new Canvas2D(10, 4);
            turret.Rect(0, 1, 10, 2, new Color32(255, 196, 196, 255));
            turret.Rect(3, 0, 7, 4, new Color32(232, 72, 62, 255));
            turret.Set(0, 1, new Color32(255, 244, 220, 255));
            PixelArt.Save(turret, FxRoot + "bullet_turret.png", new Vector2(0.5f, 0.5f), false);

            // Icono de la ametralladora pesada (recogible)
            var icon = new Canvas2D(30, 14);
            icon.Rect(2, 4, 20, 6, new Color32(58, 64, 80, 255));
            icon.Rect(2, 8, 20, 2, new Color32(96, 106, 126, 255));
            icon.Rect(20, 2, 8, 3, new Color32(52, 58, 74, 255));
            icon.Rect(20, 4, 8, 1, new Color32(120, 130, 150, 255));
            icon.Rect(21, 7, 6, 5, new Color32(44, 48, 62, 255));
            icon.Rect(8, 10, 4, 4, new Color32(44, 48, 62, 255));
            icon.Rect(4, 2, 3, 3, new Color32(214, 158, 40, 255));
            icon.Rect(0, 13, 30, 1, new Color32(255, 210, 110, 255));
            icon.AddOutline(new Color32(20, 22, 30, 255));
            PixelArt.Save(icon, FxRoot + "heavy_weapon.png", new Vector2(0.5f, 0.5f), false);

            // Nube de vapor
            var steam = new Canvas2D(28, 28);
            steam.Ellipse(14f, 10f, 10f, 8f, new Color32(226, 234, 244, 150));
            steam.Ellipse(10f, 16f, 8f, 7f, new Color32(240, 246, 255, 120));
            steam.Ellipse(19f, 17f, 7f, 6f, new Color32(214, 224, 238, 110));
            steam.Ellipse(14f, 22f, 6f, 5f, new Color32(250, 252, 255, 90));
            PixelArt.Save(steam, FxRoot + "steam_cloud.png", new Vector2(0.5f, 0.5f), false);

            // Campo eléctrico (tileable horizontalmente)
            var field = new Canvas2D(32, 32);
            field.Rect(0, 0, 32, 32, new Color32(96, 200, 255, 40));
            for (int i = 0; i < 5; i++)
            {
                float y0 = 3 + i * 6;
                var a = new Vector2(0f, y0);
                var b = new Vector2(8f, y0 + 4f);
                var cc = new Vector2(16f, y0 - 3f);
                var d = new Vector2(24f, y0 + 3f);
                var e = new Vector2(32f, y0);
                field.Line(a, b, 1.6f, new Color32(150, 230, 255, 210));
                field.Line(b, cc, 1.6f, new Color32(150, 230, 255, 210));
                field.Line(cc, d, 1.6f, new Color32(150, 230, 255, 210));
                field.Line(d, e, 1.6f, new Color32(150, 230, 255, 210));
            }
            field.Rect(0, 0, 32, 2, new Color32(210, 245, 255, 220));
            field.Rect(0, 30, 32, 2, new Color32(210, 245, 255, 220));
            PixelArt.Save(field, FxRoot + "electric_field.png", new Vector2(0.5f, 0.5f), true);

            // Chispa de escudo (impacto bloqueado)
            var spark = new Canvas2D(20, 20);
            spark.Ellipse(10f, 10f, 3f, 3f, new Color32(210, 245, 255, 255));
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                spark.Line(new Vector2(10f, 10f) + dir * 3f, new Vector2(10f, 10f) + dir * 8f, 1.5f, new Color32(140, 220, 255, 235));
            }
            PixelArt.Save(spark, FxRoot + "shield_spark.png", new Vector2(0.5f, 0.5f), false);
        }

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private static void SaveFrame(string dir, string fileName, CharStyle style, Pose pose)
        {
            var canvas = PixelArt.DrawCharacter(style, pose);
            PixelArt.Save(canvas, dir + "/" + fileName + ".png", new Vector2(0.5f, 3f / 52f), false);
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
