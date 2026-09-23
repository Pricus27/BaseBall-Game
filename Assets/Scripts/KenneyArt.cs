using System.Collections.Generic;
using UnityEngine;

namespace BaseballTyping
{
    /// <summary>
    /// Central access point for the curated Kenney sprites and fonts in Resources.
    /// Names match the targets written by the editor asset pipeline.
    /// </summary>
    public static class KenneyArt
    {
        public const int PixelsPerUnit = 18;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();

        public static class World
        {
            public const string GroundLeft = "Kenney/World/ground_left";
            public const string GroundMid = "Kenney/World/ground_mid";
            public const string GroundRight = "Kenney/World/ground_right";
            public const string Dirt = "Kenney/World/dirt";
            public const string DirtRough = "Kenney/World/dirt_rough";
            public const string GrassFill = "Kenney/World/grass_fill";
            public const string GrassSpeck = "Kenney/World/grass_speck";
            public const string DirtFill = "Kenney/World/dirt_fill";
            public const string DirtSpeck = "Kenney/World/dirt_speck";
            public const string WallLeft = "Kenney/World/wall_left";
            public const string WallMid = "Kenney/World/wall_mid";
            public const string WallRight = "Kenney/World/wall_right";
            public const string BushSmall = "Kenney/World/bush_small";
            public const string BushLarge = "Kenney/World/bush_large";
            public const string Tree = "Kenney/World/tree";
            public const string BatterA = "Kenney/World/batter_a";
            public const string BatterB = "Kenney/World/batter_b";
            public const string MachineA = "Kenney/World/machine_a";
            public const string MachineB = "Kenney/World/machine_b";
            public const string MachineC = "Kenney/World/machine_c";

            public static readonly string[] Fans =
            {
                "Kenney/World/fan_a",
                "Kenney/World/fan_b",
                "Kenney/World/fan_c",
                "Kenney/World/fan_d"
            };
        }

        public static class Props
        {
            public const string Baseball = "Kenney/Props/baseball";
            public const string Bat = "Kenney/Props/bat";
            public const string Helmet = "Kenney/Props/helmet";
        }

        public static class Ui
        {
            public const string PanelCream = "Kenney/Ui/panel_cream";
            public const string PanelBrown = "Kenney/Ui/panel_brown";
            public const string PanelBolted = "Kenney/Ui/panel_bolted";
            public const string PanelDark = "Kenney/Ui/panel_dark";
            public const string PanelFrame = "Kenney/Ui/panel_frame";
            public const string PanelRed = "Kenney/Ui/panel_red";
            public const string PanelGreen = "Kenney/Ui/panel_green";
            public const string BarFrame = "Kenney/Ui/bar_frame";
            public const string BarFill = "Kenney/Ui/bar_fill";
            public const string DotEmpty = "Kenney/Ui/dot_empty";
            public const string DotRed = "Kenney/Ui/dot_red";
            public const string DotGreen = "Kenney/Ui/dot_green";
            public const string Banner = "Kenney/Ui/banner";
        }

        private static Sprite whitePixel;

        /// <summary>
        /// Solid white 1x1 sprite for flat shapes (home plate, sparks, flashes) that would
        /// pick up unwanted colour if a tinted Kenney tile were reused.
        /// </summary>
        public static Sprite WhitePixel
        {
            get
            {
                if (whitePixel != null) return whitePixel;

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "WhitePixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                whitePixel = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
                return whitePixel;
            }
        }

        public static Sprite Sprite(string resourcePath)
        {
            if (SpriteCache.TryGetValue(resourcePath, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite loaded = Resources.Load<Sprite>(resourcePath);
            SpriteCache[resourcePath] = loaded;
            return loaded;
        }

        public static Font PixelFont => LoadFont("Fonts/KenneyPixel");

        public static Font DisplayFont => LoadFont("Fonts/KenneyBlocks");

        public static Font MonoFont => LoadFont("Fonts/KenneyMiniSquare");

        private static Font LoadFont(string resourcePath)
        {
            if (FontCache.TryGetValue(resourcePath, out Font cached) && cached != null)
            {
                return cached;
            }

            Font loaded = Resources.Load<Font>(resourcePath);
            if (loaded == null)
            {
                loaded = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            FontCache[resourcePath] = loaded;
            return loaded;
        }

        public static void ClearCaches()
        {
            SpriteCache.Clear();
            FontCache.Clear();
        }
    }
}
