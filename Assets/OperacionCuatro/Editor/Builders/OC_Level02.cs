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
    /// Fase 2 — escena Level02 «COMPLEJO DE GUERRA».
    ///
    /// Cinco secciones: ENTRADA, PRODUCCIÓN, SISTEMA DE DEFENSA, PASILLO DE SEGURIDAD y
    /// GENERADOR CENTRAL. Reutiliza LevelManager, CameraFollow2D, LevelHUD, PlayerHudPanel y los
    /// sistemas de la Fase 1; añade los tres enemigos nuevos, los peligros industriales, el
    /// recogible de ametralladora pesada y el generador como objetivo de misión.
    /// </summary>
    public static class OC_Level02
    {
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";
        private const string PrefabRoot = "Assets/OperacionCuatro/Prefabs/";
        private const string EnemyRoot = "Assets/OperacionCuatro/Resources/Enemies/";
        private const string InputAssetPath = "Assets/OperacionCuatro/Settings/OC_InputActions.inputactions";

        private static readonly Color Bone = new Color(0.96f, 0.95f, 0.9f, 1f);
        private static readonly Color Amber = new Color(1f, 0.58f, 0.16f, 1f);
        private static readonly Color Cyan = new Color(0.27f, 0.87f, 1f, 1f);
        private static readonly Color Dim = new Color(0.04f, 0.06f, 0.1f, 0.78f);

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
                new Vector3(3f, 0.8f, 0f),
                new Vector3(1.4f, 0.8f, 0f)
            };
            levelManager.checkpointPosition = new Vector3(3f, 0.8f, 0f);

            var enemiesRoot = new GameObject("Enemies").transform;
            var enemies = BuildEnemies(enemiesRoot);
            var guards = BuildFinalWave(enemiesRoot);

            BuildTriggers(systems.transform, enemies);

            var generatorGo = BuildGenerator(world, guards);
            var generator = generatorGo != null ? generatorGo.GetComponent<GeneratorObjective>() : null;

            BuildPickup(world);

            var hud = BuildHud(levelManager, generator);
            levelManager.hud = hud;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OC_Scenes.SceneFolder + "Level02.unity");
            RegisterBuildSettings();

            Debug.Log("[OC] Level02 creado: " + enemies.childCount + " enemigos en secciones + " +
                      guards.Count + " en la oleada del generador, generador=" + (generator != null) + ".");
        }

        // ==================================================================
        //  Cámara
        // ==================================================================
        private static CameraFollow2D BuildCamera()
        {
            var rig = new GameObject("CameraRig");
            rig.AddComponent<CameraShake>();

            var cam = OC_UI.NewCamera("Main Camera", new Color(0.04f, 0.06f, 0.11f), 6.2f);
            cam.transform.SetParent(rig.transform, false);
            cam.transform.position = new Vector3(11f, 3.6f, -10f);

            var follow = cam.gameObject.AddComponent<CameraFollow2D>();
            follow.minX = 11f;
            follow.maxX = 198f;
            follow.minY = 3.4f;
            follow.maxY = 7f;
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

            AddParallaxLayer(root, "Sky", "World/bg02_sky.png", -50, 4.6f, 220f, 18f, 0.03f, Color.white);
            AddParallaxLayer(root, "Facility", "World/bg02_facility.png", -40, 2.2f, 220f, 18f, 0.14f, new Color(0.82f, 0.88f, 1f, 1f));
            AddParallaxLayer(root, "Pipes", "World/bg02_pipes.png", -30, 1.6f, 220f, 18f, 0.28f, new Color(0.9f, 0.94f, 1f, 1f));
            AddParallaxLayer(root, "Haze", "World/bg02_haze.png", -20, 1.2f, 220f, 6f, 0.42f, new Color(0.95f, 0.86f, 0.78f, 0.6f));
        }

        private static void AddParallaxLayer(Transform parent, string name, string sprite, int order,
            float y, float width, float height, float factor, Color tint)
        {
            var block = NewTiledBlock(name, parent, sprite, 62f, y, width, height, "Background", order, false);
            var sr = block.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = tint;

            var layer = block.AddComponent<ParallaxLayer>();
            layer.parallaxFactor = factor;
            layer.cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        // ==================================================================
        //  Suelo: complejo continuo de x -9 a 211, superficie en y = 0
        // ==================================================================
        private static void BuildGround(Transform parent)
        {
            var root = new GameObject("Ground").transform;
            root.SetParent(parent, false);

            NewTiledBlock("Ground_Fill", root, "World/tile_metal_fill.png", 101f, -4f, 220f, 8f, "Environment", 0, true);
            NewTiledBlock("Ground_Top", root, "World/tile_metal_top.png", 101f, -0.5f, 220f, 1f, "Environment", 1, false);

            // Sección 2: escalón de carga industrial (top 0.8)
            AddLedge(root, "LoadDock", 44f, 0.8f, 8f);
            // Sección 2: plataforma de salida de la cinta (top 0.55 largo)
            AddLedge(root, "BeltRun", 56f, 0.55f, 12f);
            // Sección 4: pasarela de servicio sobre el pasillo eléctrico (top 1.9)
            AddLedge(root, "ServiceStep", 149f, 1.9f, 5f);
            // Sección 5: escalón de acceso al generador (top 1.0) y plataforma del generador (top 2.0)
            AddLedge(root, "GeneratorStep", 187f, 1.0f, 4f);
            AddLedge(root, "GeneratorBase", 196f, 2f, 14f);
        }

        private static void AddLedge(Transform parent, string name, float centerX, float topY, float width)
        {
            float centerY = topY * 0.5f;
            NewTiledBlock(name + "_Fill", parent, "World/tile_metal_fill.png", centerX, centerY, width, topY, "Environment", 0, true);
            NewTiledBlock(name + "_Top", parent, "World/tile_metal_top.png", centerX, topY - 0.5f, width, 1f, "Environment", 1, false);
        }

        // ==================================================================
        //  Plataformas
        // ==================================================================
        private static void BuildPlatforms(Transform parent)
        {
            var root = new GameObject("Platforms").transform;
            root.SetParent(parent, false);

            // Sección 1 — entrada
            NewTiledBlock("Platform_S1a", root, "World/tile_platform_metal.png", 15f, 2.2f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S1b", root, "World/tile_platform_metal.png", 30f, 3.2f, 4f, 0.62f, "Environment", 2, true);

            // Sección 2 — producción
            NewTiledBlock("Platform_S2a", root, "World/tile_platform_metal.png", 47f, 3.4f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S2b", root, "World/tile_platform_metal.png", 64f, 3.6f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S2c", root, "World/tile_platform_metal.png", 86f, 2.4f, 4f, 0.62f, "Environment", 2, true);

            // Sección 3 — sistema de defensa: pasarelas de las torretas
            NewTiledBlock("Catwalk_A", root, "World/tile_catwalk.png", 99f, 3.2f, 18f, 0.9f, "Environment", 2, true);
            NewTiledBlock("Catwalk_B", root, "World/tile_catwalk.png", 121f, 3.2f, 16f, 0.9f, "Environment", 2, true);
            NewTiledBlock("Platform_S3a", root, "World/tile_platform_metal.png", 92f, 1.8f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S3b", root, "World/tile_platform_metal.png", 111f, 1.9f, 5f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S3c", root, "World/tile_platform_metal.png", 133f, 2.4f, 4f, 0.62f, "Environment", 2, true);

            // Sección 4 — pasillo de seguridad
            NewTiledBlock("Platform_S4a", root, "World/tile_platform_metal.png", 137f, 2.2f, 4f, 0.62f, "Environment", 2, true);
            NewTiledBlock("Platform_S4b", root, "World/tile_platform_metal.png", 167f, 2.6f, 4f, 0.62f, "Environment", 2, true);

            // Sección 5 — apoyo sobre el generador
            NewTiledBlock("Platform_S5a", root, "World/tile_platform_metal.png", 204f, 3.6f, 4f, 0.62f, "Environment", 2, true);
        }

        // ==================================================================
        //  Props (la mayoría decorativos: no bloquean disparos ni paso)
        // ==================================================================
        private static void BuildProps(Transform parent)
        {
            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);

            var back = new GameObject("Props_Back").transform;
            back.SetParent(root, false);

            // Decoración de fondo (sin colisión)
            AddProp(back, "Pipe_1", "Props/pipe_vertical.png", 8f, 0f, false, "Environment", 3);
            AddProp(back, "Pipe_2", "Props/pipe_vertical.png", 38f, 0f, false, "Environment", 3);
            AddProp(back, "Pipe_3", "Props/pipe_vertical.png", 90f, 0f, false, "Environment", 3);
            AddProp(back, "Pipe_4", "Props/pipe_vertical.png", 137f, 0f, false, "Environment", 3);
            AddProp(back, "Pipe_5", "Props/pipe_vertical.png", 172f, 0f, false, "Environment", 3);
            AddProp(back, "Terminal_1", "Props/terminal.png", 24f, 0f, false, "Environment", 3);
            AddProp(back, "Terminal_2", "Props/terminal.png", 116f, 0f, false, "Environment", 3);
            AddProp(back, "Terminal_3", "Props/terminal.png", 194f, 2f, false, "Environment", 3);

            AddProp(back, "Sign_Entrada", "Props/warning_sign.png", 6f, 0f, false, "Environment", 3);
            AddProp(back, "Sign_Defensa", "Props/warning_sign.png", 94f, 0f, false, "Environment", 3);
            AddProp(back, "Sign_Seguridad", "Props/warning_sign.png", 136f, 0f, false, "Environment", 3);

            // Props sólidos pequeños (bajos: no tapan la línea de disparo)
            AddProp(root, "Crate_1", "Props/crate_metal.png", 20f, 0f, true, "Environment", 4);
            AddProp(root, "Drum_1", "Props/drum_steel.png", 33f, 0f, true, "Environment", 4);
            AddProp(root, "Crate_2", "Props/crate_metal.png", 62f, 0f, true, "Environment", 4);
            AddProp(root, "Drum_2", "Props/drum_steel.png", 84f, 0f, true, "Environment", 4);
            AddProp(root, "Crate_3", "Props/crate_metal.png", 108f, 0f, true, "Environment", 4);
            AddProp(root, "Drum_3", "Props/drum_steel.png", 130f, 0f, true, "Environment", 4);
            AddProp(root, "Crate_4", "Props/crate_metal.png", 154f, 0f, true, "Environment", 4);
            AddProp(root, "Drum_4", "Props/drum_steel.png", 170f, 0f, true, "Environment", 4);
        }

        private static void AddProp(Transform parent, string name, string spriteRel, float x, float y, bool solid,
            string sortingLayer, int order)
        {
            var sprite = S(spriteRel);
            if (sprite == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);

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
        //  Peligros: vapor, plataforma móvil, cintas y zonas eléctricas
        // ==================================================================
        private static void BuildHazards(Transform parent)
        {
            var root = new GameObject("Hazards").transform;
            root.SetParent(parent, false);

            // ---- Vapor industrial (aviso visible antes del chorro) ----
            AddSteamVent(root, "SteamVent_58", 58f, 1.3f);
            AddSteamVent(root, "SteamVent_104", 104f, 3.2f);
            AddSteamVent(root, "SteamVent_160", 160f, 1.3f);

            // ---- Plataforma móvil de producción: x 74 -> 86 a y 2.2 ----
            var mp = NewTiledBlock("MovingPlatform_1", root, "World/tile_platform_metal.png",
                74f, 2.2f, 3.4f, 0.55f, "Environment", 2, true);
            var move = mp.AddComponent<MovingPlatform>();
            move.localA = Vector2.zero;
            move.localB = new Vector2(12f, 0f);
            move.speed = 1.9f;
            move.waitTime = 0.5f;

            // ---- Cintas transportadoras ----
            AddConveyor(root, "Conveyor_1", 50f, 0.28f, 8f, 2.2f);
            AddConveyor(root, "Conveyor_2", 57f, 0.55f, 9f, 2.6f);
            AddConveyor(root, "Conveyor_3", 78f, 0.28f, 6f, -2.2f);

            // ---- Zonas eléctricas con estado seguro visible ----
            AddElectricZone(root, "ElectricZone_143", 143f, 1.5f, 6f);
            AddElectricZone(root, "ElectricZone_161", 161f, 1.5f, 6f);
        }

        private static void AddSteamVent(Transform parent, string name, float x, float baseY)
        {
            var vent = new GameObject(name);
            vent.transform.SetParent(parent, false);
            vent.transform.position = new Vector3(x, baseY, 0f);
            vent.layer = LayerMask.NameToLayer("TriggerZone");

            var box = vent.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.3f, 3f);

            NewSimpleSprite("Grate", vent.transform, "Props/vent_grate.png",
                new Vector3(0f, -1.25f, 0f), "Environment", 6, Color.white);

            var warning = NewSimpleSprite("Warning", vent.transform, "FX/steam_cloud.png",
                new Vector3(0f, -0.9f, 0f), "Effects", 3, new Color(1f, 1f, 1f, 0.45f));
            warning.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            warning.SetActive(false);

            var active = NewSimpleSprite("Active", vent.transform, "FX/steam_cloud.png",
                new Vector3(0f, -0.5f, 0f), "Effects", 4, new Color(1f, 1f, 1f, 0.9f));
            active.transform.localScale = new Vector3(1.5f, 2.4f, 1f);
            active.SetActive(false);

            var script = vent.AddComponent<SteamVent>();
            script.warningVisual = warning;
            script.activeVisual = active;
            script.cycleTime = 4.6f;
            script.warningTime = 0.9f;
            script.activeTime = 1.5f;
            script.damage = 14;
            script.startOffset = x % 3f;
        }

        private static void AddConveyor(Transform parent, string name, float x, float topY, float width, float speed)
        {
            var belt = NewTiledBlock(name, parent, "World/tile_metal_top.png",
                x, topY * 0.5f, width, topY, "Environment", 2, true);
            var conveyor = belt.AddComponent<ConveyorBelt>();
            conveyor.beltSpeed = speed;
        }

        private static void AddElectricZone(Transform parent, string name, float x, float y, float width)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.position = new Vector3(x, y, 0f);
            zone.layer = LayerMask.NameToLayer("TriggerZone");

            var box = zone.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(width, 3f);

            var field = NewSimpleSprite("Field", zone.transform, "FX/electric_field.png",
                new Vector3(0f, -1.4f, 0f), "Effects", 5, Color.white);
            field.transform.localScale = new Vector3(width / 2f, 1.1f, 1f);

            var spark = NewSimpleSprite("Spark", zone.transform, "FX/shield_spark.png",
                new Vector3(0f, -0.6f, 0f), "Effects", 6, new Color(0.8f, 0.95f, 1f, 1f));
            spark.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var script = zone.AddComponent<ElectricZone>();
            script.fieldVisual = field.GetComponent<SpriteRenderer>();
            script.sparkVisual = spark;
            script.onTime = 2.2f;
            script.offTime = 1.8f;
            script.damage = 16;
            script.startOffset = Mathf.Repeat(x, 4f);
        }

        // ==================================================================
        //  Enemigos por sección
        // ==================================================================
        private static Transform BuildEnemies(Transform parent)
        {
            // Sección 1 — ENTRADA
            SpawnEnemy(parent, "Raider", 14f, 0f);
            SpawnEnemy(parent, "Raider", 27f, 0f);
            SpawnEnemy(parent, "Gunner", 33f, 0f);

            // Sección 2 — PRODUCCIÓN
            SpawnEnemy(parent, "Armored", 52f, 0f);
            SpawnEnemy(parent, "Raider", 62f, 0f);
            SpawnEnemy(parent, "Armored", 70f, 0f);

            // Sección 3 — SISTEMA DE DEFENSA
            SpawnEnemy(parent, "Turret", 100f, 3.2f);
            SpawnEnemy(parent, "Turret", 120f, 3.2f);
            SpawnEnemy(parent, "Drone", 106f, 5.2f);
            SpawnEnemy(parent, "Drone", 113f, 6.0f);
            SpawnEnemy(parent, "Drone", 126f, 5.4f);
            SpawnEnemy(parent, "Drone", 131f, 6.2f);

            // Sección 4 — PASILLO DE SEGURIDAD
            SpawnEnemy(parent, "Armored", 152f, 0f);
            SpawnEnemy(parent, "Raider", 157f, 0f);
            SpawnEnemy(parent, "Gunner", 168f, 0f);

            return parent;
        }

        /// <summary>Oleada que protege el generador (escudo del núcleo).</summary>
        private static List<GameObject> BuildFinalWave(Transform parent)
        {
            var list = new List<GameObject>();

            list.Add(SpawnEnemy(parent, "Armored", 174f, 0f));
            list.Add(SpawnEnemy(parent, "Raider", 176f, 0f));
            list.Add(SpawnEnemy(parent, "Gunner", 178f, 0f));
            list.Add(SpawnEnemy(parent, "Drone", 176f, 5.0f));
            list.Add(SpawnEnemy(parent, "Armored", 180f, 0f));
            list.Add(SpawnEnemy(parent, "Drone", 182f, 5.6f));
            list.Add(SpawnEnemy(parent, "Raider", 183f, 0f));
            list.Add(SpawnEnemy(parent, "Gunner", 185f, 0f));

            list.RemoveAll(go => go == null);
            return list;
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
        //  Generador central
        // ==================================================================
        private static GameObject BuildGenerator(Transform parent, List<GameObject> guards)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Level02/Generator.prefab");
            if (prefab == null)
            {
                Debug.LogError("[OC] Prefab del generador no encontrado.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(196f, 2f, 0f);

            var objective = go.GetComponent<GeneratorObjective>();
            if (objective != null) objective.shieldWaveObjects = guards.ToArray();

            return go;
        }

        private static void BuildPickup(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Level02/HeavyWeaponPickup.prefab");
            if (prefab == null)
            {
                Debug.LogError("[OC] Prefab del recogible de ametralladora no encontrado.");
                return;
            }

            var root = new GameObject("Pickups").transform;
            root.SetParent(parent, false);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.transform.position = new Vector3(170f, 1.3f, 0f);
            go.name = "HeavyWeaponPickup_170";

            var second = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            second.transform.position = new Vector3(96f, 1.3f, 0f);
            second.name = "HeavyWeaponPickup_96";
        }

        // ==================================================================
        //  Triggers: checkpoints y anuncios de sección
        // ==================================================================
        private static void BuildTriggers(Transform parent, Transform enemies)
        {
            var root = new GameObject("Triggers").transform;
            root.SetParent(parent, false);

            AddCheckpoint(root, 22f, 0f);
            AddCheckpoint(root, 96f, 0f);
            AddCheckpoint(root, 150f, 0f);

            AddSection(root, "SECCIÓN 1  ·  ENTRADA", 2f, 0f);
            AddSection(root, "SECCIÓN 2  ·  PRODUCCIÓN", 42f, 0f);
            AddSection(root, "SECCIÓN 3  ·  SISTEMA DE DEFENSA", 91f, 0f);
            AddSection(root, "SECCIÓN 4  ·  PASILLO DE SEGURIDAD", 136f, 0f);
            AddSection(root, "SECCIÓN 5  ·  GENERADOR CENTRAL", 173f, 0f);

            // Zona de aviso final (no activa enemigos: la oleada del generador ya está en escena).
            var warn = new GameObject("GeneratorApproach");
            warn.transform.SetParent(root, false);
            warn.transform.position = new Vector3(190f, 4f, 0f);
            warn.layer = LayerMask.NameToLayer("TriggerZone");

            var warnBox = warn.AddComponent<BoxCollider2D>();
            warnBox.isTrigger = true;
            warnBox.size = new Vector2(6f, 9f);

            var warnTrigger = warn.AddComponent<SectionTrigger>();
            warnTrigger.sectionName = "DESTRUYE EL GENERADOR";
            warnTrigger.showOnce = true;
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
                new Vector3(0f, -1.6f, 0f), "Environment", 5, new Color(0.7f, 0.7f, 0.72f, 1f));

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
        private static LevelHUD BuildHud(LevelManager levelManager, GeneratorObjective generator)
        {
            OC_UI.EnsureEventSystem();

            var canvas = OC_UI.NewCanvas("Canvas_HUD", 20);
            var root = canvas.transform;

            var panelP1 = BuildPlayerPanel(root, "HUD_P1", true);
            var panelP2 = BuildPlayerPanel(root, "HUD_P2", false);

            var scoreText = OC_UI.NewShadowedText("Score", root, "PUNTOS 000000", 38, TextAnchor.MiddleCenter, Bone);
            scoreText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(700f, 50f));

            // ---- Mensaje central ----
            var messageGroup = OC_UI.NewGroup("MessageGroup", root, false);
            messageGroup.alpha = 0f;

            var messageText = OC_UI.NewShadowedText("Message", messageGroup.transform, "SECCIÓN", 54, TextAnchor.MiddleCenter, Amber);
            messageText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(1500f, 80f));

            // ---- Generador ----
            var generatorBar = BuildGeneratorBar(root, generator);

            // ---- Cartel de introducción ----
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

            var mcHint = OC_UI.NewText("Hint", missionPanel, "GENERADOR DESTRUIDO", 28, TextAnchor.MiddleCenter, Cyan);
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

            if (generatorBar != null) generatorBar.SetActive(generator != null);

            return hud;
        }

        private static GameObject BuildGeneratorBar(Transform parent, GeneratorObjective generator)
        {
            var rect = OC_UI.Rect("GeneratorBar", parent);
            rect.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(820f, 74f));

            var bg = OC_UI.NewImage("Bg", rect, Dim);
            bg.rectTransform.Stretch();

            var label = OC_UI.NewText("Label", rect, "GENERADOR CENTRAL", 20, TextAnchor.MiddleCenter, Bone);
            label.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(800f, 26f));

            var barBg = OC_UI.NewImage("Bar", rect, new Color(0.03f, 0.04f, 0.07f, 0.95f));
            barBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(780f, 18f));

            var fill = OC_UI.NewImage("Fill", barBg.rectTransform, new Color(0.98f, 0.78f, 0.2f));
            fill.rectTransform.Stretch();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            var ui = rect.gameObject.AddComponent<GeneratorHudUI>();
            ui.generator = generator;
            ui.root = rect.gameObject;
            ui.label = label;
            ui.fill = fill;
            ui.visibleRange = 30f;

            rect.gameObject.SetActive(false);
            return rect.gameObject;
        }

        private static void BuildIntroBanner(Transform parent)
        {
            var rect = OC_UI.Rect("IntroBanner", parent);
            rect.Stretch();

            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var logo = OC_UI.NewShadowedText("Title", rect, "MISIÓN 2", 88, TextAnchor.MiddleCenter, Bone);
            logo.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(1400f, 120f));

            var sub = OC_UI.NewText("Subtitle", rect, "LEVEL02 — COMPLEJO DE GUERRA", 40, TextAnchor.MiddleCenter, Amber);
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f), new Vector2(1400f, 60f));

            var hint = OC_UI.NewText("Hint", rect, "INFILTRACIÓN  ·  DESTRUYE EL GENERADOR", 28, TextAnchor.MiddleCenter, Cyan);
            hint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), new Vector2(1400f, 46f));

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
            string[] names = { "MainMenu", "CharacterSelection", "Level01", "Level02", "Results" };
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
