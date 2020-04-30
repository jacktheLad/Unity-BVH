#ifndef MATERIAL_H
#define MATERIAL_H

#include "Assets/Scripts/Core/Interaction.cginc"
#include "Assets/Scripts/Materials/MatDiffuse.cginc"

#define MAT_DIFFUSE 0
#define MAT_MIRROR	1

struct MaterialIndexer {
	int typeID;
	int index;
};

void CreateMaterial(MaterialIndexer indexer) {
	if (indexer.typeID == (int)MAT_DIFFUSE) {

	}
}

void ComputeShadingData(inout Interaction hit) {

}

#endif