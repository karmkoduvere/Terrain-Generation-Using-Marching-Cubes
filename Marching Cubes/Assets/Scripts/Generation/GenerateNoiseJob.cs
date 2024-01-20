using DefaultNamespace;
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
            else if (noiseGenerator == NoiseGenerator.SimplePerlinTerrain)
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

        float density = 0;

        float norm = amplitude;
        for (int i = 0; i < octaves; i++)
        {


            density += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            if (i < 2)
            {
                density += PerlinNoise3D(x * frequency, y * frequency, z * frequency) * amplitude;
            }

            frequency *= 2 - 0.01f;
            amplitude *= 0.5f;

            norm += amplitude;
        }

        density /= norm;
        density = Mathf.Pow(density, 5f);

        int height = chunkSize * worldSize.y + 1;

        return height * density - y;
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
