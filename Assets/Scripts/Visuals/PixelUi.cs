using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// UI builders that use the Kenney pixel sprites and fonts. Panels are 9-sliced so the
    /// thick outline stays one pixel wide at any size.
    /// </summary>
    public static class PixelUi
    {
        public static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("PixelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule))
                    .GetComponent<EventSystem>();
                eventSystem.sendNavigationEvents = false;
            }

            return canvas;
        }

        public static RectTransform Region(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        }

        public static Image Panel(Transform parent, string name, string spritePath, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var image = go.GetComponent<Image>();
            ApplySprite(image, spritePath);
            return image;
        }

        /// <summary>
        /// Places a sprite. Round or decorated art (dots, ribbons) must stay unsliced, since
        /// 9-slicing would stretch their shape into something unrecognisable.
        /// </summary>
        public static Image Sprite(Transform parent, string name, string spritePath, Vector2 anchoredPosition, Vector2 size, bool sliced = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            ApplySprite(image, spritePath, sliced);
            return image;
        }

        public static Text Label(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool display = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var text = go.GetComponent<Text>();
            text.font = display ? KenneyArt.DisplayFont : KenneyArt.PixelFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.text = value;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>Label with a hard pixel drop shadow, matching the Kenney art style.</summary>
        public static Text ShadowLabel(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool display = false)
        {
            Text text = Label(parent, name, value, fontSize, color, alignment, anchorMin, anchorMax, display);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            ApplySprite(image, KenneyArt.Ui.PanelBrown);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.78f);
            colors.pressedColor = new Color(0.78f, 0.68f, 0.55f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            ShadowLabel(go.transform, "Label", label, 26, new Color(1f, 0.97f, 0.88f),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return button;
        }

        /// <summary>
        /// Canvas reference PPU (100) divided by the sprite PPU (18) would blow the 8 px Kenney
        /// outline up to ~178 screen pixels, so the multiplier pins it near 16 px instead.
        /// </summary>
        private const float SliceMultiplier = 2.8f;

        private static void ApplySprite(Image image, string spritePath, bool sliced = true)
        {
            Sprite sprite = KenneyArt.Sprite(spritePath);
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = SliceMultiplier;

            if (sprite == null)
            {
                // Missing art should still render a readable block rather than nothing.
                image.color = new Color(0.16f, 0.13f, 0.11f, 0.9f);
            }
        }

        private static RectTransform Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
