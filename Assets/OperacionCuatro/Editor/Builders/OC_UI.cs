using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OCBuilders
{
    /// <summary>Utilidades para construir interfaz uGUI por código (sin TextMeshPro).</summary>
    public static class OC_UI
    {
        private static Font _font;
        private static Sprite _sprite;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Sprite PanelSprite
        {
            get
            {
                if (_sprite == null) _sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                return _sprite;
            }
        }

        // ------------------------------------------------------------------
        public static GameObject NewCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            canvas.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return go;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static Camera NewCamera(string name, Color background, float orthoSize)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(0f, 0f, -10f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 100f;

            go.AddComponent<AudioListener>();
            go.tag = "MainCamera";
            return cam;
        }

        // ------------------------------------------------------------------
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        // ------------------------------------------------------------------
        public static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.sprite = PanelSprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static Image NewRawImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return image;
        }

        public static Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
        {
            var rt = Rect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            return text;
        }

        public static Text NewShadowedText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
        {
            var text = NewText(name, parent, content, size, anchor, color);

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(3f, -3f);

            return text;
        }

        // ------------------------------------------------------------------
        public static Button NewButton(string name, Transform parent, string label, int fontSize, Color normal, Color textColor)
        {
            var rt = Rect(name, parent);

            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = normal;
            image.raycastTarget = true;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var text = NewShadowedText("Label", rt, label, fontSize, TextAnchor.MiddleCenter, textColor);
            text.rectTransform.Stretch(10f, 4f, 10f, 4f);

            return button;
        }

        public static Slider NewSlider(string name, Transform parent, Color fillColor)
        {
            var root = Rect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            var background = NewImage("Background", root, new Color(0.08f, 0.09f, 0.13f, 0.95f));
            background.rectTransform.Stretch();

            var fillArea = Rect("Fill Area", root);
            fillArea.Stretch(6f, 6f, 6f, 6f);

            var fill = NewImage("Fill", fillArea, fillColor);
            fill.rectTransform.Stretch();

            var handleArea = Rect("Handle Slide Area", root);
            handleArea.Stretch(8f, 0f, 8f, 0f);

            var handle = NewImage("Handle", handleArea, new Color(0.95f, 0.96f, 1f));
            handle.rectTransform.sizeDelta = new Vector2(22f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.transition = Selectable.Transition.ColorTint;

            return slider;
        }

        public static CanvasGroup NewGroup(string name, Transform parent, bool blocksRaycasts)
        {
            var rt = Rect(name, parent);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = blocksRaycasts;
            group.interactable = blocksRaycasts;
            rt.Stretch();
            return group;
        }
    }
}
