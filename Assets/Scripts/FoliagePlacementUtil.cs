using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliagePlacementUtil
{
    public static int GetRandomSeedForPosition(Vector3 vector)
    {
        // generate a unique random seed for a given position (precision = cm)
        int Xcm = Mathf.RoundToInt(vector.x);
        int Zcm = Mathf.RoundToInt(vector.z);
        // use the int32 hashing function to avoid patterns by spreading out distribution : 
        
        return HashCombine(Xcm, Zcm);
    }
    
    private static int HashCombine(int A, int C) //TODO ?
    {
        int B = 0x9e3779b; //TODO 이거 overflow 난다고해서 수정함.
        A += B;

        A -= B; A -= C; A ^= (C>>13);
        B -= C; B -= A; B ^= (A<<8);
        C -= A; C -= B; C ^= (B>>13);
        A -= B; A -= C; A ^= (C>>12);
        B -= C; B -= A; B ^= (A<<16);
        C -= A; C -= B; C ^= (B>>5);
        A -= B; A -= C; A ^= (C>>3);
        B -= C; B -= A; B ^= (A<<10);
        C -= A; C -= B; C ^= (B>>15);

        return C;
    }
}
