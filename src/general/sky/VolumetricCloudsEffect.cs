using System;
using System.Diagnostics.CodeAnalysis;
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

    [Export(PropertyHint.Range, "1,4,1")]
    public int ResolutionDivisor = 2;

    [Export]
    public bool ProfileGpu;

    private const string SkyResourcesDir = "res://assets/textures/sky/";
    private const string NoiseProfileFileName = "cloud_base_128.res";
    private const string RaymarcherShaderFileName = "res://shaders/sky/clouds_march.glsl";
    private const string UpsamplerShaderFileName = "res://shaders/sky/upsampler.glsl";

    private static readonly StringName CloudContextName = "volumetric_clouds";
    private static readonly StringName CloudTextureName = "cloud_half";

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Global rendering device is disposed by Godot.")]
    private RenderingDevice? renderingDevice;
    private Rid depthSampler;
    private Rid noiseSampler;
    private Rid rayMarcherShader;
    private Rid upsamplerShader;
    private Rid rayMarcherPipeline;
    private Rid upsamplerPipeline;
    private Rid noiseTexture;

    private RDShaderSpirV rayMarcherSpirv = null!;
    private RDShaderSpirV upsamplerSpirv = null!;
    private ImageTexture3D noiseProfile = null!;

    private volatile int state;

    private bool disposed = false;

    private Vector2I currentCloudSize = Vector2I.Zero;
    private uint currentCloudViews;

    public VolumetricCloudsEffect()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        AccessResolvedDepth = true;
    }

    [ExportToolButton("Reload Pipeline")]
    private Callable ReloadPipelineCallable => new(this, MethodName.Reload);

    [ExportToolButton("Generate Noise Profile")]
    private Callable GenerateNoiseProfileResourceCallable => new(this, MethodName.GenerateNoiseProfileResource);

    [ExportToolButton("Dump GPU profiler data")]
    private Callable DumpGpuProfilerData => new(this, MethodName.ReportTimestamps);

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete)
            return;
        if (renderingDevice is null)
            return;

        var rd = renderingDevice;
        var rids = new[]
        {
            rayMarcherPipeline, rayMarcherShader, upsamplerPipeline, upsamplerShader, depthSampler, noiseSampler,
        };

        RenderingServer.CallOnRenderThread(Callable.From(() =>
        {
            var current = RenderingServer.GetRenderingDevice();
            if (current is null || current != rd)
                return;

            foreach (var rid in rids)
            {
                if (rid.IsValid)
                    rd.FreeRid(rid);
            }
        }));
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        switch (state)
        {
            case 0: // kick off async load once
                state = 1;

                // TODO: defer this and then render to avoid I/O on the render thread.
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

        if (renderingDevice is null || !rayMarcherPipeline.IsValid || !upsamplerPipeline.IsValid)
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

        int divisor = Math.Max(ResolutionDivisor, 1);
        var marchSize = new Vector2I(
            Math.Max((size.X + divisor - 1) / divisor, 1),
            Math.Max((size.Y + divisor - 1) / divisor, 1));

        uint viewCount = sceneBuffers.GetViewCount();

        EnsureCloudTexture(sceneBuffers, marchSize, viewCount);

        uint marchGroupsX = ((uint)marchSize.X + 7) / 8;
        uint marchGroupsY = ((uint)marchSize.Y + 7) / 8;
        uint fullGroupsX = ((uint)size.X + 7) / 8;
        uint fullGroupsY = ((uint)size.Y + 7) / 8;

        for (uint view = 0; view < viewCount; view++)
        {
            Rid color = sceneBuffers.GetColorLayer(view);
            Rid depth = sceneBuffers.GetDepthLayer(view);
            Rid cloudTexture = sceneBuffers.GetTextureSlice(CloudContextName, CloudTextureName, view, 0, 1, 1);

            if (!cloudTexture.IsValid)
                continue;

            var projection = sceneData.GetViewProjection(view);
            var cameraTransform = sceneData.GetCamTransform();

            byte[] pushConstantBytes = BuildPushConstant(projection.Inverse(), new Projection(cameraTransform));
            byte[] paramBytes = BuildParamUniform(size, marchSize, cameraTransform.Origin);

            Rid paramUbo = renderingDevice.UniformBufferCreate((uint)paramBytes.Length, paramBytes);
            if (!paramUbo.IsValid)
                continue;

            // Pass 1: ray marcher.
            var marchUniforms = new Godot.Collections.Array<RDUniform>
            {
                MakeImage(0, cloudTexture),
                MakeSampled(1, depthSampler, depth),
                MakeSampled(2, noiseSampler, noiseTexture),
                MakeUniformBuffer(3, paramUbo),
            };

            Rid marchSet = renderingDevice.UniformSetCreate(marchUniforms, rayMarcherShader, 0);

            // Pass 2: bilateral resolve and composite
            var upsampleUniforms = new Godot.Collections.Array<RDUniform>
            {
                MakeImage(0, color),
                MakeSampled(1, depthSampler, depth),
                MakeSampled(2, depthSampler, cloudTexture),
                MakeUniformBuffer(3, paramUbo),
            };

            Rid upsampleSet = renderingDevice.UniformSetCreate(upsampleUniforms, upsamplerShader, 0);

            if (marchSet.IsValid && upsampleSet.IsValid)
            {
                if (ProfileGpu)
                    renderingDevice.CaptureTimestamp("clouds_march_begin");

                long list = renderingDevice.ComputeListBegin();
                renderingDevice.ComputeListBindComputePipeline(list, rayMarcherPipeline);
                renderingDevice.ComputeListBindUniformSet(list, marchSet, 0);
                renderingDevice.ComputeListSetPushConstant(list, pushConstantBytes,
                    (uint)pushConstantBytes.Length);
                renderingDevice.ComputeListDispatch(list, marchGroupsX, marchGroupsY, 1);
                renderingDevice.ComputeListEnd();

                if (ProfileGpu)
                    renderingDevice.CaptureTimestamp("clouds_upsample_begin");

                list = renderingDevice.ComputeListBegin();
                renderingDevice.ComputeListBindComputePipeline(list, upsamplerPipeline);
                renderingDevice.ComputeListBindUniformSet(list, upsampleSet, 0);
                renderingDevice.ComputeListSetPushConstant(list, pushConstantBytes,
                    (uint)pushConstantBytes.Length);
                renderingDevice.ComputeListDispatch(list, fullGroupsX, fullGroupsY, 1);
                renderingDevice.ComputeListEnd();

                if (ProfileGpu)
                    renderingDevice.CaptureTimestamp("clouds_end");
            }

            if (marchSet.IsValid)
                renderingDevice.FreeRid(marchSet);
            if (upsampleSet.IsValid)
                renderingDevice.FreeRid(upsampleSet);

            renderingDevice.FreeRid(paramUbo);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            CloudContextName.Dispose();
            CloudTextureName.Dispose();

            rayMarcherSpirv.Dispose();
            upsamplerSpirv.Dispose();
            noiseProfile.Dispose();
        }

        disposed = true;

        base.Dispose(disposing);
    }

    private static RDUniform MakeImage(int binding, Rid texture)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = binding,
        };
        uniform.AddId(texture);
        return uniform;
    }

    private static RDUniform MakeSampled(int binding, Rid sampler, Rid texture)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.SamplerWithTexture,
            Binding = binding,
        };
        uniform.AddId(sampler);
        uniform.AddId(texture);
        return uniform;
    }

    private static RDUniform MakeUniformBuffer(int binding, Rid buffer)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = binding,
        };
        uniform.AddId(buffer);
        return uniform;
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

    private static RDShaderSpirV LoadSpirV(string path)
    {
        var shaderFile = ResourceLoader.Load<RDShaderFile>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        var spirv = shaderFile.GetSpirV();

        return spirv.CompileErrorCompute != string.Empty ?
            throw new Exception($"Error in shader {path}: {spirv.CompileErrorCompute}") : spirv;
    }

    private void EnsureCloudTexture(RenderSceneBuffersRD sceneBuffers, Vector2I marchSize, uint viewCount)
    {
        bool exists = sceneBuffers.HasTexture(CloudContextName, CloudTextureName);

        if (exists && currentCloudSize == marchSize && currentCloudViews == viewCount)
            return;

        if (exists)
            sceneBuffers.ClearContext(CloudContextName);

        const uint usage = (uint)(RenderingDevice.TextureUsageBits.StorageBit |
            RenderingDevice.TextureUsageBits.SamplingBit);

        sceneBuffers.CreateTexture(CloudContextName, CloudTextureName,
            RenderingDevice.DataFormat.R16G16B16A16Sfloat, usage,
            RenderingDevice.TextureSamples.Samples1, marchSize, viewCount, 1, true, false);

        currentCloudSize = marchSize;
        currentCloudViews = viewCount;
    }

    private byte[] BuildParamUniform(Vector2I fullSize, Vector2I marchSize, Vector3 cameraPosition)
    {
        var bytes = new byte[128];
        int o = 0;

        o = WriteVec4(bytes, o, new Vector4(PlanetCenter.X, PlanetCenter.Y, PlanetCenter.Z, 0.0f));
        o = WriteVec4(bytes, o, new Vector4(PlanetRadius + CloudInnerHeight, PlanetRadius + CloudOuterHeight,
            CloudTileSize, DensityMultiplier));
        o = WriteVec4(bytes, o, new Vector4(fullSize.X, fullSize.Y, 1.0f / fullSize.X, 1.0f / fullSize.Y));
        o = WriteVec4(bytes, o, new Vector4(marchSize.X, marchSize.Y, 1.0f / marchSize.X, 1.0f / marchSize.Y));
        o = WriteVec4(bytes, o, new Vector4(cameraPosition.X, cameraPosition.Y, cameraPosition.Z, 0.0f));

        var sun = SunDirection.Normalized();
        o = WriteVec4(bytes, o, new Vector4(sun.X, sun.Y, sun.Z, SunEnergy));

        _ = WriteVec4(bytes, o, new Vector4(MarchSteps, LightSteps, MaxMarchDistance, Coverage));

        return bytes;
    }

    private void LoadResources()
    {
        rayMarcherSpirv = LoadSpirV(RaymarcherShaderFileName);
        upsamplerSpirv = LoadSpirV(UpsamplerShaderFileName);

        if (rayMarcherSpirv.CompileErrorCompute != string.Empty)
            throw new Exception("Error in shader clouds_march.glsl " + rayMarcherSpirv.CompileErrorCompute);

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
        if (rayMarcherSpirv == null! || upsamplerSpirv == null!)
            throw new Exception("Resources have not been loaded yet.");

        renderingDevice = RenderingServer.GetRenderingDevice();
        if (renderingDevice is null)
            return;

        rayMarcherShader = renderingDevice.ShaderCreateFromSpirV(rayMarcherSpirv);
        rayMarcherPipeline = renderingDevice.ComputePipelineCreate(rayMarcherShader);

        upsamplerShader = renderingDevice.ShaderCreateFromSpirV(upsamplerSpirv);
        upsamplerPipeline = renderingDevice.ComputePipelineCreate(upsamplerShader);

        depthSampler = renderingDevice.SamplerCreate(new RDSamplerState
        {
            MagFilter = RenderingDevice.SamplerFilter.Nearest,
            MinFilter = RenderingDevice.SamplerFilter.Nearest,
            RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge,
            RepeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge,
        });

        noiseSampler = renderingDevice.SamplerCreate(new RDSamplerState
        {
            MagFilter = RenderingDevice.SamplerFilter.Linear,
            MinFilter = RenderingDevice.SamplerFilter.Linear,
            RepeatU = RenderingDevice.SamplerRepeatMode.Repeat,
            RepeatV = RenderingDevice.SamplerRepeatMode.Repeat,
            RepeatW = RenderingDevice.SamplerRepeatMode.Repeat,
        });

        if (noiseProfile is null)
            throw new Exception("Invalid noise texture");

        noiseTexture = RenderingServer.TextureGetRdTexture(noiseProfile.GetRid());
    }

    private void FreeResources()
    {
        FreeRids();

        rayMarcherPipeline = default;
        rayMarcherShader = default;
        upsamplerPipeline = default;
        upsamplerShader = default;
        depthSampler = default;
        noiseSampler = default;
        noiseTexture = default;

        state = 0;

        currentCloudSize = Vector2I.Zero;
        currentCloudViews = 0;
    }

    private void FreeRids()
    {
        if (renderingDevice is null)
            return;

        if (rayMarcherShader.IsValid)
            renderingDevice.FreeRid(rayMarcherShader);
        if (depthSampler.IsValid)
            renderingDevice.FreeRid(depthSampler);
        if (noiseSampler.IsValid)
            renderingDevice.FreeRid(noiseSampler);
        if (rayMarcherPipeline.IsValid)
            renderingDevice.FreeRid(rayMarcherPipeline);
        if (upsamplerPipeline.IsValid)
            renderingDevice.FreeRid(upsamplerPipeline);
        if (upsamplerShader.IsValid)
            renderingDevice.FreeRid(upsamplerShader);
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

    private void ReportTimestamps()
    {
        if (renderingDevice is null)
            return;

        uint count = renderingDevice.GetCapturedTimestampsCount();
        ulong previous = 0;
        string previousName = string.Empty;

        for (uint i = 0; i < count; i++)
        {
            string name = renderingDevice.GetCapturedTimestampName(i);
            ulong gpuTime = renderingDevice.GetCapturedTimestampGpuTime(i);

            if (previousName.StartsWith("clouds_"))
                GD.Print($"{previousName} -> {name}: {gpuTime - previous}");

            previous = gpuTime;
            previousName = name;
        }
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
