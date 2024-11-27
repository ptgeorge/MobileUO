using ClassicUO.Renderer;
using UnityEngine;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class MeshHolder
    {
        public readonly Mesh Mesh;

        private readonly UnityEngine.Vector3[] vertices;
        private readonly UnityEngine.Vector2[] uvs;
        private readonly UnityEngine.Vector3[] normals;

        public MeshHolder(int quadCount)
        {
            Mesh = new Mesh();
            //Mesh.MarkDynamic();

            quadCount = Mathf.NextPowerOfTwo(quadCount);
            int vCount = quadCount * 4;

            vertices = new UnityEngine.Vector3[vCount];
            uvs = new UnityEngine.Vector2[vCount];
            normals = new UnityEngine.Vector3[vCount];

            var triangles = new int[quadCount * 6];
            for (var i = 0; i < quadCount; i++)
            {
                /*
                 *  TL    TR
                 *   0----1 0,1,2,3 = index offsets for vertex indices
                 *   |   /| TL,TR,BL,BR are vertex references in SpriteBatchItem.
                 *   |  / |
                 *   | /  |
                 *   |/   |
                 *   2----3
                 *  BL    BR
                 */
                // Triangle 1
                triangles[i * 6] = i * 4;
                triangles[i * 6 + 1] = i * 4 + 1;
                triangles[i * 6 + 2] = i * 4 + 2;
                // Triangle 2
                triangles[i * 6 + 3] = i * 4 + 1;
                triangles[i * 6 + 4] = i * 4 + 3;
                triangles[i * 6 + 5] = i * 4 + 2;
            }

            Mesh.vertices = vertices;
            Mesh.uv = uvs;
            Mesh.triangles = triangles;
            Mesh.normals = normals;
        }

        internal UnityEngine.Vector3 ConvertToUnity(Vector3 xnaVector)
        {
            return new UnityEngine.Vector3(xnaVector.X, xnaVector.Y, xnaVector.Z);
        }

        internal UnityEngine.Vector2 ConvertToUnity(Vector2 xnaVector)
        {
            return new UnityEngine.Vector2(xnaVector.X, xnaVector.Y);
        }

        internal void Populate(UltimaBatcher2D.PositionNormalTextureColor4 vertex)
        {
            vertex.TextureCoordinate0.Y = 1 - vertex.TextureCoordinate0.Y;
            vertices[0] = ConvertToUnity(vertex.Position0);
            uvs[0] = ConvertToUnity(vertex.TextureCoordinate0);
            normals[0] = ConvertToUnity(vertex.Normal0);

            vertex.TextureCoordinate1.Y = 1 - vertex.TextureCoordinate1.Y;
            vertices[1] = ConvertToUnity(vertex.Position1);
            uvs[1] = ConvertToUnity(vertex.TextureCoordinate1);
            normals[1] = ConvertToUnity(vertex.Normal1);

            vertex.TextureCoordinate2.Y = 1 - vertex.TextureCoordinate2.Y;
            vertices[2] = ConvertToUnity(vertex.Position2);
            uvs[2] = ConvertToUnity(vertex.TextureCoordinate2);
            normals[2] = ConvertToUnity(vertex.Normal2);

            vertex.TextureCoordinate3.Y = 1 - vertex.TextureCoordinate3.Y;
            vertices[3] = ConvertToUnity(vertex.Position3);
            uvs[3] = ConvertToUnity(vertex.TextureCoordinate3);
            normals[3] = ConvertToUnity(vertex.Normal3);

            Mesh.vertices = vertices;
            Mesh.uv = uvs;
            Mesh.normals = normals;
        }
    }
}
