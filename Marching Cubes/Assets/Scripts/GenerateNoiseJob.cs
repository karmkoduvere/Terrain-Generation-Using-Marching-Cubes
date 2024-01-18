using DefaultNamespace;
using System.Data;
using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
struct GenerateNoiseJob : IJobParallelFor
{
    public int chunkSize;
    public Vector3Int worldSize;
    public int xOffset;
    public int yOffset;
    public int zOffset;
    public NoiseGenerator noiseGenerator;
    public NativeArray<float> points;
    public float noiseFrequency;

    public void Execute(int index)
    {
        int z = index / ((chunkSize + 1) * (chunkSize + 1)) % (chunkSize + 1);
        int y = index / (chunkSize + 1) % (chunkSize + 1);
        int x = index % (chunkSize + 1);

        if (z + zOffset == 0 || y + yOffset == 0 || x + xOffset == 0 ||
            z + zOffset == worldSize.z * chunkSize ||
            y + yOffset == worldSize.y * chunkSize ||
            x + xOffset == worldSize.x * chunkSize)
        {
            points[index] = 0;
        }
        else
        {
            if (noiseGenerator == NoiseGenerator.PerlinNoise3D)
            {
                points[index] = PerlinNoise3D(x + xOffset, y + yOffset, z + zOffset);
            }
            else if (noiseGenerator == NoiseGenerator.PerlinNoise2D)
            {
                points[index] = Noise3(x + xOffset, y + yOffset, z + zOffset);
            }
        }
    }
    float PerlinNoise3D(float x, float y, float z)
    {
        x *= noiseFrequency;
        y *= noiseFrequency;
        z *= noiseFrequency;
        float XY = Mathf.PerlinNoise(x, y);
        float XZ = Mathf.PerlinNoise(x, z);
        float YX = Mathf.PerlinNoise(y, x);
        float YZ = Mathf.PerlinNoise(y, z);
        float ZX = Mathf.PerlinNoise(z, x);
        float ZY = Mathf.PerlinNoise(z, y);

        float XYZ = XY + XZ + YX + YZ + ZX + ZY;

        return XYZ / 6f;
    }

    private float PerlinNoise2D(float x, float y, float z)
    {
        int height = chunkSize * worldSize.y + 1;

        float noiseHeightMultiplier = 0.2f;

        float frequency = noiseFrequency;
        float amplitude = noiseHeightMultiplier;

        int numLayers = 5;

        float noiseValue = 0;

        for (int i = 0; i < numLayers; i++)
        {
            noiseValue += Mathf.PerlinNoise(x * noiseFrequency, z * noiseFrequency) * amplitude;
            amplitude *= 0.5f;
            frequency *= 2;
        }

        return height * noiseValue - y;
    }

    private float Noise1(float x, float y, float z)
    {
        x *= noiseFrequency;
        z *= noiseFrequency;

        float octave1 = 1f;
        float octave2 = 0.5f;
        float octave3 = 0.25f;
        float octave4 = 0.13f;
        float octave5 = 0.06f;
        float octave6 = 0.03f;

        float e = octave1 * Mathf.PerlinNoise(1 * x, 1 * z);
        e += octave2 * Mathf.PerlinNoise(2 * x, 2 * z);
        e += octave3 * Mathf.PerlinNoise(4 * x, 4 * z);
        e += octave4 * Mathf.PerlinNoise(8 * x, 8 * z);
        e += octave5 * Mathf.PerlinNoise(16 * x, 16 * z);
        e += octave6 * Mathf.PerlinNoise(32 * x, 32 * z);

        e = e / (octave1 + octave2 + octave3 + octave4 + octave5 + octave6);
        e = Mathf.Pow(e, 5f);

        int height = chunkSize * worldSize.y + 1;

        //return height * e - y;
        return Mathf.Min(Mathf.Max(height * e - y, 0), 1);
    }

    float Noise2(float x, float y, float z)
    {
        int height = chunkSize * worldSize.y + 1;
        Vector3 centre = new(0, 0, 0);
        float spacing = 1;
        float boundsSize = 1;
        float noiseScale = 10;
        int octaves = 6;
        float weightMultiplier = 1;
        float persistence = 0.49f;
        float lacunarity = 2;
        float floorOffset = 0;
        float noiseWeight = 9.19f;
        int hardFloor = 1;
        float hardFloorWeight = 36.98f;

        Vector3 pos = centre + new Vector3(x, y, z) * spacing - new Vector3(boundsSize / 2, boundsSize / 2, boundsSize / 2);
        float offsetNoise = 0;

        float noise = 0;

        float frequency = noiseFrequency;
        float amplitude = 1;
        float weight = 1;
        for (int j = 0; j < octaves; j++)
        {
            Vector3 coords = (pos + new Vector3(offsetNoise, offsetNoise, offsetNoise)) * frequency;
            float n = Mathf.PerlinNoise(coords.x, coords.z);
            float v = 1 - Mathf.Abs(n);
            v = v * v;
            v *= weight;
            weight = Mathf.Max(Mathf.Min(v * weightMultiplier, 1), 0);
            noise += v * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float finalVal = -(pos.y + floorOffset) + noise * noiseWeight;// + (pos.y);// %params.x) * params.y;

        if (pos.y < hardFloor)
        {
            finalVal += hardFloorWeight;
        }
        return finalVal;
        //return height * finalVal - y;
        /*
        if (closeEdges)
        {
            float3 edgeOffset = abs(pos * 2) - worldSize + spacing / 2;
            float edgeWeight = saturate(sign(max(max(edgeOffset.x, edgeOffset.y), edgeOffset.z)));
            finalVal = finalVal * (1 - edgeWeight) - 100 * edgeWeight;

        }*/
    }

    float Noise3(float x, float y, float z)
    {
        x *= noiseFrequency;
        z *= noiseFrequency;

        float ground = 10; // ground plane
        float frequency = 1; // how quickly the noise varies over space
        float amplitude = 1; // strength of the noise
        int octaves = 6; // samples of noise
        //To be optimal, the amplitude of each octave should be half that of the previous octave,
        //and the frequency should be roughly double the frequency of the previous octave.

        y -= ground;

        float density = 0;// -y;// Mathf.Min(Mathf.Max(-y,0),1);

        float norm = amplitude;
        for (int i = 0; i < octaves; i++)
        {
            /*
            if (i > 4)
            {
                float warp = Mathf.PerlinNoise(x * 0.004f, z * 0.004f) * 8;

                x += warp;
                y += warp;
            }*/

            density += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            if (i < 2)
            {
                density += PerlinNoise3D(x * frequency, y * frequency, z * frequency) * amplitude;
                //norm += amplitude;
            }

            frequency *= 2 - 0.01f;
            amplitude *= 0.5f;

            norm += amplitude;
        }

        density /= norm;
        density = Mathf.Pow(density, 5f);

        int height = chunkSize * worldSize.y + 1;

        return height * density - y;

        //return density;
    }

    float BadlandsNoise(float x, float y, float z)
    {
        x *= noiseFrequency;
        z *= noiseFrequency;

        float ground = 10; // ground plane
        float frequency = 1; // how quickly the noise varies over space
        float amplitude = 1; // strength of the noise
        int octaves = 6; // samples of noise
       
        y -= ground;

        float density = 0;

        float norm = amplitude;
        for (int i = 0; i < octaves; i++)
        {
            density += PerlinNoise3D(x * frequency, y * frequency, z * frequency) * amplitude;

            frequency *= 2 - 0.01f;
            amplitude *= 0.5f;

            norm += amplitude;
        }

        density /= norm;
        //density = Mathf.Pow(density, 5f);

        int height = chunkSize * worldSize.y + 1;

        return height * density - y;
    }
}
