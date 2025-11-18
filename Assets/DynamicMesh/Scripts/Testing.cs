using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour {
    [SerializeField] private Material lineMaterial; // Material cho đường viền
    private GameObject outlineObject;

    private void Start() {
        Debug.Log("Test");
        CreateTileMesh();
    }

    private void CreateBasicQuadMesh() {
        // Code cũ giữ nguyên
    }

    private void CreateTileMesh() {
        Mesh mesh = new Mesh();

        int width = 128;
        int height = 166;
        float tileSize = 10;

        Vector3[] vertices = new Vector3[4 * (width * height)];
        Vector2[] uv = new Vector2[4 * (width * height)];
        int[] triangles = new int[6 * (width * height)];

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                int index = i * height + j;

                vertices[index * 4 + 0] = new Vector3(tileSize * i,         tileSize * j);
                vertices[index * 4 + 1] = new Vector3(tileSize * i,         tileSize * (j + 1));
                vertices[index * 4 + 2] = new Vector3(tileSize * (i + 1),   tileSize * (j + 1));
                vertices[index * 4 + 3] = new Vector3(tileSize * (i + 1),   tileSize * j);
                
                uv[index * 4 + 0] = new Vector2(0, 0);
                uv[index * 4 + 1] = new Vector2(0, 1);
                uv[index * 4 + 2] = new Vector2(1, 1);
                uv[index * 4 + 3] = new Vector2(1, 0);

                triangles[index * 6 + 0] = index * 4 + 0;
                triangles[index * 6 + 1] = index * 4 + 1;
                triangles[index * 6 + 2] = index * 4 + 2;

                triangles[index * 6 + 3] = index * 4 + 0;
                triangles[index * 6 + 4] = index * 4 + 2;
                triangles[index * 6 + 5] = index * 4 + 3;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        GetComponent<MeshFilter>().mesh = mesh;

        // Tạo viền sau khi tạo mesh
        CreateGridOutline(width, height, tileSize);
    }

    private void CreateGridOutline(int width, int height, float tileSize)
    {
        if (outlineObject != null) {
            Destroy(outlineObject);
        }

        outlineObject = new GameObject("GridOutline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;

        // Tạo các đường ngang
        for (int i = 0; i <= height; i++)
        {
            CreateLine(
                new Vector3(0, i * tileSize, 0),
                new Vector3(width * tileSize, i * tileSize, 0)
            );
        }

        // Tạo các đường dọc
        for (int i = 0; i <= width; i++)
        {
            CreateLine(
                new Vector3(i * tileSize, 0, 0),
                new Vector3(i * tileSize, height * tileSize, 0)
            );
        }
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(outlineObject.transform);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        
        // Nếu không có material được gán, tạo material mới
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMaterial.color = Color.black;
        }
        
        line.material = lineMaterial;
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.positionCount = 2;
        line.useWorldSpace = false;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private void CreateAnimationMesh() {
        // Code cũ giữ nguyên
    }
}