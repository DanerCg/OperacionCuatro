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
    /// <summary>Módulo 6: escena Level01 (PUERTO EN RUINAS) con entorno, enemigos, triggers y HUD.</summary>
    public static class OC_Level01
    {
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";
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
            var props = BuildProps(world);

            var levelManager = systems.AddComponent<LevelManager>();
            levelManager.inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            levelManager.cameraFollow = follow;
            levelManager.spawnPositions = new[]
            {
                new Vector3(3f, 0.8f, 0f),
                new Vector3(1.4f, 0.8f, 0f)
            };
            levelManager.checkpointPosition = new Vector3(3f, 0.8f, 0f);

            var enemies = new GameObject("Enemies").transform;
            var waveEnemies = BuildEnemies(enemies);
            BuildTriggers(systems.transform, waveEnemies);

            var hud = BuildHud(levelManager);
            levelManager.hud = hud;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OC_Scenes.SceneFolder + "Level01.unity");
            OC_Scenes.RegisterBuildSettings();
            Debug.Log("[OC] Level01 creado con " + props + " props y " + enemies.childCount + " enemigos.");
        }

        // ==================================================================
        private static CameraFollow2D BuildCamera()
        {
            var rig = new GameObject("CameraRig");
            rig.AddComponent<CameraShake>();

            var cam = OC_UI.NewCamera("Main Camera", new Color(0.05f, 0.08f, 0.15f), 6.2f);
            cam.transform.SetParent(rig.transform, false);
            cam.transform.position = new Vector3(11f, 3.6f, -10f);

            var follow = cam.gameObject.AddComponent<CameraFollow2D>();
            follow.minX = 11f;
            follow.maxX = 118f;
            follow.minY = 3.4f;
            follow.maxY = 7f;
            follow.baseOrthoSize = 6.2f;
            follow.zoomOutPerUnit = 0.11f;
            follow.maxSeparation = 11f;
            follow.useBounds = true;

            return follow;
        }

        // ==================================================================
        //  Fondo con parallax
        // ==================================================================
        private static void BuildParallax(Transform parent)
        {
            var root = new GameObject("Parallax").transform;
            root.SetParent(parent, false);

            AddParallaxLayer(root, "Sky", "World/bg_sky.png", -50, 4.6f, 210f, 18f, 0.03f, Color.white);
            AddParallaxLayer(root, "Harbor", "World/bg_harbor.png", -40, 2f, 210f, 18f, 0.14f, new Color(0.85f, 0.9f, 1f, 1f));
            AddParallaxLayer(root, "Ruins", "World/bg_ruins.png", -30, 1.4f, 210f, 18f, 0.28f, new Color(0.9f, 0.92f, 1f, 1f));
            AddParallaxLayer(root, "Fog", "World/bg_fog.png", -20, 1.2f, 210f, 6f, 0.42f, new Color(0.75f, 0.85f, 1f, 0.7f));
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
        //  Suelo
        // ==================================================================
        private static void BuildGround(Transform parent)
        {
            var root = new GameObject("Ground").transform;
            root.SetParent(parent, false);

            // Suelo base continuo (top en y = 0)
            NewTiledBlock("Ground_Fill", root, "World/tile_ground_fill.png", 62f, -4f, 140f, 8f, "Environment", 0, true);
            NewTiledBlock("Ground_Top", root, "World/tile_ground_top.png", 62f, -0.5f, 140f, 1f, "Environment", 1, false);

            // Muelle A (top 1.5)
            AddLedge(root, "DockA", 36f, 1.5f, 20f);
            // Escalón hacia el muelle B (top 1.5)
            AddLedge(root, "StepB", 62f, 1.5f, 4f);
            // Muelle B (top 3.0)
            AddLedge(root, "DockB", 75f, 3f, 22f);
            // Bajada (top 1.5)
            AddLedge(root, "StepDown", 88f, 1.5f, 4f);
            // Escalón final (top 1.0)
            AddLedge(root, "StepFinal", 96f, 1f, 4f);
            // Explanada final (top 2.0)
            AddLedge(root, "FinalYard", 113f, 2f, 30f);
        }

        private static void AddLedge(Transform parent, string name, float centerX, float topY, float width)
        {
            float centerY = topY * 0.5f;
            NewTiledBlock(name + "_Fill", parent, "World/tile_ground_fill.png", centerX, centerY, width, topY, "Environment", 0, true);
            NewTiledBlock(name + "_Top", parent, "World/tile_ground_top.png", centerX, topY - 0.5f, width, 1f, "Environment", 1, false);
        }

        private static void BuildPlatforms(Transform parent)
        {
            var root = new GameObject("Platforms").transform;
            root.SetParent(parent, false);

            float[,] data =
            {
                { 14f, 2.2f, 4f },
                { 21f, 3.6f, 4f },
                { 33f, 3.4f, 5f },
                { 52f, 2.2f, 4f },
                { 58f, 3.4f, 4f },
                { 72f, 4.6f, 4f },
                { 92f, 2.4f, 4f },
                { 104f, 3.6f, 4f },
                { 112f, 4.8f, 5f }
            };

            for (int i = 0; i < data.GetLength(0); i++)
            {
                NewTiledBlock("Platform_" + i, root, "World/tile_platform.png", data[i, 0], data[i, 1], data[i, 2], 0.62f,
                    "Environment", 2, true);
            }
        }

        // ==================================================================
        //  Props del escenario
        // ==================================================================
        private static int BuildProps(Transform parent)
        {
            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);

            int count = 0;

            count += AddProp(root, "Sign", "World/sign.png", 6.5f, 0f, true);
            count += AddProp(root, "Fence_1", "Props/fence.png", 3.6f, 0f, true);
            count += AddProp(root, "Crate_1", "Props/crate.png", 9.5f, 0f, true);
            count += AddProp(root, "Barrel_1", "Props/barrel.png", 11.6f, 0f, true);
            count += AddProp(root, "Sandbags_1", "Props/sandbags.png", 17.5f, 0f, true);
            count += AddProp(root, "ContainerRed_1", "Props/container_red.png", 23f, 0f, true);

            count += AddProp(root, "Crate_DockA", "Props/crate.png", 30f, 1.5f, true);
            count += AddProp(root, "Sandbags_DockA", "Props/sandbags.png", 38f, 1.5f, true);
            count += AddProp(root, "Barrel_DockA", "Props/barrel.png", 42.5f, 1.5f, true);

            count += AddProp(root, "ContainerBlue_1", "Props/container_blue.png", 48.5f, 0f, true);
            count += AddProp(root, "ContainerRust_1", "Props/container_rust.png", 54.5f, 0f, true);

            count += AddProp(root, "ContainerRed_2", "Props/container_red.png", 66f, 3f, true);
            count += AddProp(root, "Crate_DockB", "Props/crate.png", 70.5f, 3f, true);
            count += AddProp(root, "Barrel_DockB", "Props/barrel.png", 74f, 3f, true);
            count += AddProp(root, "ContainerRust_2", "Props/container_rust.png", 79.5f, 3f, true);
            count += AddProp(root, "Fence_DockB", "Props/fence.png", 84f, 3f, true);

            count += AddProp(root, "Sandbags_2", "Props/sandbags.png", 90f, 0f, true);
            count += AddProp(root, "Crate_Final", "Props/crate.png", 100f, 2f, true);
            count += AddProp(root, "Barrel_Final", "Props/barrel.png", 108f, 2f, true);
            count += AddProp(root, "ContainerBlue_2", "Props/container_blue.png", 116f, 2f, true);
            count += AddProp(root, "Fence_Final", "Props/fence.png", 120.5f, 2f, true);

            return count;
        }

        private static int AddProp(Transform parent, string name, string spriteRel, float x, float y, bool solid)
        {
            var sprite = S(spriteRel);
            if (sprite == null) return 0;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 3;

            if (solid)
            {
                go.layer = LayerMask.NameToLayer("Ground");

                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(sprite.bounds.size.x * 0.92f, sprite.bounds.size.y * 0.95f);
                box.offset = new Vector2(sprite.bounds.center.x, sprite.bounds.center.y);
            }

            return 1;
        }

        // ==================================================================
        //  Enemigos
        // ==================================================================
        private static List<GameObject> BuildEnemies(Transform parent)
        {
            var list = new List<GameObject>();

            list.Add(SpawnEnemy(parent, "Raider", 18f, 0f, true));
            list.Add(SpawnEnemy(parent, "Raider", 34f, 1.5f, true));
            list.Add(SpawnEnemy(parent, "Gunner", 42f, 1.5f, true));
            list.Add(SpawnEnemy(parent, "Raider", 52f, 0f, true));
            list.Add(SpawnEnemy(parent, "Raider", 68f, 3f, true));
            list.Add(SpawnEnemy(parent, "Gunner", 78f, 3f, true));
            list.Add(SpawnEnemy(parent, "Raider", 82f, 3f, true));

            list.Add(SpawnEnemy(parent, "Raider", 104f, 2f, false));
            list.Add(SpawnEnemy(parent, "Raider", 110f, 2f, false));
            list.Add(SpawnEnemy(parent, "Gunner", 115f, 2f, false));

            return list;
        }

        private static GameObject SpawnEnemy(Transform parent, string prefabName, float x, float y, bool active)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OperacionCuatro/Resources/Enemies/" + prefabName + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("[OC] Prefab de enemigo no encontrado: " + prefabName);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(x, y, 0f);
            go.name = prefabName + "_" + Mathf.RoundToInt(x);
            go.SetActive(active);

            return go;
        }

        // ==================================================================
        //  Triggers: punto de control, oleada final y meta
        // ==================================================================
        private static void BuildTriggers(Transform parent, List<GameObject> waveEnemies)
        {
            var root = new GameObject("Triggers").transform;
            root.SetParent(parent, false);

            // ---- Punto de control ----
            var checkpointGo = new GameObject("Checkpoint");
            checkpointGo.transform.SetParent(root, false);
            checkpointGo.transform.position = new Vector3(47f, 1.6f, 0f);
            checkpointGo.layer = LayerMask.NameToLayer("TriggerZone");

            var cpBox = checkpointGo.AddComponent<BoxCollider2D>();
            cpBox.isTrigger = true;
            cpBox.size = new Vector2(2.6f, 3.2f);

            var respawn = new GameObject("RespawnPoint").transform;
            respawn.SetParent(checkpointGo.transform, false);
            respawn.localPosition = new Vector3(0f, -1.2f, 0f);

            var inactiveVisual = NewSimpleSprite("FlagInactive", checkpointGo.transform, "World/checkpoint_flag.png",
                new Vector3(0f, -1.6f, 0f), "Environment", 4, new Color(0.7f, 0.7f, 0.72f, 1f));

            var activeVisual = NewSimpleSprite("FlagActive", checkpointGo.transform, "World/checkpoint_flag.png",
                new Vector3(0f, -1.6f, 0f), "Environment", 4, Color.white);
            activeVisual.SetActive(false);

            var checkpoint = checkpointGo.AddComponent<Checkpoint>();
            checkpoint.respawnPoint = respawn;
            checkpoint.activeVisual = activeVisual;
            checkpoint.inactiveVisual = inactiveVisual;

            // ---- Oleada final ----
            var waveGo = new GameObject("WaveTrigger_Final");
            waveGo.transform.SetParent(root, false);
            waveGo.transform.position = new Vector3(96f, 4f, 0f);
            waveGo.layer = LayerMask.NameToLayer("TriggerZone");

            var waveBox = waveGo.AddComponent<BoxCollider2D>();
            waveBox.isTrigger = true;
            waveBox.size = new Vector2(4f, 9f);

            var wave = waveGo.AddComponent<WaveTrigger>();
            wave.enemiesToActivate = waveEnemies.ToArray();
            wave.spawnAtPoints = false;
            wave.activateDelay = 0.4f;

            // ---- Meta ----
            var gate = NewSimpleSprite("GoalGate", root, "World/goal_gate.png", new Vector3(124f, 2f, 0f), "Environment", 5, Color.white);
            gate.transform.localScale = new Vector3(1f, 1f, 1f);

            var goalGo = new GameObject("GoalZone");
            goalGo.transform.SetParent(root, false);
            goalGo.transform.position = new Vector3(124f, 3.2f, 0f);
            goalGo.layer = LayerMask.NameToLayer("TriggerZone");

            var goalBox = goalGo.AddComponent<BoxCollider2D>();
            goalBox.isTrigger = true;
            goalBox.size = new Vector2(3f, 6f);

            var goal = goalGo.AddComponent<GoalZone>();
            goal.requiresWaveCleared = true;
            goal.waveObjects = waveEnemies.ToArray();
        }

        // ==================================================================
        //  HUD
        // ==================================================================
        private static LevelHUD BuildHud(LevelManager levelManager)
        {
            OC_UI.EnsureEventSystem();

            var canvas = OC_UI.NewCanvas("Canvas_HUD", 20);
            var root = canvas.transform;

            // ---- Paneles de jugador ----
            var panelP1 = BuildPlayerPanel(root, "HUD_P1", true);
            var panelP2 = BuildPlayerPanel(root, "HUD_P2", false);

            // ---- Puntuación ----
            var scoreText = OC_UI.NewShadowedText("Score", root, "PUNTOS 000000", 38, TextAnchor.MiddleCenter, Bone);
            scoreText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(700f, 50f));

            // ---- Mensaje central ----
            var messageGroup = OC_UI.NewGroup("MessageGroup", root, false);
            messageGroup.alpha = 0f;

            var messageText = OC_UI.NewShadowedText("Message", messageGroup.transform, "PUNTO DE CONTROL", 54, TextAnchor.MiddleCenter, Amber);
            messageText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(1400f, 80f));

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

            var mcHint = OC_UI.NewText("Hint", missionPanel, "PREPARANDO RESULTADOS...", 28, TextAnchor.MiddleCenter, Cyan);
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
            hudGo.transform.SetParent(systemsRoot(levelManager), false);

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

        private static Transform systemsRoot(LevelManager levelManager)
        {
            return levelManager.transform.parent != null ? levelManager.transform.parent : levelManager.transform;
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
                new Vector2(116f, -18f), new Vector2(300f, 34f));

            var livesText = OC_UI.NewText("Lives", rect, "VIDAS 3", 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.85f));
            livesText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(116f, -50f), new Vector2(180f, 28f));

            var bombsText = OC_UI.NewText("Bombs", rect, "BOMBAS 3", 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.85f));
            bombsText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(310f, -50f), new Vector2(200f, 28f));

            var barBg = OC_UI.NewImage("HealthBar", rect, new Color(0.03f, 0.04f, 0.07f, 0.95f));
            barBg.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(116f, -84f), new Vector2(420f, 22f));

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

            return panel;
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
