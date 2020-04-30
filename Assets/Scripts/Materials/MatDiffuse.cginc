#ifndef MAT_DIFFUSE_H
#define MAT_DIFFUSE_H

struct MatDiffuse {
	float3 color;
	int diffuseTexID;
	float sigma;	// Lambertian or OrenNayar?
};

StructuredBuffer<MatDiffuse> _MatDiffuses;

#endif