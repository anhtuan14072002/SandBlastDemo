using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridOutlineOnly : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 20;
    public float cellSize = 0.5f;

    [Header("Line")]
    [Tooltip("Độ dày viền (đơn vị world)")]
    public float thickness = 0.02f;
    public Color lineColor = Color.black;

    [Header("Sorting (optional)")]
    public string sortingLayerName = "";
    public int sortingOrder = 0;

    MeshFilter _mf;
    MeshRenderer _mr;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        EnsureMaterial();
        Rebuild();
        ApplySorting();
    }

    void OnValidate()
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;
        if (cellSize <= 0f) cellSize = 0.01f;
        if (thickness <= 0f) thickness = 0.001f;

        EnsureMaterial();
        Rebuild();
        ApplySorting();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        var m = BuildOutlineMesh(width, height, cellSize, thickness);
        if (_mf == null) _mf = GetComponent<MeshFilter>();
        _mf.sharedMesh = m;

        if (_mr == null) _mr = GetComponent<MeshRenderer>();
        _mr.sharedMaterial.color = lineColor;
    }

    void ApplySorting()
    {
        if (_mr == null) _mr = GetComponent<MeshRenderer>();
        if (!string.IsNullOrEmpty(sortingLayerName))
            _mr.sortingLayerName = sortingLayerName;
        _mr.sortingOrder = sortingOrder;
    }

    void EnsureMaterial()
    {
        if (_mr == null) _mr = GetComponent<MeshRenderer>();
        var mat = _mr.sharedMaterial;
        if (mat == null)
        {
            // Ưu tiên Unlit/Color; fallback Sprites/Default nếu không có
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader);
            _mr.sharedMaterial = mat;
        }
        _mr.sharedMaterial.color = lineColor;
    }

    static Mesh BuildOutlineMesh(int W, int H, float cs, float t)
    {
        var verts = new List<Vector3>();
        var tris  = new List<int>();
        var uvs   = new List<Vector2>();

        float gridW = W * cs;
        float gridH = H * cs;
        float halfT = t * 0.5f;

        void AddVertical(float x, float y0, float y1)
        {
            int s = verts.Count;
            verts.Add(new Vector3(x - halfT, y0, 0));
            verts.Add(new Vector3(x + halfT, y0, 0));
            verts.Add(new Vector3(x + halfT, y1, 0));
            verts.Add(new Vector3(x - halfT, y1, 0));
            uvs.Add(Vector2.zero); uvs.Add(Vector2.right);
            uvs.Add(Vector2.one);  uvs.Add(Vector2.up);
            tris.Add(s+0); tris.Add(s+2); tris.Add(s+1);
            tris.Add(s+0); tris.Add(s+3); tris.Add(s+2);
        }

        void AddHorizontal(float y, float x0, float x1)
        {
            int s = verts.Count;
            verts.Add(new Vector3(x0, y - halfT, 0));
            verts.Add(new Vector3(x1, y - halfT, 0));
            verts.Add(new Vector3(x1, y + halfT, 0));
            verts.Add(new Vector3(x0, y + halfT, 0));
            uvs.Add(Vector2.zero); uvs.Add(Vector2.right);
            uvs.Add(Vector2.one);  uvs.Add(Vector2.up);
            tris.Add(s+0); tris.Add(s+2); tris.Add(s+1);
            tris.Add(s+0); tris.Add(s+3); tris.Add(s+2);
        }

        // Đường dọc: i = 0..W
        for (int i = 0; i <= W; i++)
        {
            float x = i * cs;
            AddVertical(x, 0f, gridH);
        }
        // Đường ngang: j = 0..H
        for (int j = 0; j <= H; j++)
        {
            float y = j * cs;
            AddHorizontal(y, 0f, gridW);
        }

        var mesh = new Mesh();
        mesh.name = "GridOutlineOnly";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.35f);
        var center = transform.position + new Vector3(width*cellSize*0.5f, height*cellSize*0.5f, 0);
        var size   = new Vector3(width*cellSize, height*cellSize, 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
