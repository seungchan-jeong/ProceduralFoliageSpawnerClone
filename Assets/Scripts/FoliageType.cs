using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ProceduralFoliageSpawner", menuName = "Scriptable Object/FoliageType")]
public class FoliageType : ScriptableObject //TODO FoliageType, FoliageTypeObject 둘 다 SO 가 되는게 맞나?
{
    /// <summary>
    /// Specifies the number of seeds to populate along 10 meters. The number is implicitly squared to cover a 10m x 10m area
    /// </summary>
    public float _initialSeedDensity;
    /// <summary>
    /// 
    /// </summary>
    public float _shadeRadius;
    /// <summary>
    /// 
    /// </summary>
    public float _collisionRadius;
    /// <summary>
    /// Age
    /// </summary>
    public int _maxAge;
    /// <summary>
    /// Random Seed
    /// </summary>
    public int _distributionSeed;

    public float _maxInitialSeedOffset;

    public float _randomPitchAngle;
    
    public bool _randomYaw;


     
    [SerializeField] private int _numSteps;
    public int NumSteps
    {
        get
        {
            return _numSteps;
        }
        set
        {
            _numSteps = value;
        }
    }

    public bool GetSpawnsInShade()
    {
        return false;
        throw new System.NotImplementedException();
    }

    public float GetScaleForAge(int age)
    {
        return 1.0f;
    }

    public float GetInitAge(RandomStream randomStream)
    {
        //TODO
        return 1;
    }

    public float GetScaleForAge(float age)
    {
        //TODO
        return 1;
    }

    public float GetMaxRadius()
    {
        //TODO
        return 1.0f;
    }
    
}
