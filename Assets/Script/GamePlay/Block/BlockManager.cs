using UnityEngine;

namespace Sand
{
    public class BlockManager : MonoBehaviour
    {
        [SerializeField] private Sprite[] _sprite;
        [SerializeField] public Color32 _color0;
        [SerializeField] public Color32 _color1;
        [SerializeField] public Color32 _color2;
        [SerializeField] public Color32 _color3;
        [SerializeField] public Color32 _color4;
        [SerializeField] private RenMap _renMap;

        private Sprite _currentSprite;
        public Sprite CurrentSprite => _currentSprite;
        private bool[,] _shapeData;
        public bool[,] ShapeData => _shapeData;

        private void Start()
        {
            if (_sprite == null || _sprite.Length <= 0) return;
            _currentSprite = _sprite[0];
        }

        //random
        public bool[,] GetRandomShapeData()
        {
            if (_sprite == null || _sprite.Length == 0) return null;
            var randomIndex = Random.Range(0, _sprite.Length);
            var selectedSprite = _sprite[randomIndex];
            if (selectedSprite == null || selectedSprite == null) return null;
            return ExtractShapeData(selectedSprite);
        }

        //theo type
        public bool[,] GetTypeShapeData(TypeBlock type)
        {
            if (_sprite == null || _sprite.Length == 0) return null;
            var id = GetBlockWithType(type);
            var selectedSprite = _sprite[id];
            if (selectedSprite == null || selectedSprite == null) return null;
            return ExtractShapeData(selectedSprite);
        }

        private bool[,] ExtractShapeData(Sprite sprite)
        {
            var originalTexture = sprite.texture;
            var width = (int)sprite.rect.width;
            var height = (int)sprite.rect.height;
            var startX = (int)sprite.rect.x;
            var startY = (int)sprite.rect.y;

            var renderTex = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(originalTexture, renderTex);

            var previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            var readableTexture =
                new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            var shapeData = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 originalPixel = readableTexture.GetPixel(startX + x, startY + y);
                    shapeData[x, y] = originalPixel.a > 0;
                }
            }

            DestroyImmediate(readableTexture);
            RenderTexture.ReleaseTemporary(renderTex);

            return shapeData;
        }

        public void SpawnSandWithRandomShape(Map map, SpriteRenderer spriteRenderer)
        {
            if (map == null || spriteRenderer == null || spriteRenderer.sprite == null) return;
            var shapeData = GetRandomShapeData();
            if (shapeData == null) return;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

            var spriteWidth = spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = spriteRenderer.sprite.bounds.size.y;

            if (spriteWidth <= 0 || spriteHeight <= 0) return;

            var centerX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renMap._wight);
            var centerY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renMap._hight);

            var shapeWidth = shapeData.GetLength(0);
            var shapeHeight = shapeData.GetLength(1);

            if (shapeWidth <= 0 || shapeHeight <= 0) return;
            var colorRandom = RandomColor();
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

                            if (targetX >= 0 && targetX < _renMap._wight && targetY >= 0 && targetY < _renMap._hight)
                            {
                                map.SetPixelCell(targetX, targetY, colorRandom);
                            }
                        }
                    }
                }
            }
            catch (System.IndexOutOfRangeException e)
            {
                Debug.Log("khong to duoc mau");
            }
        }

        public bool SpawnSandWithType(Map map, SpriteRenderer spriteRenderer, int id, TypeBlock type)
        {
            if (map == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return false;

            var shapeData = GetTypeShapeData(type);
            if (shapeData == null)
                return false;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

            var spriteWidth = spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = spriteRenderer.sprite.bounds.size.y;

            if (spriteWidth <= 0 || spriteHeight <= 0)
                return false;

            var centerX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renMap._wight);
            var centerY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renMap._hight);

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

            var color = GetColorWithId(id);
            for (int x = 0; x < shapeWidth; x++)
            {
                for (int y = 0; y < shapeHeight; y++)
                {
                    if (!shapeData[x, y])
                        continue;

                    var targetX = centerX - shapeWidth / 2 + x;
                    var targetY = centerY - shapeHeight / 2 + y;

                    map.SetPixelCell(targetX, targetY, color);
                }
            }

            return true;
        }

        private Color32 RandomColor()
        {
            return Random.Range(0, 4) switch
            {
                0 => _color0,
                1 => _color1,
                2 => _color2,
                3 => _color3,
                4 => _color4,
            };
        }

        private Color32 GetColorWithId(int id)
        {
            switch (id)
            {
                case 0:
                    return _color0;
                case 1:
                    return _color1;
                case 2:
                    return _color2;
                case 3:
                    return _color3;
                case 4:
                    return _color4;
                default:
                    return Color.white;
            }
        }

        private int GetBlockWithType(TypeBlock type)
        {
            return type switch
            {
                TypeBlock.Cross => 0,
                TypeBlock.Square => 1,
                TypeBlock.Line => 2,
                TypeBlock.LShape => 3,
                TypeBlock.Stair => 4,
            };
        }
    }
}