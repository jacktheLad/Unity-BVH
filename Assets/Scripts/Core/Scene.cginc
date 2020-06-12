#ifndef SCENE_H
#define SCENE_H
#include "Assets/Scripts/Utils/Common.cginc"
#include "Assets/Scripts/Core/Material.cginc"

StructuredBuffer<int3> _Triangles;

StructuredBuffer<float3> _Vertices;
StructuredBuffer<float3> _Normals;
StructuredBuffer<float2> _UV0s;

Texture2DArray _DiffuseTextures;	SamplerState sampler_DiffuseTextures;

Texture2D<float4> _SkyboxTexture;   SamplerState sampler_SkyboxTexture;

float3 GetSkyboxColor(Ray ray) {
    float theta = acos(ray.direction.y) / -PI;
    float phi = atan2(ray.direction.x, -ray.direction.z) / -PI * 0.5f;

    return _SkyboxTexture.SampleLevel(sampler_SkyboxTexture, float2(phi, theta), 0).xyz;
}

#endif