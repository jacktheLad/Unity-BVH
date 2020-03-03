using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sif
{
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

    public abstract class SBVHNode
    {
        public abstract bool IsLeaf();
        public abstract SBVHNode GetChildNode(int idx);
        public abstract int GetNumChildNodes();


        protected AABB _bounds;

        public AABB Bounds { get => _bounds; set => _bounds = value; }
    }

    public class InnerSNode : SBVHNode
    {
        public SBVHNode[] Children = new SBVHNode[2];
        public InnerSNode(AABB bounds, SBVHNode left, SBVHNode right)
        {
            _bounds = bounds;
            Children[0] = left;
            Children[1] = right;
        }

        public override SBVHNode GetChildNode(int idx)
        {
            return Children[idx];
        }

        public override int GetNumChildNodes()
        {
            return 2;
        }

        public override bool IsLeaf()
        {
            return false;
        }
    }

    public class LeafSNode : SBVHNode
    {
        private int _triBeginIdx;
        private int _triEndIdx;

        public int TriBeginIdx { get => _triBeginIdx; set => _triBeginIdx = value; }
        public int TriEndIdx { get => _triEndIdx; set => _triEndIdx = value; }

        public LeafSNode(AABB bounds, int begin, int end)
        {
            _bounds = bounds;
            _triBeginIdx = begin;
            _triEndIdx = end;
        }

        public override SBVHNode GetChildNode(int idx)
        {
            return null;
        }

        public override int GetNumChildNodes()
        {
            return 0;
        }

        public override bool IsLeaf()
        {
            return true;
        }
    }


    public class CPU_SBVHData
    {
        private BVHScene _scene;
        private SBVHNode _root;
        List<int> _triIndices; // triangle indices.

        public CPU_SBVHData(BVHScene scene)
        {
            _scene = scene;
            CPU_SBVHBuilder.Build(this);
        }

        public BVHScene Scene { get => _scene; }
        public SBVHNode Root { get => _root; }
        public List<int> TriIndices { get => _triIndices; set => _triIndices = value; }
    }
}
