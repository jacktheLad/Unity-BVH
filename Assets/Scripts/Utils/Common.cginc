#ifndef UTILS_COMMON_H
#define UTILS_COMMON_H 

#define INFINITY 1.#INF
#define PI      3.14159265358979323846
#define INV_PI   0.31830988618379067154
#define INV_2_PI  0.15915494309189533577
#define INV_4_PI  0.07957747154594766788
#define PI_OVER_2 1.57079632679489661923
#define PI_OVER_4 0.78539816339744830961
#define SQRT2   1.41421356237309504880
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