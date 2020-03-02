using System.Collections.Generic;
using UnityEngine;
namespace sif
{
    using BVHTriangle = Vector3Int;
    public class BVHScene
    {
        List<BVHTriangle> _triangles;
        List<Vector3> _vertices;

        public List<BVHTriangle> Triangles { get => _triangles; }
        public List<Vector3> Vertices { get => _vertices; }

        public BVHScene(List<BVHTriangle> tris, List<Vector3> verts)
        {
            _triangles = tris;
            _vertices = verts;
        }

    }
}