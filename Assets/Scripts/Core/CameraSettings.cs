using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSettings : MonoBehaviour
{
    [Range(1f, 3000f)]
    public float imagePlaneDistance = 5f;
    [Range(0f, 20f)]
    public float lensRadius = 0.5f;
    [HideInInspector]
    public bool hasChanged = false;

    Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    public void SetData(ComputeShader compute)
    {
        compute.SetFloat("_LensRadius", lensRadius);
        compute.SetFloat("_ImageDistance", imagePlaneDistance);
        compute.SetMatrix("_Camera2World", _camera.cameraToWorldMatrix);
        compute.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    }

    private void OnValidate()
    {
        hasChanged = true;
    }
}
