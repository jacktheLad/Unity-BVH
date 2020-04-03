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
    BVHNode root;
    BVHScene bvhScene;
    [Range(0,20)]
    public int maxDepth = 20;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        bvhScene = BVHHelper.BuildBVHScene();
        var bvhData = new CPU_BVHData(bvhScene);
        CPU_SBVHBuilder.Build(bvhData);
        root = bvhData.root;
        sw.Stop();
        string log = "Build successfully, time: " + sw.ElapsedMilliseconds + " ms";
        Debug.Log(log);
        text.text = log;
    }

    [MenuItem("Examples/Chain Actions and close")]
    static void EditorPlaying()
    {
        EditorApplication.Exit(0);
    }

    Queue<BVHNode> nodeQueue = new Queue<BVHNode>();
    void DrawBVHBoundsBFS(BVHNode root)
    {
        nodeQueue.Clear();
        nodeQueue.Enqueue(root);
        nodeQueue.Enqueue(null);
        int depth = 1;
        int nodeCount = 1;
        while (nodeQueue.Count != 0 && depth <= maxDepth)
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
            nodeCount++;
        }
    }

    private void OnDrawGizmos()
    {
        DrawBVHBoundsBFS(root);
    }
}
