using System;
using Godot;

public static class RenderingUtils
{
    public static RDUniform MakeImage(int binding, Rid texture)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = binding,
        };
        uniform.AddId(texture);
        return uniform;
    }

    public static RDUniform MakeSampled(int binding, Rid sampler, Rid texture)
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

    public static int WriteFloat(byte[] buffer, int offset, float value)
    {
        BitConverter.TryWriteBytes(buffer.AsSpan(offset, sizeof(float)), value);
        return offset + 4;
    }

    public static int WriteVec4(byte[] buf, int offset, Vector4 v)
    {
        offset = WriteFloat(buf, offset, v.X);
        offset = WriteFloat(buf, offset, v.Y);
        offset = WriteFloat(buf, offset, v.Z);
        offset = WriteFloat(buf, offset, v.W);
        return offset;
    }

    public static RDUniform MakeUniformBuffer(int binding, Rid buffer)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = binding,
        };
        uniform.AddId(buffer);
        return uniform;
    }

    public static byte[] BuildProjectionsPushConstant(Projection invViewProjection, Projection camProjection)
    {
        var bytes = new byte[128];
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

        return bytes;
    }
}
