using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionPainterFillByButton : MonoBehaviour
{
    [Header("Data")] [SerializeField] private ColoringRegionsAsset _asset; // Asset chứa dữ liệu vùng tô
    [Header("Render")] [SerializeField] private SpriteRenderer _sr; // Để render ảnh đang tô

    [Header("Matching sampledColor tolerance")] [Range(0f, 0.25f)] [SerializeField]
    private float _sampledMatchTolerance = 0.04f; // Độ chênh lệch màu khi tìm vùng

    [Header("Sand Noise")] [SerializeField, Range(0f, 0.35f)]
    private float _grainStrength = 0.12f; // lệch sáng/tối

    [SerializeField, Range(0f, 0.25f)] private float _skipChance = 0.06f; // % pixel không tô (hạt rỗ)
    [SerializeField] private int _noiseSeed = 12345; // Seed để tạo noise ổn định
    [Header("Color Boost")]
    [SerializeField, Range(0.5f, 2.5f)] private float _colorBoost = 1.25f;
    [SerializeField, Range(0.5f, 2f)] private float _saturationBoost = 1.1f;

    [Header("White Speckles")]
    [SerializeField, Range(0f, 1f)] private float _speckleDensity = 0.08f; // mật độ ô có chấm //0.5F
    [SerializeField, Range(0f, 2f)] private float _speckleIntensity = 0.4f;  // độ mạnh highlight //1
    [SerializeField, Range(1, 10)] private int _speckleSize = 2;              // kích cỡ dot (pixel) // 6
    [SerializeField, Range(0f, 1f)] private float _speckleSoftness = 0.6f;   // độ mềm biên dot

    private Texture2D _runtimeTex; // Ảnh runtime được tô (thay đổi liên tục)
    private Color32[] _pixels; // Mảng pixel của ảnh runtime

    private readonly Dictionary<int, List<ColoringRegionsAsset.RegionEntry>> _map = new(); // Map từ mã màu → danh sách vùng
    private int _w, _h; // Chiều rộng & chiều cao ảnh
    private bool _hasLoggedComplete; // Đánh dấu đã log "hoàn thành tô hết"

    private void Reset()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
    
        _hasLoggedComplete = false;
    }

    private void BuildRuntimeTextureClone()
    {
        if (_asset == null || _asset.outlineTexture == null)
        {
            Debug.LogError("[Painter] Missing outlineTexture");
            return;
        }

        var src = _asset.outlineTexture;
        _w = src.width;
        _h = src.height;

        _pixels = src.GetPixels32(); // clone OUTLINE (trắng / chưa tô)
        _runtimeTex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
        _runtimeTex.SetPixels32(_pixels);
        _runtimeTex.Apply(false, false);

        float ppu = (_sr.sprite != null) ? _sr.sprite.pixelsPerUnit : 100f;
        _sr.sprite = Sprite.Create(
            _runtimeTex,
            new Rect(0, 0, _w, _h),
            new Vector2(0.5f, 0.5f),
            ppu
        );
    }


    private void BuildMapBySampledColor()
    {
        _map.Clear();
        if (_asset == null || _asset.regions == null) return;

        foreach (var r in _asset.regions)
        {
            if (r == null || r.maskBits == null || r.maskBits.Length == 0) continue;

            int key = ColorKey(r.sampledColor);
            if (!_map.TryGetValue(key, out var list))
            {
                list = new List<ColoringRegionsAsset.RegionEntry>();
                _map.Add(key, list);
            }

            list.Add(r);
        }
    }

    public void SetAsset(ColoringRegionsAsset newAsset)
    {
        _asset = newAsset;
        _hasLoggedComplete = false;
        _map.Clear();
    
        // ✅ Tạo clone texture khi chọn ảnh
        BuildRuntimeTextureClone(); 
        BuildMapBySampledColor();
    
        // ✅ Reset trạng thái filled của các vùng
        if (_asset != null && _asset.regions != null)
            foreach (var r in _asset.regions)
                if (r != null) r.filled = false;
    }

    public void FillAllRegionsOfSampledColor(Color32 sampledColor)
    {
        if (_runtimeTex == null || _pixels == null) return;
        if (_asset == null || _asset.regions == null) return;

        int key = ColorKey(sampledColor);
        List<ColoringRegionsAsset.RegionEntry> regions = null;

        if (_map.TryGetValue(key, out var exact))
            regions = exact;
        else
        {
            regions = new List<ColoringRegionsAsset.RegionEntry>();
            foreach (var r in _asset.regions)
            {
                if (r == null || r.maskBits == null || r.maskBits.Length == 0) continue;
                if (CloseColor(r.sampledColor, sampledColor, _sampledMatchTolerance)) regions.Add(r);
            }
        }

        if (regions == null || regions.Count == 0) return;

        for (int i = 0; i < regions.Count; i++)
        {
            ApplyRegion(regions[i], sampledColor);
        }

        _runtimeTex.SetPixels32(_pixels);
        _runtimeTex.Apply(false, false);

        CheckAllFilledAndLogOnce();
    }
    
    private void ApplyRegion(ColoringRegionsAsset.RegionEntry RegionEntry, Color32 baseColor)
    {
        if (RegionEntry == null || RegionEntry.maskBits == null || RegionEntry.maskBits.Length == 0) return;

        var b = RegionEntry.bounds;
        int bw = b.width;
        int bh = b.height;
        if (bw <= 0 || bh <= 0) return;

        // seed theo region + màu để noise ổn định theo từng vùng
        int seed = _noiseSeed ^ (RegionEntry.id * 73856093) ^ (baseColor.r * 19349663) ^ (baseColor.g * 83492791) ^
                   (baseColor.b * 2654435761.GetHashCode());

        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                int bi = y * bw + x;
                if (!GetBit(RegionEntry.maskBits, bi)) continue;

                int px = b.x + x;
                int py = b.y + y;
                int pi = py * _w + px;
                if ((uint)pi >= (uint)_pixels.Length) continue;

                // --- (A) Dither: biến thiên hạt rỗ nhưng không bỏ pixel để tránh bệt/nhạt ---
                float r01 = Hash01(seed, px, py);
                float ditherWeight = (_skipChance > 0f && r01 < _skipChance) ? 0.25f : 1f;

                // --- (B) Speckle: lệch sáng/tối ngẫu nhiên ---
                float n01 = Hash01(seed + 999, px, py);
                float delta = (n01 * 2f - 1f) * _grainStrength * ditherWeight;

                Color32 c = ApplyBrightness(baseColor, delta);

                // --- (C) White speckles: chấm trắng kiểu hạt cát ---
                if (_speckleDensity > 0f && _speckleIntensity > 0f && _speckleSize > 0)
                {
                    int cellSize = Mathf.Max(1, _speckleSize);
                    int cellX = px / cellSize;
                    int cellY = py / cellSize;

                    // Xác suất có hạt trong ô
                    float cellChance = Hash01(seed + 2222, cellX, cellY);
                    if (cellChance < _speckleDensity)
                    {
                        // Tâm dot ngẫu nhiên trong ô
                        float cx01 = Hash01(seed + 4567, cellX, cellY);
                        float cy01 = Hash01(seed + 8911, cellX, cellY);
                        float cx = (cellX * cellSize) + cx01 * cellSize;
                        float cy = (cellY * cellSize) + cy01 * cellSize;

                        float dx = px - cx;
                        float dy = py - cy;
                        float r = Mathf.Max(1f, cellSize * 0.5f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);

                        if (d <= r)
                        {
                            // Trọng số theo khoảng cách + độ mềm biên
                            float t = 1f - Mathf.Clamp01(d / r);
                            t = Mathf.Pow(t, Mathf.Lerp(1f, 3f, _speckleSoftness));
                            float k = _speckleIntensity * t;

                            // Lighten về trắng
                            Color baseF = c;
                            Color result = Color.Lerp(baseF, Color.white, k);
                            result.a = baseF.a;
                            c = (Color32)result;
                        }
                    }
                }

                _pixels[pi] = c;
            }
        }

        RegionEntry.filled = true;
    }

    private void CheckAllFilledAndLogOnce()
    {
        if (_hasLoggedComplete) return;
        if (_asset == null || _asset.regions == null) return;

        for (int i = 0; i < _asset.regions.Count; i++)
        {
            var r = _asset.regions[i];
            if (r == null) continue;
            if (r.maskBits == null || r.maskBits.Length == 0) continue; // ignore empty
            if (!r.filled) return; // còn vùng chưa tô
        }

        _hasLoggedComplete = true;
        Debug.Log("🎉 TÔ HẾT TẤT CẢ VÙNG!");
    }

    // ---------------- helpers ----------------
    private static bool GetBit(byte[] bits, int index)
    {
        int byteIndex = index >> 3;
        int bit = index & 7;
        if ((uint)byteIndex >= (uint)bits.Length) return false;
        return (bits[byteIndex] & (1 << bit)) != 0;
    }

    private static bool CloseColor(Color32 a, Color32 b, float tol)
    {
        float dr = (a.r - b.r) / 255f;
        float dg = (a.g - b.g) / 255f;
        float db = (a.b - b.b) / 255f;
        return (dr * dr + dg * dg + db * db) <= (tol * tol);
    }

    private static float Hash01(int seed, int x, int y)
    {
        unchecked
        {
            int h = seed;
            h ^= x * 374761393;
            h ^= y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= (h >> 16);
            // map to 0..1
            return (h & 0x7fffffff) / 2147483647f;
        }
    }

    private Color32 ApplyBrightness(Color32 c, float delta)
    {
        // Làm việc ở không gian HSV để giữ độ đậm, tránh bị “bệt/nhạt”
        Color rgb = c;
        float h, s, v;
        Color.RGBToHSV(rgb, out h, out s, out v);

        // Boost độ sáng (Value) và áp nhiễu theo kiểu nhân để không bạc màu
        v = Mathf.Clamp01(v * _colorBoost);
        v = Mathf.Clamp01(v * (1f + delta));

        // Tăng độ bão hòa để màu đậm hơn, ít bị nhạt
        s = Mathf.Clamp01(s * _saturationBoost);

        Color outRgb = Color.HSVToRGB(h, s, v);
        outRgb.a = c.a / 255f;
        return (Color32)outRgb;
    }
    
    private static int ColorKey(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;
}