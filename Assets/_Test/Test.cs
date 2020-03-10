using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sif;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour
{
    public UnityEngine.UI.Text text;
    SBVHNode root;
    BVHScene bvhScene;
    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        bvhScene = BVHHelper.BuildBVHScene();
        var bvhData = new CPU_SBVHData(bvhScene);
        CPU_SBVHBuilder.Build(bvhData);
        root = bvhData.Root;
        sw.Stop();
        string log = "Build successfully, time: " + sw.ElapsedMilliseconds + " ms";
        Debug.Log(log);
        text.text = log;
    }

    void DrawAABB(SBVHNode node, int seed)
    {
        if (node == null)
            return;
        Random.InitState(seed);
        Gizmos.color = new Color( Random.value, Random.value, Random.value, 0.3f);

        Gizmos.DrawCube(node.Bounds.Center , node.Bounds.Max - node.Bounds.Min);
       // Debug.LogError(seed);

        if(!node.IsLeaf())
        {
            DrawAABB(node.GetChildNode(0), seed + 1);
            DrawAABB(node.GetChildNode(1), seed + 1);
        }
    }

    private void OnDrawGizmos()
    {
        //DrawAABB(root, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
