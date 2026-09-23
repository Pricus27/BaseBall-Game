using System.Collections.Generic;
using UnityEngine;

namespace BaseballTyping.Visuals
{
    /// <summary>
    /// Builds the static ballpark out of Kenney tiles: crowd rows, brick outfield wall,
    /// mowed grass, infield dirt, and the foliage beyond the fence.
    /// </summary>
    public sealed class PixelStage : MonoBehaviour
    {
        private const float Tile = 1f;

        private readonly List<Transform> crowd = new List<Transform>();
        private float crowdPhase;

        public static PixelStage Build(Camera camera)
        {
            var root = new GameObject("Ballpark");
            var stage = root.AddComponent<PixelStage>();
            stage.Construct(camera);
            return stage;
        }

        private void Construct(Camera camera)
        {
            float extent = Ballpark.HorizontalExtent(camera) + Tile;
            int columns = Mathf.CeilToInt(extent * 2f / Tile) + 2;
            float startX = -extent;

            BuildSkyline(startX, columns);
            BuildCrowd(startX, columns);
            BuildWall(startX, columns);
            BuildGrass(startX, columns, camera);
            BuildDirt(startX, columns, camera);
            BuildPlate();
        }

        private void BuildSkyline(float startX, int columns)
        {
            for (int i = 0; i < columns; i += 2)
            {
                float x = startX + i * Tile;
                bool tall = i % 4 == 0;
                string sprite = tall ? KenneyArt.World.Tree : KenneyArt.World.BushLarge;
                var tree = Place(sprite, new Vector2(x + 0.4f, Ballpark.WallTopY + 0.85f), Ballpark.Layer.Skyline);
                tree.transform.localScale = Vector3.one * (tall ? 1.35f : 1.05f);
                tree.color = new Color(0.72f, 0.82f, 0.78f);
            }
        }

        private void BuildCrowd(float startX, int columns)
        {
            var palette = new[]
            {
                new Color(0.95f, 0.45f, 0.35f),
                new Color(0.45f, 0.72f, 0.95f),
                new Color(0.98f, 0.82f, 0.36f),
                new Color(0.66f, 0.55f, 0.92f),
                new Color(0.52f, 0.88f, 0.62f)
            };

            for (int row = 0; row < 2; row++)
            {
                float y = Ballpark.WallTopY + 0.28f + row * 0.42f;
                float offset = row * 0.3f;
                for (int i = 0; i < columns * 2; i++)
                {
                    float x = startX + i * 0.52f + offset;
                    var fan = Place(
                        KenneyArt.World.Fans[(i + row) % KenneyArt.World.Fans.Length],
                        new Vector2(x, y),
                        Ballpark.Layer.Crowd + row);
                    fan.transform.localScale = Vector3.one * 0.46f;
                    fan.color = palette[(i * 3 + row) % palette.Length] * (row == 0 ? 0.82f : 1f);
                    crowd.Add(fan.transform);
                }
            }
        }

        private void BuildWall(float startX, int columns)
        {
            float bottom = Ballpark.GrassTopY;
            int rows = Mathf.CeilToInt((Ballpark.WallTopY - bottom) / Tile);

            for (int row = 0; row < rows; row++)
            {
                for (int i = 0; i < columns; i++)
                {
                    string sprite = row == rows - 1
                        ? (i % 7 == 0 ? KenneyArt.World.WallLeft : i % 7 == 6 ? KenneyArt.World.WallRight : KenneyArt.World.WallMid)
                        : KenneyArt.World.WallMid;

                    Place(sprite, new Vector2(startX + i * Tile, bottom + row * Tile + Tile * 0.5f), Ballpark.Layer.Wall);
                }
            }
        }

        private void BuildGrass(float startX, int columns, Camera camera)
        {
            int rows = Mathf.CeilToInt((Ballpark.GrassTopY - Ballpark.DirtTopY) / Tile);

            for (int row = 0; row < rows; row++)
            {
                float y = Ballpark.DirtTopY + row * Tile + Tile * 0.5f;
                for (int i = 0; i < columns; i++)
                {
                    // Only the seamless fill tile is tiled; stripes come from tinting so the
                    // tile outlines never show up as a grid across the outfield.
                    bool stripe = (i / 2) % 2 == 0;
                    var tile = Place(KenneyArt.World.GrassFill, new Vector2(startX + i * Tile, y), Ballpark.Layer.Grass);
                    tile.color = stripe ? Color.white : new Color(0.86f, 0.93f, 0.86f);
                }
            }

            // A darker band marks the warning track where the grass meets the infield dirt.
            var seam = Solid(new Vector2(0f, Ballpark.DirtTopY + 0.06f), new Vector2(columns * Tile, 0.12f),
                new Color(0.23f, 0.42f, 0.25f), Ballpark.Layer.Grass + 1);
            seam.name = "WarningTrack";
        }

        private void BuildDirt(float startX, int columns, Camera camera)
        {
            float bottom = -camera.orthographicSize - Tile;
            int rows = Mathf.CeilToInt((Ballpark.DirtTopY - bottom) / Tile);

            for (int row = 0; row < rows; row++)
            {
                float y = Ballpark.DirtTopY - row * Tile - Tile * 0.5f;
                for (int i = 0; i < columns; i++)
                {
                    var tile = Place(KenneyArt.World.DirtFill, new Vector2(startX + i * Tile, y), Ballpark.Layer.Dirt);
                    if ((i + row) % 3 == 0) tile.color = new Color(0.94f, 0.9f, 0.86f);
                }
            }
        }

        private void BuildPlate()
        {
            Solid(new Vector2(Ballpark.PlateX, Ballpark.DirtTopY - 0.18f), new Vector2(0.62f, 0.16f),
                new Color(0.97f, 0.97f, 0.94f), Ballpark.Layer.Dirt + 2).name = "HomePlate";

            Solid(new Vector2(Ballpark.BatterX, Ballpark.DirtTopY - 0.1f), new Vector2(1.5f, 0.5f),
                new Color(0.76f, 0.58f, 0.42f), Ballpark.Layer.Dirt + 1).name = "BattersBox";
        }

        private SpriteRenderer Solid(Vector2 position, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject("Solid");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = KenneyArt.WhitePixel;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void Update()
        {
            // Subtle crowd shimmer so the stands feel alive without extra art.
            crowdPhase += Time.deltaTime * 3.2f;
            for (int i = 0; i < crowd.Count; i++)
            {
                Transform fan = crowd[i];
                if (fan == null) continue;
                Vector3 position = fan.localPosition;
                position.y += Mathf.Sin(crowdPhase + i * 0.7f) * 0.0016f;
                fan.localPosition = position;
            }
        }

        private SpriteRenderer Place(string resourcePath, Vector2 position, int sortingOrder)
        {
            var go = new GameObject(resourcePath.Substring(resourcePath.LastIndexOf('/') + 1));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = KenneyArt.Sprite(resourcePath);
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
