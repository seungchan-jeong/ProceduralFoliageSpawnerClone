using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProceduralFoliageSpawner", menuName = "Scriptable Object/FoliageTypeObject")]
public class FoliageTypeObject : ScriptableObject
{
    //유니티에서 이게 필요할지 의문
    //UFoliageType 과 FFoliageTypeObject라는게 있네..? 
    /*
     * FoliageType은 FoliageTypeObject를 모른다.
     * FoliageTypeObject는 FoliageType을 field로 갖는다. 
     *     - wrapper 클래스라고 함. FoliageType과 다른 하나를 Wrapping하는듯?
     */
    [SerializeField]
    private FoliageType _typeInstance;
    public FoliageType GetInstance()
    {
        return _typeInstance;
    }
    
    public void RefreshInstance()
    {
        
    }
}
