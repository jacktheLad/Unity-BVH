using System.Collections.Generic;
using UnityEngine;
namespace sif
{
    using BVHTriangle = Vector3Int;
    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        private AABB(Vector3 min, Vector3 max) { Min = min; Max = max; }

        public static AABB New(Vector3 min, Vector3 max)
        {
            return new AABB(min, max);
        }

        public static AABB New()
        {
            return new AABB(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                new Vector3(float.MinValue, float.MinValue, float.MinValue));
        }

        public Vector3 Center => (Min + Max) * 0.5f;
        public float Area
        {
            get
            {
                if (!Valid) return 0;
                var box = Max - Min;
                return (box.x * box.y + box.x * box.z + box.y * box.z) * 2f;
            }
        }

        public float Volume
        {
            get
            {
                var box = Max - Min;
                return box.x * box.y * box.z;
            }
        }
        public void Union(Vector3 p) { Min = Vector3.Min(Min, p); Max = Vector3.Max(Max, p); }
        // 两个都无效的AABB Union会有问题
        public void Union(AABB other) { Union(other.Min); Union(other.Max); }
        // 其中一个AABB为无效的会有问题
        public void Intersect(AABB other)
        {
            Min = Vector3.Max(Min, other.Min);
            Max = Vector3.Min(Max, other.Max);
        }

        public bool Valid
        {
            get
            {
                var box = Max - Min;
                return box.x >= 0f && box.y >= 0f && box.z >= 0f;
            }
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
        public List<BVHTriangle> Triangles;
        public List<Vector3> Vertices;

        public BVHScene(List<BVHTriangle> tris, List<Vector3> verts)
        {
            Triangles = tris;
            Vertices = verts;
        }

    }
}