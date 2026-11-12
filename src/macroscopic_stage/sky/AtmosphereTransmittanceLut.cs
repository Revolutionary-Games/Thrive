using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Bakes how much sunlight survives the trip from the top of an atmosphere down to any point inside it, indexed by
///   the altitude of the point and the angle of the sun from its zenith.
/// </summary>
/// <remarks>
///   <para>
///     This is baked on the CPU so that it works with every renderer. The approach is from Bruneton and Neyret,
///     "Precomputed Atmospheric Scattering" (2008), and the mapping from Bruneton's 2017 reimplementation.
///   </para>
/// </remarks>
public static class AtmosphereTransmittanceLut
{
    public const int Width = 256;
    public const int Height = 64;

    private const int IntegrationSteps = 40;
    private const int Channels = 3;
    private const int BytesPerChannel = 2;

    public static Image Bake(AtmosphereConfig atmosphere, Vector3 rayleighScattering, TraceGasConfig traceGases,
        Vector3 ozoneAbsorption)
    {
        float groundRadius = atmosphere.GroundRadius;
        float topRadius = MathF.Max(atmosphere.TopRadius, groundRadius + 1e-3f);
        float scaleHeight = MathF.Max(atmosphere.RayleighScaleHeight, 1e-3f);
        float ozoneCenter = traceGases.OzoneLayerCenter;
        float ozoneHalfWidth = MathF.Max(traceGases.OzoneLayerWidth * 0.5f, 1e-3f);

        float topHorizonDistance = MathF.Sqrt((topRadius - groundRadius) * (topRadius + groundRadius));

        var data = new byte[Width * Height * Channels * BytesPerChannel];

        int taskCount = Math.Clamp(TaskExecutor.Instance.ParallelTasks, 1, Height);
        var tasks = new List<Task>(taskCount);

        for (int i = 0; i < taskCount; ++i)
        {
            int fromRow = Height * i / taskCount;
            int toRow = Height * (i + 1) / taskCount;

            tasks.Add(new Task(() =>
            {
                for (int y = fromRow; y < toRow; ++y)
                    BakeRow(y);
            }));
        }

        TaskExecutor.Instance.RunTasks(tasks);

        return Image.CreateFromData(Width, Height, false, Image.Format.Rgbh, data);

        void BakeRow(int y)
        {
            float pointHorizonDistance = topHorizonDistance * y / (Height - 1);
            float radius = MathF.Sqrt(pointHorizonDistance * pointHorizonDistance + groundRadius * groundRadius);
            float minDistance = topRadius - radius;
            float maxDistance = pointHorizonDistance + topHorizonDistance;

            for (int x = 0; x < Width; ++x)
            {
                float distanceToTop = minDistance + (maxDistance - minDistance) * x / (Width - 1);

                float cosZenith = distanceToTop <= 0.0f ?
                    1.0f :
                    (topHorizonDistance * topHorizonDistance - pointHorizonDistance * pointHorizonDistance -
                        distanceToTop * distanceToTop) / (2.0f * radius * distanceToTop);
                cosZenith = Math.Clamp(cosZenith, -1.0f, 1.0f);

                float stepSize = distanceToTop / IntegrationSteps;
                var opticalDepth = Vector3.Zero;

                for (int i = 0; i < IntegrationSteps; ++i)
                {
                    float distance = stepSize * (i + 0.5f);
                    float altitude = MathF.Sqrt(radius * radius + distance * distance +
                        2.0f * radius * cosZenith * distance) - groundRadius;

                    float rayleighDensity = MathF.Exp(-MathF.Max(altitude, 0.0f) / scaleHeight);
                    float ozoneDensity = MathF.Max(0.0f, 1.0f - MathF.Abs(altitude - ozoneCenter) / ozoneHalfWidth);

                    opticalDepth += (rayleighScattering * rayleighDensity + ozoneAbsorption * ozoneDensity) * stepSize;
                }

                int offset = (y * Width + x) * Channels * BytesPerChannel;
                WriteChannel(data, offset, opticalDepth.X);
                WriteChannel(data, offset + BytesPerChannel, opticalDepth.Y);
                WriteChannel(data, offset + 2 * BytesPerChannel, opticalDepth.Z);
            }
        }
    }

    private static void WriteChannel(byte[] data, int offset, float opticalDepth)
    {
        BinaryPrimitives.WriteHalfLittleEndian(data.AsSpan(offset), (Half)MathF.Exp(-opticalDepth));
    }
}
