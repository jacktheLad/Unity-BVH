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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
