#ifndef SBVH_TRAVERSAL_H
#define SBVH_TRAVERSAL_H

StructuredBuffer<float4> nodes;
StructuredBuffer<float4> woopTris;
StructuredBuffer<float4> debugTris;
StructuredBuffer<int>  triIndices;

int leafNodeCount;
int triangleCount;

#define EntrypointSentinel 0x76543210

float CopySign(float x, float s) {
    return (s >= 0) ? abs(x) : -abs(x);
}

void swapf(inout float a, inout float b) {
	float tmp = a; a = b; b = tmp;
}

void swapi(inout int a, inout int b) {
	int tmp = a; a = b; b = tmp;
}

float max_max(float a, float b, float c) { return max(max(a, b), c); }
float min_max(float a, float b, float c) { return max(min(a, b), c); }
float max_min(float a, float b, float c) { return min(max(a, b), c); }
float min_min(float a, float b, float c) { return min(min(a, b), c); }

float SpanBegin(float a0, float a1, float b0, float b1, float c0, float c1, float d) {
	return max_max(min(a0, a1), min(b0, b1), min_max(c0, c1, d)); 
}

float SpanEnd(float a0, float a1, float b0, float b1, float c0, float c1, float d) {
	return min_min(max(a0, a1), max(b0, b1), max_min(c0, c1, d));
}

void Trace(float3 rayOri, 
			float3 rayDir, 
			inout int hitTriIdx, 
			inout float hitDist, 
			inout float3 triNormal, 
			bool anyHit) {
	int 	traversalStack[64];
	int 	stackPtr;

	float   origx, origy, origz;    // Ray origin.
	float   dirx, diry, dirz;       // Ray direction.
	float   tmin;                   // t-value from which the ray starts. Usually 0.
	float   idirx, idiry, idirz;    // 1 / ray direction
	float   oodx, oody, oodz;       // ray origin / ray direction

	int     leafAddr;               // If negative, then first postponed leaf, non-negative if no leaf (innernode).
	int     nodeAddr;
	int     hitIndex;               // Triangle index of the closest intersection, -1 if none.
	float   hitT;                   // t-value of the closest intersection.

	// Initialize (stores local variables in registers)
	{
		// Fetch ray.

		// required when tracing ray batches
		// float4 o = rays[rayidx * 2 + 0];  
		// float4 d = rays[rayidx * 2 + 1];
		//__shared__ volatile int nextRayArray[MaxBlockHeight]; // Current ray index in global buffer.

		origx = rayOri.x;
		origy = rayOri.y;
		origz = rayOri.z;
		dirx = rayDir.x;
		diry = rayDir.y;
		dirz = rayDir.z;
		tmin = 0.00001f;

		// ooeps is very small number, used instead of rayDir xyz component when that component is near zero
		float ooeps = exp2(-80.0f); // Avoid div by zero, returns 1/2^80, an extremely small number
		idirx = 1.0f / (abs(rayDir.x) > ooeps ? rayDir.x : CopySign(ooeps, rayDir.x)); // inverse ray direction
		idiry = 1.0f / (abs(rayDir.y) > ooeps ? rayDir.y : CopySign(ooeps, rayDir.y)); // inverse ray direction
		idirz = 1.0f / (abs(rayDir.z) > ooeps ? rayDir.z : CopySign(ooeps, rayDir.z)); // inverse ray direction
		oodx = origx * idirx;  // ray origin / ray direction
		oody = origy * idiry;  // ray origin / ray direction
		oodz = origz * idirz;  // ray origin / ray direction

		// Setup traversal + initialisation

		traversalStack[0] = EntrypointSentinel; // Bottom-most entry. 0x76543210 (1985229328 in decimal)
		stackPtr = 0; // point stackPtr to bottom of traversal stack = EntryPointSentinel
		leafAddr = 0;   // No postponed leaf.
		nodeAddr = 0;   // Start from the root.
		hitIndex = -1;  // No triangle intersected so far.
		hitT = 1.#INF; // tmax  
	}

	while (nodeAddr != EntrypointSentinel) {
		bool searchingLeaf = true; // required for warp efficiency
		while (nodeAddr >= 0 && nodeAddr != EntrypointSentinel) {
			float4 n0xy = nodes[nodeAddr]; // childnode 0, xy-bounds (c0.lo.x, c0.hi.x, c0.lo.y, c0.hi.y)		
			float4 n1xy = nodes[nodeAddr + 1]; // childnode 1, xy-bounds (c1.lo.x, c1.hi.x, c1.lo.y, c1.hi.y)		
			float4 nz = nodes[nodeAddr + 2]; // childnode 0 and 1, z-bounds (c0.lo.z, c0.hi.z, c1.lo.z, c1.hi.z)		
            float4 tmp = nodes[nodeAddr + 3]; // contains indices to 2 childnodes in case of innernode, see below
            int2 cnodes = int2((int)tmp.x, (int)tmp.y);
            // (childindex = size of array during building, see CudaBVH.cpp)

			// compute ray intersections with BVH node bounding box

			/// RAY BOX INTERSECTION
			// Intersect the ray against the child nodes.

			float c0lox = n0xy.x * idirx - oodx; // n0xy.x = c0.lo.x, child 0 minbound x
			float c0hix = n0xy.y * idirx - oodx; // n0xy.y = c0.hi.x, child 0 maxbound x
			float c0loy = n0xy.z * idiry - oody; // n0xy.z = c0.lo.y, child 0 minbound y
			float c0hiy = n0xy.w * idiry - oody; // n0xy.w = c0.hi.y, child 0 maxbound y
			float c0loz = nz.x   * idirz - oodz; // nz.x   = c0.lo.z, child 0 minbound z
			float c0hiz = nz.y   * idirz - oodz; // nz.y   = c0.hi.z, child 0 maxbound z
			float c1loz = nz.z   * idirz - oodz; // nz.z   = c1.lo.z, child 1 minbound z
			float c1hiz = nz.w   * idirz - oodz; // nz.w   = c1.hi.z, child 1 maxbound z
			float c0min = SpanBegin(c0lox, c0hix, c0loy, c0hiy, c0loz, c0hiz, tmin); // Tesla does max4(min, min, min, tmin)
			float c0max = SpanEnd(c0lox, c0hix, c0loy, c0hiy, c0loz, c0hiz, hitT); // Tesla does min4(max, max, max, tmax)
			float c1lox = n1xy.x * idirx - oodx; // n1xy.x = c1.lo.x, child 1 minbound x
			float c1hix = n1xy.y * idirx - oodx; // n1xy.y = c1.hi.x, child 1 maxbound x
			float c1loy = n1xy.z * idiry - oody; // n1xy.z = c1.lo.y, child 1 minbound y
			float c1hiy = n1xy.w * idiry - oody; // n1xy.w = c1.hi.y, child 1 maxbound y
			float c1min = SpanBegin(c1lox, c1hix, c1loy, c1hiy, c1loz, c1hiz, tmin);
			float c1max = SpanEnd(c1lox, c1hix, c1loy, c1hiy, c1loz, c1hiz, hitT);

			//float ray_tmax = 1e20;
			bool traverseChild0 = (c0min <= c0max);//&& (c0min >= tmin) && (c0min <= ray_tmax);
			bool traverseChild1 = (c1min <= c1max);//&& (c1min >= tmin) && (c1min <= ray_tmax);
			
			if (!traverseChild0 && !traverseChild1)  
			{
	/*			hitT = nodeAddr;*/
				nodeAddr = traversalStack[stackPtr]; // fetch next node by popping stack
				stackPtr -= 1; // popping decrements stack by 4 bytes (because stackPtr is a pointer to char) 
			}
			// Otherwise => fetch child pointers.
			else  // one or both children intersected
			{
				//hitT = 0.5;
				// set nodeAddr equal to intersected childnode index (or first childnode when both children are intersected)
				nodeAddr = (traverseChild0) ? cnodes.x : cnodes.y; 
				
				// Both children were intersected => push the farther one on the stack.

				if (traverseChild0 && traverseChild1) // store closest child in nodeAddr, swap if necessary
				{   
					if (c1min < c0min)  
						swapi(nodeAddr, cnodes.y);  
					stackPtr += 1;  // pushing increments stack by 4 bytes (stackPtr is a pointer to char)
					traversalStack[stackPtr] = cnodes.y; // push furthest node on the stack
				}
			}

			// First leaf => postpone and continue traversal.
			// leafnodes have a negative index to distinguish them from inner nodes
			// if nodeAddr less than 0 -> nodeAddr is a leaf
			if (nodeAddr < 0 && leafAddr >= 0)  // if leafAddr >= 0 -> no leaf found yet (first leaf)
			{
				
				searchingLeaf = false; // required for warp efficiency
				leafAddr = nodeAddr;  
				
				nodeAddr = traversalStack[stackPtr]; // fetch next node by popping stack
				stackPtr -= 1; 
			}

			// unity 要支持warp vote太难了
			if (leafAddr < 0)
				break;
		}

		while (leafAddr < 0) {
			// 1.#INF是必须的，因为大场景的地址可能非常大
			for (int triAddr = ~leafAddr; triAddr < 1.#INF; triAddr += 3) {
				float4 v00 = woopTris[triAddr];

				// 叶节点最后一个三角形
				if(v00.x == 1e20f)
					break;

				// Compute and check intersection t-value (hit distance along ray).
				float Oz = v00.w - origx*v00.x - origy*v00.y - origz*v00.z;   // Origin z
				float invDz = 1.0f / (dirx*v00.x + diry*v00.y + dirz*v00.z);  // inverse Direction z
				float t = Oz * invDz;   
				
				if (t > tmin && t < hitT) {
					// Compute and check barycentric u.

					// fetch second precomputed triangle edge
					float4 v11 = woopTris[triAddr + 1];
					float Ox = v11.w + origx*v11.x + origy*v11.y + origz*v11.z;  // Origin.x
					float Dx = dirx * v11.x + diry * v11.y + dirz * v11.z;  // Direction.x
					float u = Ox + t * Dx; /// parametric equation of a ray (intersection point)

					if (u >= 0.0f && u <= 1.0f)
					{
						// Compute and check barycentric v.

						// fetch third precomputed triangle edge
						float4 v22 = woopTris[triAddr + 2];
						float Oy = v22.w + origx*v22.x + origy*v22.y + origz*v22.z;
						float Dy = dirx*v22.x + diry*v22.y + dirz*v22.z;
						float v = Oy + t*Dy;

						if (v >= 0.0f && u + v <= 1.0f)
						{
							// We've got a hit!
							// Record intersection.

							hitT = t;
							hitIndex = triAddr; // store triangle index for shading

							// Closest intersection not required => terminate.
							if (anyHit)  // only true for shadow rays
							{
								nodeAddr = EntrypointSentinel;
								break;
							}

							// compute normal vector by taking the cross product of two edge vectors
							// because of Woop transformation, only one set of vectors works
							
							//triNormal = cross(Vec3f(v22.x, v22.y, v22.z), Vec3f(v11.x, v11.y, v11.z));  // works
							triNormal = cross(float3(v11.x, v11.y, v11.z), float3(v22.x, v22.y, v22.z));  
						}
					}
				}
			}// end triangle intersection

			// Another leaf was postponed => process it as well.
			leafAddr = nodeAddr;
			if (nodeAddr < 0)    // nodeAddr is an actual leaf when < 0
			{
				nodeAddr = traversalStack[stackPtr]; // fetch next node by popping stack
				stackPtr -= 1;
			}
		}
	}

	if (hitIndex != -1){
		hitIndex = triIndices[hitIndex];
		// remapping tri indices delayed until this point for performance reasons
		// (slow texture memory lookup in de triIndicesTexture) because multiple triangles per node can potentially be hit
	}

	hitTriIdx = hitIndex;
	hitDist = hitT;
}

#endif