using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class MeshUtils
    {
        static readonly Dictionary<PrimitiveType, Mesh> _primitiveMeshCache = [];

        static readonly Dictionary<Vector2, Mesh> _capsuleMeshCache = [];

        public static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            if (!_primitiveMeshCache.TryGetValue(primitiveType, out Mesh mesh))
            {
                mesh = null;

                GameObject primitiveObject = GameObject.CreatePrimitive(primitiveType);
                if (primitiveObject)
                {
                    if (primitiveObject.TryGetComponent(out MeshFilter meshFilter))
                    {
                        mesh = meshFilter.sharedMesh;
                    }

                    GameObject.Destroy(primitiveObject);
                }

                if (!mesh)
                {
                    Log.Error($"Failed to find mesh for primitive type '{primitiveType}'");
                }

                _primitiveMeshCache.Add(primitiveType, mesh);
            }

            return mesh;
        }

        public static Mesh GetCapsuleMesh(float radius, float height)
        {
            if (_capsuleMeshCache.TryGetValue(new Vector2(radius, height), out Mesh cachedMesh))
                return cachedMesh;

            List<Vector3> vertices = [];
            List<int> triangles = [];

            const int Segments = 16;
            const int HeightSegments = 8;

            float halfHeight = height / 2f;

            // Generate cylinder body
            for (int i = 0; i <= HeightSegments; i++)
            {
                float y = -halfHeight + (i * height / HeightSegments);
                for (int j = 0; j < Segments; j++)
                {
                    float angle = (j / (float)Segments) * Mathf.PI * 2f;
                    float x = radius * Mathf.Cos(angle);
                    float z = radius * Mathf.Sin(angle);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            // Generate top hemisphere
            int topStartIndex = vertices.Count;
            for (int i = 1; i <= HeightSegments; i++)
            {
                float phi = (i / (float)HeightSegments) * Mathf.PI / 2f;
                float circleRadius = radius * Mathf.Cos(phi);
                float y = halfHeight + radius * Mathf.Sin(phi);
                for (int j = 0; j < Segments; j++)
                {
                    float angle = (j / (float)Segments) * Mathf.PI * 2f;
                    float x = circleRadius * Mathf.Cos(angle);
                    float z = circleRadius * Mathf.Sin(angle);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            // Generate bottom hemisphere
            int bottomStartIndex = vertices.Count;
            for (int i = 1; i <= HeightSegments; i++)
            {
                float phi = (i / (float)HeightSegments) * Mathf.PI / 2f;
                float circleRadius = radius * Mathf.Cos(phi);
                float y = -halfHeight - radius * Mathf.Sin(phi);
                for (int j = 0; j < Segments; j++)
                {
                    float angle = (j / (float)Segments) * Mathf.PI * 2f;
                    float x = circleRadius * Mathf.Cos(angle);
                    float z = circleRadius * Mathf.Sin(angle);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            // Generate cylinder triangles
            for (int i = 0; i < HeightSegments; i++)
            {
                for (int j = 0; j < Segments; j++)
                {
                    int a = i * Segments + j;
                    int b = i * Segments + ((j + 1) % Segments);
                    int c = (i + 1) * Segments + j;
                    int d = (i + 1) * Segments + ((j + 1) % Segments);

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);

                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            // Generate top hemisphere triangles
            int cylinderTopIndex = HeightSegments * Segments;
            for (int i = 0; i < HeightSegments; i++)
            {
                for (int j = 0; j < Segments; j++)
                {
                    int a = (i == 0) ? cylinderTopIndex + j : topStartIndex + (i - 1) * Segments + j;
                    int b = (i == 0) ? cylinderTopIndex + ((j + 1) % Segments) : topStartIndex + (i - 1) * Segments + ((j + 1) % Segments);
                    int c = topStartIndex + i * Segments + j;
                    int d = topStartIndex + i * Segments + ((j + 1) % Segments);

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);

                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            // Generate bottom hemisphere triangles
            int cylinderBottomIndex = 0;
            for (int i = 0; i < HeightSegments; i++)
            {
                for (int j = 0; j < Segments; j++)
                {
                    int a = (i == 0) ? cylinderBottomIndex + j : bottomStartIndex + (i - 1) * Segments + j;
                    int b = (i == 0) ? cylinderBottomIndex + ((j + 1) % Segments) : bottomStartIndex + (i - 1) * Segments + ((j + 1) % Segments);
                    int c = bottomStartIndex + i * Segments + j;
                    int d = bottomStartIndex + i * Segments + ((j + 1) % Segments);

                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);

                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(c);
                }
            }

            Mesh mesh = new Mesh
            {
                name = $"Capsule ({radius}x{height})",
                vertices = [.. vertices],
                triangles = [.. triangles]
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _capsuleMeshCache.Add(new Vector2(radius, height), mesh);

            return mesh;
        }
    }
}
