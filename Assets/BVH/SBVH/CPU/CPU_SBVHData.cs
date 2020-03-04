using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sif
{
    public abstract class SBVHNode
    {
        protected AABB _bounds;
        public AABB Bounds { get => _bounds; set => _bounds = value; }

        public abstract bool IsLeaf();
        public abstract SBVHNode GetChildNode(int idx);
        public abstract int GetNumChildNodes();
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
        public BVHScene Scene { get; set; }
        public SBVHNode Root { get; set; }
        public List<int> TriIndices { get; set; }

        public CPU_SBVHData(BVHScene scene)
        {
            Scene = scene;
            CPU_SBVHBuilder.Build(this);
        }
    }
}
