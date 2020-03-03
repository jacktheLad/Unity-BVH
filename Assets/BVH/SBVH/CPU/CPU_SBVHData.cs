using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sif
{
    public struct AABB
    {

    }

    public abstract class SBVHNode
    {
        public abstract bool IsLeaf();
        public abstract SBVHNode GetChildNode(int idx);
        public abstract int GetNumChildNodes();

        public AABB Bounds;
    }

    public class CPU_SBVHData
    {
        private BVHScene _scene;
        private SBVHNode _root;
        List<int> _indices; // triangle indices.
    }
}
