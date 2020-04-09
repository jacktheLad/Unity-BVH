using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BVHTriangle = UnityEngine.Vector3Int;

public static class BVHHelper 
{
    public static BVHScene BuildBVHScene()
    {
        List<BVHTriangle> tris = new List<BVHTriangle>();
        List<Vector3> verts = new List<Vector3>();

        var meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
        var vetexOffset = 0;
        foreach (var mf in meshFilters)
        {
            var mesh = mf.sharedMesh;
            // mesh.vertices, mesh.triangles, localToWorldMatrix是属性
            // 在一个循环数很大的for里面会产生巨大开销
            var vertices = mesh.vertices;
            var matWorld = mf.transform.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = matWorld.MultiplyPoint(vertices[i]);
                //Vector3 worldPos = matWorld.MultiplyPoint(mesh.vertices[i]); // Bad idea.
                verts.Add(worldPos);
            }

            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var tri = new BVHTriangle(  vetexOffset + triangles[i],
                                            vetexOffset + triangles[i + 1],
                                            vetexOffset + triangles[i + 2]);
                tris.Add(tri);
            }

            vetexOffset += mesh.vertices.Length;
        }

        return new BVHScene(tris, verts);
    }
}
