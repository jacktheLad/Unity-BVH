using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TracerBehaviour : MonoBehaviour
{
    public bool useCachedBVH = true;

    public ComputeShader tracingShader;
    public Texture skyboxTex;
    public Light sun;

    public GPU_BVHData gpuBVH;

    private Camera _camera;
    private RenderTexture _target;
    private RenderTexture _converged;
    private Material _addMaterial;
    private uint _currentSample = 0;

    private ComputeBuffer _nodesBuffer;
    private ComputeBuffer _woopTrisBuffer;
    private ComputeBuffer _triIndicesBuffer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (gpuBVH == null)
        {
            Debug.Log("No cached BVH data found, building BVH now.");
            var bvhScene = BVHHelper.BuildBVHScene();
            var cpuBVH = new CPU_BVHData(bvhScene);
            CPU_SBVHBuilder.Build(cpuBVH);
            gpuBVH = new GPU_BVHData().Generate(cpuBVH);
        }
        else
        {
            Debug.Log("Using cached BVH data.");
        }

        CreateComputeBuffer(ref _nodesBuffer, gpuBVH.nodes, 16);
        CreateComputeBuffer(ref _woopTrisBuffer, gpuBVH.woopTris, 16);
        CreateComputeBuffer(ref _triIndicesBuffer, gpuBVH.triIndices, 4);
    }

    private void OnDestroy()
    {
        _nodesBuffer?.Release();
        _woopTrisBuffer?.Release();
        _triIndicesBuffer?.Release();
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
    where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }

            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        //  当buffer因为出错为null的时候，为什么也能正确渲染。。。。。。。。。
        if (buffer != null)
        {
            tracingShader.SetBuffer(0, name, buffer);
        }
    }

    private void SetShaderParameters()
    {
        tracingShader.SetTexture(0, "_SkyboxTexture", skyboxTex);
        tracingShader.SetMatrix("_Camera2World", _camera.cameraToWorldMatrix);
        tracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        tracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        tracingShader.SetFloat("_Seed", Random.value);

        Vector3 l = sun.transform.forward;
        tracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, sun.intensity));

        SetComputeBuffer("nodes", _nodesBuffer);
        SetComputeBuffer("woopTris", _woopTrisBuffer);
        SetComputeBuffer("triIndices", _triIndicesBuffer);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
            {
                _target.Release();
                _converged.Release();
            }

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
            _converged = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _converged.enableRandomWrite = true;
            _converged.Create();

            // Reset sampling
            _currentSample = 0;
        }
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        tracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        tracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/IteratorShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, _converged, _addMaterial);
        Graphics.Blit(_converged, destination);
        _currentSample++;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }
}
