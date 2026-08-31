// Instead of using the TOOLS_ENABLED macro, you can use the 'clouds' command in the game console:
// -- clouds reload - Reloads the pipeline
// -- clouds profileenable - Enables profiling
// -- clouds profiledisable - Disables profiling
// -- clouds profileprint - Prints last frame's profiling data
// -- clouds generatenoiseprofile - Generates the noise profile in the game files. Only available outside of release.
//
// #define TOOLS_ENABLED

using System;
using System.Threading;
using Godot;
using Godot.Collections;

/// <summary>
///   Volumetric Clouds Effect for sky rendering.
/// </summary>
#if TOOLS_ENABLED
[Tool]
#endif
[GlobalClass]
public partial class VolumetricCloudsEffect : CompositorEffect
{
#pragma warning disable CA2213
    [Export]
    public CloudsConfig CloudsConfig = new();
#pragma warning restore CA2213

    private const uint PushConstantsBufferSize = 128;
    private const uint UniformParamsBufferSize = 128;

    // Here we are using the .res file extension because Godot doesn't have a specialized file extension for 3D
    // textures.
    private const string NoiseProfileFileName = "cloud_base_128.res";
    private const string SkyResourcesDir = "res://assets/textures/sky/";
    private const string RaymarcherShaderFileName = "res://shaders/sky/clouds_march.glsl";
    private const string UpsamplerShaderFileName = "res://shaders/sky/upsampler.glsl";

    private static readonly Lock InstanceLock = new();

    private static VolumetricCloudsEffect? activeInstance;

    private readonly StringName cloudContextName = "volumetric_clouds";
    private readonly StringName cloudTextureName = "cloud_half";
    private readonly StringName renderBuffersContext = "render_buffers";
    private readonly StringName colorTextureName = "color";
    private readonly StringName depthTextureName = "depth";

    private readonly Array<RDUniform> marchUniforms = [];
    private readonly Array<RDUniform> upsampleUniforms = [];

    private Rid depthSampler;
    private Rid noiseSampler;
    private Rid rayMarcherShader;
    private Rid upsamplerShader;
    private Rid rayMarcherPipeline;
    private Rid upsamplerPipeline;
    private Rid noiseTexture;
    private Rid paramUbo;

    private RDUniform[] uniformPool = new RDUniform[8];

    private byte[] pushConstantsBuffer = new byte[PushConstantsBufferSize];
    private byte[] uniformParamsBuffer = new byte[UniformParamsBufferSize];

    private float lastAttemptedInner = -1.0f;
    private float lastAttemptedOuter = -1.0f;

#pragma warning disable CA2213
    private RenderingDevice? renderingDevice;

    private RDShaderSpirV rayMarcherSpirv = null!;
    private RDShaderSpirV upsamplerSpirv = null!;
    private ImageTexture3D noiseProfile = null!;
#pragma warning restore CA2213

    private volatile int state;

    private bool disposed;
    private bool active;

    private bool profileGpu;

    private Vector2I currentCloudSize = Vector2I.Zero;
    private uint currentCloudViews;

    public VolumetricCloudsEffect()
    {
        // Note that the singleton slot is deliberately not claimed here. Godot constructs effects speculatively
        // (scene deserialization, and the inspector default value probe in editor builds), so a constructor claim
        // is taken by an instance that never ends up rendering anything, starving the real one.
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        AccessResolvedColor = true;
        AccessResolvedDepth = true;

        for (int i = 0; i < uniformPool.Length; ++i)
            uniformPool[i] = new RDUniform();
    }

#if TOOLS_ENABLED
    [ExportToolButton("Reload Pipeline")]
    private Callable ReloadPipelineCallable => new(this, MethodName.Reload);

    [ExportToolButton("Generate Noise Profile")]
    private Callable GenerateNoiseProfileResourceCallable => new(this, MethodName.GenerateNoiseProfileAndReload);

    [ExportToolButton("Profile GPU")]
    private Callable ProfileGpuCallable => new(this, MethodName.ToggleProfileGpu);

    [ExportToolButton("Dump GPU profiler data")]
    private Callable DumpGpuProfilerData => new(this, MethodName.ReportTimestamps);
#endif

    private enum CloudCommandParameters
    {
        Reload,
        ProfileEnable,
        ProfileDisable,
        ProfilePrint,
        GenerateNoiseProfile,
    }

    /// <summary>
    ///   Sun parameters the clouds are lit with. This is owned and set by <see cref="SkyEquippedEnvironment"/> so
    ///   that the clouds and the sky agree on where the sun is. A default is kept here for standalone use.
    /// </summary>
    public SunConfig SunConfig { get; set; } = new();

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete)
            return;

        ReleaseActive();

        if (renderingDevice is null)
            return;

        var rd = renderingDevice;
        var rids = new[]
        {
            rayMarcherPipeline, rayMarcherShader, upsamplerPipeline, upsamplerShader, depthSampler, noiseSampler,
            paramUbo,
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
        // Temporarily disable cloud rendering in editor
        if (Engine.IsEditorHint())
            return;

        // This has to come before the loading below, otherwise an instance that never renders still loads the
        // shaders and builds the compute pipelines before bailing out
        if (!TryBecomeActive())
            return;

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

        int divisor = Math.Max(CloudsConfig.ResolutionDivisor, 1);
        var marchSize = new Vector2I(Math.Max((size.X + divisor - 1) / divisor, 1),
            Math.Max((size.Y + divisor - 1) / divisor, 1));

        uint viewCount = sceneBuffers.GetViewCount();

        EnsureCloudTexture(sceneBuffers, marchSize, viewCount);

        uint marchGroupsX = ((uint)marchSize.X + 7) / 8;
        uint marchGroupsY = ((uint)marchSize.Y + 7) / 8;
        uint fullGroupsX = ((uint)size.X + 7) / 8;
        uint fullGroupsY = ((uint)size.Y + 7) / 8;

        for (uint view = 0; view < viewCount; ++view)
        {
            Rid color = sceneBuffers.GetTextureSlice(renderBuffersContext, colorTextureName, view, 0, 1, 1);
            Rid depth = sceneBuffers.GetTextureSlice(renderBuffersContext, depthTextureName, view, 0, 1, 1);
            Rid cloudTexture = sceneBuffers.GetTextureSlice(cloudContextName, cloudTextureName, view, 0, 1, 1);

            if (!cloudTexture.IsValid)
                continue;

            var projection = sceneData.GetViewProjection(view);
            var eyeOffset = sceneData.GetViewEyeOffset(view);
            var cameraTransform = sceneData.GetCamTransform();
            var viewTransform = cameraTransform.TranslatedLocal(eyeOffset);

            var paramSpan = uniformParamsBuffer.AsSpan();

            RenderingUtils.UpdateProjectionsPushConstant(pushConstantsBuffer, projection.Inverse(),
                new Projection(viewTransform));
            UpdateParamUniform(paramSpan, size, marchSize, viewTransform.Origin);

            renderingDevice.BufferUpdate(paramUbo, 0, UniformParamsBufferSize, paramSpan,
                RenderingDevice.BarrierMask.Compute);

            // Pass 1: ray marcher.
            // Uniforms 0-3 bindings 0-3
            marchUniforms.Clear();
            RenderingUtils.UpdateImage(uniformPool[0], 0, cloudTexture);
            RenderingUtils.UpdateSampled(uniformPool[1], 1, depthSampler, depth);
            RenderingUtils.UpdateSampled(uniformPool[2], 2, noiseSampler, noiseTexture);
            RenderingUtils.UpdateUniformBuffer(uniformPool[3], 3, paramUbo);
            marchUniforms.AddRange(uniformPool.AsSpan(0, 4));

            Rid marchSet = UniformSetCacheRD.GetCache(rayMarcherShader, 0, marchUniforms);

            // Pass 2: bilateral resolve and composite. Upsample pass.
            // Uniforms 4-7 bindings 0-3
            upsampleUniforms.Clear();
            RenderingUtils.UpdateImage(uniformPool[4], 0, color);
            RenderingUtils.UpdateSampled(uniformPool[5], 1, depthSampler, depth);
            RenderingUtils.UpdateSampled(uniformPool[6], 2, depthSampler, cloudTexture);
            RenderingUtils.UpdateUniformBuffer(uniformPool[7], 3, paramUbo);
            upsampleUniforms.AddRange(uniformPool.AsSpan(4, 4));

            Rid upsampleSet = UniformSetCacheRD.GetCache(upsamplerShader, 0, upsampleUniforms);

            if (marchSet.IsValid && upsampleSet.IsValid)
            {
                if (profileGpu)
                    renderingDevice.CaptureTimestamp("clouds_march_begin");

                long list = renderingDevice.ComputeListBegin();
                renderingDevice.ComputeListBindComputePipeline(list, rayMarcherPipeline);
                renderingDevice.ComputeListBindUniformSet(list, marchSet, 0);
                renderingDevice.ComputeListSetPushConstant(list, pushConstantsBuffer, PushConstantsBufferSize);
                renderingDevice.ComputeListDispatch(list, marchGroupsX, marchGroupsY, 1);
                renderingDevice.ComputeListEnd();

                if (profileGpu)
                    renderingDevice.CaptureTimestamp("clouds_upsample_begin");

                list = renderingDevice.ComputeListBegin();
                renderingDevice.ComputeListBindComputePipeline(list, upsamplerPipeline);
                renderingDevice.ComputeListBindUniformSet(list, upsampleSet, 0);
                renderingDevice.ComputeListSetPushConstant(list, pushConstantsBuffer, PushConstantsBufferSize);
                renderingDevice.ComputeListDispatch(list, fullGroupsX, fullGroupsY, 1);
                renderingDevice.ComputeListEnd();

                if (profileGpu)
                    renderingDevice.CaptureTimestamp("clouds_end");
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (InstanceLock)
        {
            if (disposed)
                return;

            disposed = true;

            ReleaseActive();
        }

        if (disposing)
        {
            cloudContextName.Dispose();
            cloudTextureName.Dispose();
            renderBuffersContext.Dispose();
            colorTextureName.Dispose();
            depthTextureName.Dispose();

            rayMarcherSpirv = null!;
            upsamplerSpirv = null!;
            noiseProfile = null!;

            RenderingServer.CallOnRenderThread(Callable.From(FreeResources));
        }

        base.Dispose(disposing);
    }

    private static RDShaderSpirV LoadSpirV(string path)
    {
        var shaderFile = ResourceLoader.Load<RDShaderFile>(path, cacheMode: ResourceLoader.CacheMode.Ignore);

        if (shaderFile is null)
            throw new Exception($"Failed to load shader file: {path}");

        var spirv = shaderFile.GetSpirV();

        if (spirv.CompileErrorCompute != string.Empty)
            throw new Exception($"Error in shader {path}: {spirv.CompileErrorCompute}");

        return spirv;
    }

    /// <summary>
    ///   This is used by the editor tool and the cloud command to generate and save a new noise profile resource.
    /// </summary>
    private static bool GenerateNoiseProfileResource(int seed)
    {
        GD.Print("Baking a new Noise Profile Resource. This may take a few seconds...");

        var texture = NoiseUtils.BakePerlinWorleyChunkParallel(128, seed);

        const string dir = SkyResourcesDir;
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(dir)))
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(dir));

        var error = ResourceSaver.Save(texture, dir + NoiseProfileFileName);

        if (error == Error.Ok)
            return true;

        GD.PrintErr(error);

        return false;
    }

    [Command("clouds", false, "Utility command for cloud effect debugging.")]
    private static bool CloudsCommand(CommandContext context, CloudCommandParameters parameter1)
    {
        VolumetricCloudsEffect? targetInstance;

        lock (InstanceLock)
        {
            targetInstance = activeInstance;
        }

        if (targetInstance is null)
        {
            context.Print("Current instance is null. Clouds aren't currently being rendered.");

            return false;
        }

        switch (parameter1)
        {
            case CloudCommandParameters.Reload:
                targetInstance.Reload();
                return true;
            case CloudCommandParameters.ProfileEnable:
                targetInstance.profileGpu = true;
                return true;
            case CloudCommandParameters.ProfileDisable:
                targetInstance.profileGpu = false;
                return true;
            case CloudCommandParameters.ProfilePrint:
                if (!targetInstance.profileGpu)
                {
                    context.PrintErr("Not currently profiling. Please execute 'clouds ProfileEnable' first.");

                    return false;
                }

                RenderingServer.CallOnRenderThread(Callable.From(() => targetInstance.ReportTimestamps()));
                return true;
            case CloudCommandParameters.GenerateNoiseProfile:
                // It's pointless to enable this in release mode, as the asset should be already baked then and the
                // res:// folder is readonly anyway.
                if (OS.HasFeature("release"))
                {
                    context.PrintErr("This command is disabled in release mode.");

                    return false;
                }

                targetInstance.GenerateNoiseProfileAndReload();
                return true;
            default:
                return false;
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

        paramUbo = renderingDevice.UniformBufferCreate(UniformParamsBufferSize, uniformParamsBuffer);
    }

    /// <summary>
    ///   Claims the single slot that is allowed to render the clouds, if it is free. Only the instance that is
    ///   actually being rendered ever asks, which is what keeps the slot away from the throwaway instances Godot
    ///   builds while loading a scene.
    /// </summary>
    /// <returns>True if this instance holds the slot and should do the cloud rendering work.</returns>
    private bool TryBecomeActive()
    {
        if (Volatile.Read(ref active))
            return true;

        lock (InstanceLock)
        {
            if (disposed)
                return false;

            if (activeInstance is not null)
                return false;

            activeInstance = this;
            Volatile.Write(ref active, true);
        }

        return true;
    }

    /// <summary>
    ///   Gives up the rendering slot if this instance holds it. Whichever instance renders next takes it over.
    /// </summary>
    private void ReleaseActive()
    {
        lock (InstanceLock)
        {
            if (!ReferenceEquals(activeInstance, this))
                return;

            activeInstance = null;
            Volatile.Write(ref active, false);
        }
    }

    private void EnsureCloudTexture(RenderSceneBuffersRD sceneBuffers, Vector2I marchSize, uint viewCount)
    {
        bool exists = sceneBuffers.HasTexture(cloudContextName, cloudTextureName);

        if (exists && currentCloudSize == marchSize && currentCloudViews == viewCount)
            return;

        if (exists)
            sceneBuffers.ClearContext(cloudContextName);

        const uint usage = (uint)(RenderingDevice.TextureUsageBits.StorageBit |
            RenderingDevice.TextureUsageBits.SamplingBit);

        sceneBuffers.CreateTexture(cloudContextName, cloudTextureName,
            RenderingDevice.DataFormat.R16G16B16A16Sfloat, usage,
            RenderingDevice.TextureSamples.Samples1, marchSize, viewCount, 1, true, false);

        currentCloudSize = marchSize;
        currentCloudViews = viewCount;
    }

    private void UpdateParamUniform(Span<byte> paramSpan, Vector2I fullSize, Vector2I marchSize, Vector3 cameraPosition)
    {
        int offset = 0;

        float cloudInnerHeight = CloudsConfig.CloudInnerHeight;
        float cloudOuterHeight = CloudsConfig.CloudOuterHeight;
        float planetRadius = CloudsConfig.PlanetRadius;
        float cloudTileSize = CloudsConfig.CloudTileSize;
        float densityMultiplier = CloudsConfig.DensityMultiplier;

        Vector3 planetCenter = CloudsConfig.PlanetCenter;

        float safeInner = cloudInnerHeight;
        float safeOuter = cloudOuterHeight;
        bool isValid = true;

        if (safeInner < 0.0f)
        {
            safeInner = 0.0f;
            isValid = false;
        }

        if (safeOuter <= safeInner)
        {
            safeOuter = safeInner + 1.0f;
            isValid = false;
        }

        if (!isValid)
        {
            if (Math.Abs(cloudInnerHeight - lastAttemptedInner) > 0.001f ||
                Math.Abs(cloudOuterHeight - lastAttemptedOuter) > 0.001f)
            {
                GD.PushError($"VolumetricCloudsEffect: Invalid cloud heights. Outer ({cloudOuterHeight})" +
                    $"must be > Inner ({cloudInnerHeight}) >= 0. Clamping to {safeOuter} and {safeInner}.");
            }
        }

        lastAttemptedInner = cloudInnerHeight;
        lastAttemptedOuter = cloudOuterHeight;

        float cloudInner = safeInner;
        float cloudOuter = Math.Max(safeOuter, safeInner + 1.0f);

        offset = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(planetCenter.X, planetCenter.Y, planetCenter.Z,
            0.0f));
        offset = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(planetRadius + cloudInner,
            planetRadius + cloudOuter, cloudTileSize, densityMultiplier));
        offset = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(fullSize.X, fullSize.Y, 1.0f / fullSize.X,
            1.0f / fullSize.Y));
        offset = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(marchSize.X, marchSize.Y, 1.0f / marchSize.X,
            1.0f / marchSize.Y));
        offset = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(cameraPosition.X, cameraPosition.Y,
            cameraPosition.Z, 0.0f));

        var sun = SunConfig.GetNormalizedDirection();
        offset = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(sun.X, sun.Y, sun.Z,
            SunConfig.SunEnergy));

        _ = RenderingUtils.WriteVec4(paramSpan, offset, new Vector4(CloudsConfig.MarchSteps, CloudsConfig.LightSteps,
            CloudsConfig.MaxMarchDistance, CloudsConfig.Coverage));
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
            noiseProfile = ResourceLoader.Load<ImageTexture3D>(noiseProfilePath,
                cacheMode: ResourceLoader.CacheMode.Replace);
        }
        else
        {
            throw new Exception("No noise profile resource asset has been found. Please generate it with the command" +
                " 'clouds GenerateNoiseProfile' and reload the current scene.");
        }
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
        paramUbo = default;

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
        if (upsamplerShader.IsValid)
            renderingDevice.FreeRid(upsamplerShader);
        if (depthSampler.IsValid)
            renderingDevice.FreeRid(depthSampler);
        if (noiseSampler.IsValid)
            renderingDevice.FreeRid(noiseSampler);
        if (paramUbo.IsValid)
            renderingDevice.FreeRid(paramUbo);
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

        for (uint i = 0; i < count; ++i)
        {
            string name = renderingDevice.GetCapturedTimestampName(i);
            ulong gpuTime = renderingDevice.GetCapturedTimestampGpuTime(i);

            // Print only clouds timestamps and prevent printing clouds_end to clouds_march_begin in multi-view cases.
            if (previousName.StartsWith("clouds_") && !name.StartsWith("clouds_march_begin"))
                GD.Print($"{previousName} -> {name}: {gpuTime - previous}");

            previous = gpuTime;
            previousName = name;
        }
    }

    private void GenerateNoiseProfileAndReload()
    {
        if (GenerateNoiseProfileResource(CloudsConfig.Seed))
            Reload();
    }

    private void ToggleProfileGpu()
    {
        profileGpu = !profileGpu;
    }
}
