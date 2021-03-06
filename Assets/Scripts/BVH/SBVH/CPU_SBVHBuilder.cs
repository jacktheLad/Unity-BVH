﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BVHTriangle = UnityEngine.Vector3Int;

public class CPU_SBVHBuilder
{
    // 写这么长是因为C#里面值类型（struct）指定默认(非0)初始化值没有更好的方法了。
    public struct PrimitiveRef
    {
        public int triangleIdx;
        public AABB bounds;

        private PrimitiveRef(int triIdx, AABB bounds)
        {
            triangleIdx = triIdx;
            this.bounds = bounds;
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
            float ca = ra.bounds.min[sortDim] + ra.bounds.max[sortDim];
            float cb = rb.bounds.min[sortDim] + rb.bounds.max[sortDim];
            return (ca < cb) ? -1 : (ca > cb) ? 1 : (ra.triangleIdx < rb.triangleIdx) ? -1 : (ra.triangleIdx > rb.triangleIdx) ? 1 : 0;
        }
    }

    /// <summary>
    /// 一个简化的Node，不包含子Node
    /// </summary>
    struct NodeSpec
    {
        public int numRef;
        public AABB bounds;

        // 不能直接new NodeSpec，必须调用此方法，否则Bounds初始化成员都是0
        public static NodeSpec New()
        {
            NodeSpec spec = new NodeSpec();
            spec.bounds = AABB.New();
            return spec;
        }
    }

    struct ObjectSplit
    {
        public float sah;
        public byte dim;
        public int numLeft;
        public AABB leftBounds;
        public AABB rightBounds;
        private ObjectSplit(int nothing)
        {
            sah = float.MaxValue;
            dim = 0;
            numLeft = 0;
            leftBounds = AABB.New();
            rightBounds = AABB.New();
        }

        public static ObjectSplit New()
        {
            return new ObjectSplit(0);
        }
    }

    struct SpatialSplit
    {
        public float sah;
        public byte dim;
        public float pos;
        private SpatialSplit(int nothing)
        {
            sah = float.MaxValue;
            dim = 0;
            pos = 0f;
        }
        public static SpatialSplit New()
        {
            return new SpatialSplit(0);
        }
    }

    struct SpatialBin
    {
        public AABB bounds;
        public int enter;
        public int exit;

        private SpatialBin(int nothing)
        {
            bounds = AABB.New();
            enter = 0;
            exit = 0;
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

    private CPU_BVHData _bvhData;
    private List<PrimitiveRef> _refStack; // A linear stack.
    private float _minOverlap;
    private List<AABB> _rightBounds;
    private SpatialBin[,] _bins = new SpatialBin[3, N_SPATIAL_BINS];
    private int _numDuplicates;
    RefComparer _refComparer = new RefComparer();

    private CPU_SBVHBuilder(CPU_BVHData bvhData)
    {
        _bvhData = bvhData;
        _refStack = new List<PrimitiveRef>();
        _rightBounds = new List<AABB>();

        int rightBoundsCount = Mathf.Max(_bvhData.scene.triangles.Count, N_SPATIAL_BINS) - 1;
        for (int i = 0; i < rightBoundsCount; i++)
        {
            _rightBounds.Add(AABB.New());
        }
    }

    public static void Build(CPU_BVHData bvhData)
    {
        var builder = new CPU_SBVHBuilder(bvhData);
        builder.Build();
    }

    private void Build()
    {
        var triangles = _bvhData.scene.triangles;
        var vetices = _bvhData.scene.vertices;

        var rootSpec = NodeSpec.New();
        rootSpec.numRef = triangles.Count;

        // 遍历所有图元（引用），计算根节点的包围盒
        for (int i = 0; i < rootSpec.numRef; i++)
        {
            var pRef = PrimitiveRef.New();
            pRef.triangleIdx = i;

            // 计算单个图元的包围盒
            for (int j = 0; j < 3; j++)
                pRef.bounds.Union(vetices[triangles[i][j]]);

            rootSpec.bounds.Union(pRef.bounds);

            _refStack.Add(pRef);
        }

        // 最小重叠面积，只有重叠面积大于这个值时才考虑进行spatial split
        _minOverlap = rootSpec.bounds.Area * SPLIT_ALPHA;

        // 递归创建BVH
        _bvhData.root = BuildNodeRecursively(rootSpec, 0);
        Debug.Log("Build Completely.");
    }

    private BVHNode BuildNodeRecursively(NodeSpec spec, int depth)
    {
        // 节点只有一个图元的时候没必要再继续分割
        if (spec.numRef <= MIN_LEAF_SIZE || depth >= MAX_DEPTH)
            return CreatLeaf(spec);

        // 挑选使用object split还是spatial split
        float leafSAH = spec.bounds.Area * spec.numRef;
        float nodeSAH = spec.bounds.Area * 0.125f;//spec.Bounds.Area * 2; // 节点遍历的固定开销，2是个经验值（不一定是最好的）
        ObjectSplit objectSplit = FindObjectSplit(spec, nodeSAH);
        SpatialSplit spatialSplit = SpatialSplit.New();
        if (depth < MAX_SPATIAL_DEPTH)
        {
            var overlap = objectSplit.leftBounds;
            overlap.Intersect(objectSplit.rightBounds);

            if (overlap.Area >= _minOverlap)
                spatialSplit = FindSpatialSplit(spec, nodeSAH);
        }

        // 叶节点胜出，不论是Object还是Spatial slpit，分割后的
        float minSAH = Mathf.Min(Mathf.Min(leafSAH, objectSplit.sah), spatialSplit.sah);
        if (minSAH == leafSAH && spec.numRef <= MAX_LEAF_SIZE)
            return CreatLeaf(spec);

        // spatial split胜出，尝试执行spatial split
        NodeSpec left = NodeSpec.New();
        NodeSpec right = NodeSpec.New();
        if (minSAH == spatialSplit.sah)
            PerformSpatialSplit(ref left, ref right, spec, spatialSplit);

        // objcet split胜出，或spatial split并未取得实质性进展，执行object split
        if (left.numRef == 0 || right.numRef == 0)
            PerformObjectSplit(ref left, ref right, spec, objectSplit);

        _numDuplicates += left.numRef + right.numRef - spec.numRef;

        // 由于后文取下标的方式，一定是先右后左
        var rightNode = BuildNodeRecursively(right, depth + 1);
        var leftNode = BuildNodeRecursively(left, depth + 1);

        return new InnerNode(spec.bounds, leftNode, rightNode);
    }

    private ObjectSplit FindObjectSplit(NodeSpec spec, float nodeSAH)
    {
        ObjectSplit split = ObjectSplit.New();
        int refIdx = _refStack.Count - spec.numRef; // CreateLeaf以后_refStack发生了变化
        for (byte dim = 0; dim < 3; dim++)
        {
            _refComparer.sortDim = dim;
            _refStack.Sort(refIdx, spec.numRef, _refComparer);

            // 从右到左，记录每一种可能的分割后，处在“右边”包围盒的
            AABB rightBounds = AABB.New();

            for (int i = spec.numRef - 1; i > 0; i--)
            {
                rightBounds.Union(_refStack[refIdx + i].bounds);
                _rightBounds[i - 1] = rightBounds; // 每一个都记录下来，后面才能比较
            }

            // 从左到右尝试分割，比较计算得到最佳SAH
            AABB leftBounds = AABB.New();
            for (int i = 1; i < spec.numRef; i++)
            {
                leftBounds.Union(_refStack[refIdx + i - 1].bounds);
                float sah = nodeSAH + leftBounds.Area * i/*左边有i个图元*/ + _rightBounds[i - 1].Area * (spec.numRef - i);
                if (sah < split.sah)
                {
                    split.sah = sah;
                    split.dim = dim;
                    split.numLeft = i;
                    split.leftBounds = leftBounds;
                    split.rightBounds = _rightBounds[i - 1];
                }
            }
        }

        return split;
    }

    private SpatialSplit FindSpatialSplit(NodeSpec spec, float nodeSAH)
    {
        // _bins变量每一次分割都被复用
        var origin = spec.bounds.min;
        var binSize = (spec.bounds.max - origin) / N_SPATIAL_BINS;
        var invBinSize = new Vector3(1f / binSize.x, 1f / binSize.y, 1f / binSize.z);

        for (int dim = 0; dim < 3; dim++)
            for (int i = 0; i < N_SPATIAL_BINS; i++)
                _bins[dim, i] = SpatialBin.New();

        // 把图元分配到3个维度的bin中
        for (int refIdx = _refStack.Count - spec.numRef; refIdx < _refStack.Count; refIdx++)
        {
            var pRef = _refStack[refIdx];
            // ....Vector3Int.FloorToInt 误用了 celling...查半天。。。。
            var firstBin = MathUtils.ClampV3Int(Vector3Int.FloorToInt((pRef.bounds.min - origin).Multiply(invBinSize)), Vector3Int.zero, new Vector3Int(N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1));
            var lastBin = MathUtils.ClampV3Int(Vector3Int.FloorToInt((pRef.bounds.max - origin).Multiply(invBinSize)), firstBin, new Vector3Int(N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1, N_SPATIAL_BINS - 1));

            for (int dim = 0; dim < 3; dim++)
            {
                var curRef = pRef;
                // 从左到右分割,curRef并不更新图元索引，只更新包围盒
                for (int i = firstBin[dim]; i < lastBin[dim]; i++)
                {
                    PrimitiveRef leftRef, rightRef;
                    SplitReference(out leftRef, out rightRef, curRef, dim, origin[dim] + binSize[dim] * (i + 1));
                    _bins[dim, i].bounds.Union(leftRef.bounds);
                    curRef = rightRef;
                }

                _bins[dim, lastBin[dim]].bounds.Union(curRef.bounds); // 分割后图元最右边的包围盒也算进来
                                                                      // 只对分割后图元所在的第一个和最后一个bin添加图元引用计数
                _bins[dim, firstBin[dim]].enter++;
                _bins[dim, lastBin[dim]].exit++;
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
                rightBounds.Union(_bins[dim, i].bounds);
                _rightBounds[i - 1] = rightBounds; //_rightBounds用来临时记录右边包围盒的，被复用
            }

            AABB leftBounds = AABB.New();
            int leftNum = 0;
            int rightNum = spec.numRef;
            for (int i = 1; i < N_SPATIAL_BINS; i++)
            {
                leftBounds.Union(_bins[dim, i - 1].bounds);
                leftNum += _bins[dim, i - 1].enter;
                rightNum -= _bins[dim, i - 1].exit;

                float sah = nodeSAH + leftBounds.Area * leftNum + _rightBounds[i - 1].Area * rightNum;
                if (sah < split.sah)
                {
                    split.sah = sah;
                    split.dim = dim;
                    split.pos = origin[dim] + binSize[dim] * i;
                }
            }
        }

        return split;
    }

    private void SplitReference(out PrimitiveRef leftRef, out PrimitiveRef rightRef, PrimitiveRef curRef, int dim, float pos)
    {
        leftRef = rightRef = PrimitiveRef.New();
        leftRef.triangleIdx = rightRef.triangleIdx = curRef.triangleIdx;

        var triangle = _bvhData.scene.triangles[curRef.triangleIdx];
        var vertices = _bvhData.scene.vertices;

        // 遍历三角形的三条边01,12,20,然后将顶点与分割平面组成包围盒
        for (byte i = 0; i < 3; i++)
        {
            var v0 = vertices[triangle[i]];
            var v1 = vertices[triangle[i + 1 == 3 ? 0 : i + 1]];
            float v0p = v0[dim];
            float v1p = v1[dim];

            if (v0p <= pos)
                leftRef.bounds.Union(v0);
            if (v0p >= pos)
                rightRef.bounds.Union(v0);

            // 求得分割平面与三角形的交点，且算进左右两边的包围盒
            if ((v0p < pos && v1p > pos) || (v0p > pos && v1p < pos))
            {
                Vector3 t = Vector3.Lerp(v0, v1, Mathf.Clamp((pos - v0p) / (v1p - v0p), 0.0f, 1.0f));
                leftRef.bounds.Union(t);
                rightRef.bounds.Union(t);
            }
        }

        leftRef.bounds.max[dim] = pos;
        rightRef.bounds.min[dim] = pos;
        // 上面得到的是图元（三角形）被分割后左右两边的包围盒，但我们希望得到的包围盒除此之外，还应该限制在分割平面左右两个bin中
        leftRef.bounds.Intersect(curRef.bounds);
        rightRef.bounds.Intersect(curRef.bounds);
    }

    private void PerformObjectSplit(ref NodeSpec left, ref NodeSpec right, NodeSpec spec, ObjectSplit split)
    {
        int refIdx = _refStack.Count - spec.numRef;
        _refComparer.sortDim = split.dim;
        _refStack.Sort(refIdx, spec.numRef, _refComparer);

        left.numRef = split.numLeft;
        left.bounds = split.leftBounds;
        right.numRef = spec.numRef - split.numLeft;
        right.bounds = split.rightBounds;
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
        int leftStart = refs.Count - spec.numRef;
        int leftEnd = leftStart;
        int rightStart = refs.Count;
        left.bounds = right.bounds = AABB.New();

        // 处理完全只在分割平面某一侧的图元
        for (int i = leftEnd; i < rightStart; i++)
        {
            // 完全在左边的往左边放
            if (refs[i].bounds.max[split.dim] <= split.pos)
            {
                left.bounds.Union(refs[i].bounds);
                refs.Swap(i, leftEnd++);
            }
            // 完全在右边的往右边放
            else if (refs[i].bounds.min[split.dim] >= split.pos)
            {
                right.bounds.Union(refs[i].bounds);
                refs.Swap(i--, --rightStart);
            }
        }

        // 处理被分割平面分开了的图元，可能被划分在左侧或右侧，或者同时划分在两侧
        while (leftEnd < rightStart)
        {
            // 初步分割引用
            PrimitiveRef lref, rref;
            SplitReference(out lref, out rref, refs[leftEnd], split.dim, split.pos);

            AABB lub = left.bounds;  // 不分割，完全划分到【左】侧时的包围盒,left unsplit bounds
            AABB rub = right.bounds; // 不分割，完全划分到【右】侧时的包围盒
            AABB ldb = left.bounds;  // 分割时划分到【左】侧的包围盒,left duplicate bounds
            AABB rdb = right.bounds; // 分割时划分到【右】侧的包围盒
            lub.Union(refs[leftEnd].bounds);
            rub.Union(refs[leftEnd].bounds);
            ldb.Union(lref.bounds);
            rdb.Union(rref.bounds);

            float lac = leftEnd - leftStart;
            float rac = refs.Count - rightStart;
            float lbc = leftEnd - leftStart + 1;
            float rbc = refs.Count - rightStart + 1;

            float unsplitLeftSAH = lub.Area * lbc + right.bounds.Area * rac;
            float unsplitRightSAH = left.bounds.Area * lac + rub.Area * rbc;
            float duplicateSAH = ldb.Area * lbc + rdb.Area * rbc;
            float minSAH = Mathf.Min(Mathf.Min(unsplitLeftSAH, unsplitRightSAH), duplicateSAH);

            // 整个图元划分到左侧
            if (minSAH == unsplitLeftSAH)
            {
                left.bounds = lub;
                leftEnd++;
            }
            // 整个图元划分到右侧
            else if (minSAH == unsplitRightSAH)
            {
                right.bounds = rub;
                refs.Swap(leftEnd, --rightStart);
            }
            // 同时划分到两侧
            else
            {
                left.bounds = ldb;
                right.bounds = rdb;
                refs[leftEnd++] = lref;
                refs.Add(rref);
            }
        }

        left.numRef = leftEnd - leftStart;
        right.numRef = refs.Count - rightStart;
    }

    BVHNode CreatLeaf(NodeSpec spec)
    {
        for (int i = 0; i < spec.numRef; i++)
        {
            var end = _refStack.Count - 1;
            var pRef = _refStack[end];
            _bvhData.triIndices.Add(pRef.triangleIdx);
            _refStack.RemoveAt(end);
        }

        return new LeafNode(spec.bounds, _bvhData.triIndices.Count - spec.numRef, _bvhData.triIndices.Count);
    }
}