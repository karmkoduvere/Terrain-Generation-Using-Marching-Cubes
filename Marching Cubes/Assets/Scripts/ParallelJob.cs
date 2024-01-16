
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public struct ParallelJob : IJobParallelFor
{
    public Vector3Int WorldSize;

    public void Execute(int i)
    {
        int x = i % WorldSize.x;
        int y = i / WorldSize.y % WorldSize.x;
        int z = i / (WorldSize.z * WorldSize.y) % WorldSize.x;
    }
}
