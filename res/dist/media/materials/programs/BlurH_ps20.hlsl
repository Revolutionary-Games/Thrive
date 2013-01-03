//-------------------------------
//BlurH_ps20.hlsl
// Horizontal Gaussian-Blur pass
//-------------------------------

sampler Blur0: register(s0);
// Simple blur filter

//We use the Normal-gauss distribution formula
//f(x) being the formula, we used f(0.5)-f(-0.5); f(1.5)-f(0.5)...
static const float samples[11] =
{//stddev=2.0
0.01222447,
0.02783468,
0.06559061,
0.12097757,
0.17466632,

0.19741265,

0.17466632,
0.12097757,
0.06559061,
0.02783468,
0.01222447
};

static const float2 pos[11] =
{
-5, 0,
-4, 0,
-3, 0,
-2, 0,
-1, 0,
 0, 0,
 1, 0,
 2, 0,
 3, 0,
 4, 0,
 5, 0,
};

float4 main(float2 texCoord: TEXCOORD0) : COLOR
{
   float4 sum = 0;
   for (int i = 0; i < 11; i++)
   {
      sum += tex2D(Blur0, texCoord + pos[i]*0.01) * samples[i];
   }
   return sum;
}
