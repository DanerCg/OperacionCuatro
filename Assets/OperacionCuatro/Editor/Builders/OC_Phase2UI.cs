using OperacionCuatro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OCBuilders
{
    /// <summary>
    /// Fase 2 — retoque quirúrgico de la escena Results: añade el botón SIGUIENTE MISIÓN y lo
    /// conecta a ResultsUI. No reconstruye la escena ni toca el resto de su contenido.
    /// </summary>
    public static class OC_Phase2UI
    {
        private const string ResultsScenePath = "Assets/OperacionCuatro/Scenes/Results.unity";

        public static void AddNextMissionButton()
        {
            if (!System.IO.File.Exists(ResultsScenePath))
            {
                Debug.LogError("[OC] No se encontró la escena Results.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ResultsScenePath, OpenSceneMode.Single);

            var logic = Object.FindFirstObjectByType<ResultsUI>();
            if (logic == null)
            {
                Debug.LogError("[OC] ResultsUI no encontrado en la escena Results.");
                return;
            }

            if (logic.nextMissionButton != null)
            {
                Debug.Log("[OC] El botón SIGUIENTE MISIÓN ya existe; no se duplica.");
                return;
            }

            Transform parent = logic.retryButton != null ? logic.retryButton.transform.parent : logic.transform;

            var button = OC_UI.NewButton("Button_NextMission", parent, "SIGUIENTE MISIÓN", 36,
                new Color(0.18f, 0.62f, 0.34f, 1f), new Color(0.05f, 0.06f, 0.1f));

            button.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 212f), new Vector2(780f, 96f));

            logic.nextMissionButton = button;

            EditorUtility.SetDirty(logic);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[OC] Botón SIGUIENTE MISIÓN añadido a Results y conectado a ResultsUI.");
        }
    }
}
