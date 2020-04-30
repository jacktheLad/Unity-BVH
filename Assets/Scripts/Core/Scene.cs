using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BVHTriangle = UnityEngine.Vector3Int;

public class Scene
{
    public bool builded;

    // per vertex data
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<Vector2> uv0s = new List<Vector2>();

    // per triangle data
    public List<BVHTriangle> triangles = new List<BVHTriangle>();
    public List<MaterialIndexer> materialIndexers = new List<MaterialIndexer>();

    // textures, since we use Texture2DArray, support 512x512 only for the moment.
    public List<Texture2D> diffuseTextures = new List<Texture2D>();

    public Scene(bool immediatelyBuild = false)
    {
        if (immediatelyBuild) ParseUnityScene();
    }

    public void ParseUnityScene()
    {
        var meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
        var vetexOffset = 0;
        foreach (var mf in meshFilters)
        {
            var mesh = mf.sharedMesh;
            // mesh.vertices, mesh.triangles, localToWorldMatrix是属性
            // 在一个循环数很大的for里面会产生巨大开销
            var verts = mesh.vertices;
            var norms = mesh.normals;
            uv0s.AddRange(mesh.uv);

            var matWorld = mf.transform.localToWorldMatrix;
            for (int i = 0; i < verts.Length; i++)
            {
                //Vector3 worldPos = matWorld.MultiplyPoint(mesh.vertices[i]); // Bad idea.
                Vector3 worldPos = matWorld.MultiplyPoint(verts[i]);
                vertices.Add(worldPos);

                Vector3 worldNormal = matWorld.MultiplyVector(norms[i]);
                normals.Add(worldNormal);
            }

            var renderer = mf.GetComponent<Renderer>();
            var indexer = ParseMaterial(renderer);
   
            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                var tri = new BVHTriangle(vetexOffset + tris[i],
                                            vetexOffset + tris[i + 1],
                                            vetexOffset + tris[i + 2]);
                triangles.Add(tri);
                materialIndexers.Add(indexer);
            }

            vetexOffset += mesh.vertices.Length;
        }

        builded = true;
    }

    public MaterialIndexer ParseMaterial(Renderer renderer)
    {
        MaterialIndexer indexer= new MaterialIndexer();

        var mat = renderer.sharedMaterial;
        var shaderName = mat.shader.name;
        if(shaderName.Contains("Diffuse"))
        {
            MatDiffuse data = new MatDiffuse();
            var c = mat.GetColor("_Color");
            data.color = new Vector3(c.r, c.g, c.b);

            var t = mat.GetTexture("_MainTex") as Texture2D;
            Debug.Assert(t.width == 512 && t.height == 512);
            if (!diffuseTextures.Contains(t))diffuseTextures.Add(t);
            data.diffuseTexIdx = diffuseTextures.IndexOf(t);

            data.sigma = mat.GetFloat("_Sigma");

            MaterialTable.diffuses.Add(data);

            indexer.typeID = (int)MaterialType.MAT_DIFFUSE;
            indexer.index = MaterialTable.diffuses.Count - 1;
        }

        return indexer;
    }
}
