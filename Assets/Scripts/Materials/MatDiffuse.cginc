#ifndef MAT_DIFFUSE_H
#define MAT_DIFFUSE_H

struct MatDiffuse {
	int diffuseTexID;
	int type;	// Lambertian or OrenNayar?
};

#endif