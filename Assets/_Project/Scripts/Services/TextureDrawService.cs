using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace HootyBird.ColoringBook.Services
{
    public class TextureDrawService : MonoBehaviour
    {
        private static TextureDrawService instance;

        [SerializeField]
        private Material brushMaterial;
        [SerializeField]
        private float brushSize = .3f;

        private CommandBuffer drawBrushCommandBuffer;
        private Mesh drawMesh;
        private Matrix4x4 viewMatrix;
        private Matrix4x4 brushMeshMatrix;

        public static TextureDrawService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("TextureDrawService");
                    go.AddComponent<TextureDrawService>();
                }

                return instance;
            }
        }

        public float BrushSize
        {
            get => brushSize;
            set => brushSize = value;
        }

        private void Awake()
        {
            if (instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (!brushMaterial)
            {
                brushMaterial = Resources.Load<Material>("Materials/BrushMaterial");
            }

            drawBrushCommandBuffer = new CommandBuffer();
            viewMatrix = Matrix4x4.TRS(new Vector3(0f, 0f, -1f), Quaternion.identity, Vector3.one);
            brushMeshMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        }

        private void OnDestroy()
        {
            ClearAssets();
        }

        public IEnumerable<Rect> BrushRectAtPositions(IEnumerable<Vector2> screenPositions, float scale)
        {
            Vector2 halfBrushSize = new Vector2(BrushSize, BrushSize) * .5f * scale;
            IEnumerable<Rect> rects =
                screenPositions.Select(pos => {
                    Vector2 worldPos = Camera.main.ScreenToWorldPoint(pos);
                    Vector2 min = worldPos - halfBrushSize;
                    Vector2 max = worldPos + halfBrushSize;

                    return new Rect(min, max - min);
                });

            return rects;
        }

        public void DrawBrush(RenderTexture texture, Color renderColor, IEnumerable<Rect> brushRects, Rect projectionRect)
        {
            brushMaterial.SetColor("_Color", renderColor);

            // Update brush mesh.
            ResetDrawMesh();
            foreach (Rect rect in brushRects)
            {
                AddRectToDrawMesh(rect, UnityEngine.Random.value * 360f);
            }

            drawBrushCommandBuffer.Clear();
            drawBrushCommandBuffer.SetViewMatrix(viewMatrix);
            drawBrushCommandBuffer.SetRenderTarget(texture);
            drawBrushCommandBuffer.SetProjectionMatrix(Matrix4x4.Ortho(
                projectionRect.min.x,
                projectionRect.max.x,
                projectionRect.min.y,
                projectionRect.max.y,
                0f,
                2f));

            drawBrushCommandBuffer.DrawMesh(
                drawMesh,
                brushMeshMatrix,
                brushMaterial,
                0);

            Graphics.ExecuteCommandBuffer(drawBrushCommandBuffer);
        }

        private void ResetDrawMesh()
        {
            Destroy(drawMesh);

            drawMesh = new Mesh();
            drawMesh.vertices = new Vector3[0];
            drawMesh.triangles = new int[0];
            drawMesh.uv = new Vector2[0];
        }

        private void AddRectToDrawMesh(Rect rect, float rotation = 0f)
        {
            Vector3[] newVerts = drawMesh.vertices.Concat(new Vector3[]
            {
                new Vector3(rect.min.x, rect.min.y),
                new Vector3(rect.min.x, rect.max.y),
                new Vector3(rect.max.x, rect.max.y),
                new Vector3(rect.max.x, rect.min.y),
            }).ToArray();

            if (rotation != 0f)
            {
                Matrix4x4 rMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation));
                for (int index = newVerts.Length - 4; index < newVerts.Length; index++)
                {
                    newVerts[index] = (Vector3)rect.center + rMatrix.MultiplyPoint3x4(newVerts[index] - (Vector3)rect.center);
                }
            }

            int vertsCount = drawMesh.vertexCount;
            int[] newTriangles = drawMesh.triangles.Concat(new int[] 
            { 
                vertsCount + 0,
                vertsCount + 1,
                vertsCount + 2,
                vertsCount + 0,
                vertsCount + 2, 
                vertsCount + 3
            }).ToArray();
            Vector2[] newUVs = drawMesh.uv.Concat(new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0, 1f),
                new Vector2(1f, 1f),
                new Vector2(1, 0f),
            }).ToArray();

            drawMesh.vertices = newVerts;
            drawMesh.triangles = newTriangles;
            drawMesh.uv = newUVs;
        }

        private void ClearAssets()
        {
            Destroy(drawMesh);
        }
    }
}
