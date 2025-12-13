using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace HootyBird.ColoringBook.Services
{
    public class TextureFillService : MonoBehaviour
    {
        private static TextureFillService instance;

        [SerializeField]
        private Material fillMaterial;
        [SerializeField]
        private float fillAnimationDuration = .4f;

        private CommandBuffer drawCircleCommandBuffer;
        private Matrix4x4 viewMatrix;
        private Mesh drawMesh;

        public static TextureFillService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("TextureFillService");
                    go.AddComponent<TextureFillService>();
                }

                return instance;
            }
        }
        public float FillAnimationDuration => fillAnimationDuration;

        private void Awake()
        {
            if (instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (fillMaterial == null)
            {
                fillMaterial = Resources.Load<Material>("Materials/TextureFillMaterial");
            }

            drawCircleCommandBuffer = new CommandBuffer();
            viewMatrix = Matrix4x4.TRS(new Vector3(0f, 0f, -1f), Quaternion.identity, Vector3.one);
        }

        private void OnDestroy()
        {
            ClearAssets();
        }

        public void DrawCircle(
            RenderTexture target,
            Color clearColor,
            Color renderColor,
            Rect projectionRect,
            Vector3 worldPos,
            float radius)
        {
            fillMaterial.SetColor("_Color", renderColor);

            ResetDrawMesh();
            AddQuadToDrawMesh(radius);

            drawCircleCommandBuffer.Clear();
            drawCircleCommandBuffer.SetViewMatrix(viewMatrix);
            drawCircleCommandBuffer.SetRenderTarget(target);
            drawCircleCommandBuffer.ClearRenderTarget(true, true, clearColor);
            drawCircleCommandBuffer.SetProjectionMatrix(Matrix4x4.Ortho(
                projectionRect.min.x,
                projectionRect.max.x,
                projectionRect.min.y,
                projectionRect.max.y,
                0f,
                2f));
            drawCircleCommandBuffer.DrawMesh(
                drawMesh,
                Matrix4x4.TRS(worldPos, Quaternion.identity, Vector2.one),
                fillMaterial,
                0);

            Graphics.ExecuteCommandBuffer(drawCircleCommandBuffer);
        }

        private void AddQuadToDrawMesh(float raidus, float rotation = 0f)
        {
            Vector3[] newVerts = drawMesh.vertices.Concat(new Vector3[]
            {
                new Vector3(-raidus, -raidus),
                new Vector3(-raidus, raidus),
                new Vector3(raidus, raidus),
                new Vector3(raidus, -raidus),
            }).ToArray();

            if (rotation != 0f)
            {
                Matrix4x4 rMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation));
                for (int index = newVerts.Length - 4; index < newVerts.Length; index++)
                {
                    newVerts[index] = rMatrix.MultiplyPoint3x4(newVerts[index]);
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

        private void ResetDrawMesh()
        {
            Destroy(drawMesh);

            drawMesh = new Mesh();
            drawMesh.vertices = new Vector3[0];
            drawMesh.triangles = new int[0];
            drawMesh.uv = new Vector2[0];
        }

        private void ClearAssets()
        {
            Destroy(drawMesh);
        }
    }
}
