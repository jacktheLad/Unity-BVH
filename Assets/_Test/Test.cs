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
    int i = 0;
    void DrawAABB(SBVHNode node)
    {
        if (node == null)
            return;
        Random.InitState(node.GetHashCode());
        Gizmos.color = new Color( Random.value, Random.value, 0 ,0.5f);


            Gizmos.DrawCube(node.Bounds.Center, node.Bounds.Max - node.Bounds.Min);

        if(!node.IsLeaf())
        {
            DrawAABB(node.GetChildNode(0));
            DrawAABB(node.GetChildNode(1));
        }
    }

    private void OnDrawGizmos()
    {
        i = 0;
        DrawAABB(root);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
