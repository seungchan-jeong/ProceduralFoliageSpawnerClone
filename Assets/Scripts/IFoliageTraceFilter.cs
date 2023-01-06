using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFoliageTraceFilter
{
    bool FilterFoliageTrace(GameObject obj); 
    /*
     * 원래는 PrimitiveComponent가 맞는데
     * 유니티에서 Terrain, SkinnedMesh, Mesh는 별도의 Component이고 상속관계가 없음.
     * 따라서 우선 GameObject로 구현.
     */
}
