using UnityEngine;

/// Gắn script này lên 1 GameObject (nên là con của RenMap).
/// Nó sẽ sinh Mesh đường viền cho grid width x height.
/// Mỗi đường kẻ là 1 quad mỏng (thickness).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridOutlineRenderer : MonoBehaviour
{
    [Header("Grid size")]
    public int width = 10;
    public int height = 20;

    [Header("Visual")]
    public float cellSize = 0.05f;     // giống _cellSize trong RenMap
    public float thickness = 0.002f;   // độ dày viền (world units)

    MeshFilter _mf;
    MeshRenderer _mr;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        Rebuild();
    }

    [ContextMenu("Rebuild Outline")]
    public void Rebuild()
    {
        if (width <= 0 || height <= 0 || cellSize <= 0f)
        {
            Debug.LogWarning("GridOutlineRenderer: tham số không hợp lệ");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.name = "GridOutlineMesh";

        // số line: (width + 1) dọc + (height + 1) ngang
        int lineCount = (width + 1) + (height + 1);

        Vector3[] vertices = new Vector3[lineCount * 4];
        int[] triangles = new int[lineCount * 6];

        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;
        float startX = -totalWidth * 0.5f;
        float startY = -totalHeight * 0.5f;

        // Đẩy viền hơi "nổi" lên để tránh z-fighting với mesh sand
        float z = 0.01f;

        int v = 0;
        int t = 0;

        // === VERTICAL LINES (các đường dọc) ===
        for (int x = 0; x <= width; x++)
        {
            float cx = startX + x * cellSize;
            float x0 = cx - thickness * 0.5f;
            float x1 = cx + thickness * 0.5f;
            float y0 = startY;
            float y1 = startY + totalHeight;

            vertices[v + 0] = new Vector3(x0, y0, z);
            vertices[v + 1] = new Vector3(x1, y0, z);
            vertices[v + 2] = new Vector3(x0, y1, z);
            vertices[v + 3] = new Vector3(x1, y1, z);

            triangles[t + 0] = v + 0;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 1;

            v += 4;
            t += 6;
        }

        // === HORIZONTAL LINES (các đường ngang) ===
        for (int y = 0; y <= height; y++)
        {
            float cy = startY + y * cellSize;
            float y0 = cy - thickness * 0.5f;
            float y1 = cy + thickness * 0.5f;
            float x0 = startX;
            float x1 = startX + totalWidth;

            vertices[v + 0] = new Vector3(x0, y0, z);
            vertices[v + 1] = new Vector3(x1, y0, z);
            vertices[v + 2] = new Vector3(x0, y1, z);
            vertices[v + 3] = new Vector3(x1, y1, z);

            triangles[t + 0] = v + 0;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 1;

            v += 4;
            t += 6;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        _mf.sharedMesh = mesh;
    }
}
