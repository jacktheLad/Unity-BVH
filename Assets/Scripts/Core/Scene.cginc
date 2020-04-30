#ifndef SCENE_H
#define SCENE_H
#include "Assets/Scripts/Core/Material.cginc"

StructuredBuffer<int3> _Triangles;
StructuredBuffer<MaterialIndexer> _MaterialIndexers;

StructuredBuffer<float3> _Vertices;
StructuredBuffer<float3> _Normals;
StructuredBuffer<float2> _UV0s;

Texture2DArray _DiffuseTextures;	SamplerState sampler_DiffuseTextures;

#endif