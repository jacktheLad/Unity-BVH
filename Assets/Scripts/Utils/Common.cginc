#ifndef UTILS_COMMON_H
#define UTILS_COMMON_H 

#define INFINITY 1.#INF
#define PI 3.14159265f
#define EPSILON 1e-8f

float sdot(float3 x, float3 y, float f = 1.0f)
{
    return saturate(dot(x, y) * f);
}

float energy(float3 color)
{
    return dot(color, 1.0f / 3.0f);
}

#endif