using UnityEditor;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>
    /// Fase 3 — arte original del LABORATORIO SUBTERRÁNEO.
    ///
    /// Genera los sprites de los tres enemigos nuevos, los tiles y props de laboratorio, los
    /// cuatro fondos de parallax bg03_* y los efectos del arma láser. Usa el mismo pipeline que
    /// la Fase 1 y la Fase 2 (PixelArt.Save: PPU 16, filtro Point, sin compresión) para que el
    /// pixel art siga siendo coherente con Level01 y Level02.
    /// </summary>
    public static class OC_Phase3Art
    {
        private const string EnemiesRoot = "Assets/OperacionCuatro/Sprites/Enemies/";
        private const string WorldRoot = "Assets/OperacionCuatro/Sprites/World/";
        private const string PropsRoot = "Assets/OperacionCuatro/Sprites/Props/";
        private const string FxRoot = "Assets/OperacionCuatro/Sprites/FX/";

        // ---- Paleta de laboratorio ----
        private static readonly Color32 Steel = new Color32(86, 96, 112, 255);
        private static readonly Color32 SteelDark = new Color32(46, 52, 64, 255);
        private static readonly Color32 SteelDeep = new Color32(30, 34, 44, 255);
        private static readonly Color32 SteelLight = new Color32(140, 154, 176, 255);
        private static readonly Color32 Edge = new Color32(20, 24, 32, 255);
        private static readonly Color32 Cyan = new Color32(86, 226, 255, 255);
        private static readonly Color32 CyanDim = new Color32(44, 132, 164, 255);
        private static readonly Color32 Red = new Color32(222, 68, 62, 255);
        private static readonly Color32 RedDim = new Color32(132, 44, 42, 255);
        private static readonly Color32 Green = new Color32(110, 226, 140, 255);
        private static readonly Color32 Amber = new Color32(230, 176, 62, 255);
        private static readonly Color32 Glass = new Color32(52, 108, 138, 255);

        public static void Build()
        {
            BuildSentinel();
            BuildLaserDrone();
            BuildGrenadier();
            BuildTiles();
            BuildProps();
            BuildBackgrounds();
            BuildEffects();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Arte de Fase 3 generado: centinela, dron láser, granadero, tiles de laboratorio, props, fondos y FX.");
        }

        // ==================================================================
        //  ENEMIGO 1 — ROBOT CENTINELA
        // ==================================================================
        private static void BuildSentinel()
        {
            var style = new CharStyle
            {
                skin = new Color32(176, 190, 206, 255),
                skinShade = new Color32(120, 134, 152, 255),
                hair = new Color32(40, 46, 58, 255),
                shirt = new Color32(96, 108, 126, 255),
                shirtShade = new Color32(58, 68, 84, 255),
                pants = new Color32(70, 80, 96, 255),
                boots = new Color32(34, 38, 48, 255),
                glasses = new Color32(86, 226, 255, 255),
                accent = new Color32(222, 68, 62, 255),
                helmet = true,
                width = 1.3f,
                legLength = 12,
                armLength = 11
            };

            string dir = EnemiesRoot + "Sentinel";
            SaveFrame(dir, "Sentinel_idle_0", style, Pose.Idle());
            SaveFrame(dir, "Sentinel_idle_1", style, Pose.Idle2());
            SaveFrame(dir, "Sentinel_walk_0", style, Pose.Walk(22f, 0));
            SaveFrame(dir, "Sentinel_walk_1", style, Pose.Walk(3f, 1));
            SaveFrame(dir, "Sentinel_walk_2", style, Pose.Walk(-20f, 0));
            SaveFrame(dir, "Sentinel_walk_3", style, Pose.Walk(3f, 1));
            SaveFrame(dir, "Sentinel_shoot", style, Pose.Shoot());
            SaveFrame(dir, "Sentinel_hurt", style, Pose.Hurt());
            SaveFrame(dir, "Sentinel_death", style, Pose.Death());

            // Destrucción: chatarra humeante (no es un personaje, es un montón de piezas).
            var wreck = new Canvas2D(52, 52);
            wreck.Ellipse(26f, 5f, 17f, 4f, new Color32(38, 42, 52, 255));
            wreck.Rect(10, 3, 32, 5, SteelDark);
            wreck.Rect(14, 7, 9, 4, Steel);
            wreck.Rect(28, 6, 8, 5, SteelLight);
            wreck.Rect(20, 9, 4, 3, RedDim);
            wreck.Rect(33, 9, 3, 2, CyanDim);
            wreck.Rect(8, 4, 3, 2, Steel);
            wreck.AddOutline(Edge);
            PixelArt.Save(wreck, dir + "/Sentinel_wreck.png", new Vector2(0.5f, 3f / 52f), false);
        }

        // ==================================================================
        //  ENEMIGO 2 — DRON LÁSER
        // ==================================================================
        private static void BuildLaserDrone()
        {
            string dir = EnemiesRoot + "LaserDrone";
            SaveDrone(dir, "LaserDrone_idle_0", 0, false);
            SaveDrone(dir, "LaserDrone_idle_1", 1, false);
            SaveDrone(dir, "LaserDrone_walk_0", 0, false);
            SaveDrone(dir, "LaserDrone_walk_1", 1, false);
            SaveDrone(dir, "LaserDrone_shoot", 2, false);
            SaveDrone(dir, "LaserDrone_hurt", 3, false);
            SaveDrone(dir, "LaserDrone_death", 4, false);
        }

        /// <summary>34x26. Variante 0/1 = rotores, 2 = disparo, 3 = daño, 4 = chatarra.</summary>
        private static void SaveDrone(string dir, string fileName, int variant, bool unused)
        {
            var c = new Canvas2D(34, 26);

            if (variant == 4)
            {
                c.Ellipse(17f, 6f, 13f, 4f, new Color32(34, 38, 48, 255));
                c.Rect(8, 4, 18, 4, SteelDark);
                c.Rect(13, 6, 6, 3, RedDim);
                c.Rect(21, 5, 4, 2, CyanDim);
                c.AddOutline(Edge);
                PixelArt.Save(c, dir + "/" + fileName + ".png", new Vector2(0.5f, 0.5f), false);
                return;
            }

            bool hurt = variant == 3;
            bool firing = variant == 2;

            // Rotores superiores
            int rotorY = 21;
            c.Rect(2, rotorY, 9, 2, SteelLight);
            c.Rect(23, rotorY, 9, 2, SteelLight);
            if (variant == 1)
            {
                c.Rect(4, rotorY + 2, 5, 1, Steel);
                c.Rect(25, rotorY + 2, 5, 1, Steel);
            }
            else
            {
                c.Rect(2, rotorY + 2, 3, 1, Steel);
                c.Rect(29, rotorY + 2, 3, 1, Steel);
            }

            // Brazos
            c.Rect(9, 17, 2, 4, SteelDark);
            c.Rect(23, 17, 2, 4, SteelDark);

            // Casco
            c.Ellipse(17f, 13f, 12f, 8f, hurt ? new Color32(150, 74, 68, 255) : Steel);
            c.Rect(6, 8, 22, 3, SteelDark);
            c.Rect(8, 6, 18, 2, SteelDeep);

            // Cúpula
            c.Ellipse(17f, 15f, 7f, 5f, Glass);
            c.Ellipse(16f, 16f, 4f, 3f, new Color32(86, 150, 182, 255));

            // Óptica láser
            Color32 optic = firing ? new Color32(255, 240, 240, 255) : Red;
            c.Ellipse(17f, 11f, 3f, 2.4f, optic);
            if (firing)
            {
                c.Ellipse(17f, 11f, 5f, 3.6f, new Color32(255, 120, 110, 200));
                c.Rect(16, 2, 2, 7, new Color32(255, 200, 196, 230));
            }

            // Cañones inferiores
            c.Rect(11, 4, 3, 3, SteelDark);
            c.Rect(20, 4, 3, 3, SteelDark);

            c.AddOutline(Edge);
            PixelArt.Save(c, dir + "/" + fileName + ".png", new Vector2(0.5f, 0.5f), false);
        }

        // ==================================================================
        //  ENEMIGO 3 — SOLDADO EXPLOSIVO
        // ==================================================================
        private static void BuildGrenadier()
        {
            var style = new CharStyle
            {
                skin = new Color32(226, 190, 158, 255),
                skinShade = new Color32(184, 146, 112, 255),
                hair = new Color32(36, 30, 26, 255),
                shirt = new Color32(112, 94, 58, 255),
                shirtShade = new Color32(76, 62, 36, 255),
                pants = new Color32(64, 56, 40, 255),
                boots = new Color32(30, 28, 24, 255),
                glasses = new Color32(24, 22, 24, 255),
                accent = new Color32(222, 68, 62, 255),
                helmet = true,
                width = 1.1f,
                legLength = 12,
                armLength = 10
            };

            string dir = EnemiesRoot + "Grenadier";
            SaveFrame(dir, "Grenadier_idle_0", style, Pose.Idle());
            SaveFrame(dir, "Grenadier_idle_1", style, Pose.Idle2());
            SaveFrame(dir, "Grenadier_walk_0", style, Pose.Walk(22f, 0));
            SaveFrame(dir, "Grenadier_walk_1", style, Pose.Walk(3f, 1));
            SaveFrame(dir, "Grenadier_walk_2", style, Pose.Walk(-20f, 0));
            SaveFrame(dir, "Grenadier_walk_3", style, Pose.Walk(3f, 1));
            SaveFrame(dir, "Grenadier_shoot", style, Pose.Throw());
            SaveFrame(dir, "Grenadier_hurt", style, Pose.Hurt());
            SaveFrame(dir, "Grenadier_death", style, Pose.Death());
        }

        // ==================================================================
        //  TILES DE LABORATORIO
        // ==================================================================
        private static void BuildTiles()
        {
            // Suelo: chapa clara con tira luminosa cian
            var top = new Canvas2D(16, 16);
            top.Rect(0, 0, 16, 16, new Color32(66, 74, 88, 255));
            top.Rect(0, 14, 16, 2, SteelLight);
            top.Rect(0, 13, 16, 1, new Color32(104, 116, 136, 255));
            top.Rect(0, 0, 16, 2, SteelDark);
            for (int x = 1; x < 16; x += 4) top.Rect(x, 11, 1, 3, Edge);
            for (int x = 6; x < 16; x += 8) top.Rect(x, 15, 2, 1, Cyan);
            PixelArt.Save(top, WorldRoot + "tile_lab_top.png", new Vector2(0.5f, 0.5f), true);

            // Relleno: paneles oscuros con pernos
            var fill = new Canvas2D(16, 16);
            fill.Rect(0, 0, 16, 16, new Color32(38, 44, 56, 255));
            fill.Rect(0, 0, 16, 1, new Color32(28, 32, 42, 255));
            fill.Rect(0, 15, 16, 1, new Color32(28, 32, 42, 255));
            fill.Rect(0, 0, 1, 16, new Color32(28, 32, 42, 255));
            fill.Rect(15, 0, 1, 16, new Color32(28, 32, 42, 255));
            fill.Set(4, 4, Steel);
            fill.Set(11, 4, Steel);
            fill.Set(4, 11, Steel);
            fill.Set(11, 11, Steel);
            PixelArt.Save(fill, WorldRoot + "tile_lab_fill.png", new Vector2(0.5f, 0.5f), true);

            // Placa de plataforma
            var plate = new Canvas2D(16, 8);
            plate.Rect(0, 0, 16, 8, new Color32(78, 88, 104, 255));
            plate.Rect(0, 7, 16, 1, SteelLight);
            plate.Rect(0, 5, 16, 1, new Color32(108, 120, 140, 255));
            plate.Rect(0, 0, 16, 1, Edge);
            for (int x = 2; x < 16; x += 6) plate.Set(x, 6, Cyan);
            PixelArt.Save(plate, WorldRoot + "tile_lab_plate.png", new Vector2(0.5f, 0.5f), true);

            // Pasarela con barandilla
            var catwalk = new Canvas2D(16, 16);
            catwalk.Rect(2, 0, 12, 4, new Color32(74, 84, 100, 255));
            catwalk.Rect(2, 3, 12, 1, SteelLight);
            catwalk.Rect(0, 6, 16, 1, new Color32(56, 64, 80, 255));
            catwalk.Rect(0, 11, 16, 1, new Color32(56, 64, 80, 255));
            catwalk.Rect(0, 6, 1, 8, new Color32(56, 64, 80, 255));
            catwalk.Rect(15, 6, 1, 8, new Color32(56, 64, 80, 255));
            catwalk.Set(8, 6, Cyan);
            PixelArt.Save(catwalk, WorldRoot + "tile_lab_catwalk.png", new Vector2(0.5f, 0.5f), true);

            // Panel de pared con monitor
            var wall = new Canvas2D(16, 16);
            wall.Rect(0, 0, 16, 16, new Color32(52, 58, 72, 255));
            wall.RectOutline(0, 0, 16, 16, new Color32(34, 38, 48, 255));
            wall.Rect(3, 8, 10, 5, new Color32(26, 40, 52, 255));
            wall.Rect(4, 9, 8, 3, CyanDim);
            wall.Set(5, 10, Cyan);
            wall.Set(7, 10, Cyan);
            wall.Rect(3, 3, 10, 1, new Color32(34, 38, 48, 255));
            PixelArt.Save(wall, WorldRoot + "tile_lab_panel.png", new Vector2(0.5f, 0.5f), true);
        }

        // ==================================================================
        //  PROPS DE LABORATORIO
        // ==================================================================
        private static void BuildProps()
        {
            // ---- Hoja de puerta blindada (16x32, se repite en vertical) ----
            var door = new Canvas2D(16, 32);
            door.Rect(0, 0, 16, 32, new Color32(74, 84, 100, 255));
            door.RectOutline(0, 0, 16, 32, Edge);
            door.Rect(2, 0, 12, 1, SteelLight);
            door.Rect(2, 10, 12, 1, SteelDark);
            door.Rect(2, 21, 12, 1, SteelDark);
            door.Rect(6, 4, 4, 5, new Color32(54, 62, 78, 255));
            door.Rect(6, 14, 4, 5, new Color32(54, 62, 78, 255));
            door.Rect(6, 24, 4, 5, new Color32(54, 62, 78, 255));
            PixelArt.Save(door, PropsRoot + "lab_door_blade.png", new Vector2(0.5f, 0.5f), true);

            // ---- Marco de puerta con luz de estado (18x48) ----
            var frame = new Canvas2D(18, 48);
            frame.Rect(0, 0, 3, 48, new Color32(58, 66, 82, 255));
            frame.Rect(15, 0, 3, 48, new Color32(58, 66, 82, 255));
            frame.Rect(0, 44, 18, 4, new Color32(58, 66, 82, 255));
            frame.Rect(3, 44, 12, 2, SteelDark);
            frame.RectOutline(0, 0, 18, 48, Edge);
            PixelArt.Save(frame, PropsRoot + "lab_door_frame.png", new Vector2(0.5f, 0.5f), false);

            // ---- Interruptor: inactivo y activo (16x20) ----
            var swOff = new Canvas2D(16, 20);
            swOff.Rect(1, 0, 14, 20, new Color32(60, 68, 84, 255));
            swOff.RectOutline(1, 0, 14, 20, Edge);
            swOff.Rect(2, 15, 12, 3, SteelDark);
            swOff.Rect(4, 8, 8, 6, new Color32(28, 34, 44, 255));
            swOff.Rect(7, 8, 2, 6, Red);
            swOff.Ellipse(8f, 4f, 2f, 2f, RedDim);
            PixelArt.Save(swOff, PropsRoot + "lab_switch_off.png", new Vector2(0.5f, 0f), false);

            var swOn = new Canvas2D(16, 20);
            swOn.Rect(1, 0, 14, 20, new Color32(60, 68, 84, 255));
            swOn.RectOutline(1, 0, 14, 20, Edge);
            swOn.Rect(2, 15, 12, 3, SteelDark);
            swOn.Rect(4, 8, 8, 6, new Color32(28, 34, 44, 255));
            swOn.Rect(7, 8, 2, 6, Green);
            swOn.Ellipse(8f, 4f, 2f, 2f, Green);
            swOn.Line(new Vector2(3f, 12f), new Vector2(12f, 14f), 1.4f, Green);
            PixelArt.Save(swOn, PropsRoot + "lab_switch_on.png", new Vector2(0.5f, 0f), false);

            // ---- Unidad de alimentación: encendida y apagada (24x40) ----
            BuildPowerUnitSprite("lab_power_unit.png", true);
            BuildPowerUnitSprite("lab_power_off.png", false);

            // ---- Plataforma de ascensor (32x8) ----
            var lift = new Canvas2D(32, 8);
            lift.Rect(0, 0, 32, 8, new Color32(80, 90, 108, 255));
            lift.Rect(0, 7, 32, 1, SteelLight);
            lift.Rect(0, 5, 32, 1, new Color32(112, 124, 144, 255));
            lift.Rect(0, 0, 32, 1, Edge);
            for (int x = 3; x < 32; x += 8) lift.Rect(x, 2, 5, 2, new Color32(58, 66, 82, 255));
            lift.Rect(0, 6, 2, 2, Cyan);
            lift.Rect(30, 6, 2, 2, Cyan);
            PixelArt.Save(lift, PropsRoot + "lab_elevator.png", new Vector2(0.5f, 0.5f), true);

            // ---- Emisor láser (10x14) ----
            var emitter = new Canvas2D(10, 14);
            emitter.Rect(1, 0, 8, 14, new Color32(64, 72, 88, 255));
            emitter.RectOutline(1, 0, 8, 14, Edge);
            emitter.Rect(3, 2, 4, 4, new Color32(26, 32, 42, 255));
            emitter.Rect(4, 4, 2, 2, Cyan);
            emitter.Rect(2, 10, 6, 2, SteelDark);
            PixelArt.Save(emitter, PropsRoot + "lab_laser_emitter.png", new Vector2(0.5f, 0f), false);

            // ---- Reactor decorativo (20x36) ----
            var reactor = new Canvas2D(20, 36);
            reactor.Rect(2, 0, 16, 36, new Color32(56, 64, 80, 255));
            reactor.RectOutline(2, 0, 16, 36, Edge);
            reactor.Rect(5, 6, 10, 24, new Color32(28, 48, 62, 255));
            reactor.Rect(6, 8, 8, 20, CyanDim);
            reactor.Rect(7, 12, 6, 3, Cyan);
            reactor.Rect(7, 20, 6, 3, Cyan);
            reactor.Rect(2, 32, 16, 2, SteelLight);
            reactor.Rect(2, 2, 16, 2, SteelDark);
            PixelArt.Save(reactor, PropsRoot + "lab_reactor.png", new Vector2(0.5f, 0f), false);

            // ---- Monitor de control (18x14) ----
            var monitor = new Canvas2D(18, 14);
            monitor.Rect(0, 0, 18, 14, new Color32(44, 50, 64, 255));
            monitor.RectOutline(0, 0, 18, 14, Edge);
            monitor.Rect(2, 3, 14, 9, new Color32(22, 34, 44, 255));
            monitor.Rect(3, 5, 5, 1, Green);
            monitor.Rect(3, 7, 8, 1, CyanDim);
            monitor.Rect(3, 9, 6, 1, CyanDim);
            monitor.Rect(0, 0, 18, 2, SteelDark);
            PixelArt.Save(monitor, PropsRoot + "lab_monitor.png", new Vector2(0.5f, 0f), false);

            // ---- Señal de advertencia (14x14) ----
            var sign = new Canvas2D(14, 14);
            sign.Rect(1, 0, 12, 14, new Color32(44, 48, 60, 255));
            sign.RectOutline(1, 0, 12, 14, Edge);
            sign.Rect(3, 5, 8, 6, Amber);
            sign.Rect(4, 10, 6, 3, Amber);
            sign.Rect(6, 6, 2, 4, new Color32(40, 30, 12, 255));
            sign.Set(7, 7, new Color32(40, 30, 12, 255));
            PixelArt.Save(sign, PropsRoot + "lab_sign.png", new Vector2(0.5f, 0f), false);

            // ---- Caja de laboratorio (18x16) ----
            var crate = new Canvas2D(18, 16);
            crate.Rect(0, 0, 18, 16, new Color32(70, 80, 96, 255));
            crate.RectOutline(0, 0, 18, 16, Edge);
            crate.Rect(1, 12, 16, 2, SteelLight);
            crate.Rect(1, 2, 16, 1, CyanDim);
            crate.Line(new Vector2(2, 3), new Vector2(16, 12), 1.3f, SteelDark);
            crate.Line(new Vector2(16, 3), new Vector2(2, 12), 1.3f, SteelDark);
            PixelArt.Save(crate, PropsRoot + "lab_crate.png", new Vector2(0.5f, 0f), false);
        }

        private static void BuildPowerUnitSprite(string fileName, bool on)
        {
            var c = new Canvas2D(24, 40);

            c.Rect(2, 0, 20, 40, new Color32(58, 66, 82, 255));
            c.RectOutline(2, 0, 20, 40, Edge);
            c.Rect(2, 34, 20, 3, SteelLight);
            c.Rect(2, 3, 20, 3, SteelDark);

            // Núcleo
            c.Rect(6, 9, 12, 22, new Color32(24, 32, 42, 255));
            if (on)
            {
                c.Rect(7, 11, 10, 18, CyanDim);
                c.Rect(9, 14, 6, 4, Cyan);
                c.Rect(9, 22, 6, 4, Cyan);
                c.Rect(11, 11, 2, 18, new Color32(210, 250, 255, 255));
            }
            else
            {
                c.Rect(7, 11, 10, 18, new Color32(46, 50, 58, 255));
                c.Rect(11, 11, 2, 18, new Color32(64, 68, 76, 255));
                c.Rect(9, 28, 6, 2, RedDim);
            }

            // Aletas
            for (int y = 6; y < 34; y += 5)
            {
                c.Rect(2, y, 3, 2, SteelDark);
                c.Rect(19, y, 3, 2, SteelDark);
            }

            // Aviso de peligro
            c.Rect(9, 1, 6, 2, RedDim);

            PixelArt.Save(c, PropsRoot + fileName, new Vector2(0.5f, 0f), false);
        }

        // ==================================================================
        //  FONDOS DE PARALLAX
        // ==================================================================
        private static void BuildBackgrounds()
        {
            var rng = new System.Random(20261123);

            // ---- Capa lejana: muro estructural del laboratorio ----
            var wall = new Canvas2D(512, 288);
            for (int y = 0; y < 288; y++)
            {
                float t = (float)y / 288f;
                Color32 col = t > 0.6f
                    ? Lerp(new Color32(20, 28, 40, 255), new Color32(30, 42, 58, 255), (t - 0.6f) / 0.4f)
                    : Lerp(new Color32(12, 16, 26, 255), new Color32(20, 28, 40, 255), t / 0.6f);
                for (int x = 0; x < 512; x++) wall.Set(x, y, col);
            }

            // Retícula de paneles
            for (int x = 0; x < 512; x += 64) wall.Rect(x, 0, 1, 288, new Color32(16, 22, 32, 255));
            for (int y = 0; y < 288; y += 72) wall.Rect(0, y, 512, 1, new Color32(16, 22, 32, 255));

            // Tiras de luz cian en el techo
            for (int x = 20; x < 512; x += 96)
            {
                wall.Rect(x, 250, 40, 3, new Color32(120, 220, 255, 150));
                wall.Rect(x + 4, 246, 32, 3, new Color32(80, 150, 190, 90));
            }

            // Vigas
            for (int x = 0; x < 512; x += 128)
            {
                wall.Rect(x + 40, 0, 8, 288, new Color32(16, 22, 32, 255));
                wall.Rect(x + 41, 0, 2, 288, new Color32(26, 34, 46, 255));
            }
            PixelArt.Save(wall, WorldRoot + "bg03_wall.png", new Vector2(0.5f, 0.5f), true);

            // ---- Capa media: maquinaria, tanques y pantallas ----
            var lab = new Canvas2D(512, 288);
            int x0 = 0;
            while (x0 < 512)
            {
                int w = rng.Next(48, 96);
                int h = rng.Next(70, 170);
                var body = new Color32((byte)rng.Next(30, 44), (byte)rng.Next(38, 54), (byte)rng.Next(52, 72), 255);
                lab.Rect(x0, 0, w, h, body);
                lab.Rect(x0, h - 2, w, 2, new Color32(
                    (byte)Mathf.Min(255, body.r + 30),
                    (byte)Mathf.Min(255, body.g + 30),
                    (byte)Mathf.Min(255, body.b + 30), 255));

                // Tanques de cristal iluminados
                for (int tx = x0 + 8; tx < x0 + w - 12; tx += 22)
                {
                    int th = rng.Next(28, 54);
                    lab.Rect(tx, 8, 12, th, new Color32(24, 48, 64, 255));
                    lab.Rect(tx + 2, 12, 8, th - 8, new Color32(48, 120, 152, 255));
                    lab.Rect(tx + 4, 18, 4, 6, new Color32(120, 220, 255, 255));
                    lab.Rect(tx + 1, 8, 10, 2, new Color32(60, 70, 86, 255));
                }

                // Pantallas
                for (int sy = 80; sy < h - 16; sy += 34)
                {
                    lab.Rect(x0 + 10, sy, 22, 12, new Color32(18, 30, 40, 255));
                    lab.Rect(x0 + 12, sy + 2, 18, 8, rng.Next(0, 2) == 0
                        ? new Color32(52, 150, 180, 255)
                        : new Color32(150, 120, 46, 255));
                }

                x0 += w + rng.Next(4, 18);
            }

            // Base de suelo de la capa
            for (int x = 0; x < 512; x++)
            {
                int bump = 42 + Mathf.RoundToInt(Mathf.Sin(x * 0.021f) * 8f);
                for (int y = 0; y < bump; y++) lab.Set(x, y, new Color32(16, 22, 32, 255));
            }
            PixelArt.Save(lab, WorldRoot + "bg03_lab.png", new Vector2(0.5f, 0.5f), true);

            // ---- Capa cercana: cables, conductos y tuberías ----
            var pipes = new Canvas2D(512, 288);
            for (int i = 0; i < 6; i++)
            {
                int y = 40 + i * 38 + rng.Next(-8, 8);
                int thickness = rng.Next(4, 8);
                pipes.Rect(0, y, 512, thickness, new Color32(44, 52, 68, 255));
                pipes.Rect(0, y + thickness - 2, 512, 2, new Color32(68, 78, 96, 255));

                for (int bx = rng.Next(20, 90); bx < 512; bx += rng.Next(80, 160))
                {
                    pipes.Rect(bx, y - 2, 6, thickness + 4, new Color32(32, 38, 50, 255));
                    pipes.Set(bx + 3, y + 2, CyanDim);
                }
            }

            // Conductos verticales
            for (int i = 0; i < 5; i++)
            {
                int px = 50 + i * 108 + rng.Next(-16, 16);
                pipes.Rect(px, 0, 10, 288, new Color32(38, 44, 58, 255));
                pipes.Rect(px + 2, 0, 2, 288, new Color32(54, 62, 78, 255));
            }

            // Cables colgantes
            for (int i = 0; i < 8; i++)
            {
                int cx = rng.Next(10, 500);
                float sag = rng.Next(20, 46);
                for (int s = 0; s < 40; s++)
                {
                    float t = s / 40f;
                    float px = cx + t * 18f;
                    float py = 288f - Mathf.Sin(t * Mathf.PI) * sag;
                    pipes.Set(Mathf.RoundToInt(px), Mathf.RoundToInt(py), new Color32(30, 36, 46, 255));
                }
            }
            PixelArt.Save(pipes, WorldRoot + "bg03_pipes.png", new Vector2(0.5f, 0.5f), true);

            // ---- Niebla inferior fría ----
            var fog = new Canvas2D(512, 96);
            for (int y = 0; y < 96; y++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Lerp(70f, 0f, (float)y / 96f));
                for (int x = 0; x < 512; x++) fog.Set(x, y, new Color32(120, 170, 200, a));
            }
            PixelArt.Save(fog, WorldRoot + "bg03_fog.png", new Vector2(0.5f, 0.5f), true);
        }

        // ==================================================================
        //  EFECTOS
        // ==================================================================
        private static void BuildEffects()
        {
            // ---- Proyectil del arma láser del jugador ----
            var bolt = new Canvas2D(16, 6);
            bolt.Rect(0, 2, 16, 2, new Color32(180, 250, 255, 255));
            bolt.Rect(3, 1, 12, 4, new Color32(96, 226, 255, 255));
            bolt.Rect(6, 0, 9, 6, new Color32(60, 170, 220, 200));
            bolt.Set(0, 2, new Color32(255, 255, 255, 255));
            bolt.Set(0, 3, new Color32(255, 255, 255, 255));
            PixelArt.Save(bolt, FxRoot + "bullet_laser.png", new Vector2(0.5f, 0.5f), false);

            // ---- Proyectil láser enemigo ----
            var enemyBolt = new Canvas2D(14, 6);
            enemyBolt.Rect(0, 2, 14, 2, new Color32(255, 208, 200, 255));
            enemyBolt.Rect(2, 1, 11, 4, new Color32(236, 74, 66, 255));
            enemyBolt.Rect(5, 0, 8, 6, new Color32(160, 40, 38, 200));
            enemyBolt.Set(0, 2, new Color32(255, 255, 255, 255));
            PixelArt.Save(enemyBolt, FxRoot + "bullet_laser_enemy.png", new Vector2(0.5f, 0.5f), false);

            // ---- Haz de la trampa láser (8x16, se repite en vertical) ----
            var beam = new Canvas2D(8, 16);
            beam.Rect(3, 0, 2, 16, new Color32(255, 240, 236, 255));
            beam.Rect(1, 0, 6, 16, new Color32(240, 90, 80, 210));
            beam.Rect(0, 0, 8, 16, new Color32(200, 50, 46, 90));
            PixelArt.Save(beam, FxRoot + "laser_beam.png", new Vector2(0.5f, 0.5f), true);

            // ---- Aviso de carga del dron ----
            var charge = new Canvas2D(12, 12);
            charge.Ellipse(6f, 6f, 5f, 5f, new Color32(220, 60, 54, 120));
            charge.Ellipse(6f, 6f, 3.4f, 3.4f, new Color32(255, 130, 120, 200));
            charge.Ellipse(6f, 6f, 1.8f, 1.8f, new Color32(255, 250, 246, 255));
            PixelArt.Save(charge, FxRoot + "laser_charge.png", new Vector2(0.5f, 0.5f), false);

            // ---- Icono del recogible de arma láser ----
            var icon = new Canvas2D(20, 10);
            icon.Rect(1, 3, 14, 4, new Color32(78, 88, 104, 255));
            icon.Rect(4, 5, 8, 2, new Color32(96, 226, 255, 255));
            icon.Rect(15, 4, 4, 2, new Color32(200, 250, 255, 255));
            icon.Rect(2, 1, 6, 2, new Color32(58, 66, 82, 255));
            icon.Rect(2, 7, 6, 2, new Color32(58, 66, 82, 255));
            icon.RectOutline(0, 2, 16, 6, new Color32(24, 28, 38, 255));
            PixelArt.Save(icon, FxRoot + "laser_weapon.png", new Vector2(0.5f, 0.5f), false);

            // ---- Chispa de impacto energética ----
            var spark = new Canvas2D(10, 10);
            spark.Line(new Vector2(5f, 0f), new Vector2(5f, 10f), 1.2f, new Color32(160, 240, 255, 255));
            spark.Line(new Vector2(0f, 5f), new Vector2(10f, 5f), 1.2f, new Color32(160, 240, 255, 255));
            spark.Ellipse(5f, 5f, 2.2f, 2.2f, new Color32(255, 255, 255, 255));
            PixelArt.Save(spark, FxRoot + "lab_spark.png", new Vector2(0.5f, 0.5f), false);
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
