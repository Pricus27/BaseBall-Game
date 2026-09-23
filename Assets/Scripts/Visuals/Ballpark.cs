using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// Shared world-space layout for the side-on ballpark, in Unity units.
    /// </summary>
    public static class Ballpark
    {
        public const float DirtTopY = -1.4f;
        public const float GrassTopY = 1.2f;
        public const float WallTopY = 2.3f;

        public const float PlateX = -4.4f;
        public const float PitchStartX = 8.4f;
        public const float BatterX = -5.7f;
        public const float MachineX = 7.4f;

        public const float LaneMinY = -0.7f;
        public const float LaneMaxY = 1.0f;

        public static class Layer
        {
            public const int Skyline = -90;
            public const int Crowd = -80;
            public const int Wall = -70;
            public const int Grass = -60;
            public const int Dirt = -50;
            public const int Machine = -10;
            public const int Batter = 10;
            public const int Bat = 12;
            public const int Ball = 20;
            public const int Particles = 30;
        }

        /// <summary>Clamps a spawn lane to the hittable band above the grass.</summary>
        public static float ClampLane(float y)
        {
            return Mathf.Clamp(y, LaneMinY, LaneMaxY);
        }

        public static float HorizontalExtent(Camera camera)
        {
            if (camera == null) return 9f;
            return PixelCamera.HorizontalExtent(camera.orthographicSize, camera.aspect);
        }
    }
}
