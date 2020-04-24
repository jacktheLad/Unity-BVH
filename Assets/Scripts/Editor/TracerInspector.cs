using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
[CustomEditor(typeof(TracerBehaviour))]
public class TracerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var tracer = target as TracerBehaviour;

        bool pressGenerateBVH = GUILayout.Button("Rebuild BVH", GUILayout.ExpandWidth(true));
        if (pressGenerateBVH)
        {
            var scene = new Scene(true);
            var bvhScene = new BVHScene(scene.triangles, scene.vertices);
            var cpuBVH = new CPU_BVHData(bvhScene);
            CPU_SBVHBuilder.Build(cpuBVH);

            SaveBVH(tracer, cpuBVH);
        }

        //bool pressTest = GUILayout.Button("Log Test", GUILayout.ExpandWidth(true));
        //if (pressTest)
        //{
        //    Debug.Log(tracer.gpuBVH.nodes.Count);
        //    Debug.Log(tracer.gpuBVH.nodes[1]);
        //}

        DrawProperty("useCachedBVH");
        DrawProperty("spps");
        DrawProperty("tracingShader");
        DrawProperty("skyboxTex");
        DrawProperty("sun");

        serializedObject.ApplyModifiedProperties();
    }

    private static void SaveBVH(TracerBehaviour tracer, CPU_BVHData cpuBVH)
    {
        var cacheDir = "Assets/Cache/BVH/";

        if (!System.IO.Directory.Exists(cacheDir))
            System.IO.Directory.CreateDirectory(cacheDir);

        var path = cacheDir + EditorSceneManager.GetActiveScene().name + "_BVH.prefab";
        var dataObj = new GameObject("__BVHData__");
        var cachedBVH = dataObj.AddComponent<GPU_BVHData>();
        cachedBVH.Generate(cpuBVH);
        var prefabRoot = PrefabUtility.SaveAsPrefabAsset(dataObj, path);
        DestroyImmediate(dataObj);
        tracer.gpuBVH = prefabRoot.GetComponent<GPU_BVHData>();
    }

    private void DrawProperty(string property, string label = null)
    {
        if (string.IsNullOrEmpty(label))
        {
            label = property;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty(property), new GUIContent(label), true);
    }
}
