using OperacionCuatro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OCBuilders
{
    /// <summary>Módulo 5: escenas de menú (MainMenu, CharacterSelection, Results).</summary>
    public static class OC_Scenes
    {
        public const string SceneFolder = "Assets/OperacionCuatro/Scenes/";
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";

        private static readonly Color Ink = new Color(0.05f, 0.06f, 0.1f, 1f);
        private static readonly Color PanelDark = new Color(0.07f, 0.09f, 0.15f, 0.93f);
        private static readonly Color Amber = new Color(1f, 0.58f, 0.16f, 1f);
        private static readonly Color Cyan = new Color(0.27f, 0.87f, 1f, 1f);
        private static readonly Color Bone = new Color(0.96f, 0.95f, 0.9f, 1f);

        // ==================================================================
        public static void BuildMenus()
        {
            BuildMainMenu();
            BuildCharacterSelection();
            BuildResults();
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[OC] Escenas de menú creadas.");
        }

        // ==================================================================
        //  MENÚ PRINCIPAL
        // ==================================================================
        private static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            OC_UI.NewCamera("Main Camera", new Color(0.03f, 0.04f, 0.08f), 6.2f);
            OC_UI.EnsureEventSystem();

            var canvas = OC_UI.NewCanvas("Canvas_MainMenu", 0);
            var root = canvas.transform;

            // Fondo
            var backdrop = OC_UI.NewRawImage("Backdrop", root, new Color(0.55f, 0.62f, 0.8f, 1f), LoadSprite("World/bg_harbor.png"));
            backdrop.rectTransform.Stretch();

            var sky = OC_UI.NewRawImage("SkyGlow", root, new Color(0.35f, 0.55f, 0.95f, 0.55f), LoadSprite("World/bg_sky.png"));
            sky.rectTransform.Stretch();
            sky.transform.SetAsFirstSibling();

            var shade = OC_UI.NewImage("Shade", root, new Color(0.02f, 0.03f, 0.07f, 0.62f));
            shade.rectTransform.Stretch();

            // Título
            var title = OC_UI.NewShadowedText("Title", root, "OPERACIÓN CUATRO", 118, TextAnchor.MiddleCenter, Bone);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -130f), new Vector2(1500f, 170f));
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<UIBob>().amplitude = 7f;

            var underline = OC_UI.NewImage("TitleRule", root, Amber);
            underline.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -296f), new Vector2(900f, 6f));

            var subtitle = OC_UI.NewShadowedText("Subtitle", root, "FASE 1  ·  PUERTO EN RUINAS", 34, TextAnchor.MiddleCenter, Cyan);
            subtitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -336f), new Vector2(1200f, 50f));

            // Panel de botones
            var mainPanel = OC_UI.Rect("MainPanel", root);
            mainPanel.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), new Vector2(700f, 520f));

            var oneBtn = OC_UI.NewButton("Button_1Player", mainPanel, "1 JUGADOR", 44, Amber, Ink);
            oneBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 170f), new Vector2(560f, 92f));

            var twoBtn = OC_UI.NewButton("Button_2Players", mainPanel, "2 JUGADORES", 44, Amber, Ink);
            twoBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(560f, 92f));

            var optBtn = OC_UI.NewButton("Button_Options", mainPanel, "OPCIONES", 44, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            optBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -50f), new Vector2(560f, 92f));

            var quitBtn = OC_UI.NewButton("Button_Quit", mainPanel, "SALIR", 44, new Color(0.3f, 0.11f, 0.13f, 1f), Bone);
            quitBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f), new Vector2(560f, 92f));

            // Récord y aviso
            var bestScore = OC_UI.NewShadowedText("BestScore", root, "RÉCORD 000000", 30, TextAnchor.MiddleCenter, Bone);
            bestScore.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 92f), new Vector2(900f, 46f));

            var hint = OC_UI.NewText("InsertCoin", root, "WASD + J K L   ·   FLECHAS + CTRL MAYÚS ENTER", 24, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
            hint.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 44f), new Vector2(1300f, 40f));
            var blink = hint.gameObject.AddComponent<UIBlink>();
            blink.target = hint;
            blink.speed = 2.6f;
            blink.minAlpha = 0.25f;

            // ---- Panel de opciones ----
            var optionsPanel = OC_UI.Rect("OptionsPanel", root);
            optionsPanel.Stretch();

            var dim = OC_UI.NewImage("Dim", optionsPanel, new Color(0.01f, 0.02f, 0.05f, 0.86f));
            dim.rectTransform.Stretch();

            var box = OC_UI.NewImage("Box", optionsPanel, PanelDark);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(980f, 640f));

            var optTitle = OC_UI.NewShadowedText("OptionsTitle", box.transform, "OPCIONES", 62, TextAnchor.MiddleCenter, Cyan);
            optTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(700f, 80f));

            var volumeLabel = OC_UI.NewText("VolumeLabel", box.transform, "VOLUMEN 80%", 32, TextAnchor.MiddleLeft, Bone);
            volumeLabel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -170f), new Vector2(760f, 46f));

            var volumeSlider = OC_UI.NewSlider("VolumeSlider", box.transform, Amber);
            volumeSlider.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -232f), new Vector2(760f, 40f));

            var brightnessLabel = OC_UI.NewText("BrightnessLabel", box.transform, "BRILLO 50%", 32, TextAnchor.MiddleLeft, Bone);
            brightnessLabel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -320f), new Vector2(760f, 46f));

            var brightnessSlider = OC_UI.NewSlider("BrightnessSlider", box.transform, Cyan);
            brightnessSlider.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -382f), new Vector2(760f, 40f));

            var resetBtn = OC_UI.NewButton("Button_Reset", box.transform, "RESTAURAR VALORES", 32, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            resetBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-200f, 74f), new Vector2(360f, 78f));

            var backBtn = OC_UI.NewButton("Button_Back", box.transform, "VOLVER", 32, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            backBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(200f, 74f), new Vector2(360f, 78f));

            optionsPanel.gameObject.SetActive(false);

            // ---- Lógica ----
            var logicGo = new GameObject("MainMenuLogic");
            var mainUi = logicGo.AddComponent<MainMenuUI>();
            mainUi.mainPanel = mainPanel.gameObject;
            mainUi.optionsPanel = optionsPanel.gameObject;
            mainUi.onePlayerButton = oneBtn;
            mainUi.twoPlayersButton = twoBtn;
            mainUi.optionsButton = optBtn;
            mainUi.quitButton = quitBtn;
            mainUi.bestScoreText = bestScore;

            var optionsUi = optionsPanel.gameObject.AddComponent<OptionsUI>();
            optionsUi.volumeSlider = volumeSlider;
            optionsUi.brightnessSlider = brightnessSlider;
            optionsUi.volumeLabel = volumeLabel;
            optionsUi.brightnessLabel = brightnessLabel;
            optionsUi.resetButton = resetBtn;
            optionsUi.backButton = backBtn;
            optionsUi.mainMenu = mainUi;

            SaveScene(scene, "MainMenu");
        }

        // ==================================================================
        //  SELECCIÓN DE PERSONAJES
        // ==================================================================
        private static void BuildCharacterSelection()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            OC_UI.NewCamera("Main Camera", new Color(0.03f, 0.04f, 0.08f), 6.2f);
            OC_UI.EnsureEventSystem();

            var canvas = OC_UI.NewCanvas("Canvas_CharacterSelect", 0);
            var root = canvas.transform;

            var backdrop = OC_UI.NewRawImage("Backdrop", root, new Color(0.4f, 0.45f, 0.6f, 1f), LoadSprite("World/bg_ruins.png"));
            backdrop.rectTransform.Stretch();

            var shade = OC_UI.NewImage("Shade", root, new Color(0.02f, 0.03f, 0.07f, 0.7f));
            shade.rectTransform.Stretch();

            var title = OC_UI.NewShadowedText("Title", root, "ELIGE TU PERSONAJE", 76, TextAnchor.MiddleCenter, Bone);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -96f), new Vector2(1400f, 110f));

            var modeText = OC_UI.NewText("ModeText", root, "MODO 1 JUGADOR", 30, TextAnchor.MiddleCenter, Cyan);
            modeText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -172f), new Vector2(900f, 44f));

            var hintText = OC_UI.NewText("HintText", root, "ELIGE TU PERSONAJE", 28, TextAnchor.MiddleCenter, Amber);
            hintText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -216f), new Vector2(1200f, 44f));

            string[] ids = { "Danner", "Cesar", "Sebastian", "Ines" };
            var buttons = new Button[4];
            var names = new Text[4];
            var roles = new Text[4];
            var p1Marks = new Image[4];
            var p2Marks = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                float x = -540f + i * 360f;

                var card = OC_UI.NewButton("Card_" + ids[i], root, "", 30, new Color(0.1f, 0.13f, 0.22f, 0.94f), Bone);
                card.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, 40f), new Vector2(320f, 560f));
                buttons[i] = card;

                var cardRect = card.GetComponent<RectTransform>();

                var portraitFrame = OC_UI.NewImage("PortraitFrame", cardRect, new Color(0.02f, 0.03f, 0.06f, 0.95f));
                portraitFrame.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -18f), new Vector2(284f, 300f));

                var portrait = OC_UI.NewRawImage("Portrait", portraitFrame.rectTransform, Color.white,
                    LoadSprite("Characters/" + ids[i] + "/" + ids[i] + "_idle_0.png"));
                portrait.preserveAspect = true;
                portrait.rectTransform.Stretch(6f, 6f, 6f, 6f);

                var nameText = OC_UI.NewShadowedText("Name", cardRect, CharacterDb.Names[i], 34, TextAnchor.MiddleCenter, Bone);
                nameText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 186f), new Vector2(300f, 46f));
                names[i] = nameText;

                var roleText = OC_UI.NewText("Role", cardRect, CharacterDb.Roles[i], 19, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
                roleText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 148f), new Vector2(300f, 40f));
                roles[i] = roleText;

                var p1 = OC_UI.NewImage("P1Mark", cardRect, CharacterDb.GetColor((CharacterId)i));
                p1.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-70f, 96f), new Vector2(120f, 44f));
                p1Marks[i] = p1;

                var p2 = OC_UI.NewImage("P2Mark", cardRect, new Color(1f, 1f, 1f, 0.06f));
                p2.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(70f, 96f), new Vector2(120f, 44f));
                p2Marks[i] = p2;
            }

            var p1Text = OC_UI.NewShadowedText("P1Selection", root, "P1: DANNER", 30, TextAnchor.MiddleCenter, CharacterDb.GetColor(CharacterId.Danner));
            p1Text.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-260f, 220f), new Vector2(460f, 46f));

            var p2Text = OC_UI.NewShadowedText("P2Selection", root, "P2: CÉSAR", 30, TextAnchor.MiddleCenter, CharacterDb.GetColor(CharacterId.Cesar));
            p2Text.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(260f, 220f), new Vector2(460f, 46f));

            var startBtn = OC_UI.NewButton("Button_Start", root, "INICIAR MISIÓN", 38, Amber, Ink);
            startBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-230f, 110f), new Vector2(440f, 88f));
            startBtn.gameObject.AddComponent<UIPulse>().amplitude = 0.02f;

            var backBtn = OC_UI.NewButton("Button_Back", root, "VOLVER", 34, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            backBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(230f, 110f), new Vector2(340f, 88f));

            var logicGo = new GameObject("CharacterSelectLogic");
            var logic = logicGo.AddComponent<CharacterSelectUI>();
            logic.characterButtons = buttons;
            logic.characterNames = names;
            logic.characterRoles = roles;
            logic.p1Marks = p1Marks;
            logic.p2Marks = p2Marks;
            logic.p1SelectionText = p1Text;
            logic.p2SelectionText = p2Text;
            logic.hintText = hintText;
            logic.modeText = modeText;
            logic.startButton = startBtn;
            logic.backButton = backBtn;

            SaveScene(scene, "CharacterSelection");
        }

        // ==================================================================
        //  RESULTADOS
        // ==================================================================
        private static void BuildResults()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            OC_UI.NewCamera("Main Camera", new Color(0.03f, 0.04f, 0.08f), 6.2f);
            OC_UI.EnsureEventSystem();

            var canvas = OC_UI.NewCanvas("Canvas_Results", 0);
            var root = canvas.transform;

            var backdrop = OC_UI.NewRawImage("Backdrop", root, new Color(0.45f, 0.5f, 0.62f, 1f), LoadSprite("World/bg_fog.png"));
            backdrop.rectTransform.Stretch();

            var shade = OC_UI.NewImage("Shade", root, new Color(0.02f, 0.03f, 0.07f, 0.8f));
            shade.rectTransform.Stretch();

            var box = OC_UI.NewImage("Box", root, PanelDark);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1100f, 720f));

            var title = OC_UI.NewShadowedText("Title", box.transform, "MISIÓN COMPLETADA", 72, TextAnchor.MiddleCenter, new Color(0.35f, 0.95f, 0.45f));
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -110f), new Vector2(1000f, 100f));

            var scoreLabel = OC_UI.NewText("ScoreLabel", box.transform, "PUNTUACIÓN", 26, TextAnchor.MiddleCenter, Cyan);
            scoreLabel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -220f), new Vector2(600f, 40f));

            var scoreText = OC_UI.NewShadowedText("Score", box.transform, "000000", 92, TextAnchor.MiddleCenter, Bone);
            scoreText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -300f), new Vector2(800f, 120f));

            var detail = OC_UI.NewText("Detail", box.transform, "BAJAS J1 0", 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            detail.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -400f), new Vector2(900f, 46f));

            var retryBtn = OC_UI.NewButton("Button_Retry", box.transform, "REINTENTAR", 36, Amber, Ink);
            retryBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-210f, 100f), new Vector2(380f, 92f));

            var menuBtn = OC_UI.NewButton("Button_Menu", box.transform, "MENÚ PRINCIPAL", 34, new Color(0.16f, 0.22f, 0.38f, 1f), Bone);
            menuBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(210f, 100f), new Vector2(380f, 92f));

            var logicGo = new GameObject("ResultsLogic");
            var logic = logicGo.AddComponent<ResultsUI>();
            logic.titleText = title;
            logic.scoreText = scoreText;
            logic.detailText = detail;
            logic.retryButton = retryBtn;
            logic.menuButton = menuBtn;

            SaveScene(scene, "Results");
        }

        // ==================================================================
        public static void RegisterBuildSettings()
        {
            string[] names = { "MainMenu", "CharacterSelection", "Level01", "Level02", "Results" };
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            for (int i = 0; i < names.Length; i++)
            {
                string path = SceneFolder + names[i] + ".unity";
                if (System.IO.File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
            }

            if (list.Count > 0) EditorBuildSettings.scenes = list.ToArray();
        }

        // ==================================================================
        private static Sprite LoadSprite(string rel)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + rel);
            if (sprite == null) Debug.LogError("[OC] Sprite no encontrado: " + SpriteRoot + rel);
            return sprite;
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneFolder + name + ".unity");
        }
    }
}
