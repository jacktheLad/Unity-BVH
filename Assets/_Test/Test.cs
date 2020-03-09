using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sif;
using UnityEditor;
using System.IO;


public class Test : MonoBehaviour
{
    public static

    SBVHNode root;
    BVHScene bvhScene;
    // Start is called before the first frame update
    void Start()
    {
        bvhScene = BVHHelper.BuildBVHScene();
        var bvhData = new CPU_SBVHData(bvhScene);
        CPU_SBVHBuilder.Build(bvhData);
        root = bvhData.Root;
    }

    void DrawAABB(SBVHNode node, int seed)
    {
        if (node == null)
            return;
        Random.InitState(node.GetHashCode());
        Gizmos.color = new Color( Random.value, Random.value, Random.value, 0.3f);

        Gizmos.DrawCube(node.Bounds.Center , node.Bounds.Max - node.Bounds.Min);
       // Debug.LogError(seed);

        if(!node.IsLeaf())
        {
            DrawAABB(node.GetChildNode(0), seed + 1);
            DrawAABB(node.GetChildNode(1), seed + 1);
        }
    }

    void DrawAABB(AABB ab)
    {
        Random.InitState(ab.GetHashCode());
        Gizmos.color = new Color(Random.value, Random.value, 1, 0.5f);
        Gizmos.DrawCube(ab.Center, ab.Max - ab.Min);
    }

    private void OnDrawGizmos()
    {
        DrawAABB(root, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
