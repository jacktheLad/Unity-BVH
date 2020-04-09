using System.Collections.Generic;
using UnityEngine;

using BVHTriangle = UnityEngine.Vector3Int;
public struct AABB
{
    public Vector3 min;
    public Vector3 max;

    private AABB(Vector3 min, Vector3 max) { this.min = min; this.max = max; }

    public static AABB New(Vector3 min, Vector3 max)
    {
        return new AABB(min, max);
    }

    public static AABB New()
    {
        return new AABB(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
            new Vector3(float.MinValue, float.MinValue, float.MinValue));
    }

    public Vector3 Center => (min + max) * 0.5f;
    public float Area
    {
        get
        {
            if (!Valid) return 0;
            var box = max - min;
            return (box.x * box.y + box.x * box.z + box.y * box.z) * 2f;
        }
    }

    public float Volume
    {
        get
        {
            var box = max - min;
            return box.x * box.y * box.z;
        }
    }
    public void Union(Vector3 p) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
    // 两个都无效的AABB Union会有bug
    public void Union(AABB other) { Union(other.min); Union(other.max); }
    // 其中一个AABB为无效的会有bug
    public void Intersect(AABB other)
    {
        min = Vector3.Max(min, other.min);
        max = Vector3.Min(max, other.max);
    }

    public bool Valid
    {
        get
        {
            var box = max - min;
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
    public List<BVHTriangle> triangles;
    public List<Vector3> vertices;

    public BVHScene(List<BVHTriangle> tris, List<Vector3> verts)
    {
        triangles = tris;
        vertices = verts;
    }

}

public abstract class BVHNode
{
    public AABB bounds;
    public abstract bool IsLeaf();
    public abstract BVHNode GetChildNode(int idx);
    public abstract int GetNumChildNodes();
}

public class InnerNode : BVHNode
{
    public BVHNode[] children = new BVHNode[2];
    public InnerNode(AABB bounds, BVHNode left, BVHNode right)
    {
        base.bounds = bounds;
        children[0] = left;
        children[1] = right;
    }

    public override BVHNode GetChildNode(int idx)
    {
        return children[idx];
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

public class LeafNode : BVHNode
{
    public int triangleStart;
    public int triangleEnd;

    public LeafNode(AABB bounds, int begin, int end)
    {
        base.bounds = bounds;
        triangleStart = begin;
        triangleEnd = end;
    }

    public override BVHNode GetChildNode(int idx)
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


public class CPU_BVHData
{
    public BVHScene scene;
    public BVHNode root;
    public List<int> triangles;

    public CPU_BVHData(BVHScene scene)
    {
        triangles = new List<int>();
        this.scene = scene;
    }
}