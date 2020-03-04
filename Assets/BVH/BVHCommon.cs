using System.Collections.Generic;
using UnityEngine;
namespace sif
{
    using BVHTriangle = Vector3Int;
    public struct AABB
    {
        private Vector3 _min;
        private Vector3 _max;

        public Vector3 Min { get => _min; set => _min = value; }
        public Vector3 Max { get => _max; set => _max = value; }

        private AABB(Vector3 min, Vector3 max) { _min = min; _max = max; }

        public static AABB New(Vector3 min, Vector3 max)
        {
            return new AABB(min, max);
        }

        public static AABB New()
        {
            return new AABB(float.MaxValue * Vector3.one, float.MinValue * Vector3.one);
        }

        public Vector3 Center => (_min + _max) * 0.5f;
        public float Area
        {
            get
            {
                var box = _max - _min;
                return (box.x * box.y + box.x * box.z + box.y * box.z) * 2f;
            }
        }

        public float Volume
        {
            get
            {
                var box = _max - _min;
                return box.x * box.y * box.z;
            }
        }
        public void Union(Vector3 p) { _min = Vector3.Min(_min, p); _max = Vector3.Max(_max, p); }
        public void Union(AABB other) { Union(other.Min); Union(other.Max); }

        public void Intersect(AABB other)
        {
            _min = Vector3.Max(_min, other.Min);
            _max = Vector3.Min(_max, other.Max);
        }

        public bool Valid()
        {
            var box = _max - _min;
            return box.x >= 0f && box.y >= 0f && box.z >= 0f;
        }

        public static AABB Union(AABB l, AABB r)
        {
            return l + r;
        }

        public static AABB operator +(AABB l, AABB r)
        {
            AABB res = l;
            res.Union(r);
            return res;
        }
    }

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