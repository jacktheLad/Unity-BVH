#ifndef MAT_DIFFUSE_H
#define MAT_DIFFUSE_H

struct MatDiffuse {
	float4 color;
	int diffuseTexID;
	float sigma;	// Lambertian or OrenNayar?
};

#endif