using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GPU_BVHData : MonoBehaviour
{
    [HideInInspector]
    public List<Vector4> nodes;// 只存储非叶节点的数据，数据包括左右节点的aabb以及索引信息
    [HideInInspector]
    public List<Vector4> woopTris;
    [HideInInspector]
    public List<Vector4> debugTris;
    [HideInInspector]
    public List<int> triIndices;

    Vector4[] _woop = new Vector4[3];
    Vector4[] _debug = new Vector4[3];

    class StackEntry
    {
        public BVHNode node;
        public int idx;

        public StackEntry(BVHNode node, int idx)
        {
            this.node = node;
            this.idx = idx;
        }
    }

    // ZeroFormatter需要无参构造函数
    public GPU_BVHData Generate(CPU_BVHData cpuData)
    {
        nodes = new List<Vector4>();
        woopTris = new List<Vector4>();
        debugTris = new List<Vector4>();
        triIndices = new List<int>();

        Stack<StackEntry> stack = new Stack<StackEntry>();
        stack.Push(new StackEntry(cpuData.root, 0));

        AABB[] cbox = new AABB[2];
        int[] cidx = new int[2];

        Vector4[] zero4 = new Vector4[4] { new Vector4(), new Vector4(), new Vector4(), new Vector4() };
        nodes.AddRange(zero4);

        while (stack.Count > 0)
        {
            var cur = stack.Pop();

            for (int i = 0; i < 2; i++)
            {
                var child = cur.node.GetChildNode(i);
                cbox[i] = child.bounds;

                if (!child.IsLeaf())
                {
                    cidx[i] = nodes.Count /* / nodeOffsetSizeDiv */;

                    stack.Push(new StackEntry(child, nodes.Count));
                    nodes.AddRange(zero4);
                    continue;
                }

                LeafNode leaf = child as LeafNode;
                cidx[i] = ~woopTris.Count;

                for (int j = leaf.triangleStart; j < leaf.triangleEnd; j++)
                {
                    woopifyTri(cpuData, j);

                    woopTris.Add(_woop[0]);
                    woopTris.Add(_woop[1]);
                    woopTris.Add(_woop[2]);

                    debugTris.Add(_debug[0]);
                    debugTris.Add(_debug[1]);
                    debugTris.Add(_debug[2]);

                    triIndices.Add(cpuData.triangles[j]);
                    triIndices.Add(0); // 对其索引，好查找
                    triIndices.Add(0);
                }

                // 用来标记属于该叶节点的三角形末端
                woopTris.Add(new Vector4(1e20f, 0, 0, 0));
                debugTris.Add(new Vector4(1e20f, 0, 0, 0));
                triIndices.Add(0);
            }

            int dstIdx = cur.idx;
            nodes[dstIdx] = new Vector4(cbox[0].min.x, cbox[0].max.x, cbox[0].min.y, cbox[0].max.y);
            nodes[dstIdx + 1] = new Vector4(cbox[1].min.x, cbox[1].max.x, cbox[1].min.y, cbox[1].max.y);
            nodes[dstIdx + 2] = new Vector4(cbox[0].min.z, cbox[0].max.z, cbox[1].min.z, cbox[1].max.z);
            nodes[dstIdx + 3] = new Vector4(cidx[0], cidx[1], 0, 0);
        }

        return this;
    }

    private void woopifyTri(CPU_BVHData bvh, int triIdx)
    {
        // fetch the 3 vertex indices of this triangle
        Vector3Int vtxInds = bvh.scene.triangles[bvh.triangles[triIdx]];
        Vector3 v0 = bvh.scene.vertices[vtxInds.x];
        Vector3 v1 = bvh.scene.vertices[vtxInds.y];
        Vector3 v2 = bvh.scene.vertices[vtxInds.z];

        // regular triangles (for debugging only)
        _debug[0] = new Vector4(v0.x, v0.y, v0.z, 0.0f);
        _debug[1] = new Vector4(v1.x, v1.y, v1.z, 0.0f);
        _debug[2] = new Vector4(v2.x, v2.y, v2.z, 0.0f);

        Matrix4x4 mtx = new Matrix4x4();
        // compute edges and transform them with a matrix 
        mtx.SetColumn(0, MathUtils.Swizzle(v0 - v2)); // sets matrix column 0 equal to a Vec4f(Vec3f, 0.0f )
        mtx.SetColumn(1, MathUtils.Swizzle(v1 - v2, 0.0f));
        mtx.SetColumn(2, MathUtils.Swizzle(Vector3.Cross(v0 - v2, v1 - v2), 0.0f));
        mtx.SetColumn(3, MathUtils.Swizzle(v2, 1.0f));
        mtx = Matrix4x4.Inverse(mtx);

        /// m_woop[3] stores 3 transformed triangle edges
        _woop[0] = new Vector4(mtx[2, 0], mtx[2, 1], mtx[2, 2], -mtx[2, 3]); // elements of 3rd row of inverted matrix
        _woop[1] = mtx.GetRow(0);
        _woop[2] = mtx.GetRow(1);
    }
}

