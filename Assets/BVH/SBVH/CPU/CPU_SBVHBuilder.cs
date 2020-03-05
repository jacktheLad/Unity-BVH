using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sif
{
    using BVHTriangle = Vector3Int;
    public class CPU_SBVHBuilder
    {
        // 写这么长是因为C#里面值类型（struct）指定默认(非0)初始化值没有更好的方法了。
        struct PrimitiveRef
        {
            private int _triangleIdx;
            private AABB _bounds;

            public int TriangleIdx { get => _triangleIdx; set => _triangleIdx = value; }
            public AABB Bounds { get => _bounds; set => _bounds = value; }

            private PrimitiveRef(int triIdx, AABB bounds)
            {
                _triangleIdx = triIdx;
                _bounds = bounds;
            }

            public static PrimitiveRef New()
            {
                return new PrimitiveRef(-1, AABB.New());
            }
        }

        /// <summary>
        /// 一个简化的Node，不包含子Node
        /// </summary>
        struct NodeSpec
        {
            public int NumRef { get; set; }
            public AABB Bounds { get; set; }
        }

        struct ObjectSplit
        {
            public float SAH { get; set; }
            public byte Dim { get; set; }
            public int NumLeftRef { get; set; }
            public AABB LeftBounds { get; set; }
            public AABB RightBounds { get; set; }
            private ObjectSplit(int nothing)
            {
                SAH = float.MaxValue;
                Dim = 0;
                NumLeftRef = 0;
                LeftBounds = AABB.New();
                RightBounds = AABB.New();
            }

            public static ObjectSplit New()
            {
                return new ObjectSplit(0);
            }
        }

        struct SpatialSplit
        {
            public float SAH { get; set; }
            public byte Dim { get; set; }
            public float Pos { get; set; }
            private SpatialSplit(int nothing)
            {
                SAH = float.MaxValue;
                Dim = 0;
                Pos = 0f;
            }
            public static SpatialSplit New()
            {
                return new SpatialSplit(0);
            }
        }

        struct SpatialBin
        {
            public AABB Bounds { get; set; }
            public int Enter { get; set; }
            public int Exit { get; set; }

            private SpatialBin(int nothing)
            {
                Bounds = AABB.New();
                Enter = 0;
                Exit = 0;
            }
            public static SpatialBin New()
            {
                return new SpatialBin(0);
            }
        }

        private const float SPLIT_ALPHA = 1E-5F;
        public static int N_SPATIAL_BINS = 32;
        public static int MAX_DEPTH = 64;
        public static int MAX_SPATIAL_DEPTH = 48;
        public static int MIN_LEAF_SIZE = 1;
        public static int MAX_LEAF_SIZE = 0x7FFFFFF;

        private CPU_SBVHData _bvhData;
        private List<PrimitiveRef> _refStack; // A linear stack.
        private float _minOverlap;
        private List<AABB> _rightBounds;
        private byte _sortDim;
        private SpatialBin[,] _bins = new SpatialBin[3, N_SPATIAL_BINS];
        private int _numDuplicates;

        private CPU_SBVHBuilder(CPU_SBVHData bvhData)
        {
            _bvhData = bvhData;
        }

        public static void Build(CPU_SBVHData bvhData)
        {
            var builder = new CPU_SBVHBuilder(bvhData);
            builder.Build();
        }

        private void Build()
        {
            var triangles = _bvhData.Scene.Triangles;
            var vetices = _bvhData.Scene.Vertices;

            var rootSpec = new NodeSpec();
            rootSpec.NumRef = triangles.Count;
            _refStack = new List<PrimitiveRef>();

            // 遍历所有图元（引用），计算根节点的包围盒
            for (int i = 0; i < rootSpec.NumRef; i++)
            {
                var pRef = PrimitiveRef.New();
                pRef.TriangleIdx = i;

                // 计算单个图元的包围盒
                for (int j = 0; j < 3; j++)
                    pRef.Bounds.Union(vetices[triangles[i][j]]);

                rootSpec.Bounds.Union(pRef.Bounds);

                _refStack.Add(pRef);
            }

            // 最小重叠面积，只有重叠面积大于这个值时才考虑进行spatial split
            _minOverlap = rootSpec.Bounds.Area * SPLIT_ALPHA;

            // 递归创建BVH
            _bvhData.Root = BuildNodeRecursively(rootSpec, 0);
        }

        private SBVHNode BuildNodeRecursively(NodeSpec spec, int depth)
        {
            // 节点只有一个图元的时候没必要再继续分割
            if (spec.NumRef <= MIN_LEAF_SIZE || depth >= MAX_DEPTH)
                return CreatLeaf(spec);

            // 挑选使用object split还是spatial split
            float leafSAH = spec.Bounds.Area * spec.NumRef;
            float nodeSAH = spec.Bounds.Area * 2; // 节点遍历的固定开销，2是个经验值（不一定是最好的）
            ObjectSplit objectSplit = FindObjectSplit(spec, nodeSAH);
            SpatialSplit spatialSplit = SpatialSplit.New();
            if (depth < MAX_SPATIAL_DEPTH)
            {
                var overlap = objectSplit.LeftBounds;
                overlap.Intersect(objectSplit.RightBounds);

                if (overlap.Area >= _minOverlap)
                    spatialSplit = FindSpatialSplit(spec, nodeSAH);
            }

            // 叶节点胜出
            float minSAH = Mathf.Min(Mathf.Min(leafSAH, objectSplit.SAH), spatialSplit.SAH);
            if (minSAH == leafSAH && spec.NumRef <= MAX_LEAF_SIZE)
                return CreatLeaf(spec);

            // spatial split胜出，尝试执行spatial split
            NodeSpec left = new NodeSpec();
            NodeSpec right = new NodeSpec();
            if (minSAH == spatialSplit.SAH)
                PerformSpatialSplit(left, right, spec, spatialSplit);

            // objcet split胜出，或spatial split并未取得实质性进展，执行object split
            if (left.NumRef == 0 || right.NumRef == 0)
                PerformObjectSplit(left, right, spec, objectSplit);

            _numDuplicates += left.NumRef + right.NumRef - spec.NumRef;

            var leftNode = BuildNodeRecursively(left, depth + 1);
            var rightNode = BuildNodeRecursively(right, depth + 1);

            return new InnerSNode(spec.Bounds, leftNode, rightNode);
        }

        private ObjectSplit FindObjectSplit(NodeSpec spec, float nodeSAH)
        {
            throw new NotImplementedException();
        }

        private SpatialSplit FindSpatialSplit(NodeSpec spec, float nodeSAH)
        {
            throw new NotImplementedException();
        }

        private void PerformObjectSplit(NodeSpec left, NodeSpec right, NodeSpec spec, ObjectSplit objectSplit)
        {
            throw new NotImplementedException();
        }

        private void PerformSpatialSplit(NodeSpec left, NodeSpec right, NodeSpec spec, SpatialSplit spatialSplit)
        {
            throw new NotImplementedException();
        }

        SBVHNode CreatLeaf(NodeSpec spec)
        {
            for (int i = 0; i < spec.NumRef; i++)
            {
                var end = _refStack.Count - 1;
                var pRef = _refStack[end];
                _bvhData.TriIndices.Add(pRef.TriangleIdx);
                _refStack.RemoveAt(end);
            }

            return new LeafSNode(spec.Bounds, _bvhData.TriIndices.Count - spec.NumRef, _bvhData.TriIndices.Count);
        }
    }
}

