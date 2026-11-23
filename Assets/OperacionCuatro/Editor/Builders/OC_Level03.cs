using System.Collections.Generic;
using OperacionCuatro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCBuilders
{
    /// <summary>
    /// Fase 3 — escena Level03 «LABORATORIO SUBTERRÁNEO».
    ///
    /// Cinco secciones: ACCESO SUBTERRÁNEO, LABORATORIOS DE INVESTIGACIÓN, CENTRO DE
    /// AUTOMATIZACIÓN, CÁMARA DE ENERGÍA y EVACUACIÓN DE EMERGENCIA. Reutiliza LevelManager,
    /// CameraFollow2D, LevelHUD, PlayerHudPanel y todos los sistemas de las fases anteriores;
    /// añade los tres enemigos nuevos, las puertas con interruptores, la red de energía, los
    /// ascensores, las trampas láser, el arma láser y la secuencia de escape.
    /// </summary>
    public static class OC_Level03
    {
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";
        private const string PrefabRoot = "Assets/OperacionCuatro/Prefabs/";
        private const string EnemyRoot = "Assets/OperacionCuatro/Resources/Enemies/";
        private const string InputAssetPath = "Assets/OperacionCuatro/Settings/OC_InputActions.inputactions";

        private static readonly Color Bone = new Color(0.94f, 0.96f, 0.99f, 1f);
        private static readonly Color Cyan = new Color(0.36f, 0.88f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.7f, 0.28f, 1f);
        private static readonly Color Dim = new Color(0.04f, 0.06f, 0.1f, 0.78f);

        private const float WorldMinX = -9f;
        private const float WorldMaxX = 259f;
        private const float WorldCenterX = (WorldMinX + WorldMaxX) * 0.5f;
        private const float WorldWidth = WorldMaxX - WorldMinX;

        // ==================================================================
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var follow = BuildCamera();
            var world = new GameObject("World").transform;
            var systems = new GameObject("Systems");

            BuildParallax(world);
            BuildGround(world);
            BuildPlatforms(world);
            BuildProps(world);
            BuildHazards(world);

            var levelManager = systems.AddComponent<LevelManager>();
            levelManager.inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            levelManager.cameraFollow = follow;
            levelManager.spawnPositions = new[]
            {
                new Vector3(3f, 0.9f, 0f),
                new Vector3(1.4f, 0.9f, 0f)
            };
            levelManager.checkpointPosition = new Vector3(3f, 0.9f, 0f);

            var security = BuildSecurity(world);
            var powerUnits = BuildPowerRoom(world);

            var enemiesRoot = new GameObject("Enemies").transform;
            BuildEnemies(enemiesRoot);

            BuildPickups(world);
            var escape = BuildEscape(systems.transform, world, powerUnits);

            BuildTriggers(systems.transform);
            BuildAlarm(world);

            var hud = BuildHud(levelManager);
            levelManager.hud = hud;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OC_Scenes.SceneFolder + "Level03.unity");
            RegisterBuildSettings();

            Debug.Log("[OC] Level03 creado: " + WorldWidth + " unidades de ancho, " +
                      enemiesRoot.childCount + " enemigos, " + security + " puertas con interruptores, " +
                      powerUnits.Count + " unidades de energía, escape=" + (escape != null) + ".");
        }

        // ==================================================================
        //  Cámara
        // ==================================================================
        private static CameraFollow2D BuildCamera()
        {
            var rig = new GameObject("CameraRig");
            rig.AddComponent<CameraShake>();

            var cam = OC_UI.NewCamera("Main Camera", new Color(0.03f, 0.05f, 0.08f), 6.2f);
            cam.transform.SetParent(rig.transform, false);
            cam.transform.position = new Vector3(11f, 3.6f, -10f);

            var follow = cam.gameObject.AddComponent<CameraFollow2D>();
            follow.minX = 11f;
            follow.maxX = 250f;
            follow.minY = 3.4f;
            follow.maxY = 8.5f;
            follow.baseOrthoSize = 6.2f;
            follow.zoomOutPerUnit = 0.11f;
            follow.maxSeparation = 11f;
            follow.useBounds = true;

            return follow;
        }

        // ==================================================================
        //  Parallax multicapa
        // ==================================================================
        private static void BuildParallax(Transform parent)
        {
            var root = new GameObject("Parallax").transform;
            root.SetParent(parent, false);

            AddParallaxLayer(root, "Wall", "World/bg03_wall.png", -50, 4.6f, 260f, 18f, 0.03f, Color.white);
            AddParallaxLayer(root, "Lab", "World/bg03_lab.png", -40, 2.2f, 260f, 18f, 0.13f, new Color(0.86f, 0.92f, 1f, 1f));
            AddParallaxLayer(root, "Pipes", "World/bg03_pipes.png", -30, 1.6f, 260f, 18f, 0.27f, new Color(0.9f, 0.95f, 1f, 1f));
            AddParallaxLayer(root, "Fog", "World/bg03_fog.png", -20, 1.2f, 260f, 6f, 0.42f, new Color(0.82f, 0.92f, 1f, 0.55f));
        }

        private static void AddParallaxLayer(Transform parent, string name, string sprite, int order,
            float y, float width, float height, float factor, Color tint)
        {
            var block = NewTiledBlock(name, parent, sprite, WorldCenterX, y, width, height, "Background", order, false);
            var sr = block.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = tint;

            var layer = block.AddComponent<ParallaxLayer>();
            layer.parallaxFactor = factor;
            layer.cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        // ==================================================================
        //  Suelo continuo de laboratorio
        // ==================================================================
        private static void BuildGround(Transform parent)
        {
            var root = new GameObject("Ground").transform;
            root.SetParent(parent, false);

            NewTiledBlock("Ground_Fill", root, "World/tile_lab_fill.png", WorldCenterX, -4f, WorldWidth, 8f, "Environment", 0, true);
            NewTiledBlock("Ground_Top", root, "World/tile_lab_top.png", WorldCenterX, -0.5f, WorldWidth, 1f, "Environment", 1, false);

            // Sección 1
            AddLedge(root, "EntryStep", 8f, 0.6f, 5f);
            AddLedge(root, "LoadingDock", 24f, 1f, 6f);

            // Sección 4 — cámara de energía
            AddLedge(root, "EnergyLedge1", 162f, 1.6f, 6f);
            AddLedge(root, "EnergyLedge2", 180f, 1.8f, 6f);
            AddLedge(root, "EnergyHighLedge", 200f, 3.2f, 6f);

            // Sección 5 — evacuación
            AddLedge(root, "EvacStep1", 212f, 0.8f, 6f);
            AddLedge(root, "EvacStep2", 222f, 1.6f, 6f);
            AddLedge(root, "EvacStep3", 234f, 0.8f, 6f);
            AddLedge(root, "EvacPad", 246f, 1.4f, 12f);
        }

        private static void AddLedge(Transform parent, string name, float centerX, float topY, float width)
        {
            float centerY = topY * 0.5f;
            NewTiledBlock(name + "_Fill", parent, "World/tile_lab_fill.png", centerX, centerY, width, topY, "Environment", 0, true);
            NewTiledBlock(name + "_Top", parent, "World/tile_lab_top.png", centerX, topY - 0.5f, width, 1f, "Environment", 1, false);
        }

        // ==================================================================
        //  Plataformas
        // ==================================================================
        private static void BuildPlatforms(Transform parent)
        {
            var root = new GameObject("Platforms").transform;
            root.SetParent(parent, false);

            // Sección 1 — acceso
            NewTiledBlock("Platform_S1a", root, "World/tile_lab_plate.png", 15f, 2.4f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S1b", root, "World/tile_lab_plate.png", 31f, 3f, 4f, 0.62f, "Environment", 2, true);

            // Sección 2 — laboratorios
            NewTiledBlock("Platform_S2a", root, "World/tile_lab_plate.png", 54f, 2.2f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S2b", root, "World/tile_lab_plate.png", 66f, 3f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S2c", root, "World/tile_lab_plate.png", 80f, 2.4f, 4f, 0.62f, "Environment", 2, true);

            // Sección 3 — centro de automatización: pasarela alta del ascensor
            NewTiledBlock("Catwalk_A", root, "World/tile_lab_catwalk.png", 110f, 5.6f, 20f, 0.5f, "Environment", 2, true);
            NewTiledBlock("Platform_S3a", root, "World/tile_lab_plate.png", 128f, 2.2f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S3b", root, "World/tile_lab_plate.png", 140f, 3f, 4f, 0.62f, "Environment", 2, true);

            // Sección 4 — cámara de energía
            NewTiledBlock("Platform_S4a", root, "World/tile_lab_plate.png", 165f, 3.4f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S4b", root, "World/tile_lab_plate.png", 192f, 3.2f, 4f, 0.62f, "Environment", 2, true);

            // Sección 5 — evacuación
            NewTiledBlock("Platform_S5a", root, "World/tile_lab_plate.png", 216f, 2.8f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S5b", root, "World/tile_lab_plate.png", 240f, 2.6f, 4f, 0.62f, "Environment", 2, true);
        }

        // ==================================================================
        //  Props y decoración
        // ==================================================================
        private static void BuildProps(Transform parent)
        {
            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);

            var back = new GameObject("Props_Back").transform;
            back.SetParent(root, false);

            // Paneles de pared: refuerzan la lectura de «laboratorio» sin tapar la línea de fuego.
            float[] panels = { 6f, 18f, 34f, 44f, 58f, 70f, 82f, 98f, 124f, 138f, 156f, 172f, 188f, 204f, 220f, 236f };
            for (int i = 0; i < panels.Length; i++)
                AddProp(back, "WallPanel_" + Mathf.RoundToInt(panels[i]), "World/tile_lab_panel.png", panels[i], 2.6f, false, "Environment", 3, 3.2f);

            // Reactores y monitores
            AddProp(back, "Reactor_1", "Props/lab_reactor.png", 27f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Reactor_2", "Props/lab_reactor.png", 62f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Reactor_3", "Props/lab_reactor.png", 112f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Reactor_4", "Props/lab_reactor.png", 178f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Reactor_5", "Props/lab_reactor.png", 232f, 0f, false, "Environment", 3, 1f);

            AddProp(back, "Monitor_1", "Props/lab_monitor.png", 24f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Monitor_2", "Props/lab_monitor.png", 88f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Monitor_3", "Props/lab_monitor.png", 150f, 0f, false, "Environment", 3, 1f);
            AddProp(back, "Monitor_4", "Props/lab_monitor.png", 208f, 0f, false, "Environment", 3, 1f);

            AddProp(back, "Sign_Acceso", "Props/lab_sign.png", 4f, 2.2f, false, "Environment", 3, 1f);
            AddProp(back, "Sign_Labs", "Props/lab_sign.png", 45f, 2.2f, false, "Environment", 3, 1f);
            AddProp(back, "Sign_Automatizacion", "Props/lab_sign.png", 99f, 2.2f, false, "Environment", 3, 1f);
            AddProp(back, "Sign_Energia", "Props/lab_sign.png", 154f, 2.2f, false, "Environment", 3, 1f);
            AddProp(back, "Sign_Evac", "Props/lab_sign.png", 210f, 2.2f, false, "Environment", 3, 1f);

            // Props sólidos bajos
            AddProp(root, "Crate_1", "Props/lab_crate.png", 20f, 0f, true, "Environment", 4, 1f);
            AddProp(root, "Crate_2", "Props/lab_crate.png", 56f, 0f, true, "Environment", 4, 1f);
            // Crate_3 estaba a 130, dentro del carril de patrulla del granadero de 132: lo detenía
            // (nunca volvía a patrullar) y le bloqueaba el retroceso a cubierto. Se sube a la
            // plataforma contigua, donde sigue leyendo como equipo almacenado.
            AddProp(root, "Crate_3", "Props/lab_crate.png", 128f, 2.51f, true, "Environment", 4, 1f);
            AddProp(root, "Crate_4", "Props/lab_crate.png", 186f, 0f, true, "Environment", 4, 1f);
            AddProp(root, "Crate_5", "Props/lab_crate.png", 238f, 0f, true, "Environment", 4, 1f);
        }

        private static void AddProp(Transform parent, string name, string spriteRel, float x, float y, bool solid,
            string sortingLayer, int order, float scale)
        {
            var sprite = S(spriteRel);
            if (sprite == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;

            if (!solid) return;

            go.layer = LayerMask.NameToLayer("Ground");
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(sprite.bounds.size.x * 0.9f, sprite.bounds.size.y * 0.95f);
            box.offset = new Vector2(sprite.bounds.center.x, sprite.bounds.center.y);
        }

        // ==================================================================
        //  Peligros: trampas láser y ascensores
        // ==================================================================
        private static void BuildHazards(Transform parent)
        {
            var root = new GameObject("Hazards").transform;
            root.SetParent(parent, false);

            // Ascensor esencial: sube del suelo a la pasarela del centro de automatización.
            AddElevator(root, "Elevator_1", 100f, 0f, 5.85f);
            // Ascensor secundario: acceso a la repisa alta de la cámara de energía.
            AddElevator(root, "Elevator_2", 196f, 0f, 3.2f);

            // Trampas láser sobre la pasarela (sección 3)
            AddLaserTrap(root, "LaserTrap_106", 106f, 5.85f, 3.2f, 0.4f, 0.8f, 1.4f, 1.6f);
            AddLaserTrap(root, "LaserTrap_114", 114f, 5.85f, 3.2f, 0.4f, 0.8f, 1.4f, 1.6f);

            // Trampas láser a nivel de suelo (secciones 4 y 5)
            AddLaserTrap(root, "LaserTrap_158", 158f, 0f, 3.4f, 1.2f, 0.8f, 1.5f, 1.7f);
            AddLaserTrap(root, "LaserTrap_174", 174f, 0f, 3.4f, 1.2f, 0.8f, 1.5f, 1.7f);
            AddLaserTrap(root, "LaserTrap_216", 216f, 0f, 3.4f, 0.2f, 0.7f, 1.2f, 1.5f);
            AddLaserTrap(root, "LaserTrap_230", 230f, 0f, 3.4f, 0.2f, 0.7f, 1.2f, 1.5f);
        }

        private static void AddElevator(Transform parent, string name, float x, float baseY, float travel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, baseY - 0.25f, 0f);
            go.layer = LayerMask.NameToLayer("Ground");

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3.2f, 0.5f);

            var visual = NewSimpleSprite("Visual", go.transform, "Props/lab_elevator.png", Vector3.zero,
                "Environment", 6, Color.white);
            var vsr = visual.GetComponent<SpriteRenderer>();
            vsr.drawMode = SpriteDrawMode.Tiled;
            vsr.size = new Vector2(3.2f, 0.5f);

            var up = NewSimpleSprite("ArrowUp", go.transform, "Props/lab_laser_emitter.png",
                new Vector3(0f, 0.42f, 0f), "Effects", 8, Cyan);
            up.transform.localScale = new Vector3(0.5f, 0.4f, 1f);
            up.SetActive(false);

            var down = NewSimpleSprite("ArrowDown", go.transform, "Props/lab_laser_emitter.png",
                new Vector3(0f, 0.42f, 0f), "Effects", 8, Amber);
            down.transform.localScale = new Vector3(0.5f, 0.4f, 1f);
            down.SetActive(false);

            var lift = go.AddComponent<ElevatorPlatform>();
            lift.travel = travel;
            lift.speed = 1.8f;
            lift.waitAtEnds = 0.9f;
            lift.arrowUpVisual = up;
            lift.arrowDownVisual = down;
        }

        private static void AddLaserTrap(Transform parent, string name, float x, float baseY, float height,
            float startOffset, float offTime, float warningTime, float activeTime)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, baseY, 0f);
            go.layer = LayerMask.NameToLayer("TriggerZone");

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.45f, height);
            box.offset = new Vector2(0f, height * 0.5f);

            var beam = NewSimpleSprite("Beam", go.transform, "FX/laser_beam.png", new Vector3(0f, height * 0.5f, 0f),
                "Effects", 8, new Color(1f, 0.3f, 0.26f, 0.95f));
            var beamSr = beam.GetComponent<SpriteRenderer>();
            beamSr.drawMode = SpriteDrawMode.Tiled;
            beamSr.size = new Vector2(0.45f, height);

            var emitter = NewSimpleSprite("Emitter", go.transform, "Props/lab_laser_emitter.png", Vector3.zero,
                "Environment", 7, Color.white);

            var warning = NewSimpleSprite("Warning", go.transform, "FX/laser_charge.png",
                new Vector3(0f, height - 0.2f, 0f), "Effects", 9, new Color(1f, 0.6f, 0.3f, 0.9f));
            warning.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            warning.SetActive(false);

            var trap = go.AddComponent<LaserTrap>();
            trap.beamVisual = beamSr;
            trap.emitterVisual = emitter.GetComponent<SpriteRenderer>();
            trap.warningVisual = warning;
            trap.damage = 18;
            trap.damageInterval = 0.5f;
            trap.startOffset = startOffset;
            trap.offTime = offTime;
            trap.warningTime = warningTime;
            trap.activeTime = activeTime;
        }

        // ==================================================================
        //  Puertas de seguridad e interruptores
        // ==================================================================
        private static int BuildSecurity(Transform parent)
        {
            var root = new GameObject("Security").transform;
            root.SetParent(parent, false);

            var doors = new GameObject("Doors").transform;
            doors.SetParent(root, false);
            var switches = new GameObject("Switches").transform;
            switches.SetParent(root, false);

            // Sección 2 — primer bloqueo: dos interruptores abren la puerta de los laboratorios.
            AddDoorSwitch(switches, "Switch_A1", 41f, 0f, "A", 2);
            AddDoorSwitch(switches, "Switch_A2", 46f, 0f, "A", 2);
            AddDoor(doors, "Door_A", 49f, 0f, 3.2f, "A", 2, false);

            // Sección 2 — segundo bloqueo antes del centro de automatización.
            AddDoorSwitch(switches, "Switch_B1", 76f, 0f, "B", 2);
            AddDoorSwitch(switches, "Switch_B2", 84f, 0f, "B", 2);
            AddDoor(doors, "Door_B", 88f, 0f, 3.2f, "B", 2, false);

            // Sección 4 → 5 — la puerta de energía solo se abre con las tres unidades destruidas.
            AddDoor(doors, "Door_P", 204f, 0f, 3.2f, "P", 3, true);

            return doors.childCount + switches.childCount;
        }

        private static void AddDoor(Transform parent, string name, float x, float topY, float height,
            string channel, int required, bool fromPower)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, topY, 0f);
            go.layer = LayerMask.NameToLayer("Ground");

            var blocker = go.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(0.6f, height);
            blocker.offset = new Vector2(0f, height * 0.5f);

            // Hoja móvil
            var leaf = new GameObject("Leaf");
            leaf.transform.SetParent(go.transform, false);
            leaf.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

            var leafSr = leaf.AddComponent<SpriteRenderer>();
            leafSr.sprite = S("Props/lab_door_blade.png");
            leafSr.drawMode = SpriteDrawMode.Tiled;
            leafSr.size = new Vector2(0.9f, height);
            leafSr.sortingLayerName = "Environment";
            leafSr.sortingOrder = 5;

            // Marco decorativo
            var frame = NewSimpleSprite("Frame", go.transform, "Props/lab_door_frame.png",
                new Vector3(0f, height * 0.5f, 0f), "Environment", 6, Color.white);
            var frameSr = frame.GetComponent<SpriteRenderer>();
            frameSr.drawMode = SpriteDrawMode.Tiled;
            frameSr.size = new Vector2(1.1f, height + 0.4f);

            // Estado cerrado agrupado: la lámpara roja y la barrera se ocultan juntas al abrir.
            var lockedGroup = new GameObject("LockedState");
            lockedGroup.transform.SetParent(go.transform, false);
            lockedGroup.transform.localPosition = Vector3.zero;

            var locked = NewSimpleSprite("StatusLocked", lockedGroup.transform, "Props/lab_sign.png",
                new Vector3(0f, height + 0.35f, 0f), "Effects", 9, new Color(1f, 0.34f, 0.3f, 1f));
            var open = NewSimpleSprite("StatusOpen", go.transform, "Props/lab_sign.png",
                new Vector3(0f, height + 0.35f, 0f), "Effects", 9, new Color(0.4f, 0.95f, 0.5f, 1f));
            open.SetActive(false);

            // Barrera de aviso mientras está bloqueada
            var barrier = NewSimpleSprite("Barrier", lockedGroup.transform, "FX/laser_beam.png",
                new Vector3(0f, height * 0.5f, 0f), "Effects", 4, new Color(1f, 0.3f, 0.26f, 0.28f));
            var barrierSr = barrier.GetComponent<SpriteRenderer>();
            barrierSr.drawMode = SpriteDrawMode.Tiled;
            barrierSr.size = new Vector2(0.3f, height);

            var door = go.AddComponent<SecurityDoor>();
            door.channel = channel;
            door.requiredCount = required;
            door.countPowerUnits = fromPower;
            door.blocker = blocker;
            door.doorLeaf = leaf.transform;
            door.openOffset = new Vector2(0f, height + 0.4f);
            door.lockedVisual = lockedGroup;
            door.openVisual = open;
            door.statusLight = locked.GetComponent<SpriteRenderer>();
            door.lockedColor = new Color(1f, 0.34f, 0.3f, 1f);
            door.openColor = new Color(0.4f, 0.95f, 0.5f, 1f);
        }

        private static void AddDoorSwitch(Transform parent, string name, float x, float topY, string channel, int hits)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, topY, 0f);
            go.layer = LayerMask.NameToLayer("Ground");

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.7f, 1.25f);
            box.offset = new Vector2(0f, 0.625f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 40;
            health.faction = Faction.Enemy;

            var off = NewSimpleSprite("VisualOff", go.transform, "Props/lab_switch_off.png", Vector3.zero,
                "Environment", 6, Color.white);
            var on = NewSimpleSprite("VisualOn", go.transform, "Props/lab_switch_on.png", Vector3.zero,
                "Environment", 7, Color.white);
            on.SetActive(false);

            var sw = go.AddComponent<DoorSwitch>();
            sw.channel = channel;
            sw.hitsRequired = hits;
            sw.health = health;
            sw.inactiveVisual = off;
            sw.activeVisual = on;
        }

        // ==================================================================
        //  Red de energía (cámara de energía)
        // ==================================================================
        private static List<PowerUnit> BuildPowerRoom(Transform parent)
        {
            var root = new GameObject("PowerGrid").transform;
            root.SetParent(parent, false);

            var list = new List<PowerUnit>();

            float[] positions = { 154f, 170f, 186f };
            for (int i = 0; i < positions.Length; i++)
                list.Add(AddPowerUnit(root, "PowerUnit_" + Mathf.RoundToInt(positions[i]), positions[i], 0f, "P"));

            return list;
        }

        private static PowerUnit AddPowerUnit(Transform parent, string name, float x, float baseY, string channel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, baseY, 0f);
            go.layer = LayerMask.NameToLayer("Enemy");

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, 2.4f);
            box.offset = new Vector2(0f, 1.2f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 120;
            health.faction = Faction.Enemy;

            var online = NewSimpleSprite("VisualOn", go.transform, "Props/lab_power_unit.png", Vector3.zero,
                "Environment", 6, Color.white);
            var onlineSr = online.GetComponent<SpriteRenderer>();
            onlineSr.drawMode = SpriteDrawMode.Sliced;
            onlineSr.size = new Vector2(1.5f, 2.5f);

            var offline = NewSimpleSprite("VisualOff", go.transform, "Props/lab_power_off.png", Vector3.zero,
                "Environment", 6, Color.white);
            var offlineSr = offline.GetComponent<SpriteRenderer>();
            offlineSr.drawMode = SpriteDrawMode.Sliced;
            offlineSr.size = new Vector2(1.5f, 2.5f);
            offline.SetActive(false);

            var unit = go.AddComponent<PowerUnit>();
            unit.channel = channel;
            unit.health = health;
            unit.onlineVisual = online;
            unit.offlineVisual = offline;
            unit.coreVisual = onlineSr;
            unit.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/Explosion.prefab");

            return unit;
        }

        // ==================================================================
        //  Enemigos por sección
        // ==================================================================
        private static void BuildEnemies(Transform parent)
        {
            // Sección 1 — ACCESO SUBTERRÁNEO
            SpawnEnemy(parent, "Raider", 12f, 0f);
            SpawnEnemy(parent, "Gunner", 20f, 0f);
            SpawnEnemy(parent, "Sentinel", 28f, 0f);
            SpawnEnemy(parent, "Raider", 35f, 0f);

            // Sección 2 — LABORATORIOS DE INVESTIGACIÓN
            SpawnEnemy(parent, "Grenadier", 52f, 0f);
            SpawnEnemy(parent, "Gunner", 58f, 0f);
            SpawnEnemy(parent, "Grenadier", 66f, 0f);
            SpawnEnemy(parent, "Sentinel", 76f, 0f);
            SpawnEnemy(parent, "Grenadier", 84f, 0f);

            // Sección 3 — CENTRO DE AUTOMATIZACIÓN
            SpawnEnemy(parent, "LaserDrone", 108f, 8.4f);
            SpawnEnemy(parent, "LaserDrone", 116f, 8.8f);
            SpawnEnemy(parent, "Sentinel", 126f, 0f);
            SpawnEnemy(parent, "Grenadier", 132f, 0f);
            SpawnEnemy(parent, "LaserDrone", 142f, 6.5f);

            // Sección 4 — CÁMARA DE ENERGÍA
            SpawnEnemy(parent, "LaserDrone", 160f, 6.2f);
            SpawnEnemy(parent, "Sentinel", 168f, 0f);
            SpawnEnemy(parent, "Grenadier", 176f, 0f);
            SpawnEnemy(parent, "LaserDrone", 184f, 6.8f);
            SpawnEnemy(parent, "Sentinel", 192f, 0f);

            // Sección 5 — EVACUACIÓN DE EMERGENCIA
            SpawnEnemy(parent, "Sentinel", 214f, 0f);
            SpawnEnemy(parent, "Grenadier", 222f, 0f);
            SpawnEnemy(parent, "LaserDrone", 228f, 6.2f);
            SpawnEnemy(parent, "Sentinel", 234f, 0f);
            SpawnEnemy(parent, "Grenadier", 242f, 0f);
        }

        private static GameObject SpawnEnemy(Transform parent, string prefabName, float x, float y)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyRoot + prefabName + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("[OC] Prefab de enemigo no encontrado: " + prefabName);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(x, y, 0f);
            go.name = prefabName + "_" + Mathf.RoundToInt(x) + (y > 3f ? "h" : "");
            return go;
        }

        // ==================================================================
        //  Recogibles
        // ==================================================================
        private static void BuildPickups(Transform parent)
        {
            var root = new GameObject("Pickups").transform;
            root.SetParent(parent, false);

            var laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Level03/LaserWeaponPickup.prefab");
            if (laserPrefab != null)
            {
                var a = (GameObject)PrefabUtility.InstantiatePrefab(laserPrefab, root);
                a.transform.position = new Vector3(96f, 1.3f, 0f);
                a.name = "LaserWeaponPickup_96";

                var b = (GameObject)PrefabUtility.InstantiatePrefab(laserPrefab, root);
                b.transform.position = new Vector3(158f, 1.3f, 0f);
                b.name = "LaserWeaponPickup_158";
            }
            else
            {
                Debug.LogError("[OC] Prefab del recogible de arma láser no encontrado.");
            }

            // La mejora de ametralladora pesada de la Fase 2 se conserva también aquí.
            var heavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Level02/HeavyWeaponPickup.prefab");
            if (heavyPrefab != null)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(heavyPrefab, root);
                c.transform.position = new Vector3(130f, 1.3f, 0f);
                c.name = "HeavyWeaponPickup_130";
            }
        }

        // ==================================================================
        //  Secuencia de escape
        // ==================================================================
        private static EscapeSequence BuildEscape(Transform systems, Transform world, List<PowerUnit> powerUnits)
        {
            var go = new GameObject("EscapeSequence");
            go.transform.SetParent(systems, false);

            var escape = go.AddComponent<EscapeSequence>();
            escape.powerUnits = powerUnits.ToArray();
            escape.autoBeginWhenPowered = true;
            escape.countdownSeconds = 75f;
            escape.alarmColor = new Color(1f, 0.24f, 0.2f, 1f);

            // Zona segura al final de la evacuación
            var exit = new GameObject("SafeExitZone");
            exit.transform.SetParent(world, false);
            exit.transform.position = new Vector3(248f, 2.5f, 0f);
            exit.layer = LayerMask.NameToLayer("TriggerZone");

            var box = exit.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(6f, 9f);

            var gateLocked = NewSimpleSprite("GateLocked", exit.transform, "World/goal_gate.png", Vector3.zero,
                "Environment", 8, new Color(0.7f, 0.35f, 0.32f, 1f));
            gateLocked.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            var gateOpen = NewSimpleSprite("GateOpen", exit.transform, "World/goal_gate.png", Vector3.zero,
                "Environment", 9, new Color(0.55f, 1f, 0.7f, 1f));
            gateOpen.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            var zone = exit.AddComponent<SafeExitZone>();
            zone.escape = escape;
            zone.lockedVisual = gateLocked;
            zone.openVisual = gateOpen;

            return escape;
        }

        private static void BuildAlarm(Transform world)
        {
            var root = new GameObject("AlarmLights").transform;
            root.SetParent(world, false);

            var lights = new List<GameObject>();
            float[] xs = { 206f, 218f, 230f, 242f, 250f };

            for (int i = 0; i < xs.Length; i++)
            {
                var lamp = NewSimpleSprite("Alarm_" + Mathf.RoundToInt(xs[i]), root, "FX/laser_charge.png",
                    new Vector3(xs[i], 4.4f, 0f), "Effects", 10, new Color(1f, 0.24f, 0.2f, 0.9f));
                lamp.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                lights.Add(lamp);
            }

            var escape = Object.FindFirstObjectByType<EscapeSequence>();
            if (escape != null)
            {
                escape.alarmVisual = root.gameObject;
                if (lights.Count > 0) escape.alarmLight = lights[0].GetComponent<SpriteRenderer>();
            }

            root.gameObject.SetActive(false);
        }

        // ==================================================================
        //  Triggers: checkpoints y anuncios de sección
        // ==================================================================
        private static void BuildTriggers(Transform parent)
        {
            var root = new GameObject("Triggers").transform;
            root.SetParent(parent, false);

            AddCheckpoint(root, 33f, 0f);
            AddCheckpoint(root, 60f, 0f);
            AddCheckpoint(root, 121f, 0f);
            AddCheckpoint(root, 163f, 0f);
            AddCheckpoint(root, 207f, 0f);

            AddSection(root, "SECCIÓN 1  ·  ACCESO SUBTERRÁNEO", 2f, 0f);
            AddSection(root, "SECCIÓN 2  ·  LABORATORIOS DE INVESTIGACIÓN", 41f, 0f);
            AddSection(root, "SECCIÓN 3  ·  CENTRO DE AUTOMATIZACIÓN", 96f, 0f);
            AddSection(root, "SECCIÓN 4  ·  CÁMARA DE ENERGÍA", 151f, 0f);
            AddSection(root, "SECCIÓN 5  ·  EVACUACIÓN DE EMERGENCIA", 208f, 0f);
        }

        private static void AddCheckpoint(Transform parent, float x, float y)
        {
            var cp = new GameObject("Checkpoint_" + Mathf.RoundToInt(x));
            cp.transform.SetParent(parent, false);
            cp.transform.position = new Vector3(x, y + 1.6f, 0f);
            cp.layer = LayerMask.NameToLayer("TriggerZone");

            var box = cp.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.6f, 3.2f);

            var respawn = new GameObject("RespawnPoint").transform;
            respawn.SetParent(cp.transform, false);
            respawn.localPosition = new Vector3(0f, -1.2f, 0f);

            var inactive = NewSimpleSprite("FlagInactive", cp.transform, "World/checkpoint_flag.png",
                new Vector3(0f, -1.6f, 0f), "Environment", 5, new Color(0.7f, 0.74f, 0.8f, 1f));

            var active = NewSimpleSprite("FlagActive", cp.transform, "World/checkpoint_flag.png",
                new Vector3(0f, -1.6f, 0f), "Environment", 5, Color.white);
            active.SetActive(false);

            var checkpoint = cp.AddComponent<Checkpoint>();
            checkpoint.respawnPoint = respawn;
            checkpoint.activeVisual = active;
            checkpoint.inactiveVisual = inactive;
        }

        private static void AddSection(Transform parent, string label, float x, float y)
        {
            var go = new GameObject("Section_" + Mathf.RoundToInt(x));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y + 4f, 0f);
            go.layer = LayerMask.NameToLayer("TriggerZone");

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 10f);

            var section = go.AddComponent<SectionTrigger>();
            section.sectionName = label;
            section.showOnce = true;
        }

        // ==================================================================
        //  HUD
        // ==================================================================
        private static LevelHUD BuildHud(LevelManager levelManager)
        {
            OC_UI.EnsureEventSystem();

            var canvas = OC_UI.NewCanvas("Canvas_HUD", 20);
            var root = canvas.transform;

            var panelP1 = BuildPlayerPanel(root, "HUD_P1", true);
            var panelP2 = BuildPlayerPanel(root, "HUD_P2", false);

            var scoreText = OC_UI.NewShadowedText("Score", root, "PUNTOS 000000", 38, TextAnchor.MiddleCenter, Bone);
            scoreText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(700f, 50f));

            // ---- Indicador de energía: bajo la puntuación, nunca encima de los paneles ----
            var energyText = OC_UI.NewText("Energy", root, "ENERGÍA  0/3", 26, TextAnchor.MiddleCenter, Amber);
            energyText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -76f), new Vector2(560f, 32f));

            var powerStatus = energyText.gameObject.AddComponent<PowerStatusUI>();
            powerStatus.label = energyText;

            // ---- Temporizador de evacuación ----
            var timerRoot = OC_UI.Rect("EscapeTimer", root);
            timerRoot.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -118f), new Vector2(900f, 48f));

            var timerText = OC_UI.NewShadowedText("Label", timerRoot, "EVACUACIÓN  75s", 34, TextAnchor.MiddleCenter,
                new Color(1f, 0.86f, 0.35f));
            timerText.rectTransform.Stretch();

            var timerLogic = new GameObject("EscapeTimerLogic");
            timerLogic.transform.SetParent(root, false);
            var countdown = timerLogic.AddComponent<EscapeCountdownUI>();
            countdown.root = timerRoot.gameObject;
            countdown.label = timerText;

            timerRoot.gameObject.SetActive(false);

            // ---- Mensaje central ----
            var messageGroup = OC_UI.NewGroup("MessageGroup", root, false);
            messageGroup.alpha = 0f;

            var messageText = OC_UI.NewShadowedText("Message", messageGroup.transform, "SECCIÓN", 54, TextAnchor.MiddleCenter, Amber);
            messageText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(1600f, 80f));

            BuildIntroBanner(root);

            // ---- Game Over ----
            var gameOverPanel = OC_UI.Rect("Panel_GameOver", root);
            gameOverPanel.Stretch();
            OC_UI.NewImage("Dim", gameOverPanel, new Color(0.01f, 0.02f, 0.05f, 0.86f)).rectTransform.Stretch();

            var goTitle = OC_UI.NewShadowedText("Title", gameOverPanel, "GAME OVER", 96, TextAnchor.MiddleCenter, new Color(0.95f, 0.28f, 0.24f));
            goTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), new Vector2(1200f, 130f));

            var gameOverScore = OC_UI.NewText("Score", gameOverPanel, "PUNTUACIÓN FINAL 000000", 38, TextAnchor.MiddleCenter, Bone);
            gameOverScore.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(1200f, 50f));

            var goRestart = OC_UI.NewButton("Button_Restart", gameOverPanel, "REINICIAR", 36, Amber, new Color(0.05f, 0.06f, 0.1f));
            goRestart.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-210f, -100f), new Vector2(380f, 92f));
            UnityEventTools.AddPersistentListener(goRestart.onClick, new UnityAction(levelManager.RestartLevel));

            var goMenu = OC_UI.NewButton("Button_Menu", gameOverPanel, "MENÚ PRINCIPAL", 34, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            goMenu.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(210f, -100f), new Vector2(380f, 92f));
            UnityEventTools.AddPersistentListener(goMenu.onClick, new UnityAction(levelManager.BackToMainMenu));

            gameOverPanel.gameObject.SetActive(false);

            // ---- Misión completada ----
            var missionPanel = OC_UI.Rect("Panel_MissionComplete", root);
            missionPanel.Stretch();
            OC_UI.NewImage("Dim", missionPanel, new Color(0.02f, 0.05f, 0.08f, 0.82f)).rectTransform.Stretch();

            var mcTitle = OC_UI.NewShadowedText("Title", missionPanel, "MISIÓN COMPLETADA", 84, TextAnchor.MiddleCenter, new Color(0.35f, 0.95f, 0.45f));
            mcTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(1400f, 120f));

            var missionScore = OC_UI.NewText("Score", missionPanel, "PUNTUACIÓN 000000", 38, TextAnchor.MiddleCenter, Bone);
            missionScore.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(1200f, 50f));

            var mcHint = OC_UI.NewText("Hint", missionPanel, "LABORATORIO EVACUADO", 28, TextAnchor.MiddleCenter, Cyan);
            mcHint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(900f, 44f));

            missionPanel.gameObject.SetActive(false);

            // ---- Pausa ----
            var pauseLogic = new GameObject("PauseUI");
            pauseLogic.transform.SetParent(canvas.transform, false);

            var pausePanel = OC_UI.Rect("Panel_Pause", pauseLogic.transform);
            pausePanel.Stretch();
            OC_UI.NewImage("Dim", pausePanel, new Color(0.01f, 0.02f, 0.05f, 0.84f)).rectTransform.Stretch();

            var pauseBox = OC_UI.NewImage("Box", pausePanel, new Color(0.07f, 0.09f, 0.15f, 0.96f));
            pauseBox.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820f, 620f));

            var pauseTitle = OC_UI.NewShadowedText("Title", pauseBox.transform, "PAUSA", 72, TextAnchor.MiddleCenter, Cyan);
            pauseTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -90f), new Vector2(600f, 90f));

            var resumeBtn = OC_UI.NewButton("Button_Resume", pauseBox.transform, "CONTINUAR", 34, Amber, new Color(0.05f, 0.06f, 0.1f));
            resumeBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(560f, 88f));

            var restartBtn = OC_UI.NewButton("Button_Restart", pauseBox.transform, "REINICIAR NIVEL", 34, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            restartBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f), new Vector2(560f, 88f));

            var menuBtn = OC_UI.NewButton("Button_Menu", pauseBox.transform, "MENÚ PRINCIPAL", 34, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            menuBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -130f), new Vector2(560f, 88f));

            var pauseUi = pauseLogic.AddComponent<PauseMenuUI>();
            pauseUi.panel = pausePanel.gameObject;
            pauseUi.resumeButton = resumeBtn;
            pauseUi.restartButton = restartBtn;
            pauseUi.menuButton = menuBtn;
            pauseUi.titleText = pauseTitle;

            // ---- LevelHUD ----
            var hudGo = new GameObject("LevelHUD");
            hudGo.transform.SetParent(levelManager.transform.parent != null ? levelManager.transform.parent : levelManager.transform, false);

            var hud = hudGo.AddComponent<LevelHUD>();
            hud.panelP1 = panelP1;
            hud.panelP2 = panelP2;
            hud.scoreText = scoreText;
            hud.messageText = messageText;
            hud.messageGroup = messageGroup;
            hud.gameOverPanel = gameOverPanel.gameObject;
            hud.missionCompletePanel = missionPanel.gameObject;
            hud.gameOverScoreText = gameOverScore;
            hud.missionScoreText = missionScore;

            return hud;
        }

        private static void BuildIntroBanner(Transform parent)
        {
            var rect = OC_UI.Rect("IntroBanner", parent);
            rect.Stretch();

            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var logo = OC_UI.NewShadowedText("Title", rect, "MISIÓN 3", 88, TextAnchor.MiddleCenter, Bone);
            logo.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(1400f, 120f));

            var sub = OC_UI.NewText("Subtitle", rect, "LEVEL03 — LABORATORIO SUBTERRÁNEO", 40, TextAnchor.MiddleCenter, Amber);
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f), new Vector2(1500f, 60f));

            var hint = OC_UI.NewText("Hint", rect, "DESACTIVA LA ENERGÍA  ·  ESCAPA DEL LABORATORIO", 28, TextAnchor.MiddleCenter, Cyan);
            hint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), new Vector2(1500f, 46f));

            var banner = rect.gameObject.AddComponent<LevelIntroBanner>();
            banner.group = group;
            banner.titleText = logo;
            banner.subtitleText = sub;
        }

        private static PlayerHudPanel BuildPlayerPanel(Transform parent, string name, bool left)
        {
            var rect = OC_UI.Rect(name, parent);
            Vector2 anchor = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            float y = -22f;

            rect.Place(anchor, anchor, anchor, new Vector2(left ? 24f : -24f, y), new Vector2(560f, 124f));

            var bg = OC_UI.NewImage("Bg", rect, Dim);
            bg.rectTransform.Stretch();

            var accent = OC_UI.NewImage("Accent", rect, left ? Cyan : Amber);
            accent.rectTransform.Place(new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(left ? 0f : 1f, 0.5f), Vector2.zero, new Vector2(6f, 124f));

            var portrait = OC_UI.NewImage("Portrait", rect, Color.white);
            portrait.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -18f), new Vector2(84f, 84f));

            var nameText = OC_UI.NewShadowedText("Name", rect, "P1 DANNER", 28, TextAnchor.MiddleLeft, Bone);
            nameText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(116f, -14f), new Vector2(320f, 34f));

            var livesText = OC_UI.NewText("Lives", rect, "VIDAS 3", 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.85f));
            livesText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(116f, -44f), new Vector2(180f, 26f));

            var bombsText = OC_UI.NewText("Bombs", rect, "BOMBAS 3", 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.85f));
            bombsText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(296f, -44f), new Vector2(180f, 26f));

            var weaponText = OC_UI.NewText("Weapon", rect, "ARMA BÁSICA", 20, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.55f));
            weaponText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(440f, -44f), new Vector2(200f, 26f));

            var barBg = OC_UI.NewImage("HealthBar", rect, new Color(0.03f, 0.04f, 0.07f, 0.95f));
            barBg.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(116f, -80f), new Vector2(420f, 22f));

            var fill = OC_UI.NewImage("Fill", barBg.rectTransform, new Color(0.29f, 0.9f, 0.36f));
            fill.rectTransform.Stretch();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            var slotText = OC_UI.NewText("Slot", rect, left ? "P1" : "P2", 18, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.5f));
            slotText.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-16f, 12f), new Vector2(80f, 24f));

            var panel = rect.gameObject.AddComponent<PlayerHudPanel>();
            panel.root = rect.gameObject;
            panel.nameText = nameText;
            panel.livesText = livesText;
            panel.bombsText = bombsText;
            panel.healthFill = fill;
            panel.portrait = portrait;
            panel.slotText = slotText;
            panel.weaponText = weaponText;

            return panel;
        }

        // ==================================================================
        //  Build settings
        // ==================================================================
        private static void RegisterBuildSettings()
        {
            string[] names = { "MainMenu", "CharacterSelection", "Level01", "Level02", "Level03", "Results" };
            var list = new List<EditorBuildSettingsScene>();

            for (int i = 0; i < names.Length; i++)
            {
                string path = OC_Scenes.SceneFolder + names[i] + ".unity";
                if (System.IO.File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
            }

            if (list.Count > 0) EditorBuildSettings.scenes = list.ToArray();
        }

        // ==================================================================
        //  Utilidades de escena
        // ==================================================================
        private static Sprite S(string rel)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + rel);
            if (sprite == null) Debug.LogError("[OC] Sprite no encontrado: " + SpriteRoot + rel);
            return sprite;
        }

        private static Vector2 PivotNorm(Sprite sprite)
        {
            if (sprite == null || sprite.rect.width <= 0f) return new Vector2(0.5f, 0.5f);
            return new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
        }

        private static GameObject NewTiledBlock(string name, Transform parent, string spriteRel, float centerX, float centerY,
            float width, float height, string sortingLayer, int order, bool solid)
        {
            var sprite = S(spriteRel);

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, centerY, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;

            Vector2 pivot = PivotNorm(sprite);
            visual.transform.localPosition = new Vector3((pivot.x - 0.5f) * width, (pivot.y - 0.5f) * height, 0f);

            if (solid)
            {
                root.layer = LayerMask.NameToLayer("Ground");
                var box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(width, height);
                box.offset = Vector2.zero;
            }

            return root;
        }

        private static GameObject NewSimpleSprite(string name, Transform parent, string spriteRel, Vector3 localPos,
            string sortingLayer, int order, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(spriteRel);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.color = tint;

            return go;
        }
    }
}
