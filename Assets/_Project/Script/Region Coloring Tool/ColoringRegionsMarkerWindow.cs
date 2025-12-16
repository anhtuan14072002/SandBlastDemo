#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ColoringRegionsMarkerWindow_Pro : EditorWindow
{
    private ColoringRegionsAsset asset; // Asset đang chỉnh sửa
    private Texture2D previewTex;
    private Vector2 scrollPreview; // Vị trí cuộn preview
    private float zoom = 1f; // Mức zoom preview
    // Selection
    private readonly HashSet<int> selected = new HashSet<int>(); // Tập hợp các vùng được chọn
    // Click mode
    private bool selectionMode = true; // Chế độ chọn vùng
    private bool addMode = false; // Chế độ thêm vùng mới
    // Control toggles (giống ảnh)
    private bool removeAttachedTexture = true; // Xóa texture khi merge/delete
    private bool removeMerged = true; // Xóa vùng gốc sau khi merge
    // Foldouts (mũi tên thu gọn)
    private bool foldRegions = true;
    private bool foldControl = true;
    private bool foldColors = true;
    private bool foldPreview = true;
    // Regions list scroll
    private Vector2 scrollRegions; // Vị trí cuộn preview
    private float regionsListHeight = 170f; // Chiều cao danh sách vùng
    [MenuItem("Tools/ColoringBook/Region Marker BETA")]
    public static void Open()
    {
        GetWindow<ColoringRegionsMarkerWindow_Pro>("Region Marker BETA");
    }

    private void OnDisable()
    {
        if (previewTex != null) DestroyImmediate(previewTex);
        previewTex = null;
        selected.Clear();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        asset = (ColoringRegionsAsset)EditorGUILayout.ObjectField("Asset", asset, typeof(ColoringRegionsAsset), false);
        if (asset == null) return;

        asset.sourceTexture =
            (Texture2D)EditorGUILayout.ObjectField("Source Texture", asset.sourceTexture, typeof(Texture2D), false);
        asset.blackThreshold = EditorGUILayout.Slider("Black Threshold", asset.blackThreshold, 0f, 0.5f);
        asset.colorTolerance = EditorGUILayout.Slider("Color Tolerance", asset.colorTolerance, 0f, 0.25f);

        if (asset.sourceTexture == null)
        {
            EditorGUILayout.HelpBox("Kéo texture vào Source Texture (nhớ bật Read/Write).", MessageType.Info);
            return;
        }

        if (!asset.sourceTexture.isReadable)
        {
            EditorGUILayout.HelpBox("Texture chưa bật Read/Write Enabled. Bật trong Import Settings rồi Reimport.",
                MessageType.Error);
            return;
        }

        EditorGUILayout.Space(8);

        // Foldouts
        foldRegions = EditorGUILayout.Foldout(foldRegions, "Regions", true);
        if (foldRegions) DrawRegionsPanel();

        EditorGUILayout.Space(4);
        foldControl = EditorGUILayout.Foldout(foldControl, "Control Panel", true);
        if (foldControl) DrawControlPanel();

        EditorGUILayout.Space(4);
        foldColors = EditorGUILayout.Foldout(foldColors, "Colors", true);
        if (foldColors) DrawColorsPanel();

        EditorGUILayout.Space(6);
        foldPreview = EditorGUILayout.Foldout(foldPreview, "Preview", true);
        if (foldPreview) DrawPreviewPanel();
    }

    // =======================
    // Regions list (checkbox + scroll)
    // =======================
    private void DrawRegionsPanel()
    {
        if (asset.regions == null) asset.regions = new List<ColoringRegionsAsset.RegionEntry>();

        // click mode (exclusive)
        EditorGUILayout.BeginHorizontal();
        bool sel = GUILayout.Toggle(selectionMode, "Selection Mode", "Button");
        bool add = GUILayout.Toggle(addMode, "Add Mode", "Button");
        EditorGUILayout.EndHorizontal();

        if (sel && !selectionMode)
        {
            selectionMode = true;
            addMode = false;
        }

        if (add && !addMode)
        {
            addMode = true;
            selectionMode = false;
        }

        if (!selectionMode && !addMode) selectionMode = true;

        // Height slider để tùy chỉnh list không đẩy UI
        regionsListHeight = EditorGUILayout.Slider("Regions List Height", regionsListHeight, 80f, 420f);

        scrollRegions = EditorGUILayout.BeginScrollView(scrollRegions, GUILayout.Height(regionsListHeight));
        for (int i = 0; i < asset.regions.Count; i++)
        {
            var r = asset.regions[i];
            if (r == null) continue;

            if (string.IsNullOrEmpty(r.displayName))
                r.displayName = $"{r.id:D3}-region";

            bool isSel = selected.Contains(r.id);

            EditorGUILayout.BeginHorizontal();
            bool newSel = EditorGUILayout.Toggle(isSel, GUILayout.Width(18));

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            if (r.hidden) labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.45f);

            EditorGUILayout.LabelField(r.displayName, labelStyle);

            GUILayout.FlexibleSpace();

            // color box
            Rect rc = GUILayoutUtility.GetRect(22, 14);
            EditorGUI.DrawRect(rc, (Color)r.overrideColor);

            EditorGUILayout.EndHorizontal();

            if (newSel != isSel)
            {
                if (newSel) selected.Add(r.id);
                else selected.Remove(r.id);
                RebuildPreview();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // =======================
    // Control Panel (buttons)
    // =======================
    private void DrawControlPanel()
    {
        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (GUILayout.Button("Grow Selected", GUILayout.Height(22)))
            {
                GrowSelected();
                RebuildPreview();
            }
        }

        removeAttachedTexture = EditorGUILayout.ToggleLeft("Remove attached texture?", removeAttachedTexture);

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (GUILayout.Button("Show Selected", GUILayout.Height(22)))
            {
                SetHiddenSelected(false);
                RebuildPreview();
            }

            if (GUILayout.Button("Hide Selected", GUILayout.Height(22)))
            {
                SetHiddenSelected(true);
                RebuildPreview();
            }
        }

        EditorGUILayout.EndHorizontal();

        removeMerged = EditorGUILayout.ToggleLeft("Remove merged?", removeMerged);

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(selected.Count < 2))
        {
            if (GUILayout.Button("Merge", GUILayout.Height(26)))
            {
                DoMerge();
                RebuildPreview();
            }
        }

        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (GUILayout.Button("Delete", GUILayout.Height(26)))
            {
                DoDeleteSelected();
                RebuildPreview();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Selection", GUILayout.Height(22)))
        {
            selected.Clear();
            RebuildPreview();
        }

        if (GUILayout.Button("Rebuild Preview", GUILayout.Height(22)))
        {
            RebuildPreview();
        }

        EditorGUILayout.EndHorizontal();
    }

    // =======================
    // Colors panel
    // =======================
    private void DrawColorsPanel()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Update colors", GUILayout.Height(22)))
        {
            Undo.RecordObject(asset, "Update colors cache");
            asset.RebuildColorsCache();
            MarkDirty();
        }

        EditorGUILayout.EndHorizontal();

        if (asset.colorsCache == null) asset.colorsCache = new List<Color32>();

        float h = 18f;
        for (int i = 0; i < asset.colorsCache.Count; i++)
        {
            Rect r = GUILayoutUtility.GetRect(10, h, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, (Color)asset.colorsCache[i]);
        }
    }

    // =======================
    // Preview + Click handling
    // =======================
    private void DrawPreviewPanel()
    {
        zoom = EditorGUILayout.Slider("Zoom", zoom, 0.2f, 5f);

        if (previewTex == null) RebuildPreview();

        Rect box = GUILayoutUtility.GetRect(position.width - 20, position.height * 0.55f);
        GUI.Box(box, GUIContent.none);

        scrollPreview = GUI.BeginScrollView(
            box, scrollPreview,
            new Rect(0, 0, previewTex.width * zoom, previewTex.height * zoom)
        );

        Rect texRect = new Rect(0, 0, previewTex.width * zoom, previewTex.height * zoom);
        GUI.DrawTexture(texRect, previewTex, ScaleMode.StretchToFill, true);

        HandleClick(texRect);

        GUI.EndScrollView();

        EditorGUILayout.HelpBox(
            "Selection Mode: click vùng đã tạo -> toggle select (vùng trắng).\n" +
            "Add Mode: click -> tạo region mới.\n" +
            "Merge/Delete/Hide/Show theo checkbox selection.",
            MessageType.None);
    }

    private void HandleClick(Rect texRect)
    {
        Event e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0) return;
        if (!texRect.Contains(e.mousePosition)) return;

        Vector2 local = e.mousePosition;
        int px = Mathf.FloorToInt(local.x / zoom);
        int py = Mathf.FloorToInt((texRect.height - local.y) / zoom);

        int w = asset.sourceTexture.width;
        int h = asset.sourceTexture.height;
        if ((uint)px >= (uint)w || (uint)py >= (uint)h) return;

        if (selectionMode)
        {
            int hitId = FindRegionIdAtPixel(px, py);
            if (hitId > 0)
            {
                if (selected.Contains(hitId)) selected.Remove(hitId);
                else selected.Add(hitId);

                RebuildPreview();
            }

            e.Use();
            return;
        }

        if (addMode)
        {
            var src = asset.sourceTexture.GetPixels32();
            int seedIndex = py * w + px;
            Color32 seedColor = src[seedIndex];

            if (IsBlack(seedColor, asset.blackThreshold)) return;

            var fr = FloodFillMask(src, w, h, px, py, seedColor, asset.colorTolerance, asset.blackThreshold);
            if (fr.count <= 0) return;

            Undo.RecordObject(asset, "Add Region");
            int id = asset.GetNextId();

            var region = new ColoringRegionsAsset.RegionEntry
            {
                id = id,
                displayName = $"{Guid.NewGuid().ToString("N").Substring(0, 8)}-texture",
                seed = new Vector2Int(px, py),
                sampledColor = seedColor,
                overrideColor = seedColor,
                hidden = false,
                mergedFlag = false,
                attachedTexture = null,
                bounds = fr.bounds,
                maskBits = fr.maskBits
            };

            asset.regions.Add(region);
            selected.Add(id);

            asset.RebuildColorsCache();
            MarkDirty();
            RebuildPreview();
            e.Use();
        }
    }

    // =======================
    // Operations
    // =======================
    private void SetHiddenSelected(bool hidden)
    {
        Undo.RecordObject(asset, "Hide/Show selected");
        foreach (var r in asset.regions)
        {
            if (r == null) continue;
            if (selected.Contains(r.id)) r.hidden = hidden;
        }

        MarkDirty();
    }

    // Grow: add các vùng “chạm/giáp” với vùng đang chọn (dựa trên bounds overlap + pixel touching)
    private void GrowSelected()
    {
        if (selected.Count == 0) return;

        var idsToAdd = new List<int>();
        var regions = asset.regions;

        for (int i = 0; i < regions.Count; i++)
        {
            var candidate = regions[i];
            if (candidate == null) continue;
            if (selected.Contains(candidate.id)) continue;

            bool touches = false;

            foreach (int sid in selected)
            {
                var sr = asset.GetRegion(sid);
                if (sr == null) continue;

                RectInt a = Expand(sr.bounds, 1);
                if (!a.Overlaps(candidate.bounds)) continue;

                if (RegionsTouch(sr, candidate))
                {
                    touches = true;
                    break;
                }
            }

            if (touches) idsToAdd.Add(candidate.id);
        }

        foreach (var id in idsToAdd) selected.Add(id);
    }

    private void DoMerge()
    {
        if (selected.Count < 2) return;

        // target = min id (muốn popup target thì nói mình thêm)
        int targetId = int.MaxValue;
        foreach (int id in selected) targetId = Mathf.Min(targetId, id);

        var target = asset.GetRegion(targetId);
        if (target == null) return;

        Undo.RecordObject(asset, "Merge regions");

        var toRemove = new List<ColoringRegionsAsset.RegionEntry>();

        foreach (int id in selected)
        {
            if (id == targetId) continue;
            var r = asset.GetRegion(id);
            if (r == null) continue;

            MergeRegion(target, r);
            r.mergedFlag = true;

            if (removeMerged)
                toRemove.Add(r);
        }

        foreach (var r in toRemove)
        {
            if (removeAttachedTexture && r.attachedTexture != null)
                DeleteTextureAssetSafe(r.attachedTexture);

            asset.regions.Remove(r);
        }

        selected.Clear();
        selected.Add(targetId);

        asset.RebuildColorsCache();
        MarkDirty();
    }

    private void DoDeleteSelected()
    {
        if (selected.Count == 0) return;

        Undo.RecordObject(asset, "Delete selected regions");

        for (int i = asset.regions.Count - 1; i >= 0; i--)
        {
            var r = asset.regions[i];
            if (r == null) continue;
            if (!selected.Contains(r.id)) continue;

            if (removeAttachedTexture && r.attachedTexture != null)
                DeleteTextureAssetSafe(r.attachedTexture);

            asset.regions.RemoveAt(i);
        }

        selected.Clear();
        asset.RebuildColorsCache();
        MarkDirty();
    }

    // =======================
    // Preview rebuild (white highlight + hide)
    // =======================
    private void RebuildPreview()
    {
        if (asset == null || asset.sourceTexture == null) return;

        if (previewTex != null) DestroyImmediate(previewTex);

        int w = asset.sourceTexture.width;
        int h = asset.sourceTexture.height;

        var src = asset.sourceTexture.GetPixels32();
        var dst = new Color32[src.Length];
        Array.Copy(src, dst, src.Length);

        foreach (var r in asset.regions)
        {
            if (r == null || r.maskBits == null || r.maskBits.Length == 0) continue;
            if (r.hidden) continue;

            Color32 c = selected.Contains(r.id) ? new Color32(255, 255, 255, 255) : r.overrideColor;
            ApplyRegion(dst, w, r, c);
        }

        previewTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        previewTex.SetPixels32(dst);
        previewTex.Apply(false, false);
        Repaint();
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    // =======================
    // Utilities
    // =======================
    private static RectInt Expand(RectInt r, int pad)
    {
        return new RectInt(r.x - pad, r.y - pad, r.width + pad * 2, r.height + pad * 2);
    }

    private static bool GetBit(byte[] bits, int index)
    {
        int byteIndex = index >> 3;
        int bit = index & 7;
        if ((uint)byteIndex >= (uint)bits.Length) return false;
        return (bits[byteIndex] & (1 << bit)) != 0;
    }

    private static void SetBit(byte[] bits, int index, bool value)
    {
        int byteIndex = index >> 3;
        int bit = index & 7;
        if ((uint)byteIndex >= (uint)bits.Length) return;
        if (value) bits[byteIndex] |= (byte)(1 << bit);
        else bits[byteIndex] &= (byte)~(1 << bit);
    }

    private static bool IsBlack(Color32 c, float threshold)
    {
        float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
        float lum = (r + g + b) / 3f;
        return lum <= threshold;
    }

    private static bool CloseColor(Color32 a, Color32 b, float tol)
    {
        float dr = (a.r - b.r) / 255f;
        float dg = (a.g - b.g) / 255f;
        float db = (a.b - b.b) / 255f;
        return (dr * dr + dg * dg + db * db) <= (tol * tol);
    }

    private static void ApplyRegion(Color32[] pixels, int texW, ColoringRegionsAsset.RegionEntry r, Color32 color)
    {
        var b = r.bounds;
        int bw = b.width;
        int bh = b.height;

        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                int bi = y * bw + x;
                if (!GetBit(r.maskBits, bi)) continue;

                int px = b.x + x;
                int py = b.y + y;
                int pi = py * texW + px;
                if ((uint)pi >= (uint)pixels.Length) continue;

                pixels[pi] = color;
            }
        }
    }

    private int FindRegionIdAtPixel(int px, int py)
    {
        for (int i = asset.regions.Count - 1; i >= 0; i--)
        {
            var r = asset.regions[i];
            if (r == null || r.maskBits == null || r.maskBits.Length == 0) continue;
            if (!r.bounds.Contains(new Vector2Int(px, py))) continue;

            int lx = px - r.bounds.x;
            int ly = py - r.bounds.y;
            int bi = ly * r.bounds.width + lx;

            if (GetBit(r.maskBits, bi)) return r.id;
        }

        return -1;
    }

    // Touching: có pixel của A chạm (4-neighbor) pixel của B không
    private static bool RegionsTouch(ColoringRegionsAsset.RegionEntry a, ColoringRegionsAsset.RegionEntry b)
    {
        int aCount = a.bounds.width * a.bounds.height;
        int bCount = b.bounds.width * b.bounds.height;

        var small = aCount <= bCount ? a : b;
        var big = aCount <= bCount ? b : a;

        int sw = small.bounds.width;
        int sh = small.bounds.height;

        for (int y = 0; y < sh; y++)
        {
            for (int x = 0; x < sw; x++)
            {
                int si = y * sw + x;
                if (!GetBit(small.maskBits, si)) continue;

                int wx = small.bounds.x + x;
                int wy = small.bounds.y + y;

                if (BigHas(big, wx + 1, wy)) return true;
                if (BigHas(big, wx - 1, wy)) return true;
                if (BigHas(big, wx, wy + 1)) return true;
                if (BigHas(big, wx, wy - 1)) return true;
            }
        }

        return false;
    }

    private static bool BigHas(ColoringRegionsAsset.RegionEntry big, int wx, int wy)
    {
        if (!big.bounds.Contains(new Vector2Int(wx, wy))) return false;
        int lx = wx - big.bounds.x;
        int ly = wy - big.bounds.y;
        int bi = ly * big.bounds.width + lx;
        return GetBit(big.maskBits, bi);
    }

    // Merge: union bounds + OR bits
    private static void MergeRegion(ColoringRegionsAsset.RegionEntry dst, ColoringRegionsAsset.RegionEntry src)
    {
        if (src.maskBits == null || src.maskBits.Length == 0) return;

        if (dst.maskBits == null || dst.maskBits.Length == 0)
        {
            dst.bounds = src.bounds;
            dst.maskBits = (byte[])src.maskBits.Clone();
            dst.seed = src.seed;
            dst.sampledColor = src.sampledColor;
            if (dst.overrideColor.a == 0) dst.overrideColor = src.overrideColor;
            return;
        }

        int minX = Mathf.Min(dst.bounds.x, src.bounds.x);
        int minY = Mathf.Min(dst.bounds.y, src.bounds.y);
        int maxX = Mathf.Max(dst.bounds.xMax, src.bounds.xMax);
        int maxY = Mathf.Max(dst.bounds.yMax, src.bounds.yMax);

        RectInt nb = new RectInt(minX, minY, maxX - minX, maxY - minY);

        int bw = nb.width;
        int bh = nb.height;
        int bitCount = bw * bh;
        int byteCount = (bitCount + 7) / 8;

        byte[] outMask = new byte[byteCount];

        CopyMaskOR(dst.bounds, dst.maskBits, nb, outMask);
        CopyMaskOR(src.bounds, src.maskBits, nb, outMask);

        dst.bounds = nb;
        dst.maskBits = outMask;
    }

    private static void CopyMaskOR(RectInt fromB, byte[] fromMask, RectInt toB, byte[] toMask)
    {
        int fw = fromB.width;
        int tw = toB.width;

        for (int y = 0; y < fromB.height; y++)
        {
            for (int x = 0; x < fromB.width; x++)
            {
                int fi = y * fw + x;
                if (!GetBit(fromMask, fi)) continue;

                int wx = fromB.x + x;
                int wy = fromB.y + y;
                int lx = wx - toB.x;
                int ly = wy - toB.y;

                int ti = ly * tw + lx;
                SetBit(toMask, ti, true);
            }
        }
    }

    // Flood fill -> bitset
    private struct FloodResult
    {
        public RectInt bounds;
        public byte[] maskBits;
        public int count;
    }

    private static FloodResult FloodFillMask(Color32[] src, int w, int h, int sx, int sy,
        Color32 seedColor, float colorTol, float blackThreshold)
    {
        var visited = new bool[w * h];
        var q = new Queue<int>();

        int minX = sx, maxX = sx, minY = sy, maxY = sy;
        int count = 0;

        int start = sy * w + sx;
        visited[start] = true;
        q.Enqueue(start);

        var pixels = new List<Vector2Int>(8192);

        while (q.Count > 0)
        {
            int idx = q.Dequeue();
            int x = idx % w;
            int y = idx / w;

            var c = src[idx];
            if (IsBlack(c, blackThreshold)) continue;
            if (!CloseColor(c, seedColor, colorTol)) continue;

            pixels.Add(new Vector2Int(x, y));
            count++;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;

            Enq(x + 1, y);
            Enq(x - 1, y);
            Enq(x, y + 1);
            Enq(x, y - 1);
        }

        RectInt bounds = new RectInt(minX, minY, (maxX - minX + 1), (maxY - minY + 1));
        int bw = bounds.width;
        int bh = bounds.height;

        int bitCount = bw * bh;
        int byteCount = (bitCount + 7) / 8;
        var mask = new byte[byteCount];

        for (int i = 0; i < pixels.Count; i++)
        {
            int lx = pixels[i].x - bounds.x;
            int ly = pixels[i].y - bounds.y;
            int bi = ly * bw + lx;
            SetBit(mask, bi, true);
        }

        return new FloodResult { bounds = bounds, maskBits = mask, count = count };

        void Enq(int nx, int ny)
        {
            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;
            int nidx = ny * w + nx;
            if (visited[nidx]) return;
            visited[nidx] = true;
            q.Enqueue(nidx);
        }
    }

    private static void DeleteTextureAssetSafe(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif