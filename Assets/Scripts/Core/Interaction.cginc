#ifndef INTERACTION_H
#define INTERACTION_H
#include "Assets/Scripts/Utils/Common.cginc"

struct Interaction
{
    float3 position;
    float distance;
    float3 normal;
    float3 albedo;
    float3 specular;
    float smoothness;
    float3 emission;
    int triIdx;
};

Interaction CreateInteraction()
{
    Interaction hit;
    hit.position = float3(0.0f, 0.0f, 0.0f);
    hit.distance = INFINITY;
    hit.normal = float3(0.0f, 0.0f, 0.0f);
    hit.albedo = float3(0.0f, 0.0f, 0.0f);
    hit.specular = float3(0.0f, 0.0f, 0.0f);
    hit.smoothness = 0.0f;
    hit.emission = float3(0.0f, 0.0f, 0.0f);
    hit.triIdx = -1;
    return hit;
}

#endif