using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Utility class to provide Forward+ boilerplate methods.
/// </summary>
public static class RenderingUtils
{
    /// <summary>
    ///   Updates an image uniform given its shader binding and a texture RID.
    /// </summary>
    public static void UpdateImage(RDUniform uniform, int binding, Rid texture)
    {
        uniform.ClearIds();
        uniform.UniformType = RenderingDevice.UniformType.Image;
        uniform.Binding = binding;
        uniform.AddId(texture);
    }

    /// <summary>
    ///   Updates a sampler uniform given its shader binding, a sampler definition, and a texture RID.
    /// </summary>
    public static void UpdateSampled(RDUniform uniform, int binding, Rid sampler, Rid texture)
    {
        uniform.ClearIds();
        uniform.UniformType = RenderingDevice.UniformType.SamplerWithTexture;
        uniform.Binding = binding;
        uniform.AddId(sampler);
        uniform.AddId(texture);
    }

    /// <summary>
    ///   Updates a UBO given its shader binding and its buffer RID.
    /// </summary>
    public static void UpdateUniformBuffer(RDUniform uniform, int binding, Rid buffer)
    {
        uniform.ClearIds();
        uniform.UniformType = RenderingDevice.UniformType.UniformBuffer;
        uniform.Binding = binding;
        uniform.AddId(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteFloat(Span<byte> buffer, int offset, float value)
    {
        MemoryMarshal.Write(buffer[offset..], value);
        return offset + 4;
    }

    public static int WriteVec4(Span<byte> buffer, int offset, Vector4 v)
    {
        offset = WriteFloat(buffer, offset, v.X);
        offset = WriteFloat(buffer, offset, v.Y);
        offset = WriteFloat(buffer, offset, v.Z);
        offset = WriteFloat(buffer, offset, v.W);
        return offset;
    }

    public static byte[] BuildProjectionsPushConstant(Projection invViewProjection, Projection camProjection)
    {
        var bytes = new byte[128];

        UpdateProjectionsPushConstant(bytes.AsSpan(), invViewProjection, camProjection);

        return bytes;
    }

    /// <summary>
    ///   Updates a byte buffer with the provided inverse view projection and camera projection.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The resulting buffer is usually used for push constants in compute shaders.
    ///   </para>
    /// </remarks>
    public static void UpdateProjectionsPushConstant(Span<byte> bytes, Projection invViewProjection,
        Projection camProjection)
    {
        int offset = 0;

        // mat4 inv_projection
        offset = WriteVec4(bytes, offset, invViewProjection.X);
        offset = WriteVec4(bytes, offset, invViewProjection.Y);
        offset = WriteVec4(bytes, offset, invViewProjection.Z);
        offset = WriteVec4(bytes, offset, invViewProjection.W);

        // mat4 cam_transform
        offset = WriteVec4(bytes, offset, camProjection.X);
        offset = WriteVec4(bytes, offset, camProjection.Y);
        offset = WriteVec4(bytes, offset, camProjection.Z);
        _ = WriteVec4(bytes, offset, camProjection.W);
    }
}
