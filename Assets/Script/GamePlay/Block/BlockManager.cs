using UnityEngine;
using Zenject;

namespace Sand
{
    public class BlockManager : MonoBehaviour
    {
        [SerializeField] private Sprite[] _sprite;
        [SerializeField] private RenderMap renderMap;
        
        CheckLevelScore _checkLevelScore;
        private Sprite _currentSprite;
        private bool[,] _shapeData;

        [Inject]
        void Construct(CheckLevelScore checkLevelScore)
        {
            _checkLevelScore = checkLevelScore;
        }
        private void Start()
        {
            if (_sprite == null || _sprite.Length <= 0) return;
            _currentSprite = _sprite[0];
        }
        
        private int GetMaxSpritesForLevel()
        {
            if (_checkLevelScore == null) return _sprite.Length; 
            int level = _checkLevelScore.CurrentLevel;
            if (level == 0) return 5;  
            if (level == 1) return 9;  
            if (level == 2) return 13;    
            return _sprite.Length;
        }


        public bool[,] GetTypeShapeData(BlockType blockType)
        {
            if (_sprite == null || _sprite.Length == 0) return null;
            var id = GetBlockWithType(blockType);
            var selectedSprite = _sprite[id];
            if (selectedSprite == null) return null;
            var (shapeData, _) = ExtractShapeAndColorData(selectedSprite);
            return shapeData;
        }
        
        public void SpawnSandWithRandomShape(Map map, SpriteRenderer spriteRenderer)
        {
            if (map == null || spriteRenderer == null || spriteRenderer.sprite == null) return;

            var maxSpritesForLevel = GetMaxSpritesForLevel();
            var randomIndex = Random.Range(0, maxSpritesForLevel);
            var selectedSprite = _sprite[randomIndex];
            var (shapeData, colorData) = ExtractShapeAndColorData(selectedSprite);
            if (shapeData == null) return;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

            var spriteWidth = spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = spriteRenderer.sprite.bounds.size.y;

            if (spriteWidth <= 0 || spriteHeight <= 0) return;

            var centerX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * renderMap._wight);
            var centerY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * renderMap._hight);

            var shapeWidth = shapeData.GetLength(0);
            var shapeHeight = shapeData.GetLength(1);

            if (shapeWidth <= 0 || shapeHeight <= 0) return;
            try
            {
                for (int x = 0; x < shapeWidth; x++)
                {
                    for (int y = 0; y < shapeHeight; y++)
                    {
                        if (x >= 0 && x < shapeWidth && y >= 0 && y < shapeHeight && shapeData[x, y])
                        {
                            var targetX = centerX - shapeWidth / 2 + x;
                            var targetY = centerY - shapeHeight / 2 + y;

                            if (targetX >= 0 && targetX < renderMap._wight && targetY >= 0 && targetY < renderMap._hight)
                            {
                                Color32 pixelColor = colorData != null ? colorData[x, y] : Color.white;
                                map.SetPixelCell(targetX, targetY, pixelColor);
                            }
                        }
                    }
                }
            }
            catch (System.IndexOutOfRangeException e)
            {
                Debug.Log("loi to mau block spawn");
            }
        }

        public bool SpawnSandWithType(Map map, SpriteRenderer spriteRenderer, int id, BlockType blockType)
        {
            if (map == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return false;

            var shapeData = GetTypeShapeData(blockType);
            if (shapeData == null)
                return false;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

            var spriteWidth = spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = spriteRenderer.sprite.bounds.size.y;

            if (spriteWidth <= 0 || spriteHeight <= 0)
                return false;

            var centerX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * renderMap._wight);
            var centerY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * renderMap._hight);

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

                    var cell = map.GetCell(targetX, targetY);
                    if (cell.hasValue == 1 || cell.isBorder == 1)
                    {
                        return false;
                    }
                }
            }

            var selectedSprite = _sprite[GetBlockWithType(blockType)];
            var (_, colorData) = ExtractShapeAndColorData(selectedSprite);

            for (int x = 0; x < shapeWidth; x++)
            {
                for (int y = 0; y < shapeHeight; y++)
                {
                    if (!shapeData[x, y])
                        continue;

                    var targetX = centerX - shapeWidth / 2 + x;
                    var targetY = centerY - shapeHeight / 2 + y;

                    Color32 pixelColor = colorData != null ? colorData[x, y] : Color.white;
                    map.SetPixelCell(targetX, targetY, pixelColor);
                }
            }

            return true;
        }
        private int GetBlockWithType(BlockType blockType)
        {
            return blockType switch
            {
                BlockType.Cross => 0,
                BlockType.Square => 1,
                BlockType.Line => 2,
                BlockType.LShape => 3,
                BlockType.Stair => 4,
            };
        }
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
                originalTexture.height, 0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);
            Graphics.Blit(originalTexture, renderTex);

            var previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            var readableTexture =
                new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
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