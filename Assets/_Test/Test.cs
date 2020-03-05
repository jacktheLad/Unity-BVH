using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sif;
using UnityEditor;
using System.IO;
public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var bvhScene = BVHHelper.BuildBVHScene();
        var bvhData = new CPU_SBVHData(bvhScene);
        CPU_SBVHBuilder.Build(bvhData);

        List<int> ls = new List<int>();
        ls.Add(1);
        ls.Add(2);
        ls.Add(1);
        ls.Remove(1);
        Debug.LogError(ls[0]);
        Debug.LogError(ls[1]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
