using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sif
{
    public abstract class SBVHNode
    {
        public AABB Bounds;
        public abstract bool IsLeaf();
        public abstract SBVHNode GetChildNode(int idx);
        public abstract int GetNumChildNodes();
    }

    public class InnerSNode : SBVHNode
    {
        public SBVHNode[] Children = new SBVHNode[2];
        public InnerSNode(AABB bounds, SBVHNode left, SBVHNode right)
        {
            Bounds = bounds;
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
        public int TriBeginIdx;
        public int TriEndIdx;

        public LeafSNode(AABB bounds, int begin, int end)
        {
            Bounds = bounds;
            TriBeginIdx = begin;
            TriEndIdx = end;
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
        public BVHScene Scene;
        public SBVHNode Root;
        public List<int> TriIndices;

        public CPU_SBVHData(BVHScene scene)
        {
            TriIndices = new List<int>();
            Scene = scene;
        }
    }
}
