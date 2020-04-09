using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

// TODO:模型导入默认开启ReadWrite

public class Test : MonoBehaviour
{
    public UnityEngine.UI.Text text;
    BVHNode root;
    BVHScene bvhScene;
    [Range(1,20)]
    public int maxDepth = 20;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        bvhScene = BVHHelper.BuildBVHScene();
        Debug.Log("bvhScene.triangles.Count = " + bvhScene.triangles.Count);

        Debug.Log(" ================================ ");

        var bvhData = new CPU_BVHData(bvhScene);
        CPU_SBVHBuilder.Build(bvhData);

        Debug.Log("bvhData.triangles.Count = " + bvhData.triangles.Count);
        Debug.Log(" ================================ ");
        // debug gpu bvh
        GPU_BVHData gpuData = new GPU_BVHData().Generate(bvhData);
        Debug.Log("gpuData.nodes.Count = " + gpuData.nodes.Count);
        Debug.Log("gpuData.woopTris.Count = " + gpuData.woopTris.Count);
        Debug.Log("gpuData.triIndices.Count = " + gpuData.triIndices.Count);


        root = bvhData.root;
        sw.Stop();
        string log = "Build successfully, time: " + sw.ElapsedMilliseconds + " ms";
        Debug.Log(log);
        text.text = log;
    }

    Queue<BVHNode> nodeQueue = new Queue<BVHNode>();
    void DrawBVHBoundsBFS(BVHNode root)
    {
        nodeQueue.Clear();
        nodeQueue.Enqueue(root);
        nodeQueue.Enqueue(null);
        int depth = 0;
        int nodeCount = 0;
        while (nodeQueue.Count != 0 && depth < maxDepth)
        {
            BVHNode node = nodeQueue.Dequeue();

            if (node == null)
            {
                if (nodeQueue.Count == 0)
                    break;

                depth++;
                nodeQueue.Enqueue(null);
                continue;
            }

            Random.InitState(depth);
            Gizmos.color = new Color(Random.value, Random.value, Random.value, 0.2f);
            Gizmos.DrawCube(node.bounds.Center, node.bounds.max - node.bounds.min);

            if (node.GetChildNode(0) != null)
            {
                nodeQueue.Enqueue(node.GetChildNode(0));
            }
            if (node.GetChildNode(1) != null)
            {
                nodeQueue.Enqueue(node.GetChildNode(1));
            }

            if (node.IsLeaf())
                nodeCount++;
        }

        //Debug.LogError("nodeCount = " + nodeCount);
    }

    private void OnDrawGizmos()
    {
        DrawBVHBoundsBFS(root);
    }
}
