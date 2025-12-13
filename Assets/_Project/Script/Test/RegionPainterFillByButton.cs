using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RegionPainterFillByButton : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ColoringRegionsAsset _asset;

    [Header("Render")]
    [SerializeField] private SpriteRenderer _sr;

    [Header("Matching sampledColor tolerance")]
    [Range(0f, 0.25f)]
    [SerializeField] private float _sampledMatchTolerance = 0.04f;
    
    [Header("Sand Noise")]
    [SerializeField, Range(0f, 0.35f)] private float _grainStrength = 0.12f; // lệch sáng/tối
    [SerializeField, Range(0f, 0.25f)] private float _skipChance = 0.06f;    // % pixel không tô (hạt rỗ)
    [SerializeField] private int _noiseSeed = 12345;                         // seed để ổn định


    private Texture2D _runtimeTex;
    private Color32[] _pixels;
    private int _w, _h;

    // map: sampledColorKey -> regions
    private readonly Dictionary<int, List<ColoringRegionsAsset.RegionEntry>> _map = new();

    private bool _hasLoggedComplete;

    private void Reset()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();

        BuildRuntimeTextureClone();
        BuildMapBySampledColor();

        // reset trạng thái filled khi vào scene
        if (_asset != null && _asset.regions != null)
        {
            foreach (var r in _asset.regions)
                if (r != null) r.filled = false;
        }

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

    /// <summary>
    /// ✅ BẤM NÚT MÀU -> TÔ LUÔN TẤT CẢ VÙNG có sampledColor ~ color
    /// </summary>
    public void FillAllRegionsOfSampledColor(Color32 sampledColor)
    {
        if (_runtimeTex == null || _pixels == null) return;
        if (_asset == null || _asset.regions == null) return;

        // nếu đã complete rồi thì vẫn cho tô, nhưng khỏi log lại
        int key = ColorKey(sampledColor);

        List<ColoringRegionsAsset.RegionEntry> regions = null;

        if (_map.TryGetValue(key, out var exact))
        {
            regions = exact;
        }
        else
        {
            // fallback: nếu sampledColor lệch nhẹ
            regions = new List<ColoringRegionsAsset.RegionEntry>();
            foreach (var r in _asset.regions)
            {
                if (r == null || r.maskBits == null || r.maskBits.Length == 0) continue;
                if (CloseColor(r.sampledColor, sampledColor, _sampledMatchTolerance))
                    regions.Add(r);
            }
        }

        if (regions == null || regions.Count == 0) return;

        // paint all regions in group
        for (int i = 0; i < regions.Count; i++)
        {
            ApplyRegion(regions[i], sampledColor);
        }

        // apply once
        _runtimeTex.SetPixels32(_pixels);
        _runtimeTex.Apply(false, false);

        // ✅ check complete -> log
        CheckAllFilledAndLogOnce();
    }

    /*private void ApplyRegion(ColoringRegionsAsset.RegionEntry r, Color32 color)
    {
        if (r == null || r.maskBits == null || r.maskBits.Length == 0) return;

        var b = r.bounds;
        int bw = b.width;
        int bh = b.height;
        if (bw <= 0 || bh <= 0) return;

        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                int bi = y * bw + x;
                if (!GetBit(r.maskBits, bi)) continue;

                int px = b.x + x;
                int py = b.y + y;
                int pi = py * _w + px;
                if ((uint)pi >= (uint)_pixels.Length) continue;

                _pixels[pi] = color;
            }
        }

        // ✅ mark filled
        r.filled = true;
    }*/

    private void ApplyRegion(ColoringRegionsAsset.RegionEntry r, Color32 baseColor)
    {
        if (r == null || r.maskBits == null || r.maskBits.Length == 0) return;

        var b = r.bounds;
        int bw = b.width;
        int bh = b.height;
        if (bw <= 0 || bh <= 0) return;

        // seed theo region + màu để noise ổn định theo từng vùng
        int seed = _noiseSeed ^ (r.id * 73856093) ^ (baseColor.r * 19349663) ^ (baseColor.g * 83492791) ^ (baseColor.b * 2654435761.GetHashCode());

        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                int bi = y * bw + x;
                if (!GetBit(r.maskBits, bi)) continue;

                int px = b.x + x;
                int py = b.y + y;
                int pi = py * _w + px;
                if ((uint)pi >= (uint)_pixels.Length) continue;

                // --- (A) Dither: bỏ qua 1 ít pixel để tạo hạt rỗ ---
                float r01 = Hash01(seed, px, py);
                if (_skipChance > 0f && r01 < _skipChance)
                    continue;

                // --- (B) Speckle: lệch sáng/tối ngẫu nhiên ---
                float n01 = Hash01(seed + 999, px, py); // 0..1
                float delta = (n01 * 2f - 1f) * _grainStrength; // -strength..+strength

                Color32 c = ApplyBrightness(baseColor, delta);

                _pixels[pi] = c;
            }
        }

        r.filled = true;
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

    private static Color32 ApplyBrightness(Color32 c, float delta)
    {
        // delta: -0.35..+0.35 (khuyên)
        float r = Mathf.Clamp01(c.r / 255f + delta);
        float g = Mathf.Clamp01(c.g / 255f + delta);
        float b = Mathf.Clamp01(c.b / 255f + delta);
        return new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), c.a);
    }

    private static int ColorKey(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;
}
