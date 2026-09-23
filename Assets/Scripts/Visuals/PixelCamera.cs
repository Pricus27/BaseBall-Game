using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// Orthographic camera sized so one Kenney pixel maps cleanly onto screen pixels.
    /// </summary>
    public static class PixelCamera
    {
        public const int ReferenceWidth = 320;
        public const int ReferenceHeight = 180;

        /// <summary>Half-height of the view in world units for a given reference resolution.</summary>
        public static float OrthographicSizeFor(int referenceHeight, int pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0) return 1f;
            return referenceHeight / 2f / pixelsPerUnit;
        }

        /// <summary>Half-width of the view in world units at the supplied aspect ratio.</summary>
        public static float HorizontalExtent(float orthographicSize, float aspect)
        {
            return orthographicSize * aspect;
        }

        public static Camera Create(Color skyColor)
        {
            var go = new GameObject("PixelCamera");
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSizeFor(ReferenceHeight, KenneyArt.PixelsPerUnit);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = skyColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.allowMSAA = false;
            camera.allowHDR = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            QualitySettings.antiAliasing = 0;
            return camera;
        }
    }
}
