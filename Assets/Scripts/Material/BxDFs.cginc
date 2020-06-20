#ifndef BXDFS_H
#define BXDFS_H

#define BSDF_REFLECTION  (1<<0)
#define BSDF_TRANSMISSION  (1<<1)
#define BSDF_DIFFUSE  (1<<2)
#define BSDF_GLOSSY  (1<<3)
#define BSDF_SPECULAR  (1<<4)
#define BSDF_ALL (BSDF_DIFFUSE | BSDF_GLOSSY | BSDF_SPECULAR | BSDF_REFLECTION | BSDF_TRANSMISSION)

struct DisneyDiffuse
{
	uint type;
	float3 R;
};

DisneyDiffuse CreateDisneyDiffuse(float3 r) {
	DisneyDiffuse dd;
	dd.type = BSDF_REFLECTION | BSDF_DIFFUSE;
	dd.R = r;
	return dd;
}

#endif