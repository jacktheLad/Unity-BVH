using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct MatDiffuse
{
    Vector4 color;
    int diffuseTexID;
    int sigma;	// Lambertian or OrenNayar?
}