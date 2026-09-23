using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// Draws each pitch's word on the UI canvas and keeps it pinned above the moving ball,
    /// so the pixel font stays crisp instead of scaling with the world sprite.
    /// </summary>
    public sealed class WordLabelLayer : MonoBehaviour
    {
        private readonly Dictionary<PitchController, RectTransform> labels =
            new Dictionary<PitchController, RectTransform>();

        private Camera worldCamera;
        private RectTransform canvasRect;

        public static WordLabelLayer Build(Transform canvas, Camera camera)
        {
            var root = new GameObject("WordLabels", typeof(RectTransform));
            root.transform.SetParent(canvas, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layer = root.AddComponent<WordLabelLayer>();
            layer.worldCamera = camera;
            layer.canvasRect = canvas.GetComponent<RectTransform>();
            return layer;
        }

        public void Track(PitchController pitch)
        {
            var labelObject = new GameObject("Word_" + pitch.Word, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 44f);

            var shadow = PixelUi.Label(labelObject.transform, "Shadow", pitch.Word, 34, new Color(0f, 0f, 0f, 0.8f),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            shadow.rectTransform.anchoredPosition = new Vector2(3f, -3f);

            PixelUi.Label(labelObject.transform, "Text", pitch.Word, 34, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            labels[pitch] = rect;
            Refresh(pitch);
        }

        public void Release(PitchController pitch)
        {
            if (!labels.TryGetValue(pitch, out RectTransform rect)) return;
            labels.Remove(pitch);
            if (rect != null) Destroy(rect.gameObject);
        }

        public void ReleaseAll()
        {
            foreach (KeyValuePair<PitchController, RectTransform> entry in labels)
            {
                if (entry.Value != null) Destroy(entry.Value.gameObject);
            }

            labels.Clear();
        }

        private void LateUpdate()
        {
            foreach (KeyValuePair<PitchController, RectTransform> entry in labels)
            {
                if (entry.Key == null || entry.Value == null) continue;
                Refresh(entry.Key);
            }
        }

        private void Refresh(PitchController pitch)
        {
            if (!labels.TryGetValue(pitch, out RectTransform rect) || rect == null) return;

            Vector2 screenPoint = worldCamera.WorldToScreenPoint(pitch.LabelAnchor);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local))
            {
                rect.anchoredPosition = local;
            }

            string display = pitch.DisplayWord();
            string typed = Highlight(display, pitch.TypedCount);

            var texts = rect.GetComponentsInChildren<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].text = texts[i].gameObject.name == "Shadow" ? display : typed;
            }
        }

        private static string Highlight(string display, int typedCount)
        {
            int count = Mathf.Clamp(typedCount, 0, display.Length);
            if (count == 0) return display;
            return "<color=#7CFF9B>" + display.Substring(0, count) + "</color>" + display.Substring(count);
        }
    }
}
