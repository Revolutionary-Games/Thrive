using System;
using Godot;

/// <summary>
///   Volumetric Clouds Effect for sky rendering.
/// </summary>
[Tool]
[GlobalClass]
public partial class VolumetricCloudsEffect : CompositorEffect
{
    [Export]
    public Vector3 PlanetCenter = Vector3.Zero;

    [Export]
    public float PlanetRadius = 2000.0f;

    [Export]
    public float CloudInnerHeight = 100.0f;

    [Export]
    public float CloudOuterHeight = 200.0f;

    [Export]
    public int Seed = 1234;

    [Export]
    public Vector3 SunDirection = new Vector3(0.4f, 0.8f, 0.3f).Normalized();

    [Export]
    public float SunEnergy = 25.0f;

    [Export]
    public float CloudTileSize = 200.0f;

    [Export]
    public float DensityMultiplier = 1.0f;

    [Export]
    public float Coverage = 0.3f;

    [Export]
    public int MarchSteps = 64;

    [Export]
    public int LightSteps = 6;

    [Export]
    public float MaxMarchDistance = 8000.0f;

    private const string SkyResourcesDir = "res://assets/textures/sky/";
    private const string NoiseProfileFileName = "cloud_base_128.res";
    private const string SkyShaderFileName = "res://shaders/sky/clouds_march.glsl";

    [ExportToolButton("Reload Pipeline")]
    private Callable ReloadPipelineCallable => new(this, MethodName.Reload);

    [ExportToolButton("Generate Noise Profile")]
    private Callable GenerateNoiseProfileResourceCallable => new(this, MethodName.GenerateNoiseProfileResource);

    private RenderingDevice? renderingDevice;
    private Rid depthSampler;
    private Rid noiseSampler;
    private Rid shader;
    private Rid pipeline;
    private Rid noiseTexture;

    private RDShaderSpirV loadedSpirv = null!;
    private ImageTexture3D noiseProfile = null!;

    private volatile int state;

    public VolumetricCloudsEffect()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        AccessResolvedDepth = true;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete)
            return;
        if (renderingDevice is null)
            return;

        var rd = renderingDevice;
        var pipelineRid = pipeline;
        var shaderRid = shader;
        var depthSamplerRid = depthSampler;
        var noiseSamplerRid = noiseSampler;

        RenderingServer.CallOnRenderThread(Callable.From(() =>
        {
            var current = RenderingServer.GetRenderingDevice();
            if (current != null && current == rd)
            {
                if (pipelineRid.IsValid)
                    rd.FreeRid(pipelineRid);
                if (shaderRid.IsValid)
                    rd.FreeRid(shaderRid);
                if (depthSamplerRid.IsValid)
                    rd.FreeRid(depthSamplerRid);
                if (noiseSamplerRid.IsValid)
                    rd.FreeRid(noiseSamplerRid);
            }
        }));
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        switch (state)
        {
            case 0: // kick off async load once
                state = 1;
                LoadResources();
                state = 2;
                return;

            case 1: // still loading
                return;

            case 2: // load done
                InitializeCompute();
                state = 3;
                return;

            case 3:
                break;
        }

        if (renderingDevice is null || !pipeline.IsValid)
            return;

        if (effectCallbackType != (int)EffectCallbackTypeEnum.PostTransparent)
            return;

        using var sceneBuffers = renderData.GetRenderSceneBuffers() as RenderSceneBuffersRD;
        var sceneData = renderData.GetRenderSceneData() as RenderSceneDataRD;
        if (sceneBuffers is null || sceneData is null)
            return;

        var size = sceneBuffers.GetInternalSize();
        if (size.X == 0 || size.Y == 0)
            return;

        uint xGroups = ((uint)size.X + 7) / 8;
        uint yGroups = ((uint)size.Y + 7) / 8;

        uint viewCount = sceneBuffers.GetViewCount();
        for (uint view = 0; view < viewCount; view++)
        {
            Rid color = sceneBuffers.GetColorLayer(view);
            Rid depth = sceneBuffers.GetDepthLayer(view);

            var colorUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.Image,
                Binding = 0,
            };
            colorUniform.AddId(color);

            var depthUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.SamplerWithTexture,
                Binding = 1,
            };
            depthUniform.AddId(depthSampler);
            depthUniform.AddId(depth);

            var noiseTextureUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.SamplerWithTexture,
                Binding = 2,
            };
            noiseTextureUniform.AddId(noiseSampler);
            noiseTextureUniform.AddId(noiseTexture);

            var renderSceneData = renderData.GetRenderSceneData();
            var projection = renderSceneData.GetViewProjection(view);
            var inverseProjection = projection.Inverse();
            var cameraTransform = renderSceneData.GetCamTransform();
            var cameraPosition = cameraTransform.Origin;
            var cameraProjection = new Projection(cameraTransform);

            byte[] pushConstantBytes = BuildPushConstant(inverseProjection, cameraProjection);
            byte[] paramUniformBytes = BuildParamUniform(new Vector2(size.X, size.Y), cameraPosition);

            Rid paramUboId = renderingDevice.UniformBufferCreate((uint)paramUniformBytes.Length, paramUniformBytes);

            if (!paramUboId.IsValid)
                continue;

            var paramUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.UniformBuffer,
                Binding = 3,
            };
            paramUniform.AddId(paramUboId);

            var uniforms = new Godot.Collections.Array<RDUniform>
            {
                colorUniform, depthUniform, noiseTextureUniform, paramUniform,
            };

            Rid uniformSet = renderingDevice.UniformSetCreate(uniforms, shader, 0);

            if (!uniformSet.IsValid)
                continue;

            long computeList = renderingDevice.ComputeListBegin();
            renderingDevice.ComputeListBindComputePipeline(computeList, pipeline);
            renderingDevice.ComputeListBindUniformSet(computeList, uniformSet, 0);
            renderingDevice.ComputeListSetPushConstant(computeList, pushConstantBytes, (uint)pushConstantBytes.Length);
            renderingDevice.ComputeListDispatch(computeList, xGroups, yGroups, 1);
            renderingDevice.ComputeListEnd();

            renderingDevice.FreeRid(uniformSet);
            renderingDevice.FreeRid(paramUboId);
        }
    }

    private static byte[] BuildPushConstant(Projection invViewProjection, Projection camProjection)
    {
        var bytes = new byte[128];
        int o = 0;

        // mat4 inv_projection
        o = WriteVec4(bytes, o, invViewProjection.X);
        o = WriteVec4(bytes, o, invViewProjection.Y);
        o = WriteVec4(bytes, o, invViewProjection.Z);
        o = WriteVec4(bytes, o, invViewProjection.W);

        // mat4 cam_transform
        o = WriteVec4(bytes, o, camProjection.X);
        o = WriteVec4(bytes, o, camProjection.Y);
        o = WriteVec4(bytes, o, camProjection.Z);
        _ = WriteVec4(bytes, o, camProjection.W);

        return bytes;
    }

    private static int WriteFloat(byte[] buf, int offset, float v)
    {
        BitConverter.GetBytes(v).CopyTo(buf, offset);
        return offset + 4;
    }

    private static int WriteVec4(byte[] buf, int offset, Vector4 v)
    {
        offset = WriteFloat(buf, offset, v.X);
        offset = WriteFloat(buf, offset, v.Y);
        offset = WriteFloat(buf, offset, v.Z);
        offset = WriteFloat(buf, offset, v.W);
        return offset;
    }

    private byte[] BuildParamUniform(Vector2 size, Vector3 cameraPosition)
    {
        var bytes = new byte[96];
        int o = 0;
        o = WriteVec4(bytes, o, new Vector4(PlanetCenter.X, PlanetCenter.Y, PlanetCenter.Z, 0f));
        o = WriteVec4(bytes, o, new Vector4(PlanetRadius + CloudInnerHeight, PlanetRadius + CloudOuterHeight,
            CloudTileSize, DensityMultiplier));
        o = WriteVec4(bytes, o, new Vector4(size.X, size.Y, 1f / size.X, 1f / size.Y));
        o = WriteVec4(bytes, o, new Vector4(cameraPosition.X, cameraPosition.Y, cameraPosition.Z, 0f));

        var sun = SunDirection.Normalized();
        o = WriteVec4(bytes, o, new Vector4(sun.X, sun.Y, sun.Z, SunEnergy));
        _ = WriteVec4(bytes, o, new Vector4(MarchSteps, LightSteps, MaxMarchDistance, Coverage));
        return bytes;
    }

    private void LoadResources()
    {
        var shaderFile = ResourceLoader.Load<RDShaderFile>(SkyShaderFileName,
            cacheMode: ResourceLoader.CacheMode.Ignore);
        loadedSpirv = shaderFile.GetSpirV();

        if (loadedSpirv.CompileErrorCompute != string.Empty)
            throw new Exception("Error in shader clouds_march.glsl " + loadedSpirv.CompileErrorCompute);

        const string noiseProfilePath = SkyResourcesDir + NoiseProfileFileName;
        if (ResourceLoader.Exists(noiseProfilePath))
        {
            noiseProfile = ResourceLoader.Load<ImageTexture3D>(noiseProfilePath);
        }
        else
        {
            GD.PrintErr("No noise profile resource has been found. The resource will be baked now on runtime," +
                "and it may take several seconds. This error must be corrected.");

            noiseProfile = NoiseUtils.BakePerlinWorleyChunkParallel(128, Seed);
        }
    }

    private void InitializeCompute()
    {
        if (loadedSpirv == null!)
            throw new Exception("Resources have not been loaded yet.");

        renderingDevice = RenderingServer.GetRenderingDevice();
        if (renderingDevice is null)
            return;

        shader = renderingDevice.ShaderCreateFromSpirV(loadedSpirv);
        pipeline = renderingDevice.ComputePipelineCreate(shader);

        var samplerState = new RDSamplerState
        {
            MagFilter = RenderingDevice.SamplerFilter.Nearest,
            MinFilter = RenderingDevice.SamplerFilter.Nearest,
            RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge,
            RepeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge,
        };

        depthSampler = renderingDevice.SamplerCreate(samplerState);

        var noiseSamplerState = new RDSamplerState
        {
            MagFilter = RenderingDevice.SamplerFilter.Linear,
            MinFilter = RenderingDevice.SamplerFilter.Linear,
            RepeatU = RenderingDevice.SamplerRepeatMode.Repeat,
            RepeatV = RenderingDevice.SamplerRepeatMode.Repeat,
            RepeatW = RenderingDevice.SamplerRepeatMode.Repeat,
        };
        noiseSampler = renderingDevice.SamplerCreate(noiseSamplerState);

        if (noiseProfile is null)
            throw new Exception("Invalid noise texture");

        noiseTexture = RenderingServer.TextureGetRdTexture(noiseProfile.GetRid());
    }

    private void FreeResources()
    {
        FreeRids();

        pipeline = default;
        shader = default;
        depthSampler = default;
        noiseSampler = default;
        noiseTexture = default;

        state = 0;
    }

    private void FreeRids()
    {
        if (shader.IsValid)
            renderingDevice?.FreeRid(shader);
        if (depthSampler.IsValid)
            renderingDevice?.FreeRid(depthSampler);
        if (noiseSampler.IsValid)
            renderingDevice?.FreeRid(noiseSampler);
        if (pipeline.IsValid)
            renderingDevice?.FreeRid(pipeline);
    }

    private void Reload()
    {
        GD.Print("Requesting sky renderer pipeline reload.");

        RenderingServer.CallOnRenderThread(Callable.From(() =>
        {
            FreeResources();

            GD.Print("Sky renderer pipeline reloaded.");
        }));
    }

    /// <summary>
    ///   This is used by the editor tool to generate a new noise profile resource.
    /// </summary>
    private void GenerateNoiseProfileResource()
    {
        GD.Print("Baking a new Noise Profile Resource...");

        var texture = NoiseUtils.BakePerlinWorleyChunkParallel(128, Seed);

        const string dir = SkyResourcesDir;
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(dir)))
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(dir));

        var error = ResourceSaver.Save(texture, dir + NoiseProfileFileName);

        if (error != Error.Ok)
        {
            GD.PrintErr(error);
        }
        else
        {
            // Here we rebuild the pipeline to make sure everything runs on the render thread.
            Reload();

            GD.Print("Noise Profile Resource baking done.");
        }
    }
}
