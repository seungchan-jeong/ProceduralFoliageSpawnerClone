using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDraw
{
    public static void DrawSphere(Vector3 position, float radius, Color color, int segments = 4)
    {
        if(segments < 2)
        {
            segments = 2;
        }
 
        int doubleSegments = segments * 2;
        float meridianStep = 180.0f / segments;
 
        for(int i = 0; i < segments; i++)
        {
            DrawCircle(position, Quaternion.Euler(0, meridianStep * i, 0), radius, doubleSegments, color);
        }
    }
    
    public static void DrawCircle(Vector3 position, Quaternion rotation, float radius, int segments, Color color)
    {
        // If either radius or number of segments are less or equal to 0, skip drawing
        if (radius <= 0.0f || segments <= 0)
        {
            return;
        }
 
        // Single segment of the circle covers (360 / number of segments) degrees
        float angleStep = (360.0f / segments);
 
        // Result is multiplied by Mathf.Deg2Rad constant which transforms degrees to radians
        // which are required by Unity's Mathf class trigonometry methods
 
        angleStep *= Mathf.Deg2Rad;
 
        // lineStart and lineEnd variables are declared outside of the following for loop
        Vector3 lineStart = Vector3.zero;
        Vector3 lineEnd = Vector3.zero;
 
        for (int i = 0; i < segments; i++)
        {
            // Line start is defined as starting angle of the current segment (i)
            lineStart.x = Mathf.Cos(angleStep * i);
            lineStart.y = Mathf.Sin(angleStep * i);
            lineStart.z = 0.0f;
 
            // Line end is defined by the angle of the next segment (i+1)
            lineEnd.x = Mathf.Cos(angleStep * (i + 1));
            lineEnd.y = Mathf.Sin(angleStep * (i + 1));
            lineEnd.z = 0.0f;
 
            // Results are multiplied so they match the desired radius
            lineStart *= radius;
            lineEnd *= radius;
 
            // Results are multiplied by the rotation quaternion to rotate them 
            // since this operation is not commutative, result needs to be
            // reassigned, instead of using multiplication assignment operator (*=)
            lineStart = rotation * lineStart;
            lineEnd = rotation * lineEnd;
 
            // Results are offset by the desired position/origin 
            lineStart += position;
            lineEnd += position;
 
            // Points are connected using DrawLine method and using the passed color
            Debug.DrawLine(lineStart, lineEnd, color, 3.0f);
        }
    }

    public static void DrawSphereCastAll(Vector3 start, float radius, Vector3 dir, float maxDistance, Color? normalColor = null, Color? hitColor = null, RaycastHit[] hits = null)
    {
        normalColor ??= Color.green;
        hitColor ??= Color.red;

        float duration = 3.0f;
        Vector3 end = start + dir * maxDistance;
        
        DrawSphere(start, radius, normalColor.Value, 6);
        DrawSphere(end, radius, normalColor.Value, 6);

        Debug.DrawLine(start, end, normalColor.Value);
        
        foreach (RaycastHit hit in hits)
        {
            DrawSphere(hit.point, radius * 0.5f, hitColor.Value, 6);
        }
    }
}
