using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaterialTable
{
    public static List<MatDiffuse> diffuses = new List<MatDiffuse>();
}

public struct MaterialIndexer
{
    public int typeID;
    public int index;
}

public enum MaterialType : int
{
    MAT_DIFFUSE = 0x00000000,
    MAT_MIRROR = 0x00000001,
}

public struct MatDiffuse
{
    public Vector3 color;
    public int diffuseTexIdx;
    public float sigma;	// Lambertian or OrenNayar?
}