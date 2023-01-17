using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralFoliageComponent))]
[CanEditMultipleObjects]
public class ProceduralFoliageComponentEditor : Editor
{
    private ProceduralFoliageComponent _proceduralFoliageComponent;
    void OnEnable()
    {
        _proceduralFoliageComponent = target as ProceduralFoliageComponent;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Simulate"))
        {
            _proceduralFoliageComponent.ResimulateProceduralFoliage();
        }
    }
}
