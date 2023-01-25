using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralFoliageBroadphase
{
    private TQuadTree<ProceduralFoliageInstance> _quadTree;
    
    public ProceduralFoliageBroadphase(float tileSize = 0.0f, float minimumQuadTreeSize = 1.0f)
    {
        _quadTree = new TQuadTree<ProceduralFoliageInstance>(
            new Bounds(Vector3.zero, new Vector3(tileSize * 2.0f, 0.0f, tileSize * 2.0f)), minimumQuadTreeSize);
    }
    private Bounds GetMaxAABB(ProceduralFoliageInstance newInst)
    {
        float Radius = newInst.GetMaxRadius();
        Vector3 location = newInst.Location;
        return new Bounds(location, Vector3.one * Radius);
    }
    
    public bool TestAgainstAABB(ProceduralFoliageInstance newInst)
    {
        Bounds maxAABB = GetMaxAABB(newInst);
        return maxAABB.Intersects(_quadTree.GetTreeBox());
    }

    public void Insert(ProceduralFoliageInstance newInst)
    {
        Bounds maxAABB = GetMaxAABB(newInst);
        _quadTree.Insert(newInst, maxAABB);
    }

    public void GetInstancesInBox(Bounds localAABB, List<ProceduralFoliageInstance> instancesInAABB)
    {
        _quadTree.GetElements(localAABB, instancesInAABB);
    }
}

/*
 * GetTreeBox() : 전체를 감싸는 AABB를 return 하는 듯?
 * Insert(Instance, AABB)
 * GetElements(AABB, Instance) : AABB안에 포함되는 모든 Instance 가져옴
*/
