using DefaultNamespace;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Noise : MonoBehaviour
{
    public static Noise instance;

    private int chunkSize;
    private Vector3Int worldSize;
    private float noiseFrequency;

    private NoiseGenerator noiseGenerator;


    private void Awake()
    {
        instance = this;
    }
    public float[,,] CreateNoisePoints2(Vector3 offset)
    {
        chunkSize = WorldGenerator.Instance.chunkSize;
        worldSize = WorldGenerator.Instance.WorldSize;
        noiseFrequency = WorldGenerator.Instance.NoiseScale / 100f;
        noiseGenerator = WorldGenerator.Instance.NoiseGenerator;
        // Create a NativeArray to store the generated points
        NativeArray<float> points = new((chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1), Allocator.TempJob);

        // Create a job and schedule it
        GenerateNoiseJob job = new GenerateNoiseJob
        {
            chunkSize = chunkSize,
            worldSize = worldSize,
            xOffset = chunkSize * (int)offset.x,
            yOffset = chunkSize * (int)offset.y,
            zOffset = chunkSize * (int)offset.z,
            noiseGenerator = noiseGenerator,
            points = points,
            noiseFrequency = noiseFrequency
        };

        JobHandle jobHandle = job.Schedule(points.Length, 64);

        // Ensure the job is complete before accessing the results
        jobHandle.Complete();

        // Access the results in the 'points' NativeArray
        float[,,] points3D = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];

        for (int z = 0; z < chunkSize + 1; z++)
        {
            for (int y = 0; y < chunkSize + 1; y++)
            {
                for (int x = 0; x < chunkSize + 1; x++)
                {
                    int index = x + (chunkSize + 1) * (y + (chunkSize + 1) * z);
                    points3D[z,y,x] = points[index];
                }
            }
        }

        // Dispose of the NativeArray
        points.Dispose();

        return points3D;
    }

    // Generates noise points with 3D array the size of lenght * height * depth
    public float[,,] CreateNoisePoints(Vector3 offset)
    {
        chunkSize = WorldGenerator.Instance.chunkSize;
        worldSize = WorldGenerator.Instance.WorldSize;
        noiseFrequency = WorldGenerator.Instance.NoiseScale / 100f;
        noiseGenerator = WorldGenerator.Instance.NoiseGenerator;


        int xOffset = chunkSize * (int)offset.x;
        int yOffset = chunkSize * (int)offset.y;
        int zOffset = chunkSize * (int)offset.z;

        float[,,] points = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];
        for (int z = 0; z < chunkSize+1; z++)
        {
            for (int y = 0; y < chunkSize+1; y++)
            {
                for (int x = 0; x < chunkSize+1; x++)
                {
                    if (z+zOffset == 0 || y + yOffset == 0 || x + xOffset == 0 || 
                        z+zOffset == worldSize.z*chunkSize || 
                        y + yOffset == worldSize.y * chunkSize || 
                        x + xOffset == worldSize.x * chunkSize)
                        points[z, y, x] = 0;
                    else
                    {
                        //points[z, y, x] = Noise.Noise1(x + xOffset, y + yOffset, z + zOffset);

                        if (noiseGenerator == NoiseGenerator.PerlinNoise3D)
                        {
                            points[z,y,x] = PerlinNoise3D(x + xOffset, y + yOffset, z + zOffset);
                        }
                        else if (noiseGenerator == NoiseGenerator.PerlinNoise2D)
                        {
                            points[z, y, x] = PerlinNoise2D(x + xOffset, y + yOffset, z + zOffset);
                        }
                        else if (noiseGenerator == NoiseGenerator.Perlid2DX3D)
                        {
                            points[z, y, x] = PerlinNoise2DX3D(x + xOffset, y + yOffset, z + zOffset);
                        }
                    }
                }
            }
        }
        return points;
    }

    public float PerlinNoise2DX3D(float x, float y, float z)
    {
        return (PerlinNoise2D(x, y, z) == 0) ? 0 : PerlinNoise3D(x, y, z);
    }

    private float PerlinNoise2D(float x, float y, float z)
    {
        int height = chunkSize * worldSize.y + 1;
        x *= noiseFrequency;
        z *= noiseFrequency;
        float xzPerlinHeight = Mathf.PerlinNoise(x, z);

        return height * xzPerlinHeight - y;
    }

    public float PerlinNoise3D(float x, float y, float z)
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

    
}
