using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sif;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var bvhScene = BVHHelper.BuildBVHScene();
        Debug.Log(bvhScene);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
