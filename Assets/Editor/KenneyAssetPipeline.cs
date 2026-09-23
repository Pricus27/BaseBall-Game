#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace BaseballTyping.Editor
{
    /// <summary>
    /// Downloads the CC0 Kenney packs and copies only the sprites the game uses into Resources.
    /// The full downloads stay outside Assets so the project and player builds stay small.
    /// </summary>
    public static class KenneyAssetPipeline
    {
        private const string DownloadRoot = "Tools/KenneyDownloads";
        private const string WorldRoot = "Assets/Resources/Kenney/World";
        private const string PropsRoot = "Assets/Resources/Kenney/Props";
        private const string UiRoot = "Assets/Resources/Kenney/Ui";
        private const string FontRoot = "Assets/Resources/Fonts";

        private static readonly string[] Packs =
        {
            "pixel-platformer",
            "ui-pack-pixel-adventure",
            "kenney-fonts",
            "sports-pack"
        };

        // Kenney ships generic tile_NNNN names, so every sprite is mapped to a readable name here.
        private static readonly (string Source, string Target)[] WorldSprites =
        {
            ("pixel-platformer/Tiles/tile_0000.png", "ground_left.png"),
            ("pixel-platformer/Tiles/tile_0001.png", "ground_mid.png"),
            ("pixel-platformer/Tiles/tile_0002.png", "ground_right.png"),
            ("pixel-platformer/Tiles/tile_0005.png", "dirt.png"),
            ("pixel-platformer/Tiles/tile_0006.png", "dirt_rough.png"),
            ("pixel-platformer/Tiles/tile_0038.png", "grass_fill.png"),
            ("pixel-platformer/Tiles/tile_0037.png", "grass_speck.png"),
            ("pixel-platformer/Tiles/tile_0024.png", "dirt_fill.png"),
            ("pixel-platformer/Tiles/tile_0025.png", "dirt_speck.png"),
            ("pixel-platformer/Tiles/tile_0012.png", "wall_left.png"),
            ("pixel-platformer/Tiles/tile_0013.png", "wall_mid.png"),
            ("pixel-platformer/Tiles/tile_0014.png", "wall_right.png"),
            ("pixel-platformer/Tiles/tile_0124.png", "bush_small.png"),
            ("pixel-platformer/Tiles/tile_0125.png", "bush_large.png"),
            ("pixel-platformer/Tiles/tile_0126.png", "tree.png"),
            ("pixel-platformer/Tiles/Characters/tile_0002.png", "batter_a.png"),
            ("pixel-platformer/Tiles/Characters/tile_0003.png", "batter_b.png"),
            ("pixel-platformer/Tiles/Characters/tile_0006.png", "fan_a.png"),
            ("pixel-platformer/Tiles/Characters/tile_0007.png", "fan_b.png"),
            ("pixel-platformer/Tiles/Characters/tile_0000.png", "fan_c.png"),
            ("pixel-platformer/Tiles/Characters/tile_0004.png", "fan_d.png"),
            ("pixel-platformer/Tiles/Characters/tile_0015.png", "machine_a.png"),
            ("pixel-platformer/Tiles/Characters/tile_0016.png", "machine_b.png"),
            ("pixel-platformer/Tiles/Characters/tile_0017.png", "machine_c.png")
        };

        private static readonly (string Source, string Target)[] PropSprites =
        {
            ("sports-pack/PNG/Equipment/ball_generic1.png", "baseball.png"),
            ("sports-pack/PNG/Equipment/bat_wood.png", "bat.png"),
            ("sports-pack/PNG/Equipment/helmet_white1.png", "helmet.png")
        };

        private static readonly (string Source, string Target)[] UiSprites =
        {
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0000.png", "panel_cream.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0001.png", "panel_brown.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0004.png", "panel_bolted.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0008.png", "panel_dark.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0009.png", "panel_frame.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0020.png", "panel_red.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0021.png", "panel_green.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0033.png", "bar_frame.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0039.png", "dot_empty.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0054.png", "dot_red.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0055.png", "dot_green.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0043.png", "banner.png"),
            ("ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline/tile_0069.png", "bar_fill.png")
        };

        private static readonly (string Source, string Target)[] Fonts =
        {
            ("kenney-fonts/Fonts/Kenney Pixel.ttf", "KenneyPixel.ttf"),
            ("kenney-fonts/Fonts/Kenney Blocks.ttf", "KenneyBlocks.ttf"),
            ("kenney-fonts/Fonts/Kenney Mini Square.ttf", "KenneyMiniSquare.ttf")
        };

        [MenuItem("Baseball Typing/Download Kenney Art")]
        public static void DownloadAndInstall()
        {
            DownloadPacks();
            InstallCuratedArt();
        }

        [MenuItem("Baseball Typing/Reinstall Kenney Art From Cache")]
        public static void InstallCuratedArt()
        {
            Directory.CreateDirectory(WorldRoot);
            Directory.CreateDirectory(PropsRoot);
            Directory.CreateDirectory(UiRoot);
            Directory.CreateDirectory(FontRoot);

            int copied = 0;
            copied += CopyAll(WorldSprites, WorldRoot);
            copied += CopyAll(PropSprites, PropsRoot);
            copied += CopyAll(UiSprites, UiRoot);
            copied += CopyAll(Fonts, FontRoot);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"Kenney art installed: {copied} files.");
        }

        private static void DownloadPacks()
        {
            Directory.CreateDirectory(DownloadRoot);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            foreach (string slug in Packs)
            {
                string extracted = Path.Combine(DownloadRoot, slug);
                if (Directory.Exists(extracted)) continue;

                string zipPath = Path.Combine(DownloadRoot, slug + ".zip");
                if (!File.Exists(zipPath))
                {
                    using var client = new WebClient();
                    client.Headers.Add("User-Agent", "Mozilla/5.0");
                    string page = client.DownloadString("https://kenney.nl/assets/" + slug);
                    Match match = Regex.Match(page, "https://kenney\\.nl/media/pages/assets/[^\"']+\\.zip");
                    if (!match.Success)
                    {
                        throw new InvalidOperationException("No zip link found on the Kenney page for " + slug);
                    }

                    client.DownloadFile(match.Value, zipPath);
                }

                ZipFile.ExtractToDirectory(zipPath, extracted);
                Debug.Log("Extracted Kenney pack: " + slug);
            }
        }

        private static int CopyAll((string Source, string Target)[] entries, string destinationFolder)
        {
            var missing = new List<string>();
            int copied = 0;

            foreach ((string source, string target) in entries)
            {
                string fullSource = Path.Combine(DownloadRoot, source.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullSource))
                {
                    missing.Add(source);
                    continue;
                }

                File.Copy(fullSource, Path.Combine(destinationFolder, target), true);
                copied++;
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning("Missing Kenney source files: " + string.Join(", ", missing));
            }

            return copied;
        }
    }
}
#endif
