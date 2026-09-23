using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// Pure motion math for a pitch, kept free of scene objects so it can be unit tested.
    /// </summary>
    public static class PitchTrajectory
    {
        /// <summary>
        /// Maps normalized time to travel progress. The changeup bursts forward and then
        /// drags, every other pitch advances linearly.
        /// </summary>
        public static float Progress(PitchKind kind, float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            if (kind != PitchKind.Changeup) return t;

            return t < 0.35f
                ? t * 1.6f
                : 0.56f + (t - 0.35f) * (0.44f / 0.65f);
        }

        public static float HorizontalAt(float progress)
        {
            return Mathf.Lerp(Ballpark.PitchStartX, Ballpark.PlateX, Mathf.Clamp01(progress));
        }

        /// <summary>Vertical offset layered on top of the pitch lane.</summary>
        public static float VerticalOffset(PitchKind kind, float progress, float elapsed, float seed)
        {
            switch (kind)
            {
                case PitchKind.Curveball:
                    return Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * 1.15f;
                case PitchKind.FlamingKnuckleball:
                    return (Mathf.PerlinNoise(seed, elapsed * 6f) - 0.5f) * 1.05f;
                default:
                    return 0f;
            }
        }

        public static float HorizontalWobble(PitchKind kind, float elapsed, float seed)
        {
            if (kind != PitchKind.FlamingKnuckleball) return 0f;
            return (Mathf.PerlinNoise(seed + 17f, elapsed * 8f) - 0.5f) * 0.36f;
        }

        /// <summary>Balls grow slightly as they close on the plate to sell depth.</summary>
        public static float ScaleAt(float progress)
        {
            return Mathf.Lerp(0.62f, 1.05f, Mathf.Clamp01(progress));
        }
    }
}
