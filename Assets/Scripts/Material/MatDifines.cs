using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialUtils
{
    public static string uberShaderName = "Tracer/Uber";
}

/// <summary>
/// Uber material base on Disney principled brdf/bssdf.
/// 36*4 = 144 bytes.
/// </summary>
public struct MatUber
{
    public Vector3 diffColor;
    public int diffTexIdx;
    public float diffExtra;	// Lambertian or OrenNayar or DisneyDiffuse?

    public float metallic;
    public int matallicTexIdx;

    public float ior;   // Index of refraction.
    public int iorTexIdx;

    public float roughness;
    public int roughnessTexIdx;

    public float specTint;
    public int specTintTexIdx;

    public float anisotropic;
    public int anisotropicTexIdx;

    public float sheen;
    public int sheenTexIdx;
    public float sheenTint;
    public int sheenTintTexIdx;

    public float clearcoat;
    public int clearcoatTexIdx;
    public float ccoatGloss;
    public int ccoatGlossTexIdx;

    public float specTrans;
    public int specTransTexIdx;

    public Vector3 scatterDistance;
    public int scatterDistanceTexIdx;

    public int thin;

    public float flatness;
    public int flatnessTexIdx;

    public float diffTrans;
    public int diffTransTexIdx;

    public float bump;
    public int bumpTexIdx;
}

/// <summary>
/// Usp ,i.e "Uber shader property"
/// </summary>
public static class UspID
{
    public static int diffColor             = Shader.PropertyToID("_Color");
    public static int diffTexIdx            = Shader.PropertyToID("_MainTex");
    public static int diffExtra             = Shader.PropertyToID("_Sigma");

    public static int metallic              = Shader.PropertyToID("_Metallic");
    public static int matallicTexIdx        = Shader.PropertyToID("_Color");

    public static int ior                   = Shader.PropertyToID("_Color");
    public static int iorTexIdx             = Shader.PropertyToID("_Color");
                                            
    public static int roughness             = Shader.PropertyToID("_Color");
    public static int roughnessTexIdx       = Shader.PropertyToID("_Color");
                                          
    public static int specTint              = Shader.PropertyToID("_Color");
    public static int specTintTexIdx        = Shader.PropertyToID("_Color");
                                            
    public static int anisotropic           = Shader.PropertyToID("_Color");
    public static int anisotropicTexIdx     = Shader.PropertyToID("_Color");

    public static int sheen                 = Shader.PropertyToID("_Color");
    public static int sheenTexIdx           = Shader.PropertyToID("_Color");
    public static int sheenTint             = Shader.PropertyToID("_Color");
    public static int sheenTintTexIdx       = Shader.PropertyToID("_Color");
                                            
    public static int clearcoat             = Shader.PropertyToID("_Color");
    public static int clearcoatTexIdx       = Shader.PropertyToID("_Color");
    public static int ccoatGloss            = Shader.PropertyToID("_Color");
    public static int ccoatGlossTexIdx      = Shader.PropertyToID("_Color");
                                            
    public static int specTrans             = Shader.PropertyToID("_Color");
    public static int specTransTexIdx       = Shader.PropertyToID("_Color");

    public static int scatterDistance       = Shader.PropertyToID("_Color");
    public static int scatterDistanceTexIdx = Shader.PropertyToID("_Color");
                                            
    public static int thin                  = Shader.PropertyToID("_Color");
                                            
    public static int flatness              = Shader.PropertyToID("_Color");
    public static int flatnessTexIdx        = Shader.PropertyToID("_Color");
                                            
    public static int diffTrans             = Shader.PropertyToID("_Color");
    public static int diffTransTexIdx       = Shader.PropertyToID("_Color");
                                            
    public static int bump                  = Shader.PropertyToID("_Color");
    public static int bumpTexIdx            = Shader.PropertyToID("_Color");
}