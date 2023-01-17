using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomStream
{
    private System.Random _randomStream;
    private int _randomSeed;

    public RandomStream(int inRandomSeed)
    {
        _randomStream = new System.Random(inRandomSeed);
        _randomSeed = inRandomSeed;
    }

    public int RandRange(int min, int max)
    {
        return _randomStream.Next(min, max + 1); //make it inclusive
    }

    public float RandRange(float min, float max)
    {
        return (float)_randomStream.NextDouble() * (max - min) + min;
    }

    public Vector3 RandRange(Vector3 min, Vector3 max)
    {
        return (float)_randomStream.NextDouble() * (max - min) + min;
    }

    public int GetUnsignedInt()
    {
        //TODO
        return 0;
    }
}
