using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BVHTriangle = UnityEngine.Vector3Int;
using Object = UnityEngine.Object;

public class Scene
{
    public bool builded;

    // per vertex data
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<Vector2> uv0s = new List<Vector2>();

    // per triangle data
    public List<BVHTriangle> triangles = new List<BVHTriangle>();
    public List<MatUber> materials = new List<MatUber>();
    public List<int> matIndices = new List<int>();

    // textures, since we use Texture2DArray, support 512x512 only for the moment.
    public List<Texture2D> diffuseTextures = new List<Texture2D>();

    public Scene(bool immediatelyBuild = false)
    {
        if (immediatelyBuild) ParseUnityScene();
    }

    public void ParseUnityScene()
    {
        var meshFilters = Object.FindObjectsOfType<MeshFilter>();
        var vertexOffset = 0;
        foreach (var mf in meshFilters)
        {
            Debug.Log(mf.name);
            var mesh = mf.sharedMesh;
            // mesh.vertices, mesh.triangles, localToWorldMatrix是属性
            // 在一个循环数很大的for里面会产生巨大开销
            var verts = mesh.vertices;
            var norms = mesh.normals;

            uv0s.AddRange(mesh.uv);
            if (mesh.uv.Length < verts.Length)
            {
                Vector2[] dummyFix = new Vector2[verts.Length - mesh.uv.Length];
                uv0s.AddRange(dummyFix);
            }

            Debug.Log(verts.Length);
            Debug.Log(norms.Length);
            Debug.Log(mesh.uv.Length);

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
            var material = ParseMaterial(renderer);
            materials.Add(material);
            var matIdx = materials.Count - 1;

            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                var tri = new BVHTriangle(vertexOffset + tris[i],
                                            vertexOffset + tris[i + 1],
                                            vertexOffset + tris[i + 2]);
                triangles.Add(tri);
                matIndices.Add(matIdx);
            }

            vertexOffset += mesh.vertices.Length;
        }

        builded = true;

        Debug.Log("Tracer: totally materials count:" + materials.Count);
        Debug.Log("Tracer: totally diffuse textures count:" + diffuseTextures.Count);
    }

    public MatUber ParseMaterial(Renderer renderer)
    {
        var mat = renderer.sharedMaterial;
        var shaderName = mat.shader.name;
        Debug.Log(mat.name);

        MatUber uber = new MatUber();

        if (shaderName == MaterialUtils.uberShaderName)
        {
            var c = mat.GetColor(UspID.diffColor);
            uber.diffColor = new Vector3(c.r, c.g, c.b);

            var t = mat.GetTexture(UspID.diffTexIdx) as Texture2D;

            if (t == null)
            {
                uber.diffTexIdx = -1;
            }
            else
            {
                Debug.Log(t.name);
                Debug.Assert(t.width == 512 && t.height == 512);
                if (!diffuseTextures.Contains(t)) diffuseTextures.Add(t);
                uber.diffTexIdx = diffuseTextures.IndexOf(t);
            }

            uber.diffExtra = mat.GetFloat(UspID.diffExtra);
        }

        return uber;
    }
}
