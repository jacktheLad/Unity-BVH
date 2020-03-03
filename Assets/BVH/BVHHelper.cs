using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using sif;
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
            var mesh = mf.mesh;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 worldPos = mf.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[i]);
                verts.Add(worldPos);
            }

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                var tri = new BVHTriangle(  vetexOffset + mesh.triangles[i],
                                            vetexOffset + mesh.triangles[i + 1],
                                            vetexOffset + mesh.triangles[i + 2]);
                tris.Add(tri);
            }

            vetexOffset += mesh.vertices.Length;
        }

        return new BVHScene(tris, verts);
    }
}
