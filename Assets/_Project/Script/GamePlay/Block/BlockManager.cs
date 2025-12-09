using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class BlockManager : MonoBehaviour
    {
        [Header("References")] 
        public bool[,] ShapeData => _shapeData;
        private Sprite[] _sprites;
        private bool[,] _shapeData;
        
        RenderMap _renderMap;
        CheckLevelScore _checkLevelScore;
        BlockSpawn _blockSpawn;

        [Inject]
        void Construct(CheckLevelScore checkLevelScore, RenderMap renderMap, BlockSpawn blockSpawn)
        {
            _checkLevelScore = checkLevelScore;
            _renderMap = renderMap;
            _blockSpawn = blockSpawn;
        }
        
        private void Start()
        {
            BuildSpritesFromPrefabs();
        }

        private void BuildSpritesFromPrefabs()
        {
            if (_blockSpawn == null) return;

            var prefabs = _blockSpawn.PrefabBlocks;
            if (prefabs == null || prefabs.Length == 0) return;
            var spriteList = new List<Sprite>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null) continue;

                var sr = prefab.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;
                spriteList.Add(sr.sprite);
            }

            _sprites = spriteList.ToArray();
        }

        private int GetMaxSpritesForLevel()
        {
            if (_sprites == null || _sprites.Length == 0) return 0;
            if (_checkLevelScore == null) return _sprites.Length;
            int level = _checkLevelScore.CurrentLevel;

            return level switch
            {
                0 => Mathf.Min(5, _sprites.Length),
                1 => Mathf.Min(9, _sprites.Length),
                2 => Mathf.Min(13, _sprites.Length),
                _ => _sprites.Length
            };
        }

        // Spawn random shape theo level
        public void SpawnSandWithRandomShape(Map map, SpriteRenderer mapRenderer)
        {
            if (map == null || mapRenderer == null)
                return;

            if (_sprites == null || _sprites.Length == 0)
                return;

            var maxSpritesForLevel = GetMaxSpritesForLevel();
            if (maxSpritesForLevel <= 0)
                return;

            var randomIndex = Random.Range(0, maxSpritesForLevel);
            var selectedSprite = _sprites[randomIndex];

            SpawnSandWithSprite(map, mapRenderer, selectedSprite);
        }

        // Sprite từ prefab block, check va chạm, rồi vẽ block xuống Map.
        public bool SpawnSandWithSprite(Map map, SpriteRenderer mapRenderer, Sprite sprite)
        {
            return SpawnSandWithSprite(map, mapRenderer, sprite, out _);
        }

        // Overload trả về vị trí spawn thực tế
        public bool SpawnSandWithSprite(Map map, SpriteRenderer mapRenderer, Sprite sprite, out Vector3 actualSpawnPosition)
        {
            actualSpawnPosition = Vector3.zero;

            if (map == null || mapRenderer == null || sprite == null) return false;

            var (shapeData, colorData) = ExtractShapeAndColorData(sprite);
            if (shapeData == null) return false;

            _shapeData = shapeData;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = _renderMap.transform.InverseTransformPoint(mouseWorldPos);

            var spriteWidth = mapRenderer.sprite.bounds.size.x;
            var spriteHeight = mapRenderer.sprite.bounds.size.y;

            if (spriteWidth <= 0 || spriteHeight <= 0) return false;

            var centerX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renderMap._wight);
            var centerY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renderMap._hight);

            var shapeWidth = shapeData.GetLength(0);
            var shapeHeight = shapeData.GetLength(1);

            if (shapeWidth <= 0 || shapeHeight <= 0)
                return false;

            for (int x = 0; x < shapeWidth; x++)
            {
                for (int y = 0; y < shapeHeight; y++)
                {
                    if (!shapeData[x, y]) continue;

                    var targetX = centerX - shapeWidth / 2 + x;
                    var targetY = centerY - shapeHeight / 2 + y;

                    if (targetX < 0 || targetX >= _renderMap._wight || targetY < 0 || targetY >= _renderMap._hight)
                        return false;

                    var cell = map.GetCell(targetX, targetY);
                    if (cell.hasValue == 1 || cell.isBorder == 1) return false;
                }
            }

            //Ghi pixel xuống map
            for (int x = 0; x < shapeWidth; x++)
            {
                for (int y = 0; y < shapeHeight; y++)
                {
                    if (!shapeData[x, y]) continue;

                    var targetX = centerX - shapeWidth / 2 + x;
                    var targetY = centerY - shapeHeight / 2 + y;

                    Color32 pixelColor = colorData != null ? colorData[x, y] : Color.white;
                    map.SetPixelCell(targetX, targetY, pixelColor);
                }
            }

            // Tính toán vị trí world thực tế của center
            float fx = ((float)centerX / _renderMap._wight - 0.5f) * spriteWidth;
            float fy = ((float)centerY / _renderMap._hight - 0.5f) * spriteHeight;
            Vector3 centerLocal = new Vector3(fx, fy, 0);
            actualSpawnPosition = _renderMap.transform.TransformPoint(centerLocal);

            return true;
        }

        // Đọc shape (pixel alpha > 0) + màu từ sprite.
        // Dùng RenderTexture để copy texture sang readable Texture2D.
        private (bool[,], Color32[,]) ExtractShapeAndColorData(Sprite sprite)
        {
            if (sprite == null) return (null, null);

            var originalTexture = sprite.texture;
            var width = (int)sprite.rect.width;
            var height = (int)sprite.rect.height;
            var startX = (int)sprite.rect.x;
            var startY = (int)sprite.rect.y;

            var renderTex = RenderTexture.GetTemporary(
                originalTexture.width,
                originalTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);

            Graphics.Blit(originalTexture, renderTex);

            var previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            var readableTexture = new Texture2D(
                originalTexture.width,
                originalTexture.height,
                TextureFormat.RGBA32,
                false);

            readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;

            var shapeData = new bool[width, height];
            var colorData = new Color32[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 pixel = readableTexture.GetPixel(startX + x, startY + y);
                    shapeData[x, y] = pixel.a > 0;
                    colorData[x, y] = pixel;
                }
            }

            Destroy(readableTexture);
            RenderTexture.ReleaseTemporary(renderTex);

            return (shapeData, colorData);
        }
    }
}