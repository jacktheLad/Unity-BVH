#ifndef CAMERA_H
#define CAMERA_H

#include "Assets/Scripts/Core/Ray.cginc"
#include "Assets/Scripts/Core/Sampling.cginc"

float4x4 _Camera2World;
float4x4 _CameraInverseProjection;
float _LensRadius;
float _ImageDistance;

Ray CreateCameraRay(uint2 pixel, uint width, uint height)
{
    float2 uv = float2((pixel /*+ float2(rand(), rand())*/) / float2(width, height) * 2.0f - 1.0f);
    float3 cameraSpaceOrigin = float3(0.0f, 0.0f, 0.0f);
    float3 cameraSpaceDirection = mul(_CameraInverseProjection, float4(uv, 0.0f, 1.0f)).xyz;
    Ray ray = CreateRay(cameraSpaceOrigin, normalize(cameraSpaceDirection));

    // Depth of field
    if (_LensRadius > 0)
    {
        // 将摄像机原点做一个偏移
        float2 pLens = _LensRadius * ConcentricSampleDisk(float2(rand(), rand()));

        // 计算成像点
        float ft = _ImageDistance / ray.direction.z;
        float3 pFocus = ray.origin + ft * ray.direction * -1;

        ray.origin = float3(pLens, 0);
        ray.direction = normalize(pFocus - ray.origin);
    }

    ray.origin = mul(_Camera2World, float4(ray.origin, 1.0f)).xyz;
    ray.direction = mul(_Camera2World, float4(ray.direction, 0.0f)).xyz;
    ray.direction = normalize(ray.direction);

    return ray;
}

#endif