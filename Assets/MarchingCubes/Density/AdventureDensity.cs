using UnityEngine;

public class AdventureDensity : DensityGenerator
{
    [Header("World Shape")]
    public int seed = 12;
    public float floorBase = -13f;
    public float floorAmplitude = 4.5f;
    public float ceilingBase = 30f;
    public float ceilingAmplitude = 7f;
    public float canyonWidth = 22f;
    public float canyonMeander = 22f;
    public float edgeWallPadding = 12f;
    public float surfaceRoughness = 2.4f;

    [Header("Scale")]
    public float floorNoiseScale = 0.035f;
    public float wallNoiseScale = 0.055f;
    public float detailNoiseScale = 0.16f;

    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing)
    {
        densityShader.SetInt("seed", seed);
        densityShader.SetFloat("floorBase", floorBase);
        densityShader.SetFloat("floorAmplitude", floorAmplitude);
        densityShader.SetFloat("ceilingBase", ceilingBase);
        densityShader.SetFloat("ceilingAmplitude", ceilingAmplitude);
        densityShader.SetFloat("canyonWidth", canyonWidth);
        densityShader.SetFloat("canyonMeander", canyonMeander);
        densityShader.SetFloat("edgeWallPadding", edgeWallPadding);
        densityShader.SetFloat("surfaceRoughness", surfaceRoughness);
        densityShader.SetFloat("floorNoiseScale", floorNoiseScale);
        densityShader.SetFloat("wallNoiseScale", wallNoiseScale);
        densityShader.SetFloat("detailNoiseScale", detailNoiseScale);

        return base.Generate(pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing);
    }
}
