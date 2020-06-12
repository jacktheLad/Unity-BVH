#ifndef MATERIAL_H
#define MATERIAL_H

#include "Assets/Scripts/Core/Interaction.cginc"
struct MatUber {
    float3 diffColor;
    int diffTexIdx;
    float diffExtra;	// Lambertian or OrenNayar or DisneyDiffuse?

    float metallic;
    int matallicTexIdx;

    float ior;   // Index of refraction.
    int iorTexIdx;

    float roughness;
    int roughnessTexIdx;

    float specTint;
    int specTintTexIdx;

    float anisotropic;
    int anisotropicTexIdx;

    float sheen;
    int sheenTexIdx;
    float sheenTint;
    int sheenTintTexIdx;

    float clearcoat;
    int clearcoatTexIdx;
    float ccoatGloss;
    int ccoatGlossTexIdx;

    float specTrans;
    int specTransTexIdx;

    float3 scatterDistance;
    int scatterDistanceTexIdx;

    int thin;

    float flatness;
    int flatnessTexIdx;

    float diffTrans;
    int diffTransTexIdx;

    float bump;
    int bumpTexIdx;
};

StructuredBuffer<MatUber> _MatUbers;

void ComputeShadingData(inout Interaction hit) {

}



#endif