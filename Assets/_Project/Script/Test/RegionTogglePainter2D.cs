using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionPaintOnceOnHover2D : MonoBehaviour
{
    [Header("Source Sprite (ảnh đã tô màu)")] [SerializeField]
    private Sprite _sourceSprite;

    [Header("Compare / Ignore")] [SerializeField, Range(0f, 0.5f)]
    private float _blackThreshold = 0.2f;

    [Header("Main Colors (để chống lệch đậm/nhạt/noise)")]
    [Tooltip("Bước gom màu theo kênh RGB (16/32/64). Lớn hơn => gom mạnh hơn.")]
    [SerializeField, Range(1, 128)]
    private int _quantStep = 32;

    [Tooltip("Chỉ giữ những bin màu có >= số pixel này là màu chính.")] [SerializeField, Range(1, 5000)]
    private int _minMainCount = 200;

    [Header("Optional")] [Tooltip("Nếu bật thì phải giữ chuột trái mới tô (tránh lướt qua tô hết).")] [SerializeField]
    private bool _requireHoldMouse = false;

    private SpriteRenderer _sr;
    private Camera _cam;

    private Texture2D _originalTex;
    private Texture2D _gameTex;

    private int _width, _height;

    // pixel buffers
    private Color32[] _origPixels;
    private Color32[] _gamePixels;

    // main colors + paintable mask
    private HashSet<int> _mainColorKeys = new();
    private bool[] _isPaintable;
    private int _remainingPaintablePixels;
    private bool _loggedComplete;

    // visited stamps for flood fill (no allocations per frame)
    private int[] _visitMark;
    private int _visitStamp = 1;

    // hover cache
    private Vector2Int _lastPixel = new Vector2Int(int.MinValue, int.MinValue);

    private static readonly Color32 White32 = new Color32(255, 255, 255, 255);

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _cam = Camera.main;

        if (_sourceSprite == null)
            _sourceSprite = _sr.sprite;

        _originalTex = _sourceSprite.texture;
        _width = _originalTex.width;
        _height = _originalTex.height;

        // read original
        _origPixels = _originalTex.GetPixels32();

        // init game pixels = original then whiten (except black + transparent)
        _gamePixels = new Color32[_origPixels.Length];
        System.Array.Copy(_origPixels, _gamePixels, _origPixels.Length);

        for (int i = 0; i < _gamePixels.Length; i++)
        {
            var o = _origPixels[i];
            if (o.a <= 2) continue; // ~ alpha < 0.01
            if (IsBlack(o)) continue; // viền đen giữ nguyên
            _gamePixels[i] = White32; // còn lại => trắng
        }

        // create game texture
        _gameTex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _gameTex.filterMode = _originalTex.filterMode;
        _gameTex.wrapMode = _originalTex.wrapMode;
        _gameTex.SetPixels32(_gamePixels);
        _gameTex.Apply();

        // display sprite from game texture (giữ rect/pivot như sprite gốc)
        var rect = _sourceSprite.rect;
        var pivot = _sourceSprite.pivot / rect.size;
        _sr.sprite = Sprite.Create(_gameTex, rect, pivot, _sourceSprite.pixelsPerUnit);

        // build "main colors" + paintable mask + remaining count
        BuildMainPaletteAndMask();

        // init visited stamps
        _visitMark = new int[_origPixels.Length];

        Debug.Log(
            $"[RegionPaintOnceOnHover2D] MainColors={_mainColorKeys.Count}, RemainingPaintablePixels={_remainingPaintablePixels}");
    }

    private void Update()
    {
        HandleHoverPaintOnce();
    }

    private void HandleHoverPaintOnce()
    {
        if (_cam == null) return;
        if (_requireHoldMouse && !Input.GetMouseButton(0)) return;

        Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = _sr.transform.position.z;

        if (!_sr.bounds.Contains(worldPos))
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector2Int pixel = LocalToPixel(localPos);

        if (pixel.x < 0 || pixel.x >= _width || pixel.y < 0 || pixel.y >= _height)
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        // tránh spam khi đứng yên 1 điểm
        if (pixel == _lastPixel) return;
        _lastPixel = pixel;

        int startIdx = pixel.y * _width + pixel.x;

        // chỉ tô trên pixel paintable (màu chính) và hiện đang trắng
        if (!_isPaintable[startIdx]) return;
        if (!IsWhite(_gamePixels[startIdx])) return; // đã tô rồi

        int targetKey = ColorKey(_origPixels[startIdx]);
        if (!_mainColorKeys.Contains(targetKey)) return; // safety

        // floodfill theo KEY (màu đã quantize) để gom shade đậm/nhạt
        List<int> region = FloodFillByKey(startIdx, targetKey);
        if (region.Count == 0) return;

        bool changed = false;

        for (int i = 0; i < region.Count; i++)
        {
            int idx = region[i];

            // chỉ paint pixel còn trắng (đã tô rồi thì bỏ)
            if (!_isPaintable[idx]) continue;
            if (!IsWhite(_gamePixels[idx])) continue;

            _gamePixels[idx] = _origPixels[idx];
            _remainingPaintablePixels--;
            changed = true;
        }

        if (changed)
        {
            _gameTex.SetPixels32(_gamePixels);
            _gameTex.Apply();

            if (!_loggedComplete && _remainingPaintablePixels <= 0)
            {
                _loggedComplete = true;
                Debug.Log("🎉 COMPLETE: Tô xong toàn bộ (theo màu chính)!");
            }
        }
    }

    // ========= MAIN COLORS + MASK =========

    private void BuildMainPaletteAndMask()
    {
        // histogram colorKey
        Dictionary<int, int> hist = new Dictionary<int, int>(2048);

        for (int i = 0; i < _origPixels.Length; i++)
        {
            var c = _origPixels[i];
            if (c.a <= 2) continue;
            if (IsBlack(c)) continue;

            int key = ColorKey(c);
            if (hist.TryGetValue(key, out int cnt)) hist[key] = cnt + 1;
            else hist[key] = 1;
        }

        _mainColorKeys.Clear();
        foreach (var kv in hist)
        {
            if (kv.Value >= _minMainCount)
                _mainColorKeys.Add(kv.Key);
        }

        _isPaintable = new bool[_origPixels.Length];
        _remainingPaintablePixels = 0;
        _loggedComplete = false;

        for (int i = 0; i < _origPixels.Length; i++)
        {
            var c = _origPixels[i];
            if (c.a <= 2) continue;
            if (IsBlack(c)) continue;

            int key = ColorKey(c);
            if (!_mainColorKeys.Contains(key)) continue; // bỏ shade lẻ/noise

            _isPaintable[i] = true;

            // ban đầu paintable đều là trắng => remaining là số paintable pixel đang trắng
            if (IsWhite(_gamePixels[i]))
                _remainingPaintablePixels++;
        }
    }

    // ========= FLOOD FILL =========

    private List<int> FloodFillByKey(int startIdx, int targetKey)
    {
        // stamp overflow protection
        _visitStamp++;
        if (_visitStamp == int.MaxValue)
        {
            System.Array.Clear(_visitMark, 0, _visitMark.Length);
            _visitStamp = 1;
        }

        List<int> region = new List<int>(1024);
        Queue<int> q = new Queue<int>(1024);

        _visitMark[startIdx] = _visitStamp;
        q.Enqueue(startIdx);

        while (q.Count > 0)
        {
            int idx = q.Dequeue();
            region.Add(idx);

            int x = idx % _width;
            int y = idx / _width;

            TryEnqueue(x + 1, y, targetKey, q);
            TryEnqueue(x - 1, y, targetKey, q);
            TryEnqueue(x, y + 1, targetKey, q);
            TryEnqueue(x, y - 1, targetKey, q);
        }

        return region;
    }

    private void TryEnqueue(int x, int y, int targetKey, Queue<int> q)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;

        int idx = y * _width + x;
        if (_visitMark[idx] == _visitStamp) return;

        // chỉ floodfill trên "paintable" (màu chính)
        if (!_isPaintable[idx]) return;

        // match theo quantized key
        int key = ColorKey(_origPixels[idx]);
        if (key != targetKey) return;

        _visitMark[idx] = _visitStamp;
        q.Enqueue(idx);
    }

    // ========= MATH / COLOR =========

    private int ColorKey(Color32 c)
    {
        int step = Mathf.Clamp(_quantStep, 1, 255);
        int r = (c.r / step) * step;
        int g = (c.g / step) * step;
        int b = (c.b / step) * step;
        return (r << 16) | (g << 8) | b;
    }

    private bool IsWhite(Color32 c)
    {
        // đủ dùng vì ta set trắng bằng (255,255,255)
        return c.a > 250 && c.r > 250 && c.g > 250 && c.b > 250;
    }

    private bool IsBlack(Color32 c)
    {
        if (c.a <= 2) return false;
        int thr = Mathf.RoundToInt(_blackThreshold * 255f);
        return c.r < thr && c.g < thr && c.b < thr;
    }

    // chuyển localPos (world unit) sang pixel trong texture
    private Vector2Int LocalToPixel(Vector3 localPos)
    {
        Sprite sprite = _sr.sprite;
        Rect rect = sprite.textureRect;
        float ppu = sprite.pixelsPerUnit;

        float px = rect.x + sprite.pivot.x + localPos.x * ppu;
        float py = rect.y + sprite.pivot.y + localPos.y * ppu;

        return new Vector2Int(Mathf.FloorToInt(px), Mathf.FloorToInt(py));
    }
}
/*using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionPaintOnceOnHover2D : MonoBehaviour
{
    [Header("Source Sprite (ảnh đã tô màu)")]
    [SerializeField] private Sprite _sourceSprite;

    [Header("Ignore")]
    [SerializeField, Range(0f, 0.5f)] private float _blackThreshold = 0.2f;

    [Header("Main Colors (chống lệch shade/noise)")]
    [Tooltip("Bước gom màu theo kênh RGB (16/32/64). Lớn hơn => gom mạnh hơn.")]
    [SerializeField, Range(1, 128)] private int _quantStep = 32;

    [Tooltip("Chỉ bỏ qua các bin màu quá nhỏ (noise).")]
    [SerializeField, Range(1, 5000)] private int _minBinCount = 10;

    [Tooltip("Lấy màu chính cho tới khi bao phủ >= % pixel hợp lệ (khuyến nghị 0.99 ~ 0.999).")]
    [SerializeField, Range(0.9f, 1f)] private float _coverage = 0.995f;

    [Header("Optional")]
    [SerializeField] private bool _requireHoldMouse = false;

    private SpriteRenderer _sr;
    private Camera _cam;

    private Texture2D _originalTex;
    private Texture2D _gameTex;

    private int _width, _height;

    private Color32[] _origPixels;
    private Color32[] _gamePixels;

    private HashSet<int> _mainColorKeys = new();
    private bool[] _isPaintable;
    private int _remainingPaintablePixels;
    private bool _loggedComplete;

    private int[] _visitMark;
    private int _visitStamp = 1;

    private Vector2Int _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
    private static readonly Color32 White32 = new Color32(255, 255, 255, 255);

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _cam = Camera.main;

        if (_sourceSprite == null)
            _sourceSprite = _sr.sprite;

        _originalTex = _sourceSprite.texture;
        _width = _originalTex.width;
        _height = _originalTex.height;

        _origPixels = _originalTex.GetPixels32();

        // init game pixels = white (except black + transparent)
        _gamePixels = new Color32[_origPixels.Length];
        for (int i = 0; i < _origPixels.Length; i++)
        {
            var o = _origPixels[i];
            if (o.a <= 2) { _gamePixels[i] = o; continue; }
            if (IsBlack(o)) { _gamePixels[i] = o; continue; }
            _gamePixels[i] = White32;
        }

        _gameTex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _gameTex.filterMode = _originalTex.filterMode;
        _gameTex.wrapMode = _originalTex.wrapMode;
        _gameTex.SetPixels32(_gamePixels);
        _gameTex.Apply();

        var rect = _sourceSprite.rect;
        var pivot = _sourceSprite.pivot / rect.size;
        _sr.sprite = Sprite.Create(_gameTex, rect, pivot, _sourceSprite.pixelsPerUnit);

        BuildMainPaletteAndMask_ByCoverage();

        _visitMark = new int[_origPixels.Length];

        Debug.Log($"[RegionPaintOnceOnHover2D] MainKeys={_mainColorKeys.Count}, RemainingPaintable={_remainingPaintablePixels}");
    }

    private void Update()
    {
        HandleHoverPaintOnce();
    }

    private void HandleHoverPaintOnce()
    {
        if (_cam == null) return;
        if (_requireHoldMouse && !Input.GetMouseButton(0)) return;

        Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = _sr.transform.position.z;

        if (!_sr.bounds.Contains(worldPos))
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector2Int pixel = LocalToPixel(localPos);

        if (pixel.x < 0 || pixel.x >= _width || pixel.y < 0 || pixel.y >= _height)
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        if (pixel == _lastPixel) return;
        _lastPixel = pixel;

        int startIdx = pixel.y * _width + pixel.x;

        if (!_isPaintable[startIdx]) return;
        if (!IsWhite(_gamePixels[startIdx])) return;

        int targetKey = ColorKey(_origPixels[startIdx]);
        if (!_mainColorKeys.Contains(targetKey)) return;

        List<int> region = FloodFillByKey(startIdx, targetKey);
        if (region.Count == 0) return;

        bool changed = false;
        for (int i = 0; i < region.Count; i++)
        {
            int idx = region[i];
            if (!_isPaintable[idx]) continue;
            if (!IsWhite(_gamePixels[idx])) continue;

            _gamePixels[idx] = _origPixels[idx];
            _remainingPaintablePixels--;
            changed = true;
        }

        if (!changed) return;

        _gameTex.SetPixels32(_gamePixels);
        _gameTex.Apply();

        if (!_loggedComplete && _remainingPaintablePixels <= 0)
        {
            _loggedComplete = true;
            Debug.Log("🎉 COMPLETE: Tô xong toàn bộ (theo coverage màu chính)!");
        }
    }

    // ===== Build main palette by coverage =====
    private void BuildMainPaletteAndMask_ByCoverage()
    {
        Dictionary<int, int> hist = new Dictionary<int, int>(2048);
        int eligibleTotal = 0;

        for (int i = 0; i < _origPixels.Length; i++)
        {
            var c = _origPixels[i];
            if (c.a <= 2) continue;
            if (IsBlack(c)) continue;

            eligibleTotal++;
            int key = ColorKey(c);
            hist.TryGetValue(key, out int cnt);
            hist[key] = cnt + 1;
        }

        // sort bins by count desc
        List<KeyValuePair<int, int>> bins = new List<KeyValuePair<int, int>>(hist.Count);
        foreach (var kv in hist)
        {
            if (kv.Value >= _minBinCount) // bỏ bin cực nhỏ (noise)
                bins.Add(kv);
        }
        bins.Sort((a, b) => b.Value.CompareTo(a.Value));

        _mainColorKeys.Clear();
        int covered = 0;
        int targetCovered = Mathf.CeilToInt(eligibleTotal * _coverage);

        for (int i = 0; i < bins.Count; i++)
        {
            _mainColorKeys.Add(bins[i].Key);
            covered += bins[i].Value;
            if (covered >= targetCovered)
                break;
        }

        _isPaintable = new bool[_origPixels.Length];
        _remainingPaintablePixels = 0;
        _loggedComplete = false;

        int excludedEligible = 0;

        for (int i = 0; i < _origPixels.Length; i++)
        {
            var c = _origPixels[i];
            if (c.a <= 2) continue;
            if (IsBlack(c)) continue;

            int key = ColorKey(c);
            if (!_mainColorKeys.Contains(key))
            {
                excludedEligible++;
                continue;
            }

            _isPaintable[i] = true;
            if (IsWhite(_gamePixels[i]))
                _remainingPaintablePixels++;
        }

        Debug.Log($"Eligible={eligibleTotal}, Covered≈{covered}, ExcludedEligible={excludedEligible}, MainKeys={_mainColorKeys.Count}");
    }

    // ===== Flood fill by quantized key =====
    private List<int> FloodFillByKey(int startIdx, int targetKey)
    {
        _visitStamp++;
        if (_visitStamp == int.MaxValue)
        {
            System.Array.Clear(_visitMark, 0, _visitMark.Length);
            _visitStamp = 1;
        }

        List<int> region = new List<int>(1024);
        Queue<int> q = new Queue<int>(1024);

        _visitMark[startIdx] = _visitStamp;
        q.Enqueue(startIdx);

        while (q.Count > 0)
        {
            int idx = q.Dequeue();
            region.Add(idx);

            int x = idx % _width;
            int y = idx / _width;

            TryEnqueue(x + 1, y, targetKey, q);
            TryEnqueue(x - 1, y, targetKey, q);
            TryEnqueue(x, y + 1, targetKey, q);
            TryEnqueue(x, y - 1, targetKey, q);
        }

        return region;
    }

    private void TryEnqueue(int x, int y, int targetKey, Queue<int> q)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;
        int idx = y * _width + x;

        if (_visitMark[idx] == _visitStamp) return;
        if (!_isPaintable[idx]) return;

        int key = ColorKey(_origPixels[idx]);
        if (key != targetKey) return;

        _visitMark[idx] = _visitStamp;
        q.Enqueue(idx);
    }

    // ===== Utils =====
    private int ColorKey(Color32 c)
    {
        int step = Mathf.Clamp(_quantStep, 1, 255);
        int r = (c.r / step) * step;
        int g = (c.g / step) * step;
        int b = (c.b / step) * step;
        return (r << 16) | (g << 8) | b;
    }

    private bool IsWhite(Color32 c)
    {
        return c.a > 250 && c.r > 250 && c.g > 250 && c.b > 250;
    }

    private bool IsBlack(Color32 c)
    {
        if (c.a <= 2) return false;
        int thr = Mathf.RoundToInt(_blackThreshold * 255f);
        return c.r < thr && c.g < thr && c.b < thr;
    }

    private Vector2Int LocalToPixel(Vector3 localPos)
    {
        Sprite sprite = _sr.sprite;
        Rect rect = sprite.textureRect;
        float ppu = sprite.pixelsPerUnit;

        float px = rect.x + sprite.pivot.x + localPos.x * ppu;
        float py = rect.y + sprite.pivot.y + localPos.y * ppu;

        return new Vector2Int(Mathf.FloorToInt(px), Mathf.FloorToInt(py));
    }
}*/

/*
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionPaintOnceOnHover2D : MonoBehaviour
{
    [Header("Source Sprite (ảnh đã tô màu)")]
    [SerializeField] private Sprite _sourceSprite;

    [Header("Compare / Ignore")]
    [SerializeField, Range(0f, 0.5f)] private float _blackThreshold = 0.2f;

    [Header("Main Colors (để chống lệch đậm/nhạt/noise)")]
    [Tooltip("Bước gom màu theo kênh RGB (16/32/64). Lớn hơn => gom mạnh hơn.")]
    [SerializeField, Range(1, 128)] private int _quantStep = 32;

    [Tooltip("Chỉ giữ những bin màu có >= số pixel này là màu chính.")]
    [SerializeField, Range(1, 5000)] private int _minMainCount = 200;

    [Header("Optional")]
    [Tooltip("Nếu bật thì phải giữ chuột trái mới tô (tránh lướt qua tô hết).")]
    [SerializeField] private bool _requireHoldMouse = false;

    private SpriteRenderer _sr;
    private Camera _cam;

    private Texture2D _originalTex;
    private Texture2D _gameTex;

    private int _width, _height;

    // pixel buffers
    private Color32[] _origPixels;
    private Color32[] _gamePixels;

    // main colors + paintable mask
    private HashSet<int> _mainColorKeys = new();
    private bool[] _isPaintable;
    private int _remainingPaintablePixels;
    private bool _loggedComplete;

    // visited stamps for flood fill (no allocations per frame)
    private int[] _visitMark;
    private int _visitStamp = 1;

    // hover cache
    private Vector2Int _lastPixel = new Vector2Int(int.MinValue, int.MinValue);

    private static readonly Color32 White32 = new Color32(255, 255, 255, 255);

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _cam = Camera.main;

        if (_sourceSprite == null)
            _sourceSprite = _sr.sprite;

        _originalTex = _sourceSprite.texture;
        _width = _originalTex.width;
        _height = _originalTex.height;

        // read original
        _origPixels = _originalTex.GetPixels32();

        // init game pixels = original then whiten (except black + transparent)
        _gamePixels = new Color32[_origPixels.Length];
        System.Array.Copy(_origPixels, _gamePixels, _origPixels.Length);

        for (int i = 0; i < _gamePixels.Length; i++)
        {
            var o = _origPixels[i];
            if (o.a <= 2) continue;     // ~ alpha < 0.01
            if (IsBlack(o)) continue;   // viền đen giữ nguyên
            _gamePixels[i] = White32;   // còn lại => trắng
        }

        // create game texture
        _gameTex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _gameTex.filterMode = _originalTex.filterMode;
        _gameTex.wrapMode = _originalTex.wrapMode;
        _gameTex.SetPixels32(_gamePixels);
        _gameTex.Apply();

        // display sprite from game texture (giữ rect/pivot như sprite gốc)
        var rect = _sourceSprite.rect;
        var pivot = _sourceSprite.pivot / rect.size;
        _sr.sprite = Sprite.Create(_gameTex, rect, pivot, _sourceSprite.pixelsPerUnit);

        // build "main colors" + paintable mask + remaining count
        BuildMainPaletteAndMask();

        // init visited stamps
        _visitMark = new int[_origPixels.Length];

        Debug.Log($"[RegionPaintOnceOnHover2D] MainColors={_mainColorKeys.Count}, RemainingPaintablePixels={_remainingPaintablePixels}");
    }

    private void Update()
    {
        HandleHoverPaintOnce();
    }

    private void HandleHoverPaintOnce()
    {
        if (_cam == null) return;
        if (_requireHoldMouse && !Input.GetMouseButton(0)) return;

        Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = _sr.transform.position.z;

        if (!_sr.bounds.Contains(worldPos))
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector2Int pixel = LocalToPixel(localPos);

        if (pixel.x < 0 || pixel.x >= _width || pixel.y < 0 || pixel.y >= _height)
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        // tránh spam khi đứng yên 1 điểm
        if (pixel == _lastPixel) return;
        _lastPixel = pixel;

        int startIdx = pixel.y * _width + pixel.x;

        // chỉ tô trên pixel paintable (màu chính) và hiện đang trắng
        if (!_isPaintable[startIdx]) return;
        if (!IsWhite(_gamePixels[startIdx])) return; // đã tô rồi

        int targetKey = ColorKey(_origPixels[startIdx]);
        if (!_mainColorKeys.Contains(targetKey)) return; // safety

        // floodfill theo KEY (màu đã quantize) để gom shade đậm/nhạt
        List<int> region = FloodFillByKey(startIdx, targetKey);
        if (region.Count == 0) return;

        bool changed = false;

        for (int i = 0; i < region.Count; i++)
        {
            int idx = region[i];

            // chỉ paint pixel còn trắng (đã tô rồi thì bỏ)
            if (!_isPaintable[idx]) continue;
            if (!IsWhite(_gamePixels[idx])) continue;

            _gamePixels[idx] = _origPixels[idx];
            _remainingPaintablePixels--;
            changed = true;
        }

        if (changed)
        {
            _gameTex.SetPixels32(_gamePixels);
            _gameTex.Apply();

            if (!_loggedComplete && _remainingPaintablePixels <= 0)
            {
                _loggedComplete = true;
                Debug.Log("🎉 COMPLETE: Tô xong toàn bộ (theo màu chính)!");
            }
        }
    }

    // ========= MAIN COLORS + MASK =========

    private void BuildMainPaletteAndMask()
    {
        // histogram colorKey
        Dictionary<int, int> hist = new Dictionary<int, int>(2048);

        for (int i = 0; i < _origPixels.Length; i++)
        {
            var c = _origPixels[i];
            if (c.a <= 2) continue;
            if (IsBlack(c)) continue;

            int key = ColorKey(c);
            if (hist.TryGetValue(key, out int cnt)) hist[key] = cnt + 1;
            else hist[key] = 1;
        }

        _mainColorKeys.Clear();
        foreach (var kv in hist)
        {
            if (kv.Value >= _minMainCount)
                _mainColorKeys.Add(kv.Key);
        }

        _isPaintable = new bool[_origPixels.Length];
        _remainingPaintablePixels = 0;
        _loggedComplete = false;

        for (int i = 0; i < _origPixels.Length; i++)
        {
            var c = _origPixels[i];
            if (c.a <= 2) continue;
            if (IsBlack(c)) continue;

            int key = ColorKey(c);
            if (!_mainColorKeys.Contains(key)) continue; // bỏ shade lẻ/noise

            _isPaintable[i] = true;

            // ban đầu paintable đều là trắng => remaining là số paintable pixel đang trắng
            if (IsWhite(_gamePixels[i]))
                _remainingPaintablePixels++;
        }
    }

    // ========= FLOOD FILL =========

    private List<int> FloodFillByKey(int startIdx, int targetKey)
    {
        // stamp overflow protection
        _visitStamp++;
        if (_visitStamp == int.MaxValue)
        {
            System.Array.Clear(_visitMark, 0, _visitMark.Length);
            _visitStamp = 1;
        }

        List<int> region = new List<int>(1024);
        Queue<int> q = new Queue<int>(1024);

        _visitMark[startIdx] = _visitStamp;
        q.Enqueue(startIdx);

        while (q.Count > 0)
        {
            int idx = q.Dequeue();
            region.Add(idx);

            int x = idx % _width;
            int y = idx / _width;

            TryEnqueue(x + 1, y, targetKey, q);
            TryEnqueue(x - 1, y, targetKey, q);
            TryEnqueue(x, y + 1, targetKey, q);
            TryEnqueue(x, y - 1, targetKey, q);
        }

        return region;
    }

    private void TryEnqueue(int x, int y, int targetKey, Queue<int> q)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;

        int idx = y * _width + x;
        if (_visitMark[idx] == _visitStamp) return;

        // chỉ floodfill trên "paintable" (màu chính)
        if (!_isPaintable[idx]) return;

        // match theo quantized key
        int key = ColorKey(_origPixels[idx]);
        if (key != targetKey) return;

        _visitMark[idx] = _visitStamp;
        q.Enqueue(idx);
    }

    // ========= MATH / COLOR =========

    private int ColorKey(Color32 c)
    {
        int step = Mathf.Clamp(_quantStep, 1, 255);
        int r = (c.r / step) * step;
        int g = (c.g / step) * step;
        int b = (c.b / step) * step;
        return (r << 16) | (g << 8) | b;
    }

    private bool IsWhite(Color32 c)
    {
        // đủ dùng vì ta set trắng bằng (255,255,255)
        return c.a > 250 && c.r > 250 && c.g > 250 && c.b > 250;
    }

    private bool IsBlack(Color32 c)
    {
        if (c.a <= 2) return false;
        int thr = Mathf.RoundToInt(_blackThreshold * 255f);
        return c.r < thr && c.g < thr && c.b < thr;
    }

    // chuyển localPos (world unit) sang pixel trong texture
    private Vector2Int LocalToPixel(Vector3 localPos)
    {
        Sprite sprite = _sr.sprite;
        Rect rect = sprite.textureRect;
        float ppu = sprite.pixelsPerUnit;

        float px = rect.x + sprite.pivot.x + localPos.x * ppu;
        float py = rect.y + sprite.pivot.y + localPos.y * ppu;

        return new Vector2Int(Mathf.FloorToInt(px), Mathf.FloorToInt(py));
    }
}

/*
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionPaintOnceOnHover2D : MonoBehaviour
{
    [Header("Source Sprite (ảnh đã tô màu)")]
    [SerializeField] private Sprite _sourceSprite;

    [Header("Settings")]
    [SerializeField, Range(0f, 0.2f)] private float _colorTolerance = 0.02f;
    [SerializeField, Range(0f, 0.5f)] private float _blackThreshold = 0.2f;

    private SpriteRenderer _sr;
    private Camera _cam;

    private Texture2D _originalTex;
    private Texture2D _gameTex;

    private int _width;
    private int _height;
    private bool _loggedComplete = false;


    // cache để khỏi floodfill liên tục khi đang đứng yên trên 1 vùng
    private Vector2Int _lastPixel = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _cam = Camera.main;

        if (_sourceSprite == null)
            _sourceSprite = _sr.sprite;

        _originalTex = _sourceSprite.texture;
        _width = _originalTex.width;
        _height = _originalTex.height;

        // clone texture gốc sang texture vẽ
        _gameTex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _gameTex.filterMode = _originalTex.filterMode;
        _gameTex.wrapMode = _originalTex.wrapMode;
        _gameTex.SetPixels(_originalTex.GetPixels());
        _gameTex.Apply();

        // init: trắng hết (trừ đen)
        var pixels = _gameTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0.01f && !IsBlack(pixels[i]))
                pixels[i] = Color.white;
        }
        _gameTex.SetPixels(pixels);
        _gameTex.Apply();

        // tạo sprite mới từ gameTex để hiển thị
        var rect = _sourceSprite.rect;
        var pivot = _sourceSprite.pivot / rect.size;
        var sprite = Sprite.Create(_gameTex, rect, pivot, _sourceSprite.pixelsPerUnit);
        _sr.sprite = sprite;
    }

    private void Update()
    {
        HandleHoverPaintOnce();
    }

    private void HandleHoverPaintOnce()
    {
        if (_cam == null) return;

        Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = _sr.transform.position.z;

        if (!_sr.bounds.Contains(worldPos))
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector2Int pixel = LocalToPixel(localPos);

        if (pixel.x < 0 || pixel.x >= _width || pixel.y < 0 || pixel.y >= _height)
        {
            _lastPixel = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        // đứng yên đúng 1 pixel thì khỏi làm lại mỗi frame
        if (pixel == _lastPixel) return;
        _lastPixel = pixel;

        Color src = _originalTex.GetPixel(pixel.x, pixel.y);
        if (src.a < 0.01f) return;       // trong suốt
        if (IsBlack(src)) return;        // viền đen thì bỏ qua

        // ===== 핵: đã tô rồi thì không tô lại =====
        Color current = _gameTex.GetPixel(pixel.x, pixel.y);
        if (!IsWhite(current)) return;   // pixel đã khác trắng -> coi như vùng đã tô
        // ========================================

        // floodfill theo màu gốc
        List<Vector2Int> region = FloodFill(pixel.x, pixel.y, src);
        if (region.Count == 0) return;

        // tô vùng đó sang màu gốc (chỉ tô những pixel hiện đang trắng để an toàn)
        foreach (var p in region)
        {
            Color cur = _gameTex.GetPixel(p.x, p.y);
            if (!IsWhite(cur)) continue;

            Color orig = _originalTex.GetPixel(p.x, p.y);
            if (orig.a < 0.01f || IsBlack(orig)) continue;

            _gameTex.SetPixel(p.x, p.y, orig);
        }

        _gameTex.Apply();
        CheckComplete();
    }

    private List<Vector2Int> FloodFill(int startX, int startY, Color targetColor)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        bool[,] visited = new bool[_width, _height];

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(startX, startY));

        while (q.Count > 0)
        {
            Vector2Int p = q.Dequeue();

            if (p.x < 0 || p.x >= _width || p.y < 0 || p.y >= _height)
                continue;

            if (visited[p.x, p.y])
                continue;

            visited[p.x, p.y] = true;

            Color c = _originalTex.GetPixel(p.x, p.y);
            if (!SameColor(c, targetColor))
                continue;

            region.Add(p);

            q.Enqueue(new Vector2Int(p.x + 1, p.y));
            q.Enqueue(new Vector2Int(p.x - 1, p.y));
            q.Enqueue(new Vector2Int(p.x, p.y + 1));
            q.Enqueue(new Vector2Int(p.x, p.y - 1));
        }

        return region;
    }

    private Vector2Int LocalToPixel(Vector3 localPos)
    {
        Sprite sprite = _sr.sprite;
        Rect rect = sprite.textureRect;
        float ppu = sprite.pixelsPerUnit;

        float px = rect.x + sprite.pivot.x + localPos.x * ppu;
        float py = rect.y + sprite.pivot.y + localPos.y * ppu;

        return new Vector2Int(Mathf.FloorToInt(px), Mathf.FloorToInt(py));
    }

    private bool SameColor(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return dr * dr + dg * dg + db * db <= _colorTolerance * _colorTolerance;
    }

    private bool IsWhite(Color c)
    {
        return c.a > 0.9f &&
               Mathf.Abs(c.r - 1f) < 0.02f &&
               Mathf.Abs(c.g - 1f) < 0.02f &&
               Mathf.Abs(c.b - 1f) < 0.02f;
    }

    private bool IsBlack(Color c)
    {
        return c.a > 0.9f &&
               c.r < _blackThreshold &&
               c.g < _blackThreshold &&
               c.b < _blackThreshold;
    }
    private void CheckComplete()
    {
        if (_loggedComplete) return;

        Color[] pixels = _gameTex.GetPixels();
        Color[] original = _originalTex.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            // bỏ qua pixel trong suốt hoặc viền đen
            if (original[i].a < 0.01f) continue;
            if (IsBlack(original[i])) continue;

            // còn pixel trắng => chưa hoàn thành
            if (IsWhite(pixels[i]))
                return;
        }

        _loggedComplete = true;
        Debug.Log("🎉 COMPLETE: Tô xong toàn bộ ảnh!");
    }

}
#1#
*/