using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TracerBehaviour : MonoBehaviour
{
    public bool useCachedBVH = true;
    [Range(1, 10)]
    public int spps = 1;
    public ComputeShader tracingShader;
    public Texture skyboxTex;
    public Light sun;

    public GPU_BVHData gpuBVH;

    private CameraSettings _cameraSettings;
    private RenderTexture _target;
    private RenderTexture _converged;
    private Material _addMaterial;
    private uint _currentSample = 0;

    // TODO: Move to a better position.
    // ======================= Compute buffers =======================
    // * bvh *
    private ComputeBuffer _nodesBuffer;
    private ComputeBuffer _woopTrisBuffer;
    private ComputeBuffer _triIndicesBuffer;

    // per vertex
    private ComputeBuffer _verticesBuffer;
    private ComputeBuffer _normalsBuffer;
    private ComputeBuffer _uv0sBuffer;

    // per triangle
    private ComputeBuffer _trianglesBuffer;
    private ComputeBuffer _materialsBuffer;

    // textures
    private Texture2DArray _diffuseTextures;
    // ======================= Compute buffers =======================

    // The scene be rendered.
    private Scene _theScene;

    private void Awake()
    {
        _cameraSettings = GetComponent<CameraSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (gpuBVH == null)
        {
            Debug.Log("No cached BVH data found, building BVH now.");
            _theScene = new Scene(true);
            var bvhScene = new BVHScene(_theScene.triangles, _theScene.vertices);
            var cpuBVH = new CPU_BVHData(bvhScene);
            CPU_SBVHBuilder.Build(cpuBVH);
            gpuBVH = new GPU_BVHData().Generate(cpuBVH);
        }
        else
        {
            Debug.Log("Using cached BVH data.");
        }

        // bind texture2darray
        var diffuseTexLen = _theScene.diffuseTextures.Count;
        if (_theScene.diffuseTextures.Count > 0)
        {
            var tex = _theScene.diffuseTextures[0];
            _diffuseTextures = new Texture2DArray(tex.width, tex.height, diffuseTexLen, tex.format, true);
            for (int i = 0; i < diffuseTexLen; i++)
            {
                Graphics.CopyTexture(_theScene.diffuseTextures[i], 0, _diffuseTextures, i);
            }
        }

        // bvh
        CreateComputeBuffer(ref _nodesBuffer, gpuBVH.nodes, 16);
        CreateComputeBuffer(ref _woopTrisBuffer, gpuBVH.woopTris, 16);
        CreateComputeBuffer(ref _triIndicesBuffer, gpuBVH.triIndices, 4);

        // per vertex
        CreateComputeBuffer(ref _verticesBuffer, _theScene.vertices, 12);
        CreateComputeBuffer(ref _normalsBuffer, _theScene.normals, 12);
        CreateComputeBuffer(ref _uv0sBuffer, _theScene.uv0s, 8);

        // per triangle
        CreateComputeBuffer(ref _trianglesBuffer, _theScene.triangles, 12);
        CreateComputeBuffer(ref _materialsBuffer, _theScene.materials, 144);
    }

    private void OnDestroy()
    {
        _nodesBuffer?.Release();
        _woopTrisBuffer?.Release();
        _triIndicesBuffer?.Release();

        _verticesBuffer?.Release();
        _normalsBuffer?.Release();
        _triIndicesBuffer?.Release();

        _trianglesBuffer?.Release();
        _materialsBuffer?.Release();
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged || _cameraSettings.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
            _cameraSettings.hasChanged = false;
        }
    }

    public static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
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
        if (buffer != null)
        {
            tracingShader.SetBuffer(0, name, buffer);
        }
    }

    private void SetShaderParameters()
    {
        _cameraSettings.SetData(tracingShader);
        tracingShader.SetInt("_SPPS", spps);
        tracingShader.SetTexture(0, "_SkyboxTexture", skyboxTex);
        tracingShader.SetFloat("_Seed", Random.value);
        

        for (int i = 0; i < _theScene.diffuseTextures.Count; i++)
        {
            tracingShader.SetTexture(0, string.Format("_Texture{0}", i), _theScene.diffuseTextures[i]);
        }

        Vector3 l = sun.transform.forward;
        tracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, sun.intensity));

        // bvh data
        SetComputeBuffer("_BVHNodes", _nodesBuffer);
        SetComputeBuffer("_BVHWoopTris", _woopTrisBuffer);
        SetComputeBuffer("_BVHTriIndices", _triIndicesBuffer);

        // scene raw data
        // per vertex
        SetComputeBuffer("_Vertices", _verticesBuffer);
        SetComputeBuffer("_Normals", _normalsBuffer);
        SetComputeBuffer("_UV0s", _uv0sBuffer);

        // per triangle
        SetComputeBuffer("_Triangles", _trianglesBuffer);
        SetComputeBuffer("_MatUbers", _materialsBuffer);

        tracingShader.SetTexture(0, "_DiffuseTextures", _diffuseTextures);
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
            //_converged.enableRandomWrite = true;
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
