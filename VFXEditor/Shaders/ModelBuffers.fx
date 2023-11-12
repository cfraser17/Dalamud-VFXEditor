cbuffer VSConstants : register(b0)
{
    float4x4 World;
    float4x4 ViewProjection;
    float4x4 CubeMatrix;
    float4x4 Projection;
    float4x4 View;
    float4x4 NormalMatrix;
}

cbuffer PSConstants : register(b0)
{
    float4 LightDirection;
    float4 LightColor;
    int ShowEdges;
}