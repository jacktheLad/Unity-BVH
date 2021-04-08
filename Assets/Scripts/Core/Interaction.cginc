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

    float2 uv;
};

Interaction CreateInteraction()
{
    Interaction hit;
    hit.position = float3(0.0f, 0.0f, 0.0f);
    hit.distance = INFINITY;
    hit.normal = float3(0.0f, 0.0f, 0.0f);
    hit.triIdx = -1;

    hit.albedo = float3(0.0f, 0.0f, 0.0f);
    hit.specular = float3(0.0f, 0.0f, 0.0f);
    hit.smoothness = 0.0f;
    hit.emission = float3(0.0f, 0.0f, 0.0f);
    hit.uv = float2(0.0f, 0.0f);

    return hit;
}

Interaction CreateInteraction(Ray ray, float hitDist, float3 triNormal, int hitTriIdx) {
    Interaction hit;
    hit.position = ray.origin + hitDist * ray.direction;
    hit.distance = hitDist;
    hit.normal = normalize(triNormal);
    hit.triIdx = hitTriIdx;

    hit.albedo = float3(0.0f, 0.0f, 0.0f);
    hit.specular = float3(0.0f, 0.0f, 0.0f);
    hit.smoothness = 0.0f;
    hit.emission = float3(0.0f, 0.0f, 0.0f);
    hit.uv = float2(0.0f, 0.0f);

    return hit;
}

void ComputeScatteringFunctions(inout Ray ray, inout Interaction hit) {

    //ComputeDifferentials(ray); //TODO：光线微分

}

#endif