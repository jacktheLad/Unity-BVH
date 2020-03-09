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
        public struct PrimitiveRef
        {
            public int TriangleIdx;
            public AABB Bounds;

            private PrimitiveRef(int triIdx, AABB bounds)
            {
                TriangleIdx = triIdx;
                Bounds = bounds;
            }

            public static PrimitiveRef New()
            {
                return new PrimitiveRef(-1, AABB.New());
            }
        }

        public class RefComparer : IComparer<PrimitiveRef>
        {
            public byte sortDim;

            public int Compare(PrimitiveRef ra, PrimitiveRef rb)
            {
                // 比较当前所处维度的中点位置，根据中点位置进行排序
                float ca = ra.Bounds.Min[sortDim] + ra.Bounds.Max[sortDim];
                float cb = rb.Bounds.Min[sortDim] + rb.Bounds.Max[sortDim];
                return (ca < cb) ? -1 : (ca > cb) ? 1 : (ra.TriangleIdx < rb.TriangleIdx) ? -1 : (ra.TriangleIdx > rb.TriangleIdx) ? 1 : 0;
            }
        }

        /// <summary>
        /// 一个简化的Node，不包含子Node
        /// </summary>
        struct NodeSpec
        {
            public int NumRef;
            public AABB Bounds;

            // 不能直接new NodeSpec，必须调用此方法，否则Bounds初始化成员都是0
            public static NodeSpec New()
            {
                NodeSpec spec = new NodeSpec();
                spec.Bounds = AABB.New();
                return spec;
            }
        }

        struct ObjectSplit
        {
            public float SAH;
            public byte Dim;
            public int NumLeftRef;
            public AABB LeftBounds;
            public AABB RightBounds;
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
            public float SAH;
            public byte Dim;
            public float Pos;
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
            public AABB Bounds;
            public int Enter;
            public int Exit;

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
        RefComparer _refComparer = new RefComparer();

        private CPU_SBVHBuilder(CPU_SBVHData bvhData)
        {
            _bvhData = bvhData;
            _refStack = new List<PrimitiveRef>();
            _rightBounds = new List<AABB>();

            int rightBoundsCount = Mathf.Max(_bvhData.Scene.Triangles.Count, N_SPATIAL_BINS) - 1;
            for (int i = 0; i < rightBoundsCount; i++)
            {
                _rightBounds.Add(AABB.New());
            }
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

            var rootSpec = NodeSpec.New();
            rootSpec.NumRef = triangles.Count;

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
            Debug.Log("Build Completely.");
        }

        private SBVHNode BuildNodeRecursively(NodeSpec spec, int depth)
        {
            // 节点只有一个图元的时候没必要再继续分割
            if (spec.NumRef <= MIN_LEAF_SIZE || depth >= MAX_DEPTH)
                return CreatLeaf(spec);

            if (spec.NumRef > 1000)
                Debug.LogError(">1000 = " + spec.NumRef);

            // 挑选使用object split还是spatial split
            float leafSAH = spec.Bounds.Area * spec.NumRef;
            float nodeSAH = spec.Bounds.Area * 0.125f;//spec.Bounds.Area * 2; // 节点遍历的固定开销，2是个经验值（不一定是最好的）
            ObjectSplit objectSplit = FindObjectSplit(spec, nodeSAH);
            SpatialSplit spatialSplit = SpatialSplit.New();
            if (depth < MAX_SPATIAL_DEPTH)
            {
                var overlap = objectSplit.LeftBounds;
                overlap.Intersect(objectSplit.RightBounds);

                if (overlap.Area >= _minOverlap)
                    spatialSplit = FindSpatialSplit(spec, nodeSAH);
            }

            // 叶节点胜出，不论是Object还是Spatial slpit，分割后的
            float minSAH = Mathf.Min(Mathf.Min(leafSAH, objectSplit.SAH), spatialSplit.SAH);
            if (minSAH == leafSAH && spec.NumRef <= MAX_LEAF_SIZE)
                return CreatLeaf(spec);

            // spatial split胜出，尝试执行spatial split
            NodeSpec left = NodeSpec.New();
            NodeSpec right = NodeSpec.New();
            if (minSAH == spatialSplit.SAH)
                PerformSpatialSplit(ref left, ref right, spec, spatialSplit);

            // objcet split胜出，或spatial split并未取得实质性进展，执行object split
            if (left.NumRef == 0 || right.NumRef == 0)
                PerformObjectSplit(ref left, ref right, spec, objectSplit);

            _numDuplicates += left.NumRef + right.NumRef - spec.NumRef;

            // 由于后文取下标的方式，一定是先右后左
            var rightNode = BuildNodeRecursively(right, depth + 1);
            var leftNode = BuildNodeRecursively(left, depth + 1);

            return new InnerSNode(spec.Bounds, leftNode, rightNode);
        }

        private ObjectSplit FindObjectSplit(NodeSpec spec, float nodeSAH)
        {
            ObjectSplit split = ObjectSplit.New();
            int refIdx = _refStack.Count - spec.NumRef; // CreateLeaf以后_refStack发生了变化
            for (_sortDim = 0; _sortDim < 3; _sortDim++)
            {
                _refComparer.sortDim = _sortDim;
                _refStack.Sort(refIdx, spec.NumRef, _refComparer);

                // 从右到左，记录每一种可能的分割后，处在“右边”包围盒的
                AABB rightBounds = AABB.New();

                for (int i = spec.NumRef - 1; i > 0; i--)
                {
                    rightBounds.Union(_refStack[refIdx + i].Bounds);
                    _rightBounds[i - 1] = rightBounds; // 每一个都记录下来，后面才能比较
                }

                // 从左到右尝试分割，比较计算得到最佳SAH
                AABB leftBounds = AABB.New();
                for (int i = 1; i < spec.NumRef; i++)
                {
                    leftBounds.Union(_refStack[refIdx + i - 1].Bounds);
                    float sah = nodeSAH + leftBounds.Area * i/*左边有i个图元*/ + _rightBounds[i - 1].Area * (spec.NumRef - i);
                    if (sah < split.SAH)
                    {
                        split.SAH = sah;
                        split.Dim = _sortDim;
                        split.NumLeftRef = i;
                        split.LeftBounds = leftBounds;
                        split.RightBounds = _rightBounds[i - 1];
                    }
                }
            }

            return split;
        }

        private SpatialSplit FindSpatialSplit(NodeSpec spec, float nodeSAH)
        {
            // _bins变量每一次分割都被复用
            var origin = spec.Bounds.Min;
            var binSize = (spec.Bounds.Max - origin) / N_SPATIAL_BINS;
            var invBinSize = new Vector3(1f / binSize.x, 1f / binSize.y, 1f / binSize.z);

            for (int dim = 0; dim < 3; dim++)
                for (int i = 0; i < N_SPATIAL_BINS; i++)
                    _bins[dim, i] = SpatialBin.New();

            // 把图元分配到3个维度的bin中
            for (int refIdx = _refStack.Count - spec.NumRef; refIdx < _refStack.Count; refIdx++)
            {
                var pRef = _refStack[refIdx];
                // ....Vector3Int.FloorToInt 误用了 celling...查半天。。。。
                var firstBin = Utils.ClampV3Int(Vector3Int.FloorToInt((pRef.Bounds.Min - origin).Multiply(invBinSize)), Vector3Int.zero, new Vector3Int(N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1));
                var lastBin = Utils.ClampV3Int(Vector3Int.FloorToInt((pRef.Bounds.Max - origin).Multiply(invBinSize)), firstBin, new Vector3Int(N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1));

                for (int dim = 0; dim < 3; dim++)
                {
                    var curRef = pRef;
                    // 从左到右分割,curRef并不更新图元索引，只更新包围盒
                    for (int i = firstBin[dim]; i < lastBin[dim] ; i++)
                    {
                        PrimitiveRef leftRef, rightRef;
                        SplitReference(out leftRef, out rightRef, curRef, dim, origin[dim] + binSize[dim] * (i + 1));
                        _bins[dim, i].Bounds.Union(leftRef.Bounds);
                        curRef = rightRef;
                    }

                    _bins[dim, lastBin[dim]].Bounds.Union(curRef.Bounds); // 分割后图元最右边的包围盒也算进来
                    // 只对分割后图元所在的第一个和最后一个bin添加图元引用计数
                    _bins[dim, firstBin[dim]].Enter++;
                    _bins[dim, lastBin[dim]].Exit++;
                }
            }

            // 根据分割好的bins，来选择最佳分割平面,跟FindObjectSplit类似,只不过是以bin为单位，而不是图元
            SpatialSplit split = SpatialSplit.New();
            for (byte dim = 0; dim < 3; dim++)
            {
                // 从右到左，记录每一种可能的分割后，处在“右边”包围盒的
                AABB rightBounds = AABB.New();
                for (int i = N_SPATIAL_BINS - 1; i > 0; i--)
                {
                    rightBounds.Union(_bins[dim, i].Bounds);
                    _rightBounds[i - 1] = rightBounds; //_rightBounds用来临时记录右边包围盒的，被复用
                }

                AABB leftBounds = AABB.New();
                int leftNum = 0;
                int rightNum = spec.NumRef;
                for (int i = 1; i < N_SPATIAL_BINS; i++)
                {
                    leftBounds.Union(_bins[dim, i - 1].Bounds);
                    leftNum += _bins[dim, i - 1].Enter;
                    rightNum -= _bins[dim, i - 1].Exit;

                    float sah = nodeSAH + leftBounds.Area * leftNum + _rightBounds[i - 1].Area * rightNum;
                    if (sah < split.SAH)
                    {
                        split.SAH = sah;
                        split.Dim = dim;
                        split.Pos = origin[dim] + binSize[dim] * i;
                    }
                }
            }

            return split;
        }
        static int testi = 0;
        private void SplitReference(out PrimitiveRef leftRef, out PrimitiveRef rightRef, PrimitiveRef curRef, int dim, float pos)
        {
            leftRef = rightRef = PrimitiveRef.New();
            leftRef.TriangleIdx = rightRef.TriangleIdx = curRef.TriangleIdx;

            var triangle = _bvhData.Scene.Triangles[curRef.TriangleIdx];
            var vertices = _bvhData.Scene.Vertices;

            testi++;

            // 遍历三角形的三条边01,12,20,然后将顶点与分割平面组成包围盒
            for (byte i = 0; i < 3; i++)
            {
                var v0 = vertices[triangle[i]];
                var v1 = vertices[triangle[i + 1 == 3 ? 0 : i + 1]];
                float v0p = v0[dim];
                float v1p = v1[dim];

                if (v0p <= pos)
                    leftRef.Bounds.Union(v0);
                if (v0p >= pos)
                    rightRef.Bounds.Union(v0);

                // 求得分割平面与三角形的交点，且算进左右两边的包围盒
                if ((v0p < pos && v1p > pos) || (v0p > pos && v1p < pos))
                {
                    Vector3 t = Vector3.Lerp(v0, v1, Mathf.Clamp((pos - v0p) / (v1p - v0p), 0.0f, 1.0f));
                    leftRef.Bounds.Union(t);
                    rightRef.Bounds.Union(t);
                }
            }
            // ??:原代码里面有下面两句，暂时没设想出是什么情况
            leftRef.Bounds.Max[dim] = pos;
            if (leftRef.Bounds.Max[dim] == float.MinValue)
                Debug.Log(testi);
            if (rightRef.Bounds.Min[dim] == float.MaxValue)
                Debug.Log(testi);
            rightRef.Bounds.Min[dim] = pos;
            // 上面得到的是图元（三角形）被分割后左右两边的包围盒，但我们希望得到的包围盒除此之外，还应该限制在分割平面左右两个bin中
            leftRef.Bounds.Intersect(curRef.Bounds);
            rightRef.Bounds.Intersect(curRef.Bounds);
        }

        private void PerformObjectSplit(ref NodeSpec left, ref NodeSpec right, NodeSpec spec, ObjectSplit split)
        {
            int refIdx = _refStack.Count - spec.NumRef;
            _refComparer.sortDim = _sortDim = split.Dim;
            _refStack.Sort(refIdx, spec.NumRef, _refComparer);

            left.NumRef = split.NumLeftRef;
            left.Bounds = split.LeftBounds;
            right.NumRef = spec.NumRef - split.NumLeftRef;
            right.Bounds = split.RightBounds;
        }

        /// <summary>
        /// 给定分割平面后划分当前节点下的所有图元
        /// </summary>
        private void PerformSpatialSplit(ref NodeSpec left, ref NodeSpec right, NodeSpec spec, SpatialSplit split)
        {
            // 划分在左侧：   [leftStart, leftEnd]
            // 被分割的：     [leftEnd, rightStart]
            // 划分在右侧：   [rightStart, refs.Count]

            var refs = _refStack;
            int leftStart = refs.Count - spec.NumRef;
            int leftEnd = leftStart;
            int rightStart = refs.Count;
            left.Bounds = right.Bounds = AABB.New();

            // 处理完全只在分割平面某一侧的图元
            for (int i = leftEnd; i < rightStart; i++)
            {
                // 完全在左边的往左边放
                if (refs[i].Bounds.Max[split.Dim] <= split.Pos)
                {
                    left.Bounds.Union(refs[i].Bounds);
                    refs.Swap(i, leftEnd++);
                }
                // 完全在右边的往右边放
                else if (refs[i].Bounds.Min[split.Dim] >= split.Pos)
                {
                    right.Bounds.Union(refs[i].Bounds);
                    refs.Swap(i--, --rightStart);
                }
            }

            // 处理被分割平面分开了的图元，可能被划分在左侧或右侧，或者同时划分在两侧
            while (leftEnd < rightStart)
            {
                // 初步分割引用
                PrimitiveRef lref, rref;
                SplitReference(out lref, out rref, refs[leftEnd], split.Dim, split.Pos);

                AABB lub = left.Bounds;  // 不分割，完全划分到【左】侧时的包围盒,left unsplit bounds
                AABB rub = right.Bounds; // 不分割，完全划分到【右】侧时的包围盒
                AABB ldb = left.Bounds;  // 分割时划分到【左】侧的包围盒,left duplicate bounds
                AABB rdb = right.Bounds; // 分割时划分到【右】侧的包围盒
                lub.Union(refs[leftEnd].Bounds);
                rub.Union(refs[leftEnd].Bounds);
                ldb.Union(lref.Bounds);
                rdb.Union(rref.Bounds);

                float lac = leftEnd - leftStart;
                float rac = refs.Count - rightStart;
                float lbc = leftEnd - leftStart + 1;
                float rbc = refs.Count - rightStart + 1;

                float unsplitLeftSAH = lub.Area * lbc + right.Bounds.Area * rac;
                float unsplitRightSAH = left.Bounds.Area * lac + rub.Area * rbc;
                float duplicateSAH = ldb.Area * lbc + rdb.Area * rbc;
                float minSAH = Mathf.Min(Mathf.Min(unsplitLeftSAH, unsplitRightSAH), duplicateSAH);

                // 整个图元划分到左侧
                if (minSAH == unsplitLeftSAH)
                {
                    left.Bounds = lub;
                    leftEnd++;
                }
                // 整个图元划分到右侧
                else if (minSAH == unsplitRightSAH)
                {
                    right.Bounds = rub;
                    refs.Swap(leftEnd, --rightStart);
                }
                // 同时划分到两侧
                else
                {
                    left.Bounds = ldb;
                    right.Bounds = rdb;
                    refs[leftEnd++] = lref;
                    refs.Add(rref);
                }
            }

            left.NumRef = leftEnd - leftStart;
            right.NumRef = refs.Count - rightStart;
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